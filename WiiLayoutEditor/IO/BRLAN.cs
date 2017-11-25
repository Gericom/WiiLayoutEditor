using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using Tao.OpenGl;

namespace WiiLayoutEditor.IO
{
	public class BRLAN
	{
		public const String Signature = "RLAN";
		public BRLAN(byte[] file, Dictionary<String, byte[]> Files)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			bool OK;
			Header = new BRLANHeader(er, Signature, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 1"); goto end; }
			for (int i = 0; i < Header.NrEntries; i++)
			{
				string s;
				switch (s = er.ReadString(Encoding.ASCII, 4))
				{
					case "pai1":
						PAI1 = new pai1(er, Files);
						break;
					default:
						er.BaseStream.Position += er.ReadUInt32() - 4;
						break;
				}
			}
		end:
			er.Close();
		}
		public BRLANHeader Header;
		public class BRLANHeader
		{
			public BRLANHeader(EndianBinaryReader er, String Signature, out bool OK)
			{
				Type = er.ReadString(ASCIIEncoding.ASCII, 4);
				if (Type != Signature) { OK = false; return; }
				Magic = er.ReadUInt32();
				FileSize = er.ReadUInt32();
				Version = er.ReadUInt16();
				NrEntries = er.ReadUInt16();
				OK = true;
			}
			public String Type;
			public UInt32 Magic;
			public UInt32 FileSize;
			public UInt16 Version;
			public UInt16 NrEntries;
		}
		public class pat1
		{

		}
		public pai1 PAI1;
		public class pai1
		{
			public pai1(EndianBinaryReader er, Dictionary<String, byte[]> Files)
			{
				Length = er.ReadUInt32();
				NrFrames = er.ReadUInt16();
				Loop = er.ReadByte() == 1;
				Unknown = er.ReadByte();
				NrFiles = er.ReadUInt16();
				NrAnim = er.ReadUInt16();
				AnimOffsetsOffset = er.ReadUInt32();

				FileNameOffsets = er.ReadUInt32s(NrFiles);
				FileNames = new string[NrFiles];
				TPLs = new libWiiSharp.TPL[NrFiles];
				for (int i = 0; i < NrFiles; i++)
				{
					FileNames[i] = er.ReadStringNT(Encoding.ASCII);
					TPLs[i] = libWiiSharp.TPL.Load(new MemoryStream(Files[FileNames[i]]));
				}
				while ((er.BaseStream.Position % 4) != 0) er.ReadByte();
				AnimOffsets = er.ReadUInt32s(NrAnim);

				Animations = new PAI1Animation[NrAnim];
				for (int i = 0; i < NrAnim; i++)
				{
					Animations[i] = new PAI1Animation(er);
				}
			}
			public UInt32 Length;
			public UInt16 NrFrames;
			public bool Loop;//?
			public byte Unknown;
			public UInt16 NrFiles;
			public UInt16 NrAnim;
			public UInt32 AnimOffsetsOffset;

			public UInt32[] FileNameOffsets;
			public String[] FileNames;
			public libWiiSharp.TPL[] TPLs;

			public UInt32[] AnimOffsets;

			public PAI1Animation[] Animations;

			public void BindTextures(BRLYT Layout)
			{
				/*for (int j = 0; j < NrFiles; j++)
				{
					libWiiSharp.TPL i = TPLs[j];
					byte[] data = i.ExtractTextureBitmapData(0);
					Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, (Layout.MAT1.NrEntries+1)*16);
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
				}*/
			}

			public class PAI1Animation
			{
				public PAI1Animation(EndianBinaryReader er)
				{
					Name = er.ReadString(ASCIIEncoding.ASCII, 20).Replace("\0", "");
					NrTags = er.ReadByte();
					IsMaterial = er.ReadByte() == 1;
					Unknown = er.ReadUInt16();
					TagOffsets = er.ReadUInt32s(NrTags);

					Tags = new PAI1AnimationTag[NrTags];
					for (int i = 0; i < NrTags; i++)
					{
						Tags[i] = new PAI1AnimationTag(er);
					}
				}
				public String Name;
				public byte NrTags;
				public bool IsMaterial;
				public UInt16 Unknown;

				public UInt32[] TagOffsets;

				public PAI1AnimationTag[] Tags;
				public class PAI1AnimationTag
				{
					public PAI1AnimationTag(EndianBinaryReader er)
					{
						Tag = er.ReadString(Encoding.ASCII, 4);
						NrEntries = er.ReadByte();
						Unknown = er.ReadBytes(3);
						EntryOffsets = er.ReadUInt32s(NrEntries);

						Entries = new PAI1AnimationTagEntry[NrEntries];
						for (int i = 0; i < NrEntries; i++)
						{
							Entries[i] = new PAI1AnimationTagEntry(er);
						}
					}
					public String Tag;
					public byte NrEntries;
					public byte[] Unknown;//Padding?

					public UInt32[] EntryOffsets;

					public PAI1AnimationTagEntry[] Entries;
					public class PAI1AnimationTagEntry
					{
						public PAI1AnimationTagEntry(EndianBinaryReader er)
						{
							Index = er.ReadByte();
							Target = er.ReadByte();
							DataType = er.ReadByte();
							Unknown1 = er.ReadByte();
							NrKey = er.ReadUInt16();
							Unknown2 = er.ReadUInt16();
							KeyOffset = er.ReadUInt32();

							Keys = new PAI1AnimationTagEntryKey[NrKey];
							for (int i = 0; i < NrKey; i++)
							{
								if (DataType == 1) Keys[i] = new PAI1AnimationTagEntryStepKey(er);
								else Keys[i] = new PAI1AnimationTagEntryHermiteKey(er);
							}
						}
						public byte Index;
						public byte Target;
						public byte DataType;
						public byte Unknown1;
						public UInt16 NrKey;
						public UInt16 Unknown2;

						public UInt32 KeyOffset;

						public PAI1AnimationTagEntryKey[] Keys;

						public class PAI1AnimationTagEntryKey
						{
							public Single FrameNr;
						}
						public class PAI1AnimationTagEntryStepKey : PAI1AnimationTagEntryKey
						{
							public PAI1AnimationTagEntryStepKey(EndianBinaryReader er)
							{
								FrameNr = er.ReadSingle();
								Value = er.ReadUInt16();
								Padding = er.ReadUInt16();
							}
							public UInt16 Value;
							public UInt16 Padding;
							public static UInt16 GetValue(float Frame, PAI1AnimationTagEntry e)
							{
								if (e.NrKey == 1)
								{
									return ((PAI1AnimationTagEntryStepKey)e.Keys[0]).Value;
								}
								else
								{
									if(e.Keys[0].FrameNr >= Frame) return ((PAI1AnimationTagEntryStepKey)e.Keys[0]).Value;
									if (e.Keys.Last().FrameNr <= Frame) return ((PAI1AnimationTagEntryStepKey)e.Keys.Last()).Value;
									int nr = 0;
									for (int i = 0; i < e.NrKey; i++)
									{
										if (e.Keys[i].FrameNr < Frame) nr = i;
									}
									return ((PAI1AnimationTagEntryStepKey)e.Keys[nr]).Value;
									//int frame2 = 0;
									//while (e.Keys[frame2].FrameNr < Frame && e.NrKey - 1 > frame2) { frame2++; }
									//return ((PAI1AnimationTagEntryStepKey)e.Keys[frame2]).Value;
								}
							}
						}
						public class PAI1AnimationTagEntryHermiteKey : PAI1AnimationTagEntryKey
						{
							public PAI1AnimationTagEntryHermiteKey(EndianBinaryReader er)
							{
								FrameNr = er.ReadSingle();
								Value = er.ReadSingle();
								Blend = er.ReadSingle();
							}
							public Single Value;
							public Single Blend;
							public static float Interpolate(float Frame, PAI1AnimationTagEntry e)
							{
								PAI1AnimationTagEntryHermiteKey prev;
								PAI1AnimationTagEntryHermiteKey next;
								if (e.NrKey != 1)
								{
									int k = 1;
									while (e.Keys[k].FrameNr < Frame && e.NrKey - 1 > k) { k++; }
									prev = (PAI1AnimationTagEntryHermiteKey)e.Keys[k - 1];
									next = (PAI1AnimationTagEntryHermiteKey)e.Keys[k];
								}
								else
								{
									prev = (PAI1AnimationTagEntryHermiteKey)e.Keys[0];
									next = (PAI1AnimationTagEntryHermiteKey)e.Keys[0];
								}

								float nf = next.FrameNr - prev.FrameNr;
								if (Math.Abs(nf) < 0.01)
								{
									// same frame numbers, just return the first's value
									return prev.Value;
								}
								Frame = Clamp(Frame, prev.FrameNr, next.FrameNr);

								float t = (Frame - prev.FrameNr) / nf;

								return (float)(
										prev.Blend * nf * (t + Math.Pow(t, 3) - 2 * Math.Pow(t, 2)) +
										next.Blend * nf * (Math.Pow(t, 3) - Math.Pow(t, 2)) +
										prev.Value * (1 + (2 * Math.Pow(t, 3) - 3 * Math.Pow(t, 2))) +
										next.Value * (-2 * Math.Pow(t, 3) + 3 * Math.Pow(t, 2)));
							}
							private static float Clamp(float value, float min, float max)
							{
								return (value < min) ? min : (value < max) ? value : max;
							}

						}
						public float GetValue(float Frame)
						{
							if (DataType == 1) return PAI1AnimationTagEntryStepKey.GetValue(Frame, this);
							else return PAI1AnimationTagEntryHermiteKey.Interpolate(Frame, this);
						}
					}
				}
			}
		}
		public class AnimatedPan1
		{
			public bool Visible { get; set; }
			public byte Alpha { get; set; }

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
		}
		public class AnimatedPic1 : AnimatedPan1
		{
			[Category("Vertex Colors"), DisplayName("Top Left")]
			public Color VertexColorTopLeft { get; set; }
			[Category("Vertex Colors"), DisplayName("Top Right")]
			public Color VertexColorTopRight { get; set; }
			[Category("Vertex Colors"), DisplayName("Bottom Left")]
			public Color VertexColorBottomLeft { get; set; }
			[Category("Vertex Colors"), DisplayName("Bottom Right")]
			public Color VertexColorBottomRight { get; set; }
		}
		public class AnimatedMat1
		{
			public BRLYT.mat1.MAT1Entry.MAT1TextureSRTEntry[] TextureSRTEntries;
			public BRLYT.mat1.MAT1Entry.MAT1TextureSRTEntry[] IndirectTextureSRTEntries;
			public Color ForeColor { get; set; }
			public Color BackColor { get; set; }
			public Color ColorReg3 { get; set; }
			public Color TevColor1 { get; set; }
			public Color TevColor2 { get; set; }
			public Color TevColor3 { get; set; }
			public Color TevColor4 { get; set; }
			public Color MatColor { get; set; }
		}
		public AnimatedMat1 GetMatValue(float Frame, BRLYT.mat1.MAT1Entry Pane)
		{
			pai1.PAI1Animation aa = null;
			foreach (pai1.PAI1Animation a in PAI1.Animations)
			{
				if (a.Name == Pane.Name && a.IsMaterial)
				{
					aa = a;
					break;
				}
			}
			AnimatedMat1 p = new AnimatedMat1();
			p.TextureSRTEntries = new BRLYT.mat1.MAT1Entry.MAT1TextureSRTEntry[Pane.TextureSRTEntries.Length];
			for (int i = 0; i < p.TextureSRTEntries.Length; i++)
			{
				p.TextureSRTEntries[i] = new BRLYT.mat1.MAT1Entry.MAT1TextureSRTEntry();
			}

			bool[] TextureSRTEntriesgot = new bool[5 * Pane.TextureSRTEntries.Length];
			bool[] got = new bool[0x20];
			if (aa != null)
			{
				foreach (pai1.PAI1Animation.PAI1AnimationTag t in aa.Tags)
				{
					foreach (pai1.PAI1Animation.PAI1AnimationTag.PAI1AnimationTagEntry e in t.Entries)
					{
						float value = e.GetValue(Frame);
						switch (t.Tag)
						{
							case "RLTS":
								if (e.Target < 5&&e.Index < Pane.TextureSRTEntries.Length)
								{
									TextureSRTEntriesgot[5 * e.Index + e.Target] = true;
									switch (e.Target)
									{
										case 0: p.TextureSRTEntries[e.Index].Ts = value; break;
										case 1: p.TextureSRTEntries[e.Index].Tt = value; break;
										case 2: p.TextureSRTEntries[e.Index].R = value; break;
										case 3: p.TextureSRTEntries[e.Index].Ss = value; break;
										case 4: p.TextureSRTEntries[e.Index].St = value; break;
									}
								}
								break;
							case "RLMC":
								got[e.Target] = true;
								switch (e.Target)
								{
									case 0x0:
										p.MatColor = Color.FromArgb(p.MatColor.A, (byte)value, p.MatColor.G, p.MatColor.B);
										break;
									case 0x1:
										p.MatColor = Color.FromArgb(p.MatColor.A, p.MatColor.R, (byte)value, p.MatColor.B);
										break;
									case 0x2:
										p.MatColor = Color.FromArgb(p.MatColor.A, p.MatColor.R, p.MatColor.G, (byte)value);
										break;
									case 0x3:
										p.MatColor = Color.FromArgb((byte)value, p.MatColor.R, p.MatColor.G, p.MatColor.B);
										break;
									case 0x4:
										p.ForeColor = Color.FromArgb(p.ForeColor.A, (byte)value, p.ForeColor.G, p.ForeColor.B);
										break;
									case 0x5:
										p.ForeColor = Color.FromArgb(p.ForeColor.A, p.ForeColor.R, (byte)value, p.ForeColor.B);
										break;
									case 0x6:
										p.ForeColor = Color.FromArgb(p.ForeColor.A, p.ForeColor.R, p.ForeColor.G, (byte)value);
										break;
									case 0x7:
										p.ForeColor = Color.FromArgb((byte)value, p.ForeColor.R, p.ForeColor.G, p.ForeColor.B);
										break;
									case 0x8:
										p.BackColor = Color.FromArgb(p.BackColor.A, (byte)value, p.BackColor.G, p.BackColor.B);
										break;
									case 0x9:
										p.BackColor = Color.FromArgb(p.BackColor.A, p.BackColor.R, (byte)value, p.BackColor.B);
										break;
									case 0xA:
										p.BackColor = Color.FromArgb(p.BackColor.A, p.BackColor.R, p.BackColor.G, (byte)value);
										break;
									case 0xB:
										p.BackColor = Color.FromArgb((byte)value, p.BackColor.R, p.BackColor.G, p.BackColor.B);
										break;
									case 0xC:
										p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, (byte)value, p.ColorReg3.G, p.ColorReg3.B);
										break;
									case 0xD:
										p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, p.ColorReg3.R, (byte)value, p.ColorReg3.B);
										break;
									case 0xE:
										p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, p.ColorReg3.R, p.ColorReg3.G, (byte)value);
										break;
									case 0xF:
										p.ColorReg3 = Color.FromArgb((byte)value, p.ColorReg3.R, p.ColorReg3.G, p.ColorReg3.B);
										break;
									case 0x10:
										p.TevColor1 = Color.FromArgb(p.TevColor1.A, (byte)value, p.TevColor1.G, p.TevColor1.B);
										break;
									case 0x11:
										p.TevColor1 = Color.FromArgb(p.TevColor1.A, p.TevColor1.R, (byte)value, p.TevColor1.B);
										break;
									case 0x12:
										p.TevColor1 = Color.FromArgb(p.TevColor1.A, p.TevColor1.R, p.TevColor1.G, (byte)value);
										break;
									case 0x13:
										p.TevColor1 = Color.FromArgb((byte)value, p.TevColor1.R, p.TevColor1.G, p.TevColor1.B);
										break;
									case 0x14:
										p.TevColor2 = Color.FromArgb(p.TevColor2.A, (byte)value, p.TevColor2.G, p.TevColor2.B);
										break;
									case 0x15:
										p.TevColor2 = Color.FromArgb(p.TevColor2.A, p.TevColor2.R, (byte)value, p.TevColor2.B);
										break;
									case 0x16:
										p.TevColor2 = Color.FromArgb(p.TevColor2.A, p.TevColor2.R, p.TevColor2.G, (byte)value);
										break;
									case 0x17:
										p.TevColor2 = Color.FromArgb((byte)value, p.TevColor2.R, p.TevColor2.G, p.TevColor2.B);
										break;
									case 0x18:
										p.TevColor3 = Color.FromArgb(p.TevColor3.A, (byte)value, p.TevColor3.G, p.TevColor3.B);
										break;
									case 0x19:
										p.TevColor3 = Color.FromArgb(p.TevColor3.A, p.TevColor3.R, (byte)value, p.TevColor3.B);
										break;
									case 0x1A:
										p.TevColor3 = Color.FromArgb(p.TevColor3.A, p.TevColor3.R, p.TevColor3.G, (byte)value);
										break;
									case 0x1B:
										p.TevColor3 = Color.FromArgb((byte)value, p.TevColor3.R, p.TevColor3.G, p.TevColor3.B);
										break;
									case 0x1C:
										p.TevColor4 = Color.FromArgb(p.TevColor4.A, (byte)value, p.TevColor4.G, p.TevColor4.B);
										break;
									case 0x1D:
										p.TevColor4 = Color.FromArgb(p.TevColor4.A, p.TevColor4.R, (byte)value, p.TevColor4.B);
										break;
									case 0x1E:
										p.TevColor4 = Color.FromArgb(p.TevColor4.A, p.TevColor4.R, p.TevColor4.G, (byte)value);
										break;
									case 0x1F:
										p.TevColor4 = Color.FromArgb((byte)value, p.TevColor4.R, p.TevColor4.G, p.TevColor4.B);
										break;
								}
								break;
							case "RLTP":
								break;
						}
					}
				}
			}
			for (int i = 0; i < p.TextureSRTEntries.Length; i++)
			{
				if (!TextureSRTEntriesgot[i * 5 + 0]) p.TextureSRTEntries[i].Ts = Pane.TextureSRTEntries[i].Ts;
				if (!TextureSRTEntriesgot[i * 5 + 1]) p.TextureSRTEntries[i].Tt = Pane.TextureSRTEntries[i].Tt;
				if (!TextureSRTEntriesgot[i * 5 + 2]) p.TextureSRTEntries[i].R = Pane.TextureSRTEntries[i].R;
				if (!TextureSRTEntriesgot[i * 5 + 3]) p.TextureSRTEntries[i].Ss = Pane.TextureSRTEntries[i].Ss;
				if (!TextureSRTEntriesgot[i * 5 + 4]) p.TextureSRTEntries[i].St = Pane.TextureSRTEntries[i].St;
			}
			if (!got[0]) p.MatColor = Color.FromArgb(p.MatColor.A, Pane.MatColor.R, p.MatColor.G, p.MatColor.B);
			if (!got[1]) p.MatColor = Color.FromArgb(p.MatColor.A, p.MatColor.R, Pane.MatColor.G, p.MatColor.B);
			if (!got[2]) p.MatColor = Color.FromArgb(p.MatColor.A, p.MatColor.R, p.MatColor.G, Pane.MatColor.B);
			if (!got[3]) p.MatColor = Color.FromArgb(Pane.MatColor.A, p.MatColor.R, p.MatColor.G, p.MatColor.B);
			if (!got[4]) p.ForeColor = Color.FromArgb(p.ForeColor.A, Pane.ForeColor.R, p.ForeColor.G, p.ForeColor.B);
			if (!got[5]) p.ForeColor = Color.FromArgb(p.ForeColor.A, p.ForeColor.R, Pane.ForeColor.G, p.ForeColor.B);
			if (!got[6]) p.ForeColor = Color.FromArgb(p.ForeColor.A, p.ForeColor.R, p.ForeColor.G, Pane.ForeColor.B);
			if (!got[7]) p.ForeColor = Color.FromArgb(Pane.ForeColor.A, p.ForeColor.R, p.ForeColor.G, p.ForeColor.B);
			if (!got[8]) p.BackColor = Color.FromArgb(p.BackColor.A, Pane.BackColor.R, p.BackColor.G, p.BackColor.B);
			if (!got[9]) p.BackColor = Color.FromArgb(p.BackColor.A, p.BackColor.R, Pane.BackColor.G, p.BackColor.B);
			if (!got[10]) p.BackColor = Color.FromArgb(p.BackColor.A, p.BackColor.R, p.BackColor.G, Pane.BackColor.B);
			if (!got[11]) p.BackColor = Color.FromArgb(Pane.BackColor.A, p.BackColor.R, p.BackColor.G, p.BackColor.B);
			if (!got[12]) p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, Pane.ColorReg3.R, p.ColorReg3.G, p.ColorReg3.B);
			if (!got[13]) p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, p.ColorReg3.R, Pane.ColorReg3.G, p.ColorReg3.B);
			if (!got[14]) p.ColorReg3 = Color.FromArgb(p.ColorReg3.A, p.ColorReg3.R, p.ColorReg3.G, Pane.ColorReg3.B);
			if (!got[15]) p.ColorReg3 = Color.FromArgb(Pane.ColorReg3.A, p.ColorReg3.R, p.ColorReg3.G, p.ColorReg3.B);

			if (!got[16]) p.TevColor1 = Color.FromArgb(p.TevColor1.A, Pane.TevColor1.R, p.TevColor1.G, p.TevColor1.B);
			if (!got[17]) p.TevColor1 = Color.FromArgb(p.TevColor1.A, p.TevColor1.R, Pane.TevColor1.G, p.TevColor1.B);
			if (!got[18]) p.TevColor1 = Color.FromArgb(p.TevColor1.A, p.TevColor1.R, p.TevColor1.G, Pane.TevColor1.B);
			if (!got[19]) p.TevColor1 = Color.FromArgb(Pane.TevColor1.A, p.TevColor1.R, p.TevColor1.G, p.TevColor1.B);

			if (!got[20]) p.TevColor2 = Color.FromArgb(p.TevColor2.A, Pane.TevColor2.R, p.TevColor2.G, p.TevColor2.B);
			if (!got[21]) p.TevColor2 = Color.FromArgb(p.TevColor2.A, p.TevColor2.R, Pane.TevColor2.G, p.TevColor2.B);
			if (!got[22]) p.TevColor2 = Color.FromArgb(p.TevColor2.A, p.TevColor2.R, p.TevColor2.G, Pane.TevColor2.B);
			if (!got[23]) p.TevColor2 = Color.FromArgb(Pane.TevColor2.A, p.TevColor2.R, p.TevColor2.G, p.TevColor2.B);

			if (!got[24]) p.TevColor3 = Color.FromArgb(p.TevColor3.A, Pane.TevColor3.R, p.TevColor3.G, p.TevColor3.B);
			if (!got[25]) p.TevColor3 = Color.FromArgb(p.TevColor3.A, p.TevColor3.R, Pane.TevColor3.G, p.TevColor3.B);
			if (!got[26]) p.TevColor3 = Color.FromArgb(p.TevColor3.A, p.TevColor3.R, p.TevColor3.G, Pane.TevColor3.B);
			if (!got[27]) p.TevColor3 = Color.FromArgb(Pane.TevColor3.A, p.TevColor3.R, p.TevColor3.G, p.TevColor3.B);

			if (!got[28]) p.TevColor4 = Color.FromArgb(p.TevColor4.A, Pane.TevColor4.R, p.TevColor4.G, p.TevColor4.B);
			if (!got[29]) p.TevColor4 = Color.FromArgb(p.TevColor4.A, p.TevColor4.R, Pane.TevColor4.G, p.TevColor4.B);
			if (!got[30]) p.TevColor4 = Color.FromArgb(p.TevColor4.A, p.TevColor4.R, p.TevColor4.G, Pane.TevColor4.B);
			if (!got[31]) p.TevColor4 = Color.FromArgb(Pane.TevColor4.A, p.TevColor4.R, p.TevColor4.G, p.TevColor4.B);
			return p;
		}
		public AnimatedPan1 GetPaneValue(float Frame, BRLYT.pan1 Pane)
		{
			pai1.PAI1Animation aa = null;
			foreach (pai1.PAI1Animation a in PAI1.Animations)
			{
				if (a.Name == Pane.Name)
				{
					aa = a;
					break;
				}
			}
			AnimatedPan1 p = new AnimatedPan1();
			bool[] got = new bool[12];
			if (aa != null)
			{
				foreach (pai1.PAI1Animation.PAI1AnimationTag t in aa.Tags)
				{
					foreach (pai1.PAI1Animation.PAI1AnimationTag.PAI1AnimationTagEntry e in t.Entries)
					{
						float value = e.GetValue(Frame);
						switch (t.Tag)
						{
							case "RLPA":
								if (e.Target < 10)
								{
									got[e.Target] = true;
									switch (e.Target)
									{
										case 0: p.Tx = value; break;
										case 1: p.Ty = value; break;
										case 2: p.Tz = value; break;
										case 3: p.Rx = value; break;
										case 4: p.Ry = value; break;
										case 5: p.Rz = value; break;
										case 6: p.Sx = value; break;
										case 7: p.Sy = value; break;
										case 8: p.Width = value; break;
										case 9: p.Height = value; break;
									}
								}
								break;
							case "RLVI":
								got[10] = true;
								p.Visible = value == 1;
								break;
							case "RLVC":
								if (e.Target == 0x10)
								{
									got[11] = true;
									p.Alpha = (byte)value;
								}
								break;
						}
					}
				}
			}
			if (!got[0]) p.Tx = Pane.Tx;
			if (!got[1]) p.Ty = Pane.Ty;
			if (!got[2]) p.Tz = Pane.Tz;
			if (!got[3]) p.Rx = Pane.Rx;
			if (!got[4]) p.Ry = Pane.Ry;
			if (!got[5]) p.Rz = Pane.Rz;
			if (!got[6]) p.Sx = Pane.Sx;
			if (!got[7]) p.Sy = Pane.Sy;
			if (!got[8]) p.Width = Pane.Width;
			if (!got[9]) p.Height = Pane.Height;
			if (!got[10]) p.Visible = Pane.Visible;
			if (!got[11]) p.Alpha = Pane.Alpha;
			return p;
		}
		public AnimatedPic1 GetPicValue(float Frame, BRLYT.pic1 Pane)
		{
			pai1.PAI1Animation aa = null;
			foreach (pai1.PAI1Animation a in PAI1.Animations)
			{
				if (a.Name == Pane.Name)
				{
					aa = a;
					break;
				}
			}
			AnimatedPic1 p = new AnimatedPic1();
			p.VertexColorBottomLeft = new Color();
			p.VertexColorBottomRight = new Color();
			p.VertexColorTopLeft = new Color();
			p.VertexColorTopRight = new Color();
			bool[] got = new bool[28];
			if (aa != null)
			{
				foreach (pai1.PAI1Animation.PAI1AnimationTag t in aa.Tags)
				{
					foreach (pai1.PAI1Animation.PAI1AnimationTag.PAI1AnimationTagEntry e in t.Entries)
					{
						float value = e.GetValue(Frame);
						switch (t.Tag)
						{
							case "RLPA":
								if (e.Target < 10)
								{
									got[e.Target] = true;
									switch (e.Target)
									{
										case 0: p.Tx = value; break;
										case 1: p.Ty = value; break;
										case 2: p.Tz = value; break;
										case 3: p.Rx = value; break;
										case 4: p.Ry = value; break;
										case 5: p.Rz = value; break;
										case 6: p.Sx = value; break;
										case 7: p.Sy = value; break;
										case 8: p.Width = value; break;
										case 9: p.Height = value; break;
									}
								}
								break;
							case "RLVI":
								got[10] = true;
								p.Visible =value == 1;
								break;
							case "RLVC":
								got[e.Target + 11] = true;
								switch (e.Target)
								{
									case 0x0:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, (byte)value, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
										break;
									case 0x1:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, (byte)value, p.VertexColorTopLeft.B);
										break;
									case 0x2:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, (byte)value);
										break;
									case 0x3:
										p.VertexColorTopLeft = Color.FromArgb((byte)value, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
										break;
									case 0x4:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, (byte)value, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
										break;
									case 0x5:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, (byte)value, p.VertexColorTopRight.B);
										break;
									case 0x6:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, (byte)value);
										break;
									case 0x7:
										p.VertexColorTopRight = Color.FromArgb((byte)value, p.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
										break;
									case 0x8:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, (byte)value, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
										break;
									case 0x9:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, (byte)value, p.VertexColorBottomLeft.B);
										break;
									case 0xA:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, (byte)value);
										break;
									case 0xB:
										p.VertexColorBottomLeft = Color.FromArgb((byte)value, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
										break;
									case 0xC:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, (byte)value, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
										break;
									case 0xD:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, (byte)value, p.VertexColorBottomRight.B);
										break;
									case 0xE:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, (byte)value);
										break;
									case 0xF:
										p.VertexColorBottomRight = Color.FromArgb((byte)value, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
										break;
									case 0x10:
										p.Alpha = (byte)value;
										break;
								}
								break;
						}
					}
				}
			}
			if (!got[0]) p.Tx = Pane.Tx;
			if (!got[1]) p.Ty = Pane.Ty;
			if (!got[2]) p.Tz = Pane.Tz;
			if (!got[3]) p.Rx = Pane.Rx;
			if (!got[4]) p.Ry = Pane.Ry;
			if (!got[5]) p.Rz = Pane.Rz;
			if (!got[6]) p.Sx = Pane.Sx;
			if (!got[7]) p.Sy = Pane.Sy;
			if (!got[8]) p.Width = Pane.Width;
			if (!got[9]) p.Height = Pane.Height;
			if (!got[10]) p.Visible = Pane.Visible;
			if (!got[11]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, Pane.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[12]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, Pane.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[13]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, Pane.VertexColorTopLeft.B);
			if (!got[14]) p.VertexColorTopLeft = Color.FromArgb(Pane.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[15]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, Pane.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[16]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, Pane.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[17]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, Pane.VertexColorTopRight.B);
			if (!got[18]) p.VertexColorTopRight = Color.FromArgb(Pane.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[19]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, Pane.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[20]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, Pane.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[21]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, Pane.VertexColorBottomLeft.B);
			if (!got[22]) p.VertexColorBottomLeft = Color.FromArgb(Pane.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[23]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, Pane.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[24]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, Pane.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[25]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, Pane.VertexColorBottomRight.B);
			if (!got[26]) p.VertexColorBottomRight = Color.FromArgb(Pane.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[27]) p.Alpha = Pane.Alpha;
			return p;
		}
		public AnimatedPic1 GetWndValue(float Frame, BRLYT.wnd1 Pane)
		{
			pai1.PAI1Animation aa = null;
			foreach (pai1.PAI1Animation a in PAI1.Animations)
			{
				if (a.Name == Pane.Name)
				{
					aa = a;
					break;
				}
			}
			AnimatedPic1 p = new AnimatedPic1();
			p.VertexColorBottomLeft = new Color();
			p.VertexColorBottomRight = new Color();
			p.VertexColorTopLeft = new Color();
			p.VertexColorTopRight = new Color();
			bool[] got = new bool[28];
			if (aa != null)
			{
				foreach (pai1.PAI1Animation.PAI1AnimationTag t in aa.Tags)
				{
					foreach (pai1.PAI1Animation.PAI1AnimationTag.PAI1AnimationTagEntry e in t.Entries)
					{
						float value = e.GetValue(Frame);
						switch (t.Tag)
						{
							case "RLPA":
								if (e.Target < 10)
								{
									got[e.Target] = true;
									switch (e.Target)
									{
										case 0: p.Tx = value; break;
										case 1: p.Ty = value; break;
										case 2: p.Tz = value; break;
										case 3: p.Rx = value; break;
										case 4: p.Ry = value; break;
										case 5: p.Rz = value; break;
										case 6: p.Sx = value; break;
										case 7: p.Sy = value; break;
										case 8: p.Width = value; break;
										case 9: p.Height = value; break;
									}
								}
								break;
							case "RLVI":
								got[10] = true;
								p.Visible = value == 1;
								break;
							case "RLVC":
								got[e.Target + 11] = true;
								switch (e.Target)
								{
									case 0x0:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, (byte)value, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
										break;
									case 0x1:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, (byte)value, p.VertexColorTopLeft.B);
										break;
									case 0x2:
										p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, (byte)value);
										break;
									case 0x3:
										p.VertexColorTopLeft = Color.FromArgb((byte)value, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
										break;
									case 0x4:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, (byte)value, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
										break;
									case 0x5:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, (byte)value, p.VertexColorTopRight.B);
										break;
									case 0x6:
										p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, (byte)value);
										break;
									case 0x7:
										p.VertexColorTopRight = Color.FromArgb((byte)value, p.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
										break;
									case 0x8:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, (byte)value, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
										break;
									case 0x9:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, (byte)value, p.VertexColorBottomLeft.B);
										break;
									case 0xA:
										p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, (byte)value);
										break;
									case 0xB:
										p.VertexColorBottomLeft = Color.FromArgb((byte)value, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
										break;
									case 0xC:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, (byte)value, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
										break;
									case 0xD:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, (byte)value, p.VertexColorBottomRight.B);
										break;
									case 0xE:
										p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, (byte)value);
										break;
									case 0xF:
										p.VertexColorBottomRight = Color.FromArgb((byte)value, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
										break;
									case 0x10:
										p.Alpha = (byte)value;
										break;
								}
								break;
						}
					}
				}
			}
			if (!got[0]) p.Tx = Pane.Tx;
			if (!got[1]) p.Ty = Pane.Ty;
			if (!got[2]) p.Tz = Pane.Tz;
			if (!got[3]) p.Rx = Pane.Rx;
			if (!got[4]) p.Ry = Pane.Ry;
			if (!got[5]) p.Rz = Pane.Rz;
			if (!got[6]) p.Sx = Pane.Sx;
			if (!got[7]) p.Sy = Pane.Sy;
			if (!got[8]) p.Width = Pane.Width;
			if (!got[9]) p.Height = Pane.Height;
			if (!got[10]) p.Visible = Pane.Visible;
			if (!got[11]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, Pane.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[12]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, Pane.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[13]) p.VertexColorTopLeft = Color.FromArgb(p.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, Pane.VertexColorTopLeft.B);
			if (!got[14]) p.VertexColorTopLeft = Color.FromArgb(Pane.VertexColorTopLeft.A, p.VertexColorTopLeft.R, p.VertexColorTopLeft.G, p.VertexColorTopLeft.B);
			if (!got[15]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, Pane.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[16]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, Pane.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[17]) p.VertexColorTopRight = Color.FromArgb(p.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, Pane.VertexColorTopRight.B);
			if (!got[18]) p.VertexColorTopRight = Color.FromArgb(Pane.VertexColorTopRight.A, p.VertexColorTopRight.R, p.VertexColorTopRight.G, p.VertexColorTopRight.B);
			if (!got[19]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, Pane.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[20]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, Pane.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[21]) p.VertexColorBottomLeft = Color.FromArgb(p.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, Pane.VertexColorBottomLeft.B);
			if (!got[22]) p.VertexColorBottomLeft = Color.FromArgb(Pane.VertexColorBottomLeft.A, p.VertexColorBottomLeft.R, p.VertexColorBottomLeft.G, p.VertexColorBottomLeft.B);
			if (!got[23]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, Pane.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[24]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, Pane.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[25]) p.VertexColorBottomRight = Color.FromArgb(p.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, Pane.VertexColorBottomRight.B);
			if (!got[26]) p.VertexColorBottomRight = Color.FromArgb(Pane.VertexColorBottomRight.A, p.VertexColorBottomRight.R, p.VertexColorBottomRight.G, p.VertexColorBottomRight.B);
			if (!got[27]) p.Alpha = Pane.Alpha;
			return p;
		}
	}
}
