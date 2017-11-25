using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WiiLayoutEditor.IO
{
	public class TPL
	{
		public TPL(byte[] file)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			bool OK;
			Header = new TPLHeader(er, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 1"); goto end; }
			ImageHeaderOffsets = new UInt32[Header.NrImages];
			PaletteHeaderOffsets = new UInt32[Header.NrImages];
			for (int i = 0; i < Header.NrImages; i++)
			{
				ImageHeaderOffsets[i] = er.ReadUInt32();
				PaletteHeaderOffsets[i] = er.ReadUInt32();
			}
			Images = new TPLImageHeader[Header.NrImages];
			Palettes = new TPLPaletteHeader[Header.NrImages];
			for (int i = 0; i < Header.NrImages; i++)
			{
				er.BaseStream.Position = ImageHeaderOffsets[i];
				Images[i] = new TPLImageHeader(er);
				if (PaletteHeaderOffsets[i] != 0)
				{
					er.BaseStream.Position = PaletteHeaderOffsets[i];
					Palettes[i] = new TPLPaletteHeader(er);
				}
			}
		end:
			er.Close();
		}
		public TPLHeader Header;
		public class TPLHeader
		{
			public TPLHeader(EndianBinaryReader er, out bool OK)
			{
				Type = er.ReadBytes(4);
				if (Type[0] != 0x00 || Type[1] != 0x20 || Type[2] != 0xAF || Type[3] != 0x30) { OK = false; return; }
				NrImages = er.ReadUInt32();
				ImageTableOffset = er.ReadUInt32();
				OK = true;
			}
			public byte[] Type;
			public UInt32 NrImages;
			public UInt32 ImageTableOffset;
		}
		public UInt32[] ImageHeaderOffsets;
		public UInt32[] PaletteHeaderOffsets;

		public TPLPaletteHeader[] Palettes;
		public TPLImageHeader[] Images;
		public class TPLPaletteHeader
		{
			public enum PaletteFormats
			{
				IA8 = 0,
				RGB565 = 1,
				RGB5A3 = 2
			}
			public TPLPaletteHeader(EndianBinaryReader er)
			{
				NrEntries = er.ReadUInt16();
				Unpacked = er.ReadByte();
				Padding = er.ReadByte();
				PaletteFormat = (PaletteFormats)er.ReadUInt32();
				PaletteDataOffset = er.ReadUInt32();
				long curpos = er.BaseStream.Position;
				er.BaseStream.Position = PaletteDataOffset;
				Data = er.ReadBytes(NrEntries * 2);
				er.BaseStream.Position = curpos;
			}
			public UInt16 NrEntries;
			public byte Unpacked;
			public byte Padding;
			public PaletteFormats PaletteFormat;
			public UInt32 PaletteDataOffset;

			public byte[] Data;
		}
		public class TPLImageHeader
		{
			public enum ImageFormats
			{
				I4 = 0x0,
				I8 = 0x1,
				IA4 = 0x2,
				IA8 = 0x3,
				RGB565 = 0x4,
				RGB5A3 = 0x5,
				RGBA8 = 0x6,
				C4 = 0x8,
				C8 = 0x9,
				C14X2 = 0xA,
				CMPR = 0xE
			}
			public TPLImageHeader(EndianBinaryReader er)
			{
				Width = er.ReadUInt16();
				Height = er.ReadUInt16();
				ImageFormat = (ImageFormats)er.ReadUInt32();
				ImageDataOffset = er.ReadUInt32();
				WrapS = er.ReadUInt32();
				WrapT = er.ReadUInt32();
				MinFilter = er.ReadUInt32();
				MagFilter = er.ReadUInt32();
				LodBias = er.ReadSingle();
				EdgeLod = er.ReadByte() == 1;
				MinLod = er.ReadByte();
				MaxLod = er.ReadByte();
				Unpacked = er.ReadByte();
				long curpos = er.BaseStream.Position;
				er.BaseStream.Position = ImageDataOffset;
				Data = er.ReadBytes(GetTexSize());
				er.BaseStream.Position = curpos;
			}
			private int GetTexSize()
			{
				switch (ImageFormat)
				{
					case ImageFormats.I4:
						return Util.AddPadding(Width, 8) * Util.AddPadding(Height, 8) / 2;
					case ImageFormats.I8:
					case ImageFormats.IA4:
						return Util.AddPadding(Width, 8) * Util.AddPadding(Height, 4);
					case ImageFormats.IA8:
					case ImageFormats.RGB565:
					case ImageFormats.RGB5A3:
						return Util.AddPadding(Width, 4) * Util.AddPadding(Height, 4) * 2;
					case ImageFormats.RGBA8:
						return Util.AddPadding(Width, 4) * Util.AddPadding(Height, 4) * 4;
					case ImageFormats.C4:
						return Util.AddPadding(Width, 8) * Util.AddPadding(Height, 8) / 2;
					case ImageFormats.C8:
						return Util.AddPadding(Width, 8) * Util.AddPadding(Height, 4);
					case ImageFormats.C14X2:
						return Util.AddPadding(Width, 4) * Util.AddPadding(Height, 4) * 2;
					case ImageFormats.CMPR:
						return Util.AddPadding(Width, 8) * Util.AddPadding(Height, 8);
					default:
						throw new FormatException("Unsupported Texture Format!");
				}
			}

			public UInt16 Width;
			public UInt16 Height;
			public ImageFormats ImageFormat;
			public UInt32 ImageDataOffset;

			public UInt32 WrapS;
			public UInt32 WrapT;
			public UInt32 MinFilter;
			public UInt32 MagFilter;
			public Single LodBias;
			public bool EdgeLod;
			public byte MinLod;
			public byte MaxLod;
			public byte Unpacked;

			public byte[] Data;

			public byte[] GetData(TPLPaletteHeader Pal)
			{
				List<byte> b = new List<byte>();
				switch (ImageFormat)
				{
					case ImageFormats.I4:
						{
							foreach (byte d in Data)
							{
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add(0xFF);

								b.Add((byte)(((d >> 4) & 0xF) * 0x11));
								b.Add((byte)(((d >> 4) & 0xF) * 0x11));
								b.Add((byte)(((d >> 4) & 0xF) * 0x11));
								b.Add(0xFF);
							}
							break;
						}
					case ImageFormats.I8:
						{
							foreach (byte d in Data)
							{
								b.Add(d);
								b.Add(d);
								b.Add(d);
								b.Add(0xFF);
							}
							break;
						}
					case ImageFormats.IA4:
						{
							foreach (byte d in Data)
							{
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add((byte)((d & 0xF) * 0x11));
								b.Add((byte)(((d >> 4) & 0xF) * 0x11));
							}
							break;
						}
					case ImageFormats.IA8:
						{
							for (int i = 0; i < Data.Length; i += 2)
							{
								b.Add(Data[i]);
								b.Add(Data[i]);
								b.Add(Data[i]);
								b.Add(Data[i + 1]);
							}
							break;
						}
					case ImageFormats.RGB565:
						{
							for (int i = 0; i < Data.Length; i += 2)
							{
								ushort d = (ushort)(Data[i] << 8 | Data[i + 1]);
								b.Add((byte)((d & 31) * 8));
								
								b.Add((byte)(((d >> 5) & 63) * 4));
								b.Add((byte)(((d >> 11) & 31) * 8));
								b.Add(0xFF);
							}
							break;
						}
					case ImageFormats.RGB5A3:
						{
							for (int i = 0; i < Data.Length; i += 2)
							{
								ushort d = (ushort)(Data[i] << 8 | Data[i + 1]);
								if ((d >> 15) == 1)//alpha
								{
									b.Add((byte)((d & 15) * 0x11));
									b.Add((byte)(((d >> 4) & 15) * 0x11));
									
									b.Add((byte)(((d >> 8) & 15) * 0x11));
									b.Add((byte)(((d >> 12) & 7) * 0x20));
								}
								else//no alpha
								{
									b.Add((byte)((d & 31) * 8));
									b.Add((byte)(((d >> 5) & 31) * 8));
									
									b.Add((byte)(((d >> 10) & 31) * 8));
									b.Add(0xFF);
								}
							}
							break;
						}
					case ImageFormats.RGBA8:
						{
							for (int i = 0; i < Data.Length; i += 4)
							{
								b.Add(Data[i + 1]);
								b.Add(Data[i + 2]);
								b.Add(Data[i + 3]);
								b.Add(Data[i]);
							}
							break;
						}
				}
				return b.ToArray();
			}
		}
	}
}
