using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Tao.OpenGl;
using System.Drawing.Imaging;
using System.ComponentModel;

namespace WiiLayoutEditor.IO
{
	public class BRLYT
	{
		public const String Signature = "RLYT";
		public BRLYT(byte[] file, Dictionary<String, byte[]> Files)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			bool OK;
			Header = new BRLYTHeader(er, Signature, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 1"); goto end; }
			Stack<pan1> Panels = new Stack<pan1>();
			Panels.Push(null);
			pan1 LastPanel = null;
			for (int i = 0; i < Header.NrEntries; i++)
			{
				string s;
				switch (s = er.ReadString(Encoding.ASCII, 4))
				{
					case "lyt1":
						LYT1 = new lyt1(er);
						break;
					case "txl1":
						TXL1 = new txl1(er, Files);
						break;
					case "fnl1":
						FNL1 = new fnl1(er, Files);
						break;
					case "mat1":
						MAT1 = new mat1(er);
						break;
					case "pan1":
						if (Panels.Peek() != null) Panels.Peek().Add(LastPanel = new pan1(er, Panels.Peek()));
						else PAN1 = LastPanel = new pan1(er, Panels.Peek());
						break;
					case "bnd1":
						Panels.Peek().Add(LastPanel = new bnd1(er, Panels.Peek()));
						break;
					case "pic1":
						Panels.Peek().Add(LastPanel = new pic1(er, Panels.Peek()));
						break;
					case "txt1":
						Panels.Peek().Add(LastPanel = new txt1(er, Panels.Peek()));
						break;
					case "wnd1":
						Panels.Peek().Add(LastPanel = new wnd1(er, Panels.Peek()));
						break;
					case "pas1":
						er.ReadInt32();
						Panels.Push(LastPanel);
						break;
					case "pae1":
						er.ReadInt32();
						Panels.Pop();
						break;
					case "grp1":
						if (GRP1 == null) GRP1 = new grp1(er);
						else GRP1[GRP1.Length - 1].Add(new grp1(er));
						break;
					case "grs1":
						GRP1.Add(new grs1(er));
						break;
					default:
						er.BaseStream.Position += er.ReadUInt32() - 4;
						break;
				}
			}
		end:
			er.Close();
		}
		private Dictionary<string, bool> LNGDraw = new Dictionary<string, bool>();
		public bool Language(string name)
		{
			if (LNGDraw.ContainsKey(name))
			{
				return LNGDraw[name];
			}
			else
			{


				// hide panes of non-matching languages
				if (GRP1.Length != 0)
				{
					List<string> LNG = new List<string>();
					foreach (grs1 g in GRP1)
					{
						foreach (grp1 g2 in g)
						{
							if (g2.Name.Length == 3 && !LNG.Contains(g2.Name))
							{
								LNG.Add(g2.Name);
							}
						}
					}

					bool ok = false;
					bool got = false;
					if (LNG.Contains(sellng))
					{
						foreach (grs1 g in GRP1)
						{
							foreach (grp1 g2 in g)
							{
								if (g2.Objects.Contains(name) && g2.Name != sellng && g2.Name.Length == 3)
								{
									if (ok != true)
									{
										ok = true;
									}
									got = true;
								}
								else if (g2.Objects.Contains(name) && g2.Name == sellng)
								{
									LNGDraw.Add(name, false);
									got = true;
									ok = true;
									return false;
								}
							}
						}
						if (got)
						{
							LNGDraw.Add(name, true);
							return true;
						}
					}
					else if (LNG.Contains("ENG"))
					{
						foreach (grs1 g in GRP1)
						{
							foreach (grp1 g2 in g)
							{
								if (g2.Objects.Contains(name) && g2.Name != "ENG" && g2.Name.Length == 3)
								{
									if (ok != true)
									{
										ok = true;
									}
									got = true;
								}
								else if (g2.Objects.Contains(name) && g2.Name == "ENG")
								{
									LNGDraw.Add(name, false);
									got = true;
									ok = true;
									return false;
								}
							}
						}
						if (got)
						{
							LNGDraw.Add(name, true);
							return true;
						}
					}
				}
				LNGDraw.Add(name, false);
				return false;
			}
		}
		string sellng = "ENG";

		public byte[] Write()
		{
			MemoryStream m = new MemoryStream();
			EndianBinaryWriter er = new EndianBinaryWriter(m, Endianness.BigEndian);
			Header.Write(er);
			int nr = 0;
			LYT1.Write(er);
			nr++;
			if (TXL1 != null) { TXL1.Write(er); nr++; }
			if (FNL1 != null) { FNL1.Write(er); nr++; }
			MAT1.Write(er);
			nr++;
			PAN1.Write(er, ref nr);
			GRP1.Write(er, ref nr);
			er.BaseStream.Position = 8;
			er.Write((UInt32)er.BaseStream.Length);
			er.BaseStream.Position = 0xe;
			er.Write((UInt16)nr);
			byte[] b = m.ToArray();
			er.Close();
			return b;
		}
		public void BindTextures()
		{
			int i = 0;
			foreach (mat1.MAT1Entry m in MAT1.Entries)
			{
				List<int> tex = new List<int>();
				for (int j = 0; j < m.TextureEntries.Length; j++)
				{
					m.TextureEntries[j].BindTexture(this, i * 16 + j + 1);
					tex.Add(i * 16 + j + 1);
				}
				if (m.TextureEntries.Length == 0)
				{
					Bitmap b = new Bitmap(8, 8);
					using (Graphics g = Graphics.FromImage(b))
					{
						g.Clear(Color.White);
					}
					BitmapData d = b.LockBits(new Rectangle(0, 0, 8, 8), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, i * 16 + 1);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE);
					Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, 8, 8, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, d.Scan0);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_FALSE);
					b.UnlockBits(d);

					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT);


					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
					tex.Add(i * 16 + 1);
				}
				m.GlShader = new Shader(m, tex.ToArray());
				m.GlShader.Compile();
				i++;
			}
		}
		public BRLYTHeader Header;
		public class BRLYTHeader
		{
			public BRLYTHeader(EndianBinaryReader er, String Signature, out bool OK)
			{
				Type = er.ReadString(ASCIIEncoding.ASCII, 4);
				if (Type != Signature) { OK = false; return; }
				Magic = er.ReadUInt32();
				FileSize = er.ReadUInt32();
				Version = er.ReadUInt16();
				NrEntries = er.ReadUInt16();
				OK = true;
			}
			public void Write(EndianBinaryWriter er)
			{
				er.Write(Type, ASCIIEncoding.ASCII, false);
				er.Write(Magic);
				er.Write((UInt32)0);
				er.Write(Version);
				er.Write((UInt16)0);
			}
			public String Type;
			public UInt32 Magic;
			public UInt32 FileSize;
			public UInt16 Version;
			public UInt16 NrEntries;
		}
		public grp1 GRP1;
		public class grp1
		{
			public grp1(EndianBinaryReader er)
			{
				Size = er.ReadUInt32();
				Name = er.ReadString(ASCIIEncoding.ASCII, 16).Replace("\0", "");
				NrObjects = er.ReadUInt16();
				Unknown = er.ReadUInt16();
				Objects = new string[NrObjects];
				for (int i = 0; i < NrObjects; i++)
				{
					Objects[i] = er.ReadString(ASCIIEncoding.ASCII, 16).Replace("\0", "");
				}
				Children = new List<grs1>();
			}
			public void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("grp1", Encoding.ASCII, false);
				er.Write((UInt32)(28 + Objects.Length * 16));
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write((UInt16)Objects.Length);
				er.Write(Unknown);
				for (int i = 0; i < Objects.Length; i++)
				{
					if (Objects[i].Length > 16) er.Write(Objects[i].Remove(16), Encoding.ASCII, false);
					else er.Write(Objects[i].PadRight(16, '\0'), Encoding.ASCII, false);
				}
				nr++;
				if (Children.Count != 0)
				{
					foreach (grs1 s in Children)
					{
						s.Write(er, ref nr);
					}
					er.Write("gre1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
			public List<grs1>.Enumerator GetEnumerator()
			{
				return Children.GetEnumerator();
			}
			public UInt32 Size;
			public String Name;
			public UInt16 NrObjects;
			public UInt16 Unknown;
			public String[] Objects;
			private List<grs1> Children;

			public void Add(grs1 Item)
			{
				Children.Add(Item);
			}
			public void RemoveAt(int Idx)
			{
				Children.RemoveAt(Idx);
			}

			public grs1 this[int index]
			{
				get { return Children[index]; }
				set { Children[index] = value; }
			}
			public int Length
			{
				get { return Children.Count; }
			}
			public override string ToString()
			{
				return Name;
			}
		}
		public class grs1
		{
			public grs1(EndianBinaryReader er)
			{
				Size = er.ReadUInt32();
				Children = new List<grp1>();
			}
			public UInt32 Size;
			public void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("grs1", Encoding.ASCII, false);
				er.Write(Size);
				nr++;
				foreach (grp1 s in Children)
				{
					s.Write(er, ref nr);
				}
			}
			private List<grp1> Children;

			public void Add(grp1 Item)
			{
				Children.Add(Item);
			}
			public void RemoveAt(int Idx)
			{
				Children.RemoveAt(Idx);
			}

			public List<grp1>.Enumerator GetEnumerator()
			{
				return Children.GetEnumerator();
			}

			public grp1 this[int index]
			{
				get { return Children[index]; }
				set { Children[index] = value; }
			}
		}
		public lyt1 LYT1;
		public class lyt1
		{
			public lyt1(EndianBinaryReader er)
			{
				Size = er.ReadUInt32();
				DrawFromMiddle = er.ReadByte() == 1;
				Unknown = er.ReadBytes(3);
				Width = er.ReadSingle();
				Height = er.ReadSingle();
			}
			public void Write(EndianBinaryWriter er)
			{
				er.Write("lyt1", Encoding.ASCII, false);
				er.Write(Size);
				er.Write((byte)(DrawFromMiddle ? 1 : 0));
				er.Write(Unknown, 0, 3);
				er.Write(Width);
				er.Write(Height);
			}
			public UInt32 Size;
			public bool DrawFromMiddle;
			public byte[] Unknown;
			public Single Width;
			public Single Height;
		}
		public txl1 TXL1;
		public fnl1 FNL1;
		public class txl1
		{
			public txl1(EndianBinaryReader er, Dictionary<String, byte[]> Files)
			{
				Size = er.ReadUInt32();
				NrEntries = er.ReadUInt16();
				Unknown1 = er.ReadUInt16();
				FileNames = new string[NrEntries];
				Offsets = new uint[NrEntries];
				Unknown2 = new uint[NrEntries];
				TPLs = new libWiiSharp.TPL[NrEntries];
				for (int i = 0; i < NrEntries; i++)
				{
					Offsets[i] = er.ReadUInt32();
					Unknown2[i] = er.ReadUInt32();
				}
				for (int i = 0; i < NrEntries; i++)
				{
					FileNames[i] = er.ReadStringNT(ASCIIEncoding.ASCII);
					try
					{
						TPLs[i] = libWiiSharp.TPL.Load(new MemoryStream(Files[FileNames[i]]));
					}
					catch
					{
						TPLs[i] = libWiiSharp.TPL.Load(new MemoryStream(Files[FileNames[i].ToLower()]));
					}
				}
				while ((er.BaseStream.Position % 4) != 0) er.ReadByte();
			}
			public void Write(EndianBinaryWriter er)
			{
				long basepos = er.BaseStream.Position;
				er.Write("txl1", Encoding.ASCII, false);
				er.Write((UInt32)0);
				er.Write((UInt16)FileNames.Length);
				er.Write(Unknown1);
				long basepos2 = er.BaseStream.Position;
				er.Write(new UInt64[FileNames.Length], 0, FileNames.Length);
				long curpos = 0;
				for (int i = 0; i < FileNames.Length; i++)
				{
					curpos = er.BaseStream.Position;
					er.BaseStream.Position = basepos2 + i * 8;
					er.Write((UInt32)(curpos - basepos2));
					er.BaseStream.Position = curpos;
					er.Write(FileNames[i], Encoding.ASCII, true);
				}
				while ((er.BaseStream.Position % 4) != 0) er.Write((byte)0);
				curpos = er.BaseStream.Position;
				er.BaseStream.Position = basepos + 4;
				er.Write((UInt32)(curpos - basepos));
				er.BaseStream.Position = curpos;
			}
			public UInt32 Size;
			public UInt16 NrEntries;
			public UInt16 Unknown1;
			public UInt32[] Offsets;
			public UInt32[] Unknown2;
			public String[] FileNames;
			public libWiiSharp.TPL[] TPLs;
		}
		public class fnl1
		{
			public fnl1(EndianBinaryReader er, Dictionary<String, byte[]> Files)
			{
				Size = er.ReadUInt32();
				NrEntries = er.ReadUInt16();
				Unknown1 = er.ReadUInt16();
				FileNames = new string[NrEntries];
				Fonts = new BRFNT[NrEntries];
				Offsets = new uint[NrEntries];
				Unknown2 = new uint[NrEntries];
				for (int i = 0; i < NrEntries; i++)
				{
					Offsets[i] = er.ReadUInt32();
					Unknown2[i] = er.ReadUInt32();
				}
				for (int i = 0; i < NrEntries; i++)
				{
					FileNames[i] = er.ReadStringNT(ASCIIEncoding.ASCII);
					if (Files.ContainsKey(FileNames[i]))
					{
						Fonts[i] = new BRFNT(Files[FileNames[i]]);
					}
				}
				while ((er.BaseStream.Position % 4) != 0) er.ReadByte();
			}
			public void Write(EndianBinaryWriter er)
			{
				long basepos = er.BaseStream.Position;
				er.Write("fnl1", Encoding.ASCII, false);
				er.Write((UInt32)0);
				er.Write((UInt16)FileNames.Length);
				er.Write(Unknown1);
				long basepos2 = er.BaseStream.Position;
				er.Write(new UInt64[FileNames.Length], 0, FileNames.Length);
				long curpos = 0;
				for (int i = 0; i < FileNames.Length; i++)
				{
					curpos = er.BaseStream.Position;
					er.BaseStream.Position = basepos2 + i * 8;
					er.Write((UInt32)(curpos - basepos2));
					er.BaseStream.Position = curpos;
					er.Write(FileNames[i], Encoding.ASCII, true);
				}
				while ((er.BaseStream.Position % 4) != 0) er.Write((byte)0);
				curpos = er.BaseStream.Position;
				er.BaseStream.Position = basepos + 4;
				er.Write((UInt32)(curpos - basepos));
				er.BaseStream.Position = curpos;
			}
			public UInt32 Size;
			public UInt16 NrEntries;
			public UInt16 Unknown1;
			public UInt32[] Offsets;
			public UInt32[] Unknown2;
			public String[] FileNames;
			public BRFNT[] Fonts;
		}
		public mat1 MAT1;
		public class mat1
		{
			public mat1(EndianBinaryReader er)
			{
				Size = er.ReadUInt32();
				NrEntries = er.ReadUInt16();
				Unknown1 = er.ReadUInt16();
				Offsets = er.ReadUInt32s(NrEntries);
				Entries = new MAT1Entry[NrEntries];
				for (int i = 0; i < NrEntries; i++)
				{
					Entries[i] = new MAT1Entry(er);
				}
			}
			public void Write(EndianBinaryWriter er)
			{
				long basepos = er.BaseStream.Position;
				er.Write("mat1", Encoding.ASCII, false);
				er.Write((UInt32)0);
				er.Write((UInt16)Entries.Length);
				er.Write(Unknown1);
				long basepos2 = er.BaseStream.Position;
				long curpos = 0;
				er.Write(new UInt32[Entries.Length], 0, Entries.Length);
				for (int i = 0; i < Entries.Length; i++)
				{
					curpos = er.BaseStream.Position;
					er.BaseStream.Position = basepos2 + i * 4;
					er.Write((UInt32)(curpos - basepos));
					er.BaseStream.Position = curpos;
					Entries[i].Write(er);
				}
				curpos = er.BaseStream.Position;
				er.BaseStream.Position = basepos + 4;
				er.Write((UInt32)(curpos - basepos));
				er.BaseStream.Position = curpos;
			}
			public UInt32 Size;
			public UInt16 NrEntries;
			public UInt16 Unknown1;
			public UInt32[] Offsets;
			public MAT1Entry[] Entries;
			public class MAT1Entry
			{
				public enum GX_WRAP_TAG
				{
					GX_CLAMP = 0,
					GX_REPEAT = 1,
					GX_MIRROR = 2,
					GX_MAXTEXWRAPMODE = 3
				}
				public MAT1Entry(EndianBinaryReader er)
				{
					Name = er.ReadString(ASCIIEncoding.ASCII, 20).Replace("\0", "");
					ForeColor = er.ReadColor16();
					BackColor = er.ReadColor16();
					ColorReg3 = er.ReadColor16();
					TevColor1 = er.ReadColor8();
					TevColor2 = er.ReadColor8();
					TevColor3 = er.ReadColor8();
					TevColor4 = er.ReadColor8();

					MaterialFlag = er.ReadUInt32();

					TextureEntries = new MAT1TextureEntry[BitExtract(MaterialFlag, 28, 31)];
					for (int i = 0; i < BitExtract(MaterialFlag, 28, 31); i++)
					{
						TextureEntries[i] = new MAT1TextureEntry(er);
					}
					TextureSRTEntries = new MAT1TextureSRTEntry[BitExtract(MaterialFlag, 24, 27)];
					for (int i = 0; i < BitExtract(MaterialFlag, 24, 27); i++)
					{
						TextureSRTEntries[i] = new MAT1TextureSRTEntry(er);
					}
					TexCoordGenEntries = new MAT1TexCoordGenEntry[BitExtract(MaterialFlag, 20, 23)];
					for (int i = 0; i < BitExtract(MaterialFlag, 20, 23); i++)
					{
						TexCoordGenEntries[i] = new MAT1TexCoordGenEntry(er);
					}
					if (BitExtract(MaterialFlag, 6, 100) == 1) ChanControl = new MAT1ChanControl(er);
					else ChanControl = new MAT1ChanControl();

					if (BitExtract(MaterialFlag, 4, 100) == 1) MatColor = er.ReadColor8();
					else MatColor = Color.White;

					if (BitExtract(MaterialFlag, 19, 100) == 1) TevSwapModeTable = new MAT1TevSwapModeTable(er);
					else TevSwapModeTable = new MAT1TevSwapModeTable();

					IndirectTextureSRTEntries = new MAT1TextureSRTEntry[BitExtract(MaterialFlag, 17, 18)];
					for (int i = 0; i < BitExtract(MaterialFlag, 17, 18); i++)
					{
						IndirectTextureSRTEntries[i] = new MAT1TextureSRTEntry(er);
					}

					IndirectTextureOrderEntries = new MAT1IndirectTextureOrderEntry[BitExtract(MaterialFlag, 14, 16)];
					for (int i = 0; i < BitExtract(MaterialFlag, 14, 16); i++)
					{
						IndirectTextureOrderEntries[i] = new MAT1IndirectTextureOrderEntry(er);
					}

					TevStageEntries = new MAT1TevStageEntry[BitExtract(MaterialFlag, 9, 13)];
					for (int i = 0; i < BitExtract(MaterialFlag, 9, 13); i++)
					{
						TevStageEntries[i] = new MAT1TevStageEntry(er);
					}

					if (BitExtract(MaterialFlag, 8, 8) == 1) AlphaCompare = new MAT1AlphaCompare(er);
					else AlphaCompare = new MAT1AlphaCompare();

					if (BitExtract(MaterialFlag, 7, 7) == 1) BlendMode = new MAT1BlendMode(er);
					else BlendMode = new MAT1BlendMode();
				}
				public void Write(EndianBinaryWriter er)
				{
					if (Name.Length > 20) er.Write(Name.Remove(20), Encoding.ASCII, false);
					else er.Write(Name.PadRight(20, '\0'), Encoding.ASCII, false);
					er.Write(ForeColor, true);
					er.Write(BackColor, true);
					er.Write(ColorReg3, true);
					er.Write(TevColor1, false);
					er.Write(TevColor2, false);
					er.Write(TevColor3, false);
					er.Write(TevColor4, false);
					UInt32 MatFlag = 0;
					if (MatColor == Color.White && BitExtract(MaterialFlag, 4, 100) == 0) MatFlag |= (0 << 27);
					else MatFlag |= (1 << 27);
					if (ChanControl.ColorMaterialSource == 1 &&
						ChanControl.AlphaMaterialSource == 1 &&
						ChanControl.Unknown1 == 0 &&
						ChanControl.Unknown2 == 0 && BitExtract(MaterialFlag, 6, 100) == 0) MatFlag |= (0 << 25);
					else MatFlag |= (1 << 25);
					if (BlendMode.Type == 1 &&
						BlendMode.Source == 4 &&
						BlendMode.Destination == 5 &&
						BlendMode.Operator == 3 && BitExtract(MaterialFlag, 7, 7) == 0) MatFlag |= (0 << 24);
					else MatFlag |= (1 << 24);
					if (AlphaCompare.Comp0 == 6 &&
						AlphaCompare.Comp1 == 6 &&
						AlphaCompare.Ref0 == 0 &&
						AlphaCompare.Ref1 == 0 &&
						AlphaCompare.AlphaOp == 0 && BitExtract(MaterialFlag, 8, 8) == 0) MatFlag |= (0 << 23);
					else MatFlag |= (1 << 23);
					MatFlag |= (UInt32)((TevStageEntries.Length & 31) << 18);
					MatFlag |= (UInt32)((IndirectTextureOrderEntries.Length & 0x7) << 15);
					MatFlag |= (UInt32)((IndirectTextureSRTEntries.Length & 0x3) << 13);
					if (TevSwapModeTable.AR == 0 &&
						TevSwapModeTable.AG == 1 &&
						TevSwapModeTable.AB == 2 &&
						TevSwapModeTable.AA == 3 &&
						TevSwapModeTable.BR == 0 &&
						TevSwapModeTable.BG == 1 &&
						TevSwapModeTable.BB == 2 &&
						TevSwapModeTable.BA == 3 &&
						TevSwapModeTable.CR == 0 &&
						TevSwapModeTable.CG == 1 &&
						TevSwapModeTable.CB == 2 &&
						TevSwapModeTable.CA == 3 &&
						TevSwapModeTable.DR == 0 &&
						TevSwapModeTable.DG == 1 &&
						TevSwapModeTable.DB == 2 &&
						TevSwapModeTable.DA == 3 && BitExtract(MaterialFlag, 19, 100) == 0) MatFlag |= (0 << 12);
					else MatFlag |= (1 << 12);
					MatFlag |= (UInt32)((TexCoordGenEntries.Length & 0xF) << 8);
					MatFlag |= (UInt32)((TextureSRTEntries.Length & 0xF) << 4);
					MatFlag |= (UInt32)((TextureEntries.Length & 0xF) << 0);

					er.Write((UInt32)MatFlag);

					for (int i = 0; i < BitExtract(MatFlag, 28, 31); i++)
					{
						TextureEntries[i].Write(er);
					}
					for (int i = 0; i < BitExtract(MatFlag, 24, 27); i++)
					{
						TextureSRTEntries[i].Write(er);
					}
					for (int i = 0; i < BitExtract(MatFlag, 20, 23); i++)
					{
						TexCoordGenEntries[i].Write(er);
					}
					if (BitExtract(MatFlag, 6, 100) == 1) ChanControl.Write(er);

					if (BitExtract(MatFlag, 4, 100) == 1) er.Write(MatColor);

					if (BitExtract(MatFlag, 19, 100) == 1) TevSwapModeTable.Write(er);

					for (int i = 0; i < BitExtract(MatFlag, 17, 18); i++)
					{
						IndirectTextureSRTEntries[i].Write(er);
					}

					for (int i = 0; i < BitExtract(MatFlag, 14, 16); i++)
					{
						IndirectTextureOrderEntries[i].Write(er);
					}

					for (int i = 0; i < BitExtract(MatFlag, 9, 13); i++)
					{
						TevStageEntries[i].Write(er);
					}

					if (BitExtract(MatFlag, 8, 8) == 1) AlphaCompare.Write(er);

					if (BitExtract(MatFlag, 7, 7) == 1) BlendMode.Write(er);
				}
				public void GetTreeNode(System.Windows.Forms.TreeNodeCollection t)
				{
					System.Windows.Forms.TreeNode n = t.Add(Name);
					n.Tag = "mat1";
					n.ImageIndex = 3;
					n.SelectedImageIndex = 3;
				}
				public void SetAlphaCompareBlendModes()
				{
					int[] alpha_funcs =
										{
											Gl.GL_NEVER,
											Gl.GL_EQUAL,
											Gl.GL_LEQUAL,
											Gl.GL_GREATER,
											Gl.GL_NOTEQUAL,
											Gl.GL_GEQUAL,
											Gl.GL_ALWAYS,
											Gl.GL_ALWAYS,	 // blah
										};
					int[] blend_types =
										{
											0,	// none
											Gl.GL_FUNC_ADD,
											Gl.GL_FUNC_REVERSE_SUBTRACT,	// LOGIC??
											Gl.GL_FUNC_SUBTRACT,
										};
					int[] blend_factors =
										{
											Gl.GL_ZERO,
											Gl.GL_ONE,
											Gl.GL_SRC_COLOR,
											Gl.GL_ONE_MINUS_SRC_COLOR,
											Gl.GL_SRC_ALPHA,
											Gl.GL_ONE_MINUS_SRC_ALPHA,
											Gl.GL_DST_ALPHA,
											Gl.GL_ONE_MINUS_DST_ALPHA,
										};
					int[] logic_ops =
										{
											Gl.GL_CLEAR,
											Gl.GL_AND,
											Gl.GL_AND_REVERSE,
											Gl.GL_COPY,
											Gl.GL_AND_INVERTED,
											Gl.GL_NOOP,
											Gl.GL_XOR,
											Gl.GL_OR,
											Gl.GL_NOR,
											Gl.GL_EQUIV,
											Gl.GL_INVERT,
											Gl.GL_OR_REVERSE,
											Gl.GL_COPY_INVERTED,
											Gl.GL_OR_INVERTED,
											Gl.GL_NAND,
											Gl.GL_SET,
										};
					Gl.glEnable(Gl.GL_ALPHA_TEST);
					Gl.glAlphaFunc(alpha_funcs[AlphaCompare.Comp0], AlphaCompare.Ref0 / 255f);
					Gl.glAlphaFunc(alpha_funcs[AlphaCompare.Comp1], AlphaCompare.Ref1 / 255f);
					if (BlendMode.Type != 0)
					{
						Gl.glEnable(Gl.GL_BLEND);
						Gl.glBlendEquation(blend_types[BlendMode.Type]);
					}
					else
					{
						Gl.glDisable(Gl.GL_BLEND);
					}
					Gl.glBlendFunc(blend_factors[BlendMode.Source], blend_factors[BlendMode.Destination]);
					Gl.glLogicOp(logic_ops[BlendMode.Operator]);
				}

				public Shader GlShader;

				public String Name { get; private set; }
				[Category("Colors"), DisplayName("ForeColor")]
				public Color ForeColor { get; set; }
				[Category("Colors"), DisplayName("BackColor")]
				public Color BackColor { get; set; }
				[Category("Colors"), DisplayName("Color Register 3")]
				public Color ColorReg3 { get; set; }
				[Category("Colors"), DisplayName("Tev Color 1")]
				public Color TevColor1 { get; set; }
				[Category("Colors"), DisplayName("Tev Color 2")]
				public Color TevColor2 { get; set; }
				[Category("Colors"), DisplayName("Tev Color 3")]
				public Color TevColor3 { get; set; }
				[Category("Colors"), DisplayName("Tev Color 4")]
				public Color TevColor4 { get; set; }

				public UInt32 MaterialFlag;

				[Category("Textures"), DisplayName("Textures")]
				public MAT1TextureEntry[] TextureEntries { get; set; }
				public class MAT1TextureEntry
				{
					public MAT1TextureEntry(EndianBinaryReader er)
					{
						TexIndex = er.ReadUInt16();
						SWrap = (GX_WRAP_TAG)er.ReadByte();
						TWrap = (GX_WRAP_TAG)er.ReadByte();
					}
					public MAT1TextureEntry()
					{

					}
					public UInt16 TexIndex { get; set; }
					public GX_WRAP_TAG SWrap { get; set; }
					public GX_WRAP_TAG TWrap { get; set; }

					public void Write(EndianBinaryWriter er)
					{
						er.Write(TexIndex);
						er.Write((byte)SWrap);
						er.Write((byte)TWrap);
					}
					public void BindTexture(BRLYT Layout, int id)
					{
						libWiiSharp.TPL i = Layout.TXL1.TPLs[TexIndex];
						byte[] data = i.ExtractTextureBitmapData(0);
						Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
						Gl.glBindTexture(Gl.GL_TEXTURE_2D, id);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE);
						Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, i.GetTextureSize(0).Width, i.GetTextureSize(0).Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, data);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_FALSE);

						int[] wrapmode = new int[]
						{
							Gl.GL_CLAMP,
							Gl.GL_REPEAT,
							Gl.GL_MIRRORED_REPEAT, 
							Gl.GL_CLAMP_TO_EDGE
						};

						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, wrapmode[(int)SWrap & 0x3]);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, wrapmode[(int)TWrap & 0x3]);

						// texture filter
						int[] filters = new int[]
			{
				Gl.GL_NEAREST,
				Gl.GL_LINEAR,
				Gl.GL_NEAREST_MIPMAP_NEAREST,
				Gl.GL_LINEAR_MIPMAP_NEAREST,
				Gl.GL_NEAREST_MIPMAP_LINEAR,
				Gl.GL_LINEAR_MIPMAP_LINEAR,
				Gl.GL_NEAREST,	// blah
				Gl.GL_NEAREST,
			};

						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, filters[i.GetTextureHeader(0).MinFilter]);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, filters[i.GetTextureHeader(0).MagFilter]);
					}
				}
				[Category("Textures"), DisplayName("Texture SRT")]
				public MAT1TextureSRTEntry[] TextureSRTEntries { get; set; }
				public class MAT1TextureSRTEntry
				{
					public MAT1TextureSRTEntry(EndianBinaryReader er)
					{
						Ts = er.ReadSingle();
						Tt = er.ReadSingle();
						R = er.ReadSingle();
						Ss = er.ReadSingle();
						St = er.ReadSingle();
					}
					public MAT1TextureSRTEntry() { }
					public void Write(EndianBinaryWriter er)
					{
						er.Write(Ts);
						er.Write(Tt);
						er.Write(R);
						er.Write(Ss);
						er.Write(St);
					}
					[DisplayName("S"), Category("Translation")]
					public float Ts { get; set; }
					[DisplayName("T"), Category("Translation")]
					public float Tt { get; set; }
					[DisplayName("R"), Category("Rotation")]
					public float R { get; set; }
					[DisplayName("S"), Category("Scale")]
					public float Ss { get; set; }
					[DisplayName("T"), Category("Scale")]
					public float St { get; set; }
				}
				[Category("Textures"), DisplayName("Texture Coordinate Generation")]
				public MAT1TexCoordGenEntry[] TexCoordGenEntries { get; set; }
				public class MAT1TexCoordGenEntry
				{
					public enum TexCoordGenTypes
					{
						GX_TG_MTX3x4 = 0,
						GX_TG_MTX2x4 = 1,
						GX_TG_BUMP0 = 2,
						GX_TG_BUMP1 = 3,
						GX_TG_BUMP2 = 4,
						GX_TG_BUMP3 = 5,
						GX_TG_BUMP4 = 6,
						GX_TG_BUMP5 = 7,
						GX_TG_BUMP6 = 8,
						GX_TG_BUMP7 = 9,
						GX_TG_SRTG = 0xA
					}
					public enum TexCoordGenSource
					{
						GX_TG_POS,
						GX_TG_NRM,
						GX_TG_BINRM,
						GX_TG_TANGENT,
						GX_TG_TEX0,
						GX_TG_TEX1,
						GX_TG_TEX2,
						GX_TG_TEX3,
						GX_TG_TEX4,
						GX_TG_TEX5,
						GX_TG_TEX6,
						GX_TG_TEX7,
						GX_TG_TEXCOORD0,
						GX_TG_TEXCOORD1,
						GX_TG_TEXCOORD2,
						GX_TG_TEXCOORD3,
						GX_TG_TEXCOORD4,
						GX_TG_TEXCOORD5,
						GX_TG_TEXCOORD6,
						GX_TG_COLOR0,
						GX_TG_COLOR1
					}
					public enum TexCoordGenMatrixSource
					{
						GX_PNMTX0,
						GX_PNMTX1,
						GX_PNMTX2,
						GX_PNMTX3,
						GX_PNMTX4,
						GX_PNMTX5,
						GX_PNMTX6,
						GX_PNMTX7,
						GX_PNMTX8,
						GX_PNMTX9,
						GX_TEXMTX0,
						GX_TEXMTX1,
						GX_TEXMTX2,
						GX_TEXMTX3,
						GX_TEXMTX4,
						GX_TEXMTX5,
						GX_TEXMTX6,
						GX_TEXMTX7,
						GX_TEXMTX8,
						GX_TEXMTX9,
						GX_IDENTITY,
						GX_DTTMTX0,
						GX_DTTMTX1,
						GX_DTTMTX2,
						GX_DTTMTX3,
						GX_DTTMTX4,
						GX_DTTMTX5,
						GX_DTTMTX6,
						GX_DTTMTX7,
						GX_DTTMTX8,
						GX_DTTMTX9,
						GX_DTTMTX10,
						GX_DTTMTX11,
						GX_DTTMTX12,
						GX_DTTMTX13,
						GX_DTTMTX14,
						GX_DTTMTX15,
						GX_DTTMTX16,
						GX_DTTMTX17,
						GX_DTTMTX18,
						GX_DTTMTX19,
						GX_DTTIDENTITY
					}
					public MAT1TexCoordGenEntry(EndianBinaryReader er)
					{
						Type = (TexCoordGenTypes)er.ReadByte();
						Source = (TexCoordGenSource)er.ReadByte();
						MatrixSource = (TexCoordGenMatrixSource)er.ReadByte();
						Unknown = er.ReadByte();
					}
					public MAT1TexCoordGenEntry() { }
					public void Write(EndianBinaryWriter er)
					{
						er.Write((byte)Type);
						er.Write((byte)Source);
						er.Write((byte)MatrixSource);
						er.Write(Unknown);
					}
					public TexCoordGenTypes Type { get; set; }
					public TexCoordGenSource Source { get; set; }
					public TexCoordGenMatrixSource MatrixSource { get; set; }
					public byte Unknown { get; set; }
				}
				[DisplayName("Channel Control")]
				public MAT1ChanControl ChanControl { get; set; }
				public class MAT1ChanControl
				{
					public MAT1ChanControl(EndianBinaryReader er)
					{
						ColorMaterialSource = er.ReadByte();
						AlphaMaterialSource = er.ReadByte();
						Unknown1 = er.ReadByte();
						Unknown2 = er.ReadByte();
					}
					public MAT1ChanControl()
					{
						ColorMaterialSource = 1;
						AlphaMaterialSource = 1;
						Unknown1 = 0;
						Unknown2 = 0;
					}
					public void Write(EndianBinaryWriter er)
					{
						er.Write(ColorMaterialSource);
						er.Write(AlphaMaterialSource);
						er.Write(Unknown1);
						er.Write(Unknown2);
					}
					public byte ColorMaterialSource { get; set; }
					public byte AlphaMaterialSource { get; set; }
					public byte Unknown1 { get; set; }
					public byte Unknown2 { get; set; }
				}

				[Category("Colors"), DisplayName("Material Color")]
				public Color MatColor { get; set; }
				[DisplayName("Tev Swap Mode Table"), TypeConverter(typeof(ExpandableObjectConverter))]
				public MAT1TevSwapModeTable TevSwapModeTable { get; set; }
				public class MAT1TevSwapModeTable
				{
					public MAT1TevSwapModeTable(EndianBinaryReader er)
					{
						byte c = er.ReadByte();
						AR = (byte)((c >> 0) & 0x3);
						AG = (byte)((c >> 2) & 0x3);
						AB = (byte)((c >> 4) & 0x3);
						AA = (byte)((c >> 6) & 0x3);
						c = er.ReadByte();
						BR = (byte)((c >> 0) & 0x3);
						BG = (byte)((c >> 2) & 0x3);
						BB = (byte)((c >> 4) & 0x3);
						BA = (byte)((c >> 6) & 0x3);
						c = er.ReadByte();
						CR = (byte)((c >> 0) & 0x3);
						CG = (byte)((c >> 2) & 0x3);
						CB = (byte)((c >> 4) & 0x3);
						CA = (byte)((c >> 6) & 0x3);
						c = er.ReadByte();
						DR = (byte)((c >> 0) & 0x3);
						DG = (byte)((c >> 2) & 0x3);
						DB = (byte)((c >> 4) & 0x3);
						DA = (byte)((c >> 6) & 0x3);
					}
					public MAT1TevSwapModeTable()
					{
						AR = 0;
						AG = 1;
						AB = 2;
						AA = 3;

						BR = 0;
						BG = 1;
						BB = 2;
						BA = 3;

						CR = 0;
						CG = 1;
						CB = 2;
						CA = 3;

						DR = 0;
						DG = 1;
						DB = 2;
						DA = 3;
					}
					public void Write(EndianBinaryWriter er)
					{
						byte c = 0;
						c |= (byte)((AA & 0x3) << 6);
						c |= (byte)((AB & 0x3) << 4);
						c |= (byte)((AG & 0x3) << 2);
						c |= (byte)((AR & 0x3) << 0);
						er.Write(c);
						c = 0;
						c |= (byte)((BA & 0x3) << 6);
						c |= (byte)((BB & 0x3) << 4);
						c |= (byte)((BG & 0x3) << 2);
						c |= (byte)((BR & 0x3) << 0);
						er.Write(c);
						c = 0;
						c |= (byte)((CA & 0x3) << 6);
						c |= (byte)((CB & 0x3) << 4);
						c |= (byte)((CG & 0x3) << 2);
						c |= (byte)((CR & 0x3) << 0);
						er.Write(c);
						c = 0;
						c |= (byte)((DA & 0x3) << 6);
						c |= (byte)((DB & 0x3) << 4);
						c |= (byte)((DG & 0x3) << 2);
						c |= (byte)((DR & 0x3) << 0);
						er.Write(c);
					}
					[DisplayName("Red A")]
					public byte AR { get; set; }
					[DisplayName("Green A")]
					public byte AG { get; set; }
					[DisplayName("Blue A")]
					public byte AB { get; set; }
					[DisplayName("Alpha A")]
					public byte AA { get; set; }

					[DisplayName("Red B")]
					public byte BR { get; set; }
					[DisplayName("Green B")]
					public byte BG { get; set; }
					[DisplayName("Blue B")]
					public byte BB { get; set; }
					[DisplayName("Alpha B")]
					public byte BA { get; set; }

					[DisplayName("Red C")]
					public byte CR { get; set; }
					[DisplayName("Green C")]
					public byte CG { get; set; }
					[DisplayName("Blue C")]
					public byte CB { get; set; }
					[DisplayName("Alpha C")]
					public byte CA { get; set; }

					[DisplayName("Red D")]
					public byte DR { get; set; }
					[DisplayName("Green D")]
					public byte DG { get; set; }
					[DisplayName("Blue D")]
					public byte DB { get; set; }
					[DisplayName("Alpha D")]
					public byte DA { get; set; }
				}

				public MAT1TextureSRTEntry[] IndirectTextureSRTEntries { get; set; }

				public MAT1IndirectTextureOrderEntry[] IndirectTextureOrderEntries { get; set; }
				public class MAT1IndirectTextureOrderEntry
				{
					public MAT1IndirectTextureOrderEntry(EndianBinaryReader er)
					{
						TexCoord = er.ReadByte();
						TexMap = er.ReadByte();
						ScaleS = er.ReadByte();
						ScaleT = er.ReadByte();
					}
					public MAT1IndirectTextureOrderEntry() { }
					public void Write(EndianBinaryWriter er)
					{
						er.Write(TexCoord);
						er.Write(TexMap);
						er.Write(ScaleS);
						er.Write(ScaleT);
					}
					public byte TexCoord { get; set; }
					public byte TexMap { get; set; }
					public byte ScaleS { get; set; }
					public byte ScaleT { get; set; }
				}

				[DisplayName("Tev Stages")]
				public MAT1TevStageEntry[] TevStageEntries { get; set; }


				public class MAT1TevStageEntry
				{
					public MAT1TevStageEntry(EndianBinaryReader er)
					{
						TexCoord = er.ReadByte();
						Color = er.ReadByte();

						UInt16 tmp16 = er.ReadUInt16();
						TexMap = (UInt16)(tmp16 & 0x1ff);
						RasSel = (byte)((tmp16 & 0x7ff) >> 9);
						TexSel = (byte)(tmp16 >> 11);

						Byte tmp8 = er.ReadByte();
						ColorA = (byte)(tmp8 & 0xf);
						ColorB = (byte)(tmp8 >> 4);
						tmp8 = er.ReadByte();
						ColorC = (byte)(tmp8 & 0xf);
						ColorD = (byte)(tmp8 >> 4);
						tmp8 = er.ReadByte();
						ColorOp = (byte)(tmp8 & 0xf);
						ColorBias = (byte)((tmp8 & 0x3f) >> 4);
						ColorScale = (byte)(tmp8 >> 6);
						tmp8 = er.ReadByte();
						ColorClamp = (tmp8 & 0x1) == 1;
						ColorRegID = (byte)((tmp8 & 0x7) >> 1);
						ColorConstantSel = (byte)(tmp8 >> 3);


						tmp8 = er.ReadByte();
						AlphaA = (byte)(tmp8 & 0xf);
						AlphaB = (byte)(tmp8 >> 4);
						tmp8 = er.ReadByte();
						AlphaC = (byte)(tmp8 & 0xf);
						AlphaD = (byte)(tmp8 >> 4);
						tmp8 = er.ReadByte();
						AlphaOp = (byte)(tmp8 & 0xf);
						AlphaBias = (byte)((tmp8 & 0x3f) >> 4);
						AlphaScale = (byte)(tmp8 >> 6);
						tmp8 = er.ReadByte();
						AlphaClamp = (tmp8 & 0x1) == 1;
						AlphaRegID = (byte)((tmp8 & 0x7) >> 1);
						AlphaConstantSel = (byte)(tmp8 >> 3);

						tmp8 = er.ReadByte();
						TexID = (byte)(tmp8 & 0x3);
						tmp8 = er.ReadByte();
						Bias = (byte)(tmp8 & 0x7);
						Matrix = (byte)((tmp8 & 0x7F) >> 3);
						tmp8 = er.ReadByte();
						WrapS = (byte)(tmp8 & 0x7);
						WrapT = (byte)((tmp8 & 0x3F) >> 3);
						tmp8 = er.ReadByte();
						Format = (byte)(tmp8 & 0x3);
						AddPrevious = (byte)((tmp8 & 0x7) >> 2);
						UTCLod = (byte)((tmp8 & 0xF) >> 3);
						Alpha = (byte)((tmp8 & 0x3F) >> 4);
					}
					public MAT1TevStageEntry() { }
					public void Write(EndianBinaryWriter er)
					{
						er.Write(TexCoord);
						er.Write(Color);
						UInt16 tmp16 = 0;
						tmp16 |= (UInt16)((TexSel & 0x3F) << 11);
						tmp16 |= (UInt16)((RasSel & 0x7) << 9);
						tmp16 |= (UInt16)((TexMap & 0x1ff) << 0);
						er.Write(tmp16);
						Byte tmp8 = 0;
						tmp8 |= (byte)((ColorB & 0xf) << 4);
						tmp8 |= (byte)((ColorA & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((ColorD & 0xf) << 4);
						tmp8 |= (byte)((ColorC & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((ColorScale & 0x3) << 6);
						tmp8 |= (byte)((ColorBias & 0x3) << 4);
						tmp8 |= (byte)((ColorOp & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((ColorConstantSel & 0x1F) << 3);
						tmp8 |= (byte)((ColorRegID & 0x7) << 1);
						tmp8 |= (byte)((ColorClamp ? 1 : 0) << 0);
						er.Write(tmp8);

						tmp8 = 0;
						tmp8 |= (byte)((AlphaB & 0xf) << 4);
						tmp8 |= (byte)((AlphaA & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((AlphaD & 0xf) << 4);
						tmp8 |= (byte)((AlphaC & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((AlphaScale & 0x3) << 6);
						tmp8 |= (byte)((AlphaBias & 0x3) << 4);
						tmp8 |= (byte)((AlphaOp & 0xf) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((AlphaConstantSel & 0x1F) << 3);
						tmp8 |= (byte)((AlphaRegID & 0x7) << 1);
						tmp8 |= (byte)((AlphaClamp ? 1 : 0) << 0);
						er.Write(tmp8);

						er.Write((byte)(TexID & 0x3));
						tmp8 = 0;
						tmp8 |= (byte)((Matrix & 0x1F) << 3);
						tmp8 |= (byte)((Bias & 0x7) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((WrapT & 0x7) << 3);
						tmp8 |= (byte)((WrapS & 0x7) << 0);
						er.Write(tmp8);
						tmp8 = 0;
						tmp8 |= (byte)((Alpha & 0xF) << 4);
						tmp8 |= (byte)((UTCLod & 0x1) << 3);
						tmp8 |= (byte)((AddPrevious & 0x1) << 2);
						tmp8 |= (byte)((Format & 0x3) << 0);
						er.Write(tmp8);
					}
					public byte TexCoord { get; set; }
					public byte Color { get; set; }
					public UInt16 TexMap { get; set; }
					public byte RasSel { get; set; }
					public byte TexSel { get; set; }

					[Category("Color"), DisplayName("A")]
					public byte ColorA { get; set; }
					[Category("Color"), DisplayName("B")]
					public byte ColorB { get; set; }
					[Category("Color"), DisplayName("C")]
					public byte ColorC { get; set; }
					[Category("Color"), DisplayName("D")]
					public byte ColorD { get; set; }

					[Category("Color"), DisplayName("Operator")]
					public byte ColorOp { get; set; }
					[Category("Color"), DisplayName("Bias")]
					public byte ColorBias { get; set; }
					[Category("Color"), DisplayName("Scale")]
					public byte ColorScale { get; set; }
					[Category("Color"), DisplayName("Clamp")]
					public bool ColorClamp { get; set; }
					[Category("Color"), DisplayName("RegisterID")]
					public byte ColorRegID { get; set; }
					[Category("Color"), DisplayName("ConstantSel")]
					public byte ColorConstantSel { get; set; }

					[Category("Alpha"), DisplayName("A")]
					public byte AlphaA { get; set; }
					[Category("Alpha"), DisplayName("B")]
					public byte AlphaB { get; set; }
					[Category("Alpha"), DisplayName("C")]
					public byte AlphaC { get; set; }
					[Category("Alpha"), DisplayName("D")]
					public byte AlphaD { get; set; }

					[Category("Alpha"), DisplayName("Operator")]
					public byte AlphaOp { get; set; }
					[Category("Alpha"), DisplayName("Bias")]
					public byte AlphaBias { get; set; }
					[Category("Alpha"), DisplayName("Scale")]
					public byte AlphaScale { get; set; }
					[Category("Alpha"), DisplayName("Clamp")]
					public bool AlphaClamp { get; set; }
					[Category("Alpha"), DisplayName("RegisterID")]
					public byte AlphaRegID { get; set; }
					[Category("Alpha"), DisplayName("ConstantSel")]
					public byte AlphaConstantSel { get; set; }

					public byte TexID { get; set; }
					public byte Bias { get; set; }
					public byte Matrix { get; set; }
					public byte WrapS { get; set; }
					public byte WrapT { get; set; }
					public byte Format { get; set; }
					public byte AddPrevious { get; set; }
					public byte UTCLod { get; set; }
					public byte Alpha { get; set; }
				}
				[TypeConverter(typeof(ExpandableObjectConverter))]
				public MAT1AlphaCompare AlphaCompare { get; set; }
				public class MAT1AlphaCompare
				{
					public MAT1AlphaCompare(EndianBinaryReader er)
					{
						byte c = er.ReadByte();
						Comp0 = (byte)(c & 0x7);
						Comp1 = (byte)((c >> 4) & 0x7);
						AlphaOp = er.ReadByte();
						Ref0 = er.ReadByte();
						Ref1 = er.ReadByte();
					}
					public MAT1AlphaCompare()
					{
						Comp0 = 6;
						Comp1 = 6;
						Ref0 = 0;
						Ref1 = 0;
					}
					public void Write(EndianBinaryWriter er)
					{
						byte c = 0;
						c |= (byte)((Comp1 & 0x7) << 4);
						c |= (byte)((Comp0 & 0x7) << 0);
						er.Write(c);
						er.Write(AlphaOp);
						er.Write(Ref0);
						er.Write(Ref1);
					}
					public byte Comp0 { get; set; }
					public byte Comp1 { get; set; }
					public byte AlphaOp { get; set; }
					public byte Ref0 { get; set; }
					public byte Ref1 { get; set; }
				}
				[TypeConverter(typeof(ExpandableObjectConverter))]
				public MAT1BlendMode BlendMode { get; set; }
				public class MAT1BlendMode
				{
					public MAT1BlendMode(EndianBinaryReader er)
					{
						Type = er.ReadByte();
						Source = er.ReadByte();
						Destination = er.ReadByte();
						Operator = er.ReadByte();
					}
					public MAT1BlendMode()
					{
						Type = 1;
						Source = 4;
						Destination = 5;
						Operator = 3;
					}
					public void Write(EndianBinaryWriter er)
					{
						er.Write(Type);
						er.Write(Source);
						er.Write(Destination);
						er.Write(Operator);
					}
					public byte Type { get; set; }
					public byte Source { get; set; }
					public byte Destination { get; set; }
					public byte Operator { get; set; }
				}

				private int BitExtract(uint num, int start, int end)
				{
					if (end == 100) end = start;
					int mask;
					int first;
					int firstMask = 1;
					for (first = 0; first < 31 - start + 1; first++)
					{
						firstMask *= 2;
					}
					firstMask -= 1;
					int secondMask = 1;

					for (first = 0; first < 31 - end; first++)
					{
						secondMask *= 2;
					}
					secondMask -= 1;
					mask = firstMask - secondMask;
					int ret = (int)((int)(num & mask) >> (int)(31 - end));
					return ret;
				}
				public override string ToString()
				{
					return Name;
				}
			}
		}

		public pan1 PAN1;
		public class pan1
		{
			public enum XOrigin
			{
				Left = 0,
				Center = 1,
				Right = 2
			}
			public enum YOrigin
			{
				Top = 0,
				Center = 1,
				Bottom = 2
			}

			public pan1(EndianBinaryReader er, pan1 Parent)
			{
				Children = new List<pan1>();
				this.Parent = Parent;

				Size = er.ReadUInt32();

				Flag = er.ReadByte();
				Visible = (Flag & 0x1) == 1;
				InfluenceAlpha = ((Flag >> 1) & 0x1) == 0;
				WidescreenAffected = ((Flag >> 2) & 0x1) == 0;


				byte flag = er.ReadByte();
				OriginX = (XOrigin)(flag % 3);
				OriginY = (YOrigin)(flag / 3);

				Alpha = er.ReadByte();
				Unknown1 = er.ReadByte();

				Name = er.ReadString(ASCIIEncoding.ASCII, 0x10).Replace("\0", "");
				Unknown2 = er.ReadBytes(8);

				Tx = er.ReadSingle();
				Ty = er.ReadSingle();
				Tz = er.ReadSingle();

				Rx = er.ReadSingle();
				Ry = er.ReadSingle();
				Rz = er.ReadSingle();

				Sx = er.ReadSingle();
				Sy = er.ReadSingle();

				Width = er.ReadSingle();
				Height = er.ReadSingle();
			}

			public pan1 Parent;
			protected List<pan1> Children;

			public void Add(pan1 Item)
			{
				Children.Add(Item);
			}
			public void RemoveAt(int Idx)
			{
				Children.RemoveAt(Idx);
			}

			public pan1 this[int index]
			{
				get { return Children[index]; }
				set { Children[index] = value; }
			}

			public UInt32 Size;

			public byte Flag;

			public bool Visible { get; set; }
			public bool WidescreenAffected { get; set; }
			public bool InfluenceAlpha { get; set; }

			[Category("Origin"), DisplayName("X")]
			public XOrigin OriginX { get; set; }
			[Category("Origin"), DisplayName("Y")]
			public YOrigin OriginY { get; set; }

			public byte Alpha { get; set; }
			public byte Unknown1 { get; set; }

			public String Name { get; private set; }
			public byte[] Unknown2 { get; set; }

			[Category("Translation"), DisplayName("X")]
			public Single Tx { get; set; }
			[Category("Translation"), DisplayName("Y")]
			public Single Ty { get; set; }
			[Category("Translation"), DisplayName("Z")]
			public Single Tz { get; set; }

			[Category("Rotation"), DisplayName("X")]
			public Single Rx { get; set; }
			[Category("Rotation"), DisplayName("Y")]
			public Single Ry { get; set; }
			[Category("Rotation"), DisplayName("Z")]
			public Single Rz { get; set; }

			[Category("Scale"), DisplayName("X")]
			public Single Sx { get; set; }
			[Category("Scale"), DisplayName("Y")]
			public Single Sy { get; set; }

			[Category("Size")]
			public Single Width { get; set; }
			[Category("Size")]
			public Single Height { get; set; }

			public virtual void Render(BRLYT Layout, ref int idx, byte alpha = 255, bool picking = false, BRLAN Animation = null, float Frame = 0, BRLAN.AnimatedPan1 ParentA = null, BRLAN.AnimatedMat1[] Materials = null)
			{
				BRLAN.AnimatedPan1 pp = null;
				if (Animation != null) { pp = Animation.GetPaneValue(Frame, this); if (!pp.Visible || Layout.Language(Name)) return; }
				else if (!Visible || Layout.Language(Name)) return;
				Gl.glPushMatrix();
				{
					if (Animation == null)
					{
						Gl.glTranslatef(Tx, Ty, Tz);
						Gl.glRotatef(Rx, 1, 0, 0);
						Gl.glRotatef(Ry, 0, 1, 0);
						Gl.glRotatef(Rz, 0, 0, 1);
						Gl.glScalef(Sx, Sy, 1);
					}
					else
					{
						Gl.glTranslatef(pp.Tx, pp.Ty, pp.Tz);
						Gl.glRotatef(pp.Rx, 1, 0, 0);
						Gl.glRotatef(pp.Ry, 0, 1, 0);
						Gl.glRotatef(pp.Rz, 0, 0, 1);
						Gl.glScalef(pp.Sx, pp.Sy, 1);
					}
					foreach (pan1 p in Children)
					{
						p.Render(Layout, ref idx, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)), picking, Animation, Frame, pp, Materials);
					}
				}
				Gl.glPopMatrix();
			}
			public virtual void GetTreeNodes(System.Windows.Forms.TreeNodeCollection t)
			{
				System.Windows.Forms.TreeNode n = t.Add(Name, Name, 0);
				n.Tag = "pan1";
				foreach (pan1 p in Children)
				{
					p.GetTreeNodes(n.Nodes);
				}
			}
			public override string ToString()
			{
				return Name;
			}

			public pan1 GetByName(String Name)
			{
				if (this.Name == Name) return this;
				foreach (pan1 p in Children)
				{
					pan1 pp = p.GetByName(Name);
					if (pp != null) return pp;
				}
				return null;
			}
			public virtual pan1 GetByID(int idx, ref int idx2)
			{
				if (!Visible) return null;
				foreach (pan1 p in Children)
				{
					pan1 pp = p.GetByID(idx, ref idx2);
					if (pp != null) return pp;
				}
				return null;
			}
			public virtual void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("pan1", Encoding.ASCII, false);
				er.Write(Size);
				byte flag = Flag;
				flag &= 0xF8;
				flag |= (byte)(Visible ? 1 : 0);
				flag |= (byte)((InfluenceAlpha ? 1 : 0) << 1);
				flag |= (byte)((WidescreenAffected ? 1 : 0) << 2);
				er.Write((byte)Flag);
				er.Write((byte)(((int)OriginX) + ((int)OriginY * 3)));
				er.Write(Alpha);
				er.Write(Unknown1);
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write(Unknown2, 0, 8);
				er.Write(Tx);
				er.Write(Ty);
				er.Write(Tz);
				er.Write(Rx);
				er.Write(Ry);
				er.Write(Rz);
				er.Write(Sx);
				er.Write(Sy);
				er.Write(Width);
				er.Write(Height);
				nr++;
				if (Children.Count != 0)
				{
					er.Write("pas1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
					foreach (pan1 p in Children)
					{
						p.Write(er, ref nr);
					}
					er.Write("pae1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
		}
		public class bnd1 : pan1
		{
			public bnd1(EndianBinaryReader er, pan1 Parent)
				: base(er, Parent) { }

			public override void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("bnd1", Encoding.ASCII, false);
				er.Write(Size);
				byte flag = Flag;
				flag &= 0xF8;
				flag |= (byte)(Visible ? 1 : 0);
				flag |= (byte)((InfluenceAlpha ? 1 : 0) << 1);
				flag |= (byte)((WidescreenAffected ? 1 : 0) << 2);
				er.Write((byte)Flag);
				er.Write((byte)(((int)OriginX) + ((int)OriginY * 3)));
				er.Write(Alpha);
				er.Write(Unknown1);
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write(Unknown2, 0, 8);
				er.Write(Tx);
				er.Write(Ty);
				er.Write(Tz);
				er.Write(Rx);
				er.Write(Ry);
				er.Write(Rz);
				er.Write(Sx);
				er.Write(Sy);
				er.Write(Width);
				er.Write(Height);
				nr++;
				if (Children.Count != 0)
				{
					er.Write("pas1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
					foreach (pan1 p in Children)
					{
						p.Write(er, ref nr);
					}
					er.Write("pae1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
			public virtual void GetTreeNodes(System.Windows.Forms.TreeNodeCollection t)
			{
				System.Windows.Forms.TreeNode n = t.Add(Name, Name, 0);
				n.Tag = "bnd1";
				foreach (pan1 p in Children)
				{
					p.GetTreeNodes(n.Nodes);
				}
			}
		}
		public class pic1 : pan1
		{
			public pic1(EndianBinaryReader er, pan1 Parent)
				: base(er, Parent)
			{
				VertexColorTopLeft = er.ReadColor8();
				VertexColorTopRight = er.ReadColor8();
				VertexColorBottomLeft = er.ReadColor8();
				VertexColorBottomRight = er.ReadColor8();
				MaterialID = er.ReadUInt16();
				NrTexCoordSets = er.ReadByte();
				Unknown3 = er.ReadByte();

				TexCoordSets = new TexCoordSet[NrTexCoordSets];
				for (int i = 0; i < NrTexCoordSets; i++)
				{
					TexCoordSets[i] = new TexCoordSet(er);
				}
			}
			[Category("Vertex Colors"), DisplayName("Top Left")]
			public Color VertexColorTopLeft { get; set; }
			[Category("Vertex Colors"), DisplayName("Top Right")]
			public Color VertexColorTopRight { get; set; }
			[Category("Vertex Colors"), DisplayName("Bottom Left")]
			public Color VertexColorBottomLeft { get; set; }
			[Category("Vertex Colors"), DisplayName("Bottom Right")]
			public Color VertexColorBottomRight { get; set; }
			public UInt16 MaterialID { get; set; }
			public byte NrTexCoordSets { get; set; }
			public byte Unknown3 { get; set; }

			public TexCoordSet[] TexCoordSets { get; set; }
			public class TexCoordSet
			{
				public TexCoordSet(EndianBinaryReader er)
				{
					TopLeftS = er.ReadSingle();
					TopLeftT = er.ReadSingle();

					TopRightS = er.ReadSingle();
					TopRightT = er.ReadSingle();

					BottomLeftS = er.ReadSingle();
					BottomLeftT = er.ReadSingle();

					BottomRightS = er.ReadSingle();
					BottomRightT = er.ReadSingle();
				}
				public TexCoordSet()
				{

				}
				public void Write(EndianBinaryWriter er)
				{
					er.Write(TopLeftS);
					er.Write(TopLeftT);
					er.Write(TopRightS);
					er.Write(TopRightT);
					er.Write(BottomLeftS);
					er.Write(BottomLeftT);
					er.Write(BottomRightS);
					er.Write(BottomRightT);
				}
				public float TopLeftS { get; set; }
				public float TopLeftT { get; set; }

				public float TopRightS { get; set; }
				public float TopRightT { get; set; }

				public float BottomLeftS { get; set; }
				public float BottomLeftT { get; set; }

				public float BottomRightS { get; set; }
				public float BottomRightT { get; set; }
			}

			public override void Render(BRLYT Layout, ref int idx, byte alpha = 255, bool picking = false, BRLAN Animation = null, float Frame = 0, BRLAN.AnimatedPan1 ParentA = null, BRLAN.AnimatedMat1[] Materials = null)
			{
				BRLAN.AnimatedPic1 pp = null;
				if (Animation != null) { pp = Animation.GetPicValue(Frame, this); if (!pp.Visible || Layout.Language(Name)) return; }
				else if (!Visible || Layout.Language(Name)) return;
				Gl.glPushMatrix();
				{
					mat1.MAT1Entry mat = Layout.MAT1.Entries[MaterialID];
					if (!picking)
					{
						mat.SetAlphaCompareBlendModes();

						for (int o = 0; o < mat.TextureEntries.Length; o++)
						{
							Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
							Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + o + 1);
							Gl.glEnable(Gl.GL_TEXTURE_2D);
						}
						if (mat.TextureEntries.Length == 0)
						{
							Gl.glActiveTexture(Gl.GL_TEXTURE0);
							Gl.glColor4f(1, 1, 1, 1);
							Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + 1);
							Gl.glEnable(Gl.GL_TEXTURE_2D);
						}

						Gl.glMatrixMode(Gl.GL_TEXTURE);
						for (int o = 0; o < mat.TextureEntries.Length; o++)
						{
							Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
							Gl.glLoadIdentity();
							mat1.MAT1Entry.MAT1TextureSRTEntry srt;
							if (Animation == null) srt = mat.TextureSRTEntries[(int)mat.TexCoordGenEntries[o].MatrixSource / 3 - 10];
							else srt = Materials[MaterialID].TextureSRTEntries[(int)mat.TexCoordGenEntries[o].MatrixSource / 3 - 10];
							Gl.glTranslatef(0.5f, 0.5f, 0.0f);
							Gl.glRotatef(srt.R, 0.0f, 0.0f, 1.0f);
							Gl.glScalef(srt.Ss, srt.St, 1.0f);
							Gl.glTranslatef(srt.Ts / srt.Ss - 0.5f, srt.Tt / srt.St - 0.5f, 0.0f);
						}
						if (mat.TextureEntries.Length == 0)
						{
							Gl.glActiveTexture(Gl.GL_TEXTURE0);
							Gl.glLoadIdentity();
						}

						Gl.glMatrixMode(Gl.GL_MODELVIEW);

						if (Animation == null) mat.GlShader.RefreshColors(mat);
						else mat.GlShader.RefreshColors(Materials[MaterialID]);
						mat.GlShader.Enable();

						if (Animation == null)
						{
							Gl.glTranslatef(Tx, Ty, Tz);
							Gl.glRotatef(Rx, 1, 0, 0);
							Gl.glRotatef(Ry, 0, 1, 0);
							Gl.glRotatef(Rz, 0, 0, 1);
							Gl.glScalef(Sx, Sy, 1);
						}
						else
						{
							Gl.glTranslatef(pp.Tx, pp.Ty, pp.Tz);
							Gl.glRotatef(pp.Rx, 1, 0, 0);
							Gl.glRotatef(pp.Ry, 0, 1, 0);
							Gl.glRotatef(pp.Rz, 0, 0, 1);
							Gl.glScalef(pp.Sx, pp.Sy, 1);
						}
						Gl.glPushMatrix();
						{
							if (Animation == null)
								Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
							else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

							float[,] Vertex2 = new float[4, 2];

							if (Animation == null)
							{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = Width;
								Vertex2[2, 0] = Width;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -Height;
								Vertex2[3, 1] = -Height;
							}
							else
							{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = pp.Width;
								Vertex2[2, 0] = pp.Width;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -pp.Height;
								Vertex2[3, 1] = -pp.Height;
							}
							float[] TL2;
							float[] TR2;
							float[] BL2;
							float[] BR2;
							if (Animation == null)
							{
								TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
							}
							else
							{
								TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
							}

							/*	if (p.nosrcalpha)
								{
									TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
									TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
									BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
									BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
								}*/
							Gl.glBegin(Gl.GL_QUADS);

							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopLeftS,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopLeftT
									);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);
							}
							Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
							Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopRightS,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopRightT
									);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 1, 0);
							}
							Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
							Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomRightS,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomRightT
									);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 1, 1);
							}
							Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
							Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomLeftS,
									TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomLeftT
									);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 1);
							}
							Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
							Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
							Gl.glEnd();
						}
						Gl.glPopMatrix();
					}
					else
					{
						Gl.glTranslatef(Tx, Ty, Tz);
						Gl.glRotatef(Rx, 1, 0, 0);
						Gl.glRotatef(Ry, 0, 1, 0);
						Gl.glRotatef(Rz, 0, 0, 1);
						Gl.glScalef(Sx, Sy, 1);
						if (Alpha > 127)
						{
							Gl.glPushMatrix();
							{
								Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);

								float[,] Vertex2 = new float[4, 2];

								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = Width;
								Vertex2[2, 0] = Width;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -Height;
								Vertex2[3, 1] = -Height;

								Color c = Color.FromArgb(idx + 1);
								Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 1);
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
						}
						idx++;
					}
					foreach (pan1 p in Children)
					{
						p.Render(Layout, ref idx, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)), picking, Animation, Frame, pp, Materials);
					}
				}
				Gl.glPopMatrix();
			}
			public override void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("pic1", Encoding.ASCII, false);
				er.Write((UInt32)(76 + 20 + NrTexCoordSets * 32));
				byte flag = Flag;
				flag &= 0xF8;
				flag |= (byte)(Visible ? 1 : 0);
				flag |= (byte)((InfluenceAlpha ? 1 : 0) << 1);
				flag |= (byte)((WidescreenAffected ? 1 : 0) << 2);
				er.Write((byte)Flag);
				er.Write((byte)(((int)OriginX) + ((int)OriginY * 3)));
				er.Write(Alpha);
				er.Write(Unknown1);
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write(Unknown2, 0, 8);
				er.Write(Tx);
				er.Write(Ty);
				er.Write(Tz);
				er.Write(Rx);
				er.Write(Ry);
				er.Write(Rz);
				er.Write(Sx);
				er.Write(Sy);
				er.Write(Width);
				er.Write(Height);
				er.Write(VertexColorTopLeft);
				er.Write(VertexColorTopRight);
				er.Write(VertexColorBottomLeft);
				er.Write(VertexColorBottomRight);
				er.Write(MaterialID);
				er.Write((byte)TexCoordSets.Length);
				er.Write(Unknown3);
				foreach (TexCoordSet s in TexCoordSets) s.Write(er);
				nr++;
				if (Children.Count != 0)
				{
					er.Write("pas1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
					foreach (pan1 p in Children)
					{
						p.Write(er, ref nr);
					}
					er.Write("pae1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
			public override void GetTreeNodes(System.Windows.Forms.TreeNodeCollection t)
			{
				System.Windows.Forms.TreeNode n = t.Add(Name, Name, 1);
				n.SelectedImageIndex = 1;
				n.Tag = "pic1";
				foreach (pan1 p in Children)
				{
					p.GetTreeNodes(n.Nodes);
				}
			}
			public override pan1 GetByID(int idx, ref int idx2)
			{
				if (!Visible) return null;
				if (idx == idx2) return this;
				idx2++;
				foreach (pan1 p in Children)
				{
					pan1 pp = p.GetByID(idx, ref idx2);
					if (pp != null) return pp;
				}
				return null;
			}
		}

		public class txt1 : pan1
		{
			public txt1(EndianBinaryReader er, pan1 Parent)
				: base(er, Parent)
			{
				NrCharacters = er.ReadUInt16();
				Unknown3 = er.ReadUInt16();
				MaterialID = er.ReadUInt16();
				Text = new Font(er);
				er.BaseStream.Position = er.BaseStream.Position - 116 + Size;
			}

			public bool texbind = false;

			public UInt16 NrCharacters;
			public UInt16 Unknown3 { get; set; }
			public UInt16 MaterialID { get; set; }

			[TypeConverter(typeof(ExpandableObjectConverter))]
			public Font Text { get; set; }

			public class Font
			{
				public Font(EndianBinaryReader er)
				{
					FontIndex = er.ReadUInt16();
					byte flag = er.ReadByte();
					XAlignment = (XOrigin)(flag % 3);
					YAlignment = (YOrigin)(flag / 3);
					Unknown = er.ReadBytes(3);
					TextOffset = er.ReadUInt32();
					TopColor = er.ReadColor8();
					BottomColor = er.ReadColor8();
					XSize = er.ReadSingle();
					YSize = er.ReadSingle();
					CharSize = er.ReadSingle();
					LineSize = er.ReadSingle();
					long curpos = er.BaseStream.Position;
					er.BaseStream.Position = curpos - 116 + TextOffset;
					Text = er.ReadStringNT(Encoding.Unicode);
					er.BaseStream.Position = curpos;
				}
				public UInt16 FontIndex { get; set; }
				[DisplayName("X Alignment")]
				public XOrigin XAlignment { get; set; }
				[DisplayName("Y Alignment")]
				public YOrigin YAlignment { get; set; }
				public byte[] Unknown { get; set; }
				public UInt32 TextOffset;
				public Color TopColor { get; set; }
				public Color BottomColor { get; set; }
				[DisplayName("X Size")]
				public Single XSize { get; set; }
				[DisplayName("Y Size")]
				public Single YSize { get; set; }
				public Single CharSize { get; set; }
				public Single LineSize { get; set; }
				[Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(System.Drawing.Design.UITypeEditor))]
				public String Text { get; set; }
				public override string ToString()
				{
					return Text;
				}
				public void Write(EndianBinaryWriter er)
				{
					er.Write(FontIndex);
					er.Write((byte)(((int)XAlignment) + ((int)YAlignment * 3)));
					er.Write(Unknown, 0, 3);
					er.Write(TextOffset);
					er.Write(TopColor);
					er.Write(BottomColor);
					er.Write(XSize);
					er.Write(YSize);
					er.Write(CharSize);
					er.Write(LineSize);
					er.Write(Text, Encoding.Unicode, true);
				}
			}
			public override void Render(BRLYT Layout, ref int idx, byte alpha = 255, bool picking = false, BRLAN Animation = null, float Frame = 0, BRLAN.AnimatedPan1 ParentA = null, BRLAN.AnimatedMat1[] Materials = null)
			{
				BRLAN.AnimatedPan1 pp = null;
				if (Animation != null) { pp = Animation.GetPaneValue(Frame, this); if (!pp.Visible || Layout.Language(Name)) return; }
				else if (!Visible || Layout.Language(Name)) return;
				Gl.glPushMatrix();
				{
					mat1.MAT1Entry mat = Layout.MAT1.Entries[MaterialID];

					if (!texbind && Layout.FNL1.Fonts[Text.FontIndex] != null)
					{
						BRFNT t = Layout.FNL1.Fonts[Text.FontIndex];
						Bitmap b2 = t.GetBitmap(Text.Text, false, Text);
						int width;
						int height;
						if (Animation == null)
						{
							width = (int)Width;
							height = (int)Height;
						}
						else
						{
							width = (int)pp.Width;
							height = (int)pp.Height;
						}

						switch (Text.XAlignment + "_" + Text.YAlignment)
						{
							case "Center_Center":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width / 2f - b2.Width / 2f, b.Height / 2f - b2.Height / 2f);
									}
									b2 = b;
									break;
								}
							case "Left_Center":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, 0, b.Height / 2f - b2.Height / 2f);
									}
									b2 = b;
									break;
								}
							case "Right_Center":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width - b2.Width, b.Height / 2f - b2.Height / 2f);
									}
									b2 = b;
									break;
								}
							case "Center_Top":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width / 2f - b2.Width / 2f, 0);
									}
									b2 = b;
									break;
								}
							case "Center_Bottom":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width / 2f - b2.Width / 2f, b.Height - b2.Height);
									}
									b2 = b;
									break;
								}
							case "Left_Top":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, 0, 0);
									}
									b2 = b;
									break;
								}
							case "Right_Top":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width - b2.Width, 0);
									}
									b2 = b;
									break;
								}
							case "Left_Bottom":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, 0, b.Height - b2.Height);
									}
									b2 = b;
									break;
								}
							case "Right_Bottom":
								{
									Bitmap b = new Bitmap((int)width, (int)height);
									using (Graphics g = Graphics.FromImage(b))
									{
										g.DrawImage(b2, b.Width - b2.Width, b.Height - b2.Height);
									}
									b2 = b;
									break;
								}
						}

						Gl.glMatrixMode(Gl.GL_TEXTURE);
						Gl.glLoadIdentity();
						BitmapData bd = b2.LockBits(new Rectangle(0, 0, b2.Width, b2.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
						Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
						Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + 14 + 1);
						//Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE);
						Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, b2.Width, b2.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bd.Scan0);
						//Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_FALSE);
						b2.UnlockBits(bd);

						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
						Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
						Gl.glMatrixMode(Gl.GL_MODELVIEW);
						texbind = true;
					}
					if (texbind)
					{
						if (!picking)
						{
							mat.SetAlphaCompareBlendModes();

							Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + 14 + 1);
							Gl.glEnable(Gl.GL_TEXTURE_2D);

							Gl.glMatrixMode(Gl.GL_TEXTURE);
							Gl.glLoadIdentity();

							Gl.glMatrixMode(Gl.GL_MODELVIEW);

							if (Animation == null) mat.GlShader.RefreshColors(mat);
							else mat.GlShader.RefreshColors(Materials[MaterialID]);
							mat.GlShader.Enable();

							if (Animation == null)
							{
								Gl.glTranslatef(Tx, Ty, Tz);
								Gl.glRotatef(Rx, 1, 0, 0);
								Gl.glRotatef(Ry, 0, 1, 0);
								Gl.glRotatef(Rz, 0, 0, 1);
								Gl.glScalef(Sx, Sy, 1);
							}
							else
							{
								Gl.glTranslatef(pp.Tx, pp.Ty, pp.Tz);
								Gl.glRotatef(pp.Rx, 1, 0, 0);
								Gl.glRotatef(pp.Ry, 0, 1, 0);
								Gl.glRotatef(pp.Rz, 0, 0, 1);
								Gl.glScalef(pp.Sx, pp.Sy, 1);
							}
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								float[,] Vertex2 = new float[4, 2];
								if (Animation == null)
								{
									Vertex2[0, 0] = 0;
									Vertex2[1, 0] = Width;
									Vertex2[2, 0] = Width;
									Vertex2[3, 0] = 0;

									Vertex2[0, 1] = 0;
									Vertex2[1, 1] = 0;
									Vertex2[2, 1] = -Height;
									Vertex2[3, 1] = -Height;
								}
								else
								{
									Vertex2[0, 0] = 0;
									Vertex2[1, 0] = pp.Width;
									Vertex2[2, 0] = pp.Width;
									Vertex2[3, 0] = 0;

									Vertex2[0, 1] = 0;
									Vertex2[1, 1] = 0;
									Vertex2[2, 1] = -pp.Height;
									Vertex2[3, 1] = -pp.Height;
								}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									Text.TopColor.R / 255f,
									Text.TopColor.G / 255f,
									Text.TopColor.B / 255f,
									Util.MixColors(Text.TopColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									Text.TopColor.R / 255f,
									Text.TopColor.G / 255f,
									Text.TopColor.B / 255f,
									Util.MixColors(Text.TopColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									Text.BottomColor.R / 255f,
									Text.BottomColor.G / 255f,
									Text.BottomColor.B / 255f,
									Util.MixColors(Text.BottomColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									Text.BottomColor.R / 255f,
									Text.BottomColor.G / 255f,
									Text.BottomColor.B / 255f,
									Util.MixColors(Text.BottomColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									Text.TopColor.R / 255f,
									Text.TopColor.G / 255f,
									Text.TopColor.B / 255f,
									Util.MixColors(Text.TopColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									Text.TopColor.R / 255f,
									Text.TopColor.G / 255f,
									Text.TopColor.B / 255f,
									Util.MixColors(Text.TopColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									Text.BottomColor.R / 255f,
									Text.BottomColor.G / 255f,
									Text.BottomColor.B / 255f,
									Util.MixColors(Text.BottomColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									Text.BottomColor.R / 255f,
									Text.BottomColor.G / 255f,
									Text.BottomColor.B / 255f,
									Util.MixColors(Text.BottomColor.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}

								/*	if (p.nosrcalpha)
									{
										TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
										TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
										BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
										BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
									}*/
								Gl.glBegin(Gl.GL_QUADS);

								Gl.glTexCoord2f(0, 0);
								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glTexCoord2f(1, 0);

								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glTexCoord2f(1, 1);

								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glTexCoord2f(0, 1);

								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
						}
						else
						{
							Gl.glTranslatef(Tx, Ty, Tz);
							Gl.glRotatef(Rx, 1, 0, 0);
							Gl.glRotatef(Ry, 0, 1, 0);
							Gl.glRotatef(Rz, 0, 0, 1);
							Gl.glScalef(Sx, Sy, 1);
							if (Alpha > 127)
							{
								Gl.glPushMatrix();
								{
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);

									float[,] Vertex2 = new float[4, 2];

									Vertex2[0, 0] = 0;
									Vertex2[1, 0] = Width;
									Vertex2[2, 0] = Width;
									Vertex2[3, 0] = 0;

									Vertex2[0, 1] = 0;
									Vertex2[1, 1] = 0;
									Vertex2[2, 1] = -Height;
									Vertex2[3, 1] = -Height;

									Color c = Color.FromArgb(idx + 1);
									Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 1);
									Gl.glBegin(Gl.GL_QUADS);
									Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
									Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
									Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
									Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
									Gl.glEnd();
								}
								Gl.glPopMatrix();
							}
							idx++;
						}
					}
					foreach (pan1 p in Children)
					{
						p.Render(Layout, ref idx, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)), picking, Animation, Frame, pp, Materials);
					}
				}
				Gl.glPopMatrix();
			}
			public override void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("txt1", Encoding.ASCII, false);
				int length = 76 + 40 + Text.Text.Length * 2 + 2;
				length += length % 4;
				er.Write((UInt32)length);
				byte flag = Flag;
				flag &= 0xF8;
				flag |= (byte)(Visible ? 1 : 0);
				flag |= (byte)((InfluenceAlpha ? 1 : 0) << 1);
				flag |= (byte)((WidescreenAffected ? 1 : 0) << 2);
				er.Write((byte)Flag);
				er.Write((byte)(((int)OriginX) + ((int)OriginY * 3)));
				er.Write(Alpha);
				er.Write(Unknown1);
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write(Unknown2, 0, 8);
				er.Write(Tx);
				er.Write(Ty);
				er.Write(Tz);
				er.Write(Rx);
				er.Write(Ry);
				er.Write(Rz);
				er.Write(Sx);
				er.Write(Sy);
				er.Write(Width);
				er.Write(Height);


				er.Write((UInt16)(Text.Text.Length * 2 + 2));
				er.Write(Unknown3);
				er.Write(MaterialID);
				Text.Write(er);
				while ((er.BaseStream.Position % 4) != 0) er.Write((byte)0);
				nr++;
				if (Children.Count != 0)
				{
					er.Write("pas1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
					foreach (pan1 p in Children)
					{
						p.Write(er, ref nr);
					}
					er.Write("pae1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
			public override void GetTreeNodes(System.Windows.Forms.TreeNodeCollection t)
			{
				System.Windows.Forms.TreeNode n = t.Add(Name, Name, 2);
				n.SelectedImageIndex = 2;
				n.Tag = "txt1";
				foreach (pan1 p in Children)
				{
					p.GetTreeNodes(n.Nodes);
				}
			}
			public override pan1 GetByID(int idx, ref int idx2)
			{
				if (!Visible) return null;
				if (idx == idx2) return this;
				idx2++;
				foreach (pan1 p in Children)
				{
					pan1 pp = p.GetByID(idx, ref idx2);
					if (pp != null) return pp;
				}
				return null;
			}
		}
		public class wnd1 : pan1
		{
			public wnd1(EndianBinaryReader er, pan1 Parent)
				: base(er, Parent)
			{
				Coordinate1 = er.ReadSingle();
				Coordinate2 = er.ReadSingle();
				Coordinate3 = er.ReadSingle();
				Coordinate4 = er.ReadSingle();
				FrameCount = er.ReadByte();
				Unknown3 = er.ReadBytes(3);
				Offset1 = er.ReadUInt32();
				Offset2 = er.ReadUInt32();

				VertexColorTopLeft = er.ReadColor8();
				VertexColorTopRight = er.ReadColor8();
				VertexColorBottomLeft = er.ReadColor8();
				VertexColorBottomRight = er.ReadColor8();
				MaterialID = er.ReadUInt16();
				NrTexCoordSets = er.ReadByte();
				Unknown4 = er.ReadByte();

				TexCoordSets = new pic1.TexCoordSet[NrTexCoordSets];
				for (int i = 0; i < NrTexCoordSets; i++)
				{
					TexCoordSets[i] = new pic1.TexCoordSet(er);
				}

				WND4s = new wnd4[FrameCount];

				for (int i = 0; i < FrameCount; i++)
				{
					WND4s[i] = new wnd4(er);
				}

				WND4Mats = new wnd4Mat[FrameCount];

				for (int i = 0; i < FrameCount; i++)
				{
					WND4Mats[i] = new wnd4Mat(er);
				}
			}
			public Single Coordinate1 { get; set; }
			public Single Coordinate2 { get; set; }
			public Single Coordinate3 { get; set; }
			public Single Coordinate4 { get; set; }
			public byte FrameCount;
			public byte[] Unknown3 { get; set; }
			public UInt32 Offset1;
			public UInt32 Offset2;

			public Color VertexColorTopLeft { get; set; }
			public Color VertexColorTopRight { get; set; }
			public Color VertexColorBottomLeft { get; set; }
			public Color VertexColorBottomRight { get; set; }
			public UInt16 MaterialID { get; set; }
			public byte NrTexCoordSets;
			public byte Unknown4 { get; set; }
			public pic1.TexCoordSet[] TexCoordSets { get; set; }

			public wnd4[] WND4s { get; set; }
			public class wnd4
			{
				public wnd4(EndianBinaryReader er)
				{
					Offset = er.ReadUInt32();
				}
				public void Write(EndianBinaryWriter er)
				{
					er.Write(Offset);
				}
				public UInt32 Offset;
			}

			public wnd4Mat[] WND4Mats { get; set; }
			public class wnd4Mat
			{
				public wnd4Mat(EndianBinaryReader er)
				{
					MaterialID = er.ReadUInt16();
					Index = er.ReadByte();
					Unknown = er.ReadByte();
				}
				public void Write(EndianBinaryWriter er)
				{
					er.Write(MaterialID);
					er.Write(Index);
					er.Write(Unknown);
				}
				public UInt16 MaterialID { get; set; }
				public byte Index { get; set; }
				public byte Unknown { get; set; }
			}

			public override void Render(BRLYT Layout, ref int idx, byte alpha = 255, bool picking = false, BRLAN Animation = null, float Frame = 0, BRLAN.AnimatedPan1 ParentA = null, BRLAN.AnimatedMat1[] Materials = null)
			{
				BRLAN.AnimatedPic1 pp = null;
				if (Animation != null) { pp = Animation.GetWndValue(Frame, this); if (!pp.Visible || Layout.Language(Name)) return; }
				else if (!Visible || Layout.Language(Name)) return;
				Gl.glPushMatrix();
				{
					mat1.MAT1Entry mat = Layout.MAT1.Entries[MaterialID];
					mat1.MAT1Entry[] frame = new mat1.MAT1Entry[WND4Mats.Length];
					for (int i = 0; i < WND4Mats.Length; i++)
					{
						frame[i] = Layout.MAT1.Entries[WND4Mats[i].MaterialID];
					}
					if (!picking)
					{
						if (frame[0].TextureEntries.Length == 0)
						{
							mat.SetAlphaCompareBlendModes();

							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
								Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + o + 1);
								Gl.glEnable(Gl.GL_TEXTURE_2D);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0);
								Gl.glColor4f(1, 1, 1, 1);
								Gl.glBindTexture(Gl.GL_TEXTURE_2D, MaterialID * 16 + 1);
								Gl.glEnable(Gl.GL_TEXTURE_2D);
							}

							Gl.glMatrixMode(Gl.GL_TEXTURE);
							for (int o = 0; o < mat.TextureEntries.Length; o++)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
								Gl.glLoadIdentity();
								mat1.MAT1Entry.MAT1TextureSRTEntry srt;
								if (Animation == null) srt = mat.TextureSRTEntries[(int)mat.TexCoordGenEntries[o].MatrixSource / 3 - 10];
								else srt = Materials[MaterialID].TextureSRTEntries[(int)mat.TexCoordGenEntries[o].MatrixSource / 3 - 10];
								Gl.glTranslatef(0.5f, 0.5f, 0.0f);
								Gl.glRotatef(srt.R, 0.0f, 0.0f, 1.0f);
								Gl.glScalef(srt.Ss, srt.St, 1.0f);
								Gl.glTranslatef(srt.Ts / srt.Ss - 0.5f, srt.Tt / srt.St - 0.5f, 0.0f);
							}
							if (mat.TextureEntries.Length == 0)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0);
								Gl.glLoadIdentity();
							}

							Gl.glMatrixMode(Gl.GL_MODELVIEW);

							if (Animation == null) mat.GlShader.RefreshColors(mat);
							else mat.GlShader.RefreshColors(Materials[MaterialID]);
							mat.GlShader.Enable();
						}
						if (Animation == null)
						{
							Gl.glTranslatef(Tx, Ty, Tz);
							Gl.glRotatef(Rx, 1, 0, 0);
							Gl.glRotatef(Ry, 0, 1, 0);
							Gl.glRotatef(Rz, 0, 0, 1);
							Gl.glScalef(Sx, Sy, 1);
						}
						else
						{
							Gl.glTranslatef(pp.Tx, pp.Ty, pp.Tz);
							Gl.glRotatef(pp.Rx, 1, 0, 0);
							Gl.glRotatef(pp.Ry, 0, 1, 0);
							Gl.glRotatef(pp.Rz, 0, 0, 1);
							Gl.glScalef(pp.Sx, pp.Sy, 1);
						}
						if (frame[0].TextureEntries.Length == 0)
						{
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								float[,] Vertex2 = new float[4, 2];

								if (Animation == null)
								{
									Vertex2[0, 0] = 0;
									Vertex2[1, 0] = Width;
									Vertex2[2, 0] = Width;
									Vertex2[3, 0] = 0;

									Vertex2[0, 1] = 0;
									Vertex2[1, 1] = 0;
									Vertex2[2, 1] = -Height;
									Vertex2[3, 1] = -Height;
								}
								else
								{
									Vertex2[0, 0] = 0;
									Vertex2[1, 0] = pp.Width;
									Vertex2[2, 0] = pp.Width;
									Vertex2[3, 0] = 0;

									Vertex2[0, 1] = 0;
									Vertex2[1, 1] = 0;
									Vertex2[2, 1] = -pp.Height;
									Vertex2[3, 1] = -pp.Height;
								}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								Gl.glBegin(Gl.GL_QUADS);

								for (int o = 0; o < mat.TextureEntries.Length; o++)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopLeftS,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopLeftT
										);
								}
								if (mat.TextureEntries.Length == 0)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);
								}
								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								for (int o = 0; o < mat.TextureEntries.Length; o++)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopRightS,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].TopRightT
										);
								}
								if (mat.TextureEntries.Length == 0)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 1, 0);
								}
								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								for (int o = 0; o < mat.TextureEntries.Length; o++)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomRightS,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomRightT
										);
								}
								if (mat.TextureEntries.Length == 0)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 1, 1);
								}
								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								for (int o = 0; o < mat.TextureEntries.Length; o++)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0 + o,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomLeftS,
										TexCoordSets[(int)mat.TexCoordGenEntries[o].Source - 4].BottomLeftT
										);
								}
								if (mat.TextureEntries.Length == 0)
								{
									Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 1);
								}
								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
						}
						else
						{
							//TopLeft
							frame[0].SetAlphaCompareBlendModes();
							for (int o = 0; o < frame[0].TextureEntries.Length; o++)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
								Gl.glBindTexture(Gl.GL_TEXTURE_2D, WND4Mats[0].MaterialID * 16 + o + 1);
								Gl.glEnable(Gl.GL_TEXTURE_2D);
							}
							if (frame[0].TextureEntries.Length == 0)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0);
								Gl.glColor4f(1, 1, 1, 1);
								Gl.glBindTexture(Gl.GL_TEXTURE_2D, WND4Mats[0].MaterialID * 16 + 1);
								Gl.glEnable(Gl.GL_TEXTURE_2D);
							}

							Gl.glMatrixMode(Gl.GL_TEXTURE);
							for (int o = 0; o < frame[0].TextureEntries.Length; o++)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0 + o);
								Gl.glLoadIdentity();
								mat1.MAT1Entry.MAT1TextureSRTEntry srt;
								if (Animation == null) srt = frame[0].TextureSRTEntries[(int)frame[0].TexCoordGenEntries[o].MatrixSource / 3 - 10];
								else srt = Materials[WND4Mats[0].MaterialID].TextureSRTEntries[(int)frame[0].TexCoordGenEntries[o].MatrixSource / 3 - 10];
								Gl.glTranslatef(0.5f, 0.5f, 0.0f);
								Gl.glRotatef(srt.R, 0.0f, 0.0f, 1.0f);
								Gl.glScalef(srt.Ss, srt.St, 1.0f);
								Gl.glTranslatef(srt.Ts / srt.Ss - 0.5f, srt.Tt / srt.St - 0.5f, 0.0f);
							}
							if (frame[0].TextureEntries.Length == 0)
							{
								Gl.glActiveTexture(Gl.GL_TEXTURE0);
								Gl.glLoadIdentity();
							}

							Gl.glMatrixMode(Gl.GL_MODELVIEW);

							if (Animation == null) frame[0].GlShader.RefreshColors(frame[0]);
							else frame[0].GlShader.RefreshColors(Materials[WND4Mats[0].MaterialID]);
							frame[0].GlShader.Enable();
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								Gl.glTranslatef(0, 0, 0);


								float[,] Vertex2 = new float[4, 2];

								//if (Animation == null)
								//{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = (Animation == null ? Width : pp.Width) / 2f;
								Vertex2[2, 0] = (Animation == null ? Width : pp.Width) / 2f;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -(Animation == null ? Height : pp.Height) / 2f;
								Vertex2[3, 1] = -(Animation == null ? Height : pp.Height) / 2f;
								//}
								//else
								//{
								//	Vertex2[0, 0] = 0;
								//	Vertex2[1, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[2, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[3, 0] = 0;
								//
								//	Vertex2[0, 1] = 0;
								//	Vertex2[1, 1] = 0;
								//	Vertex2[2, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//	Vertex2[3, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}

								/*	if (p.nosrcalpha)
									{
										TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
										TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
										BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
										BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
									}*/
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);

								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), 0);

								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
							//TopRight
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								Gl.glTranslatef((Animation == null ? Width : pp.Width), 0, 0);

								float[,] Vertex2 = new float[4, 2];

								//if (Animation == null)
								//{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = -(Animation == null ? Width : pp.Width) / 2f;
								Vertex2[2, 0] = -(Animation == null ? Width : pp.Width) / 2f;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -(Animation == null ? Height : pp.Height) / 2f;
								Vertex2[3, 1] = -(Animation == null ? Height : pp.Height) / 2f;
								//}
								//else
								//{
								//	Vertex2[0, 0] = 0;
								//	Vertex2[1, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[2, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[3, 0] = 0;
								//
								//	Vertex2[0, 1] = 0;
								//	Vertex2[1, 1] = 0;
								//	Vertex2[2, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//	Vertex2[3, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}

								/*	if (p.nosrcalpha)
									{
										TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
										TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
										BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
										BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
									}*/
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);

								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), 0);

								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
							//BottomRight
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								Gl.glTranslatef((Animation == null ? Width : pp.Width), -(Animation == null ? Height : pp.Height), 0);

								float[,] Vertex2 = new float[4, 2];

								//if (Animation == null)
								//{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = -(Animation == null ? Width : pp.Width) / 2f;
								Vertex2[2, 0] = -(Animation == null ? Width : pp.Width) / 2f;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = (Animation == null ? Height : pp.Height) / 2f;
								Vertex2[3, 1] = (Animation == null ? Height : pp.Height) / 2f;
								//}
								//else
								//{
								//	Vertex2[0, 0] = 0;
								//	Vertex2[1, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[2, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[3, 0] = 0;
								//
								//	Vertex2[0, 1] = 0;
								//	Vertex2[1, 1] = 0;
								//	Vertex2[2, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//	Vertex2[3, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}

								/*	if (p.nosrcalpha)
									{
										TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
										TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
										BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
										BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
									}*/
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);

								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), 0);

								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
							//BottomLeft
							Gl.glPushMatrix();
							{
								if (Animation == null)
									Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);
								else Gl.glTranslatef(-0.5f * pp.Width * (float)OriginX, -0.5f * pp.Height * (-(float)OriginY), 0);

								Gl.glTranslatef(0, -(Animation == null ? Height : pp.Height), 0);

								float[,] Vertex2 = new float[4, 2];

								//if (Animation == null)
								//{
								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = (Animation == null ? Width : pp.Width) / 2f;
								Vertex2[2, 0] = (Animation == null ? Width : pp.Width) / 2f;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = (Animation == null ? Height : pp.Height) / 2f;
								Vertex2[3, 1] = (Animation == null ? Height : pp.Height) / 2f;
								//}
								//else
								//{
								//	Vertex2[0, 0] = 0;
								//	Vertex2[1, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[2, 0] = (pp.Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width);
								//	Vertex2[3, 0] = 0;
								//
								//	Vertex2[0, 1] = 0;
								//	Vertex2[1, 1] = 0;
								//	Vertex2[2, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//	Vertex2[3, 1] = -(pp.Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height - Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height);
								//}
								float[] TL2;
								float[] TR2;
								float[] BL2;
								float[] BR2;
								if (Animation == null)
								{
									TL2 = new float[]
								{
									VertexColorTopLeft.R / 255f,
									VertexColorTopLeft.G / 255f,
									VertexColorTopLeft.B / 255f,
									Util.MixColors(VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									VertexColorTopRight.R / 255f,
									VertexColorTopRight.G / 255f,
									VertexColorTopRight.B / 255f,
									Util.MixColors(VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									VertexColorBottomRight.R / 255f,
									VertexColorBottomRight.G / 255f,
									VertexColorBottomRight.B / 255f,
									Util.MixColors(VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									VertexColorBottomLeft.R / 255f,
									VertexColorBottomLeft.G / 255f,
									VertexColorBottomLeft.B / 255f,
									Util.MixColors(VertexColorBottomLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}
								else
								{
									TL2 = new float[]
								{
									pp.VertexColorTopLeft.R / 255f,
									pp.VertexColorTopLeft.G / 255f,
									pp.VertexColorTopLeft.B / 255f,
									Util.MixColors(pp.VertexColorTopLeft.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									TR2 = new float[]
								{
									pp.VertexColorTopRight.R / 255f,
									pp.VertexColorTopRight.G / 255f,
									pp.VertexColorTopRight.B / 255f,
									Util.MixColors(pp.VertexColorTopRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BR2 = new float[]
								{
									pp.VertexColorBottomRight.R / 255f,
									pp.VertexColorBottomRight.G / 255f,
									pp.VertexColorBottomRight.B / 255f,
									Util.MixColors(pp.VertexColorBottomRight.A, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
									BL2 = new float[]
								{
									pp.VertexColorBottomLeft.R / 255f,
									pp.VertexColorBottomLeft.G / 255f,
									pp.VertexColorBottomLeft.B / 255f,
									Util.MixColors(pp.VertexColorBottomLeft.A,(this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)))
								};
								}

								/*	if (p.nosrcalpha)
									{
										TL2[3] = MixColors(p.Colors.vtxColorTL.A, p.alpha);
										TR2[3] = MixColors(p.Colors.vtxColorTR.A, p.alpha);
										BR2[3] = MixColors(p.Colors.vtxColorBR.A, p.alpha);
										BL2[3] = MixColors(p.Colors.vtxColorBL.A, p.alpha);
									}*/
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, 0);

								Gl.glColor4f(TL2[0], TL2[1], TL2[2], TL2[3]);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), 0);

								Gl.glColor4f(TR2[0], TR2[1], TR2[2], TR2[3]);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, ((Animation == null ? Width : pp.Width) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Width), ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BR2[0], BR2[1], BR2[2], BR2[3]);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glMultiTexCoord2f(Gl.GL_TEXTURE0, 0, ((Animation == null ? Height : pp.Height) / 2f / Layout.TXL1.TPLs[frame[0].TextureEntries[0].TexIndex].GetTextureSize(0).Height));

								Gl.glColor4f(BL2[0], BL2[1], BL2[2], BL2[3]);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
						}
					}
					else
					{
						Gl.glTranslatef(Tx, Ty, Tz);
						Gl.glRotatef(Rx, 1, 0, 0);
						Gl.glRotatef(Ry, 0, 1, 0);
						Gl.glRotatef(Rz, 0, 0, 1);
						Gl.glScalef(Sx, Sy, 1);
						if (Alpha > 127)
						{
							Gl.glPushMatrix();
							{
								Gl.glTranslatef(-0.5f * Width * (float)OriginX, -0.5f * Height * (-(float)OriginY), 0);

								float[,] Vertex2 = new float[4, 2];

								Vertex2[0, 0] = 0;
								Vertex2[1, 0] = Width;
								Vertex2[2, 0] = Width;
								Vertex2[3, 0] = 0;

								Vertex2[0, 1] = 0;
								Vertex2[1, 1] = 0;
								Vertex2[2, 1] = -Height;
								Vertex2[3, 1] = -Height;

								Color c = Color.FromArgb(idx + 1);
								Gl.glColor4f(c.R / 255f, c.G / 255f, c.B / 255f, 1);
								Gl.glBegin(Gl.GL_QUADS);
								Gl.glVertex3f(Vertex2[0, 0], Vertex2[0, 1], 0);
								Gl.glVertex3f(Vertex2[1, 0], Vertex2[1, 1], 0);
								Gl.glVertex3f(Vertex2[2, 0], Vertex2[2, 1], 0);
								Gl.glVertex3f(Vertex2[3, 0], Vertex2[3, 1], 0);
								Gl.glEnd();
							}
							Gl.glPopMatrix();
						}
						idx++;
					}
					foreach (pan1 p in Children)
					{
						p.Render(Layout, ref idx, (this.InfluenceAlpha ? (byte)(((float)(Animation == null ? Alpha : pp.Alpha) * (float)alpha) / 255f) : (Animation == null ? Alpha : pp.Alpha)), picking, Animation, Frame, pp, Materials);
					}
				}
				Gl.glPopMatrix();
			}
			public override void Write(EndianBinaryWriter er, ref int nr)
			{
				er.Write("wnd1", Encoding.ASCII, false);
				er.Write((UInt32)(76 + 48 + TexCoordSets.Length * 32 + WND4s.Length * 8));
				byte flag = Flag;
				flag &= 0xF8;
				flag |= (byte)(Visible ? 1 : 0);
				flag |= (byte)((InfluenceAlpha ? 1 : 0) << 1);
				flag |= (byte)((WidescreenAffected ? 1 : 0) << 2);
				er.Write((byte)Flag);
				er.Write((byte)(((int)OriginX) + ((int)OriginY * 3)));
				er.Write(Alpha);
				er.Write(Unknown1);
				if (Name.Length > 16) er.Write(Name.Remove(16), Encoding.ASCII, false);
				else er.Write(Name.PadRight(16, '\0'), Encoding.ASCII, false);
				er.Write(Unknown2, 0, 8);
				er.Write(Tx);
				er.Write(Ty);
				er.Write(Tz);
				er.Write(Rx);
				er.Write(Ry);
				er.Write(Rz);
				er.Write(Sx);
				er.Write(Sy);
				er.Write(Width);
				er.Write(Height);

				er.Write(Coordinate1);
				er.Write(Coordinate2);
				er.Write(Coordinate3);
				er.Write(Coordinate4);
				er.Write((byte)WND4s.Length);
				er.Write(Unknown3, 0, 3);
				er.Write(Offset1);
				er.Write(Offset2);
				er.Write(VertexColorTopLeft);
				er.Write(VertexColorTopRight);
				er.Write(VertexColorBottomLeft);
				er.Write(VertexColorBottomRight);
				er.Write(MaterialID);
				er.Write((byte)TexCoordSets.Length);
				er.Write(Unknown4);
				for (int i = 0; i < TexCoordSets.Length; i++)
				{
					TexCoordSets[i].Write(er);
				}
				for (int i = 0; i < WND4s.Length; i++)
				{
					WND4s[i].Write(er);
				}
				for (int i = 0; i < WND4Mats.Length; i++)
				{
					WND4Mats[i].Write(er);
				}
				nr++;
				if (Children.Count != 0)
				{
					er.Write("pas1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
					foreach (pan1 p in Children)
					{
						p.Write(er, ref nr);
					}
					er.Write("pae1", Encoding.ASCII, false);
					er.Write((UInt32)8);
					nr++;
				}
			}
			public override void GetTreeNodes(System.Windows.Forms.TreeNodeCollection t)
			{
				System.Windows.Forms.TreeNode n = t.Add(Name, Name, 10);
				n.SelectedImageIndex = 10;
				n.Tag = "wnd1";
				foreach (pan1 p in Children)
				{
					p.GetTreeNodes(n.Nodes);
				}
			}
			public override pan1 GetByID(int idx, ref int idx2)
			{
				if (!Visible) return null;
				if (idx == idx2) return this;
				idx2++;
				foreach (pan1 p in Children)
				{
					pan1 pp = p.GetByID(idx, ref idx2);
					if (pp != null) return pp;
				}
				return null;
			}
		}
	}
}
