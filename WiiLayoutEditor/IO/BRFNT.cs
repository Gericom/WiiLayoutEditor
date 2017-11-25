using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using libWiiSharp;

namespace WiiLayoutEditor.IO
{
	public class BRFNT
	{
		public const String Signature = "RFNT";
		public BRFNT(byte[] file)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			bool OK;
			Header = new BRFNTHeader(er, Signature, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 1"); goto end; }
			FINF = new FINFSection(er, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 2"); goto end; }
			er.BaseStream.Position = FINF.TGLPOffset - 8;
			TGLP = new TGLPSection(er, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 3"); goto end; }
			er.BaseStream.Position = FINF.CWDHOffset - 8;
			CWDH = new CWDHSection(er, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 4"); goto end; }
			List<CMAPSection> cmap = new List<CMAPSection>();
			int offset = (int)FINF.CMAPOffset;
			do { offset -= 8; er.BaseStream.Position = offset; cmap.Add(new CMAPSection(er, out OK)); if (!OK) { System.Windows.Forms.MessageBox.Show("Error 5"); goto end; } }
			while ((offset = (int)cmap.Last().NextCMAPOffset) != 0);
			CMAP = cmap.ToArray();

			Chars = new SortedDictionary<char, Bitmap>();
			float width = (TGLP.ImageWidth - 2) / TGLP.CharsARow;
			float height = (TGLP.ImageHeight - 2) / TGLP.CharsAColumn;
			int idx3 = 0;
			foreach (CMAPSection cm in CMAP)
			{
				foreach (Char c in cm.CharIndex.Keys)
				{
					try
					{
						Bitmap b = new Bitmap((TGLP.FontWidth) * 1, (TGLP.FontHeight + 2) * 1);
						using (Graphics g = Graphics.FromImage(b))
						{
							int idx;
							if (cm.CharIndex.ContainsKey(c))
							{
								idx = cm.CharIndex[c];
							}
							else
							{
								continue;
							}
							int idx2 = idx3++; //f.Chars.Values.ToList().IndexOf(idx);
							int x = 0;
							int y = 0;
							int image = 0;
							Decimal d = (decimal)idx / (decimal)TGLP.CharsARow;
							y = (int)Decimal.Floor(d);
							x = (int)Decimal.Round((d - y) * TGLP.CharsARow);
							image = (int)Decimal.Floor(y / TGLP.CharsAColumn);
							y -= image * TGLP.CharsAColumn;
							int x2 = 0;
							for (int j = 0; j < x; j++)
							{
								x2 += TGLP.FontWidth + 2;//this.Charspacing[(char)(text[i] - j)][1];
							}
							g.DrawImage(TGLP.Images[image], new Rectangle(0, 0, CWDH.Charspacings[idx2].Unknown2, (int)height), new Rectangle(x2 + 1, 1 + y * (TGLP.FontHeight + 2), CWDH.Charspacings[idx2].Unknown2, (int)height), GraphicsUnit.Pixel);
						}
						Chars.Add(c, b);
					}
					catch { }
				}
			}
		end:
			er.Close();
		}
		public BRFNTHeader Header;
		public class BRFNTHeader
		{
			public BRFNTHeader(EndianBinaryReader er, String Signature, out bool OK)
			{
				Type = er.ReadString(ASCIIEncoding.ASCII, 4);
				if (Type != Signature) { OK = false; return; }
				Magic = er.ReadUInt32();
				FileSize = er.ReadUInt32();
				HeaderSize = er.ReadUInt16();
				Unknown = er.ReadUInt16();
				OK = true;
			}
			public String Type;
			public UInt32 Magic;
			public UInt32 FileSize;
			public UInt16 HeaderSize;
			public UInt16 Unknown;
		}
		public FINFSection FINF;
		public class FINFSection
		{
			public const String Signature = "FINF";
			public FINFSection(EndianBinaryReader er, out bool OK)
			{
				bool OK2;
				Header = new FINFHeader(er, Signature, out OK2);
				if (!OK2) { OK = false; return; }
				Unknown1 = er.ReadByte();
				Unknown2 = er.ReadByte();
				Unknown3 = er.ReadUInt16();
				Unknown4 = er.ReadByte();
				Unknown5 = er.ReadByte();
				Unknown6 = er.ReadByte();
				Unknown7 = er.ReadByte();
				TGLPOffset = er.ReadUInt32();
				CWDHOffset = er.ReadUInt32();
				CMAPOffset = er.ReadUInt32();
				OK = true;
			}
			public FINFHeader Header;
			public class FINFHeader
			{
				public FINFHeader(EndianBinaryReader er, String Signature, out bool OK)
				{
					Type = er.ReadString(ASCIIEncoding.ASCII, 4);
					if (Type != Signature) { OK = false; return; }
					SectionSize = er.ReadUInt32();
					OK = true;
				}
				public String Type;
				public UInt32 SectionSize;
			}
			public Byte Unknown1;
			public Byte Unknown2;
			public UInt16 Unknown3;
			public Byte Unknown4;
			public Byte Unknown5;
			public Byte Unknown6;
			public Byte Unknown7;
			public UInt32 TGLPOffset;//-8
			public UInt32 CWDHOffset;//-8
			public UInt32 CMAPOffset;//-8
		}
		public TGLPSection TGLP;
		public class TGLPSection
		{
			public const String Signature = "TGLP";
			public TGLPSection(EndianBinaryReader er, out bool OK)
			{
				bool OK2;
				Header = new TGLPHeader(er, Signature, out OK2);
				if (!OK2) { OK = false; return; }
				FontWidth = (byte)(er.ReadByte() - 1);
				FontHeight = (byte)(er.ReadByte() - 1);
				CharWidth = (byte)(er.ReadByte() - 1);
				CharHeight = (byte)(er.ReadByte() - 1);
				ImageLength = er.ReadUInt32();
				NrImages = er.ReadUInt16();
				Unknown1 = er.ReadByte();
				ImageFormat = er.ReadByte();
				CharsARow = er.ReadUInt16();
				CharsAColumn = er.ReadUInt16();
				ImageWidth = er.ReadUInt16();
				ImageHeight = er.ReadUInt16();
				ImageOffset = er.ReadUInt32();

				er.BaseStream.Position = ImageOffset;
				Images = new Bitmap[NrImages];
				for (int i = 0; i < NrImages; i++)
				{
					byte[] Texturedata = er.ReadBytes((int)ImageLength);
					byte[] rgbaData;

					switch ((TPL_TextureFormat)ImageFormat)
					{
						case TPL_TextureFormat.I4:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.I4.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.I8:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.I8.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.IA4:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.IA4.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.IA8:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.IA8.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.RGB565:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.RGB565.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.RGB5A3:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.RGB5A3.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						case TPL_TextureFormat.RGBA8:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.Rgba32.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						//case TPL_TextureFormat.CI4:
						//	rgbaData = fromCI4(textureData[index], paletteToRgba(index), tplTextureHeaders[index].TextureWidth, tplTextureHeaders[index].TextureHeight);
						//	break;
						//case TPL_TextureFormat.CI8:
						//	rgbaData = fromCI8(textureData[index], paletteToRgba(index), tplTextureHeaders[index].TextureWidth, tplTextureHeaders[index].TextureHeight);
						//	break;
						//case TPL_TextureFormat.CI14X2:
						//	rgbaData = fromCI14X2(textureData[index], paletteToRgba(index), tplTextureHeaders[index].TextureWidth, tplTextureHeaders[index].TextureHeight);
						//	break;
						case TPL_TextureFormat.CMP:
							rgbaData = Chadsoft.CTools.Image.ImageDataFormat.Cmpr.ConvertFrom(Texturedata, ImageWidth, ImageHeight, null);
							break;
						default:
							throw new FormatException("Unsupported Texture Format!");
					}

					Images[i] = libWiiSharp.TPL.rgbaToImage(rgbaData, ImageWidth, ImageHeight);
				}
				OK = true;
			}
			public TGLPHeader Header;
			public class TGLPHeader
			{
				public TGLPHeader(EndianBinaryReader er, String Signature, out bool OK)
				{
					Type = er.ReadString(ASCIIEncoding.ASCII, 4);
					if (Type != Signature) { OK = false; return; }
					SectionSize = er.ReadUInt32();
					OK = true;
				}
				public String Type;
				public UInt32 SectionSize;
			}
			public Byte FontWidth;
			public Byte FontHeight;
			public Byte CharWidth;
			public Byte CharHeight;
			public UInt32 ImageLength;
			public UInt16 NrImages;
			public Byte Unknown1;
			public Byte ImageFormat;
			public UInt16 CharsARow;
			public UInt16 CharsAColumn;
			public UInt16 ImageWidth;
			public UInt16 ImageHeight;
			public UInt32 ImageOffset;

			public Bitmap[] Images;
		}
		public CWDHSection CWDH;
		public class CWDHSection
		{
			public const String Signature = "CWDH";
			public CWDHSection(EndianBinaryReader er, out bool OK)
			{
				bool OK2;
				Header = new CWDHHeader(er, Signature, out OK2);
				if (!OK2) { OK = false; return; }
				NrChars = er.ReadUInt32();
				FirstChar = (Char)er.ReadUInt32();
				Charspacings = new CharSpacing[NrChars];
				for (int i = 0; i < NrChars; i++)
				{
					Charspacings[i] = new CharSpacing(er);
				}
				OK = true;
			}
			public CWDHHeader Header;
			public class CWDHHeader
			{
				public CWDHHeader(EndianBinaryReader er, String Signature, out bool OK)
				{
					Type = er.ReadString(ASCIIEncoding.ASCII, 4);
					if (Type != Signature) { OK = false; return; }
					SectionSize = er.ReadUInt32();
					OK = true;
				}
				public String Type;
				public UInt32 SectionSize;
			}
			public UInt32 NrChars;
			public Char FirstChar;
			public CharSpacing[] Charspacings;
			public class CharSpacing
			{
				public CharSpacing(EndianBinaryReader er)
				{
					Unknown1 = er.ReadByte();
					Unknown2 = er.ReadByte();
					Unknown3 = er.ReadByte();
				}
				public byte Unknown1;
				public byte Unknown2;
				public byte Unknown3;
			}
		}
		public CMAPSection[] CMAP;
		public class CMAPSection
		{
			public const String Signature = "CMAP";
			public CMAPSection(EndianBinaryReader er, out bool OK)
			{
				bool OK2;
				Header = new CMAPHeader(er, Signature, out OK2);
				if (!OK2) { OK = false; return; }
				CharIndex = new Dictionary<char, ushort>();
				FirstChar = er.ReadChar(ASCIIEncoding.Unicode);
				LastChar = er.ReadChar(ASCIIEncoding.Unicode);
				Unknown1 = er.ReadUInt32();
				NextCMAPOffset = er.ReadUInt32();
				if (FirstChar == 0 && LastChar == 0xFFFF)
				{
					NrChar = er.ReadUInt16();
					for (int i = 0; i < NrChar; i++)
					{
						CharIndex.Add(er.ReadChar(Encoding.Unicode), er.ReadUInt16());
					}
				}
				else
				{
					NrChar = (ushort)(LastChar - FirstChar);
					if (er.ReadUInt32() == 0)
					{
						for (int i = 0; i < NrChar; i++)
						{
							CharIndex.Add((char)(FirstChar + i), (ushort)i);
						}
					}
					else
					{
						er.BaseStream.Position -= 4;
						for (int i = 0; i < NrChar; i++)
						{
							ushort idx = er.ReadUInt16();
							if (idx != 0xFFFF)
							{
								try
								{
									CharIndex.Add((char)(FirstChar + i), (ushort)idx);
								}
								catch
								{

								}
							}
						}
					}

				}
				OK = true;
			}
			public CMAPHeader Header;
			public class CMAPHeader
			{
				public CMAPHeader(EndianBinaryReader er, String Signature, out bool OK)
				{
					Type = er.ReadString(ASCIIEncoding.ASCII, 4);
					if (Type != Signature) { OK = false; return; }
					SectionSize = er.ReadUInt32();
					OK = true;
				}
				public String Type;
				public UInt32 SectionSize;
			}
			public Char FirstChar;
			public Char LastChar;
			public UInt32 Unknown1;
			public UInt32 NextCMAPOffset;
			public UInt16 NrChar;
			public Dictionary<Char, UInt16> CharIndex;
		}
		private SortedDictionary<Char, Bitmap> Chars;
		private String getLengthLongestString(String[] array)
		{
			int maxLength = 0;
			String longestString = null;
			foreach (String s in array)
			{
				if (s.Length > maxLength)
				{
					maxLength = s.Length;
					longestString = s;
				}
			}
			return longestString;
		}

		public Bitmap GetBitmap(String text, bool reversewh, BRLYT.txt1.Font fon)
		{
			int fontwidth = TGLP.FontWidth;
			int fontheight = TGLP.FontHeight;
			if (fon.XSize != 0)
			{
				fontwidth = (int)fon.XSize - 2;
				fontheight = (int)fon.YSize - 2;
			}
			float widthmult = (float)fontwidth / (float)TGLP.FontWidth;
			float heightmult = (float)fontheight / (float)TGLP.FontHeight;
			//float width = (this.CharacterMaps[0].Width - 2) / this.CharactersRow;
			float height = (TGLP.ImageHeight - 2) / TGLP.CharsAColumn;

			int width = int.MinValue;

			int pos = 0;
			int top = 0;
			for (int i = 0; i < text.Length; i++)
			{
				try
				{
					if (text[i] == '\n')
					{
						if (pos > width)
						{
							width = pos;
						}
						pos = 0;
						top += fontheight + 2;
						top += (int)fon.LineSize;
						continue;
					}
					int idx;
					if (Chars.ContainsKey(text[i]))
					{
						idx = Chars.Keys.ToList().IndexOf(text[i]);
					}
					else continue;
					int idx2 = idx;// Chars.Values.ToList().IndexOf(idx);
					pos += (int)(CWDH.Charspacings[idx2].Unknown3 * widthmult)/* - this.CharacterSize.Width */ + (int)fon.CharSize;
				}
				catch
				{

				}
			}
			top += fontheight + 2;
			top += (int)fon.LineSize;
			int height2 = top;
			if (width == int.MinValue)
			{
				width = pos;
			}
			width += 5;
			Bitmap b = new Bitmap(/*(this.FontSize.Width + 2) * getLengthLongestString(text.Split('\n')).Length, (this.FontSize.Height + 2) * (text.Count(x => x == '\n') + 1)*/width, height2);
			pos = 0;
			top = 0;
			using (Graphics g = Graphics.FromImage(b))
			{
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
				for (int i = 0; i < text.Length; i++)
				{
					try
					{
						if (text[i] == '\n')
						{
							pos = 0;
							top += fontheight + 2;
							top += (int)fon.LineSize;
							continue;
						}
						int idx;
						if (Chars.ContainsKey(text[i]))
						{
							idx = Chars.Keys.ToList().IndexOf(text[i]);
						}
						else continue;
						int idx2 = idx;// Chars.Values.ToList().IndexOf(idx);
						g.DrawImage(Chars[text[i]], new RectangleF(pos, top, CWDH.Charspacings[idx2].Unknown2 * widthmult, height * heightmult), new Rectangle(0, 0, CWDH.Charspacings[idx2].Unknown2, (int)height), GraphicsUnit.Pixel);
						pos += (int)(CWDH.Charspacings[idx2].Unknown3 * widthmult)/* - this.CharacterSize.Width */ + (int)fon.CharSize;
					}
					catch
					{

					}
				}
			}
			//if (fon.xsize != 0)
			//{
			//	this.FontSize = fs;
			//}
			return b;
		}

	}
}
