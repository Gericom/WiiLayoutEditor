using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace WiiLayoutEditor.IO.Misc
{
	public class THP
	{
		public const String Signature = "THP\0";
		//private EndianBinaryReader er;
		//private List<long> Frameoffset = new List<long>();// ImageDataOffsetSize = new Dictionary<uint, uint>();
		public THP(byte[] file)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			bool OK;
			Header = new THPHeader(er, Signature, out OK);
			if (!OK) { System.Windows.Forms.MessageBox.Show("Error 1"); goto end; }
			er.BaseStream.Position = Header.ComponentDataOffset;
			Components = new THPComponents(er, Header.Version);
			long NextFrameOffset = Header.FirstFrameOffset;
			UInt32 NextFrameSize = Header.FirstFrameSize;
			Frames = new THPFrame[Header.NrFrames];
			for (int i = 0; i < Header.NrFrames; i++)
			{
				er.BaseStream.Position = NextFrameOffset;
				//Frameoffset.Add(er.BaseStream.Position);
				//	er.BaseStream.Position = NextFrameOffset;
				Frames[i] = new THPFrame(er, (int)(Components.THPInfos[1] != null ? ((THPComponents.THPAudioInfo)Components.THPInfos[1]).NrData : 0), (int)(Components.THPInfos[1] != null ? ((THPComponents.THPAudioInfo)Components.THPInfos[1]).NrChannels : 0));
				//NextFrameOffset += NextFrameSize;
				UInt32 nextframesize = Frames[i].Header.NextTotalSize;//er.ReadUInt32();
				NextFrameOffset += NextFrameSize;
				NextFrameSize = nextframesize;
				//NextFrameSize = Frames[i].Header.NextTotalSize;
			}
		//return;
		end:
			er.Close();
		}
		UI.ProgressDialog d;
		public THP(string AVIPath)
		{
			d = new UI.ProgressDialog();
			UI.QualitySelector q = new UI.QualitySelector();
			q.ShowDialog();
			System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
			b.WorkerReportsProgress = true;
			b.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(b_RunWorkerCompleted);
			b.DoWork += new System.ComponentModel.DoWorkEventHandler(CreateTHP);
			b.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(b_ProgressChanged);
			b.RunWorkerAsync(new object[]{AVIPath,q.Quality});
			d.ShowDialog();
		}

		private void b_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			d.Close();
		}

		private void b_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			d.SetProgress(e.ProgressPercentage, e.UserState as String);
		}
		private void CreateTHP(object Sender, System.ComponentModel.DoWorkEventArgs e)
		{
			{
				
				/*WPFMediaKit.DirectShow.MediaPlayers.MediaDetector d = new WPFMediaKit.DirectShow.MediaPlayers.MediaDetector();
				d.LoadMedia((string)e.Argument);
				Bitmap b = d.GetImage(new TimeSpan(0,5,0));
				d.Dispose();*/
				//AForge.Video.DirectShow.FileVideoSource d = new AForge.Video.DirectShow.FileVideoSource((string)e.Argument);
			}
			((System.ComponentModel.BackgroundWorker)Sender).ReportProgress(0, "Reading AVI File");
			AviFile.AviManager Avi = new AviFile.AviManager((string)((object[])e.Argument)[0], true);
			AviFile.VideoStream Video = Avi.GetVideoStream();
			AviFile.AudioStream Audio = null;
			try { Audio = Avi.GetWaveStream(); }
			catch { }
			int SamplesPerFrame = 0;
			if (Audio != null) SamplesPerFrame = (int)((double)Audio.CountSamplesPerSecond / Video.FrameRate);
			Header = new THPHeader(Video);
			Header.MaxAudioSamples = (uint)SamplesPerFrame;
			Components = new THPComponents(Video, Audio);
			Frames = new THPFrame[Video.CountFrames];
			((System.ComponentModel.BackgroundWorker)Sender).ReportProgress(7, "Generating Frames");
			float percent = 7;
			float add = 92f / Video.CountFrames;
			Video.GetFrameOpen();
			for (int i = 0; i < Video.CountFrames; i++)
			{
				((System.ComponentModel.BackgroundWorker)Sender).ReportProgress((int)(percent), "Generating Frame " + (i + 1));
				Frames[i] = new THPFrame(Video, Audio, SamplesPerFrame, i, (int)((object[])e.Argument)[1]);
				percent += add;
			}
			((System.ComponentModel.BackgroundWorker)Sender).ReportProgress(99, "Calculating Frame Sizes");
			Header.FirstFrameSize = (uint)(Frames[0].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
			Header.FirstFrameSize += Header.FirstFrameSize % 4;
			Header.FirstFrameOffset = 32 + 48 + (uint)(Audio != null ? 16 : 0);
			UInt32 Offset = Header.FirstFrameOffset;
			UInt32 MaxBuffer = 0;
			for (int i = 0; i < Video.CountFrames; i++)
			{
				uint size = (UInt32)(Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12) + ((Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12)) % 4));
				if (MaxBuffer < size) MaxBuffer = size;
				if (i == 0)
				{
					Offset += (UInt32)(Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12) + ((Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12)) % 4));
					Frames[i].Header.PrevTotalSize = (uint)(Frames[Video.CountFrames - 1].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.PrevTotalSize += Frames[i].Header.PrevTotalSize % 4;
					Frames[i].Header.NextTotalSize = (uint)(Frames[i + 1].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.NextTotalSize += Frames[i].Header.NextTotalSize % 4;
				}
				else if (i >= Video.CountFrames - 1)
				{
					Frames[i].Header.PrevTotalSize = (uint)(Frames[i - 1].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.PrevTotalSize += Frames[i].Header.PrevTotalSize % 4;
					Frames[i].Header.NextTotalSize = (uint)(Frames[0].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.NextTotalSize += Frames[i].Header.NextTotalSize % 4;
				}
				else
				{
					Offset += (UInt32)(Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12) + ((Frames[i].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12)) % 4));
					Frames[i].Header.PrevTotalSize = (uint)(Frames[i - 1].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.PrevTotalSize += Frames[i].Header.PrevTotalSize % 4;
					Frames[i].Header.NextTotalSize = (uint)(Frames[i + 1].ImageData.Length + (Audio != null ? 16 + 80 + Frames[0].AudioFrames[0].Data.Length : 12));
					Frames[i].Header.NextTotalSize += Frames[i].Header.NextTotalSize % 4;
				}
			}
			Header.LastFrameOffset = Offset;
			Header.MaxBufferSize = MaxBuffer;
			Video.GetFrameClose();
			((System.ComponentModel.BackgroundWorker)Sender).ReportProgress(100, "Finished!");
			Avi.Close();
		}

		public void Write(String file)
		{
			//LiquidEngine.Tools.MemoryTributary m = new LiquidEngine.Tools.MemoryTributary();
			EndianBinaryWriter er = new EndianBinaryWriter(File.Create(file), Endianness.BigEndian);
			Header.Write(er);
			Components.Write(er);
			foreach (THPFrame f in Frames) f.Write(er);
			//byte[] res = m.ToArray();
			er.Close();
			//return res;
		}
		public THPHeader Header;
		public class THPHeader
		{
			public THPHeader(EndianBinaryReader er, String Signature, out bool OK)
			{
				Type = er.ReadString(ASCIIEncoding.ASCII, 4);
				if (Type != Signature) { OK = false; return; }
				Version = (er.ReadUInt32() == 0x00011000 ? 1.1f : 1.0f);
				MaxBufferSize = er.ReadUInt32();
				MaxAudioSamples = er.ReadUInt32();
				FPS = er.ReadSingle();
				NrFrames = er.ReadUInt32();
				FirstFrameSize = er.ReadUInt32();
				DataSize = er.ReadUInt32();
				ComponentDataOffset = er.ReadUInt32();
				OffsetsDataOffset = er.ReadUInt32();
				FirstFrameOffset = er.ReadUInt32();
				LastFrameOffset = er.ReadUInt32();
				OK = true;
			}
			public THPHeader(AviFile.VideoStream Video)
			{
				Type = Signature;
				Version = 1.1f;
				MaxBufferSize = 0;
				FPS = (float)Video.FrameRate;
				NrFrames = (uint)Video.CountFrames;
				FirstFrameSize = 0;
				DataSize = 0;
				ComponentDataOffset = 48;
				OffsetsDataOffset = 0;
				FirstFrameOffset = 0;
				LastFrameOffset = 0;
			}
			public void Write(EndianBinaryWriter er)
			{
				er.Write(Type, ASCIIEncoding.ASCII, false);
				er.Write((UInt32)(Version == 1.1f ? 0x00011000 : 0x00010000));
				er.Write(MaxBufferSize);
				er.Write(MaxAudioSamples);
				er.Write(FPS);
				er.Write(NrFrames);
				er.Write(FirstFrameSize);
				er.Write(DataSize);
				er.Write(ComponentDataOffset);
				er.Write(OffsetsDataOffset);
				er.Write(FirstFrameOffset);
				er.Write(LastFrameOffset);
			}
			public String Type;
			public Single Version;
			public UInt32 MaxBufferSize;
			public UInt32 MaxAudioSamples;
			public Single FPS;
			public UInt32 NrFrames;
			public UInt32 FirstFrameSize;
			public UInt32 DataSize;
			public UInt32 ComponentDataOffset;
			public UInt32 OffsetsDataOffset;
			public UInt32 FirstFrameOffset;
			public UInt32 LastFrameOffset;
		}
		public THPComponents Components;
		public class THPComponents
		{
			public THPComponents(EndianBinaryReader er, float Version)
			{
				NrComponents = er.ReadUInt32();
				ComponentTypes = er.ReadBytes(16);
				THPInfos = new THPInfo[16];
				for (int i = 0; i < 16; i++)
				{
					switch (ComponentTypes[i])
					{
						case 0://Video
							THPInfos[i] = new THPVideoInfo(er, Version);
							break;
						case 1://Audio
							THPInfos[i] = new THPAudioInfo(er, Version);
							break;
						case 0xFF://Nothing
							THPInfos[i] = null;
							break;
					}
				}
			}
			public THPComponents(AviFile.VideoStream Video, AviFile.AudioStream Audio)
			{
				if (Audio != null)
				{
					NrComponents = 2;
					ComponentTypes = new byte[] { 0, 1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
					THPInfos = new THPInfo[] { new THPVideoInfo(Video), new THPAudioInfo(Audio), null, null, null, null, null, null, null, null, null, null, null, null, null, null };
				}
				else
				{
					NrComponents = 1;
					ComponentTypes = new byte[] { 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
					THPInfos = new THPInfo[] { new THPVideoInfo(Video), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
				}
			}
			public void Write(EndianBinaryWriter er)
			{
				er.Write(NrComponents);
				er.Write(ComponentTypes, 0, 16);
				foreach (THPInfo i in THPInfos)
				{
					if (i != null) i.Write(er);
				}
			}
			public UInt32 NrComponents;
			public Byte[] ComponentTypes; //16
			public THPInfo[] THPInfos;//16

			public class THPInfo { public virtual void Write(EndianBinaryWriter er) { } }
			public class THPVideoInfo : THPInfo
			{
				public THPVideoInfo(EndianBinaryReader er, float Version)
				{
					Width = er.ReadUInt32();
					Height = er.ReadUInt32();
					if (Version == 1.1f) VideoFormat = er.ReadUInt32();
				}
				public THPVideoInfo(AviFile.VideoStream Video)
				{
					Width = (uint)Video.Width;
					Height = (uint)Video.Height;
					VideoFormat = 0;
				}
				public override void Write(EndianBinaryWriter er)
				{
					er.Write(Width);
					er.Write(Height);
					er.Write(VideoFormat);
				}
				public UInt32 Width;
				public UInt32 Height;
				public UInt32 VideoFormat;
			}
			public class THPAudioInfo : THPInfo
			{
				public THPAudioInfo(EndianBinaryReader er, float Version)
				{
					NrChannels = er.ReadUInt32();
					Frequentie = er.ReadUInt32();
					NrSamples = er.ReadUInt32();
					if (Version == 1.1f) NrData = er.ReadUInt32();
				}
				public THPAudioInfo(AviFile.AudioStream Audio)
				{
					NrChannels = (uint)Audio.CountChannels;
					Frequentie = (uint)Audio.CountSamplesPerSecond;
					AviFile.Avi.AVISTREAMINFO Stream = new AviFile.Avi.AVISTREAMINFO();
					AviFile.Avi.PCMWAVEFORMAT WaveFormat = new AviFile.Avi.PCMWAVEFORMAT();
					int length = 0;
					IntPtr ptr = Audio.GetStreamData(ref Stream, ref WaveFormat, ref length);
					System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
					NrSamples = (uint)(length / 2 / Audio.CountChannels);
					NrData = 1;
				}
				public override void Write(EndianBinaryWriter er)
				{
					er.Write(NrChannels);
					er.Write(Frequentie);
					er.Write(NrSamples);
					er.Write(NrData);
				}
				public UInt32 NrChannels;
				public UInt32 Frequentie;
				public UInt32 NrSamples;
				public UInt32 NrData;
			}
		}
		public THPFrame[] Frames;
		public class THPFrame
		{
			public THPFrame(EndianBinaryReader er, int NrAudioData, int NrAudioChannels)
			{
				Header = new THPFrameHeader(er, NrAudioData > 0);
				ImageData = new byte[Header.ImageSize];
				er.BaseStream.Read(ImageData, 0, (int)Header.ImageSize);
				if (NrAudioData > 0)
				{
					AudioFrames = new THPAudioFrame[NrAudioData];
					for (int i = 0; i < NrAudioData; i++) AudioFrames[i] = new THPAudioFrame(er, NrAudioChannels);
				}
			}
			public THPFrame(AviFile.VideoStream Video, AviFile.AudioStream Audio, int SamplesPerFrame, int FrameNr, int Quality)
			{
				Bitmap b = Video.GetBitmap(FrameNr);
				MemoryStream d = new MemoryStream();
				ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
				ImageCodecInfo ici = null;

				foreach (ImageCodecInfo codec in codecs)
				{
					if (codec.MimeType == "image/jpeg")
						ici = codec;
				}

				EncoderParameters ep = new EncoderParameters();
				ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Quality);
				b.Save(d, ici, ep);
				b.Dispose();
				byte[] data = d.ToArray();
				int Start = 0;
				int End = 0;
				int Size = countRequiredThpSize(data, data.Length, ref Start, ref End);
				ImageData = new byte[Size];
				convertToThpJpeg(ImageData, data, data.Length, Start, End);
				if (Audio != null)
				{
					AudioFrames = new THPAudioFrame[1];
					byte[] rawaudio = new byte[SamplesPerFrame * 2 * Audio.CountChannels];
					AviFile.Avi.AVISTREAMINFO Stream = new AviFile.Avi.AVISTREAMINFO();
					AviFile.Avi.PCMWAVEFORMAT WaveFormat = new AviFile.Avi.PCMWAVEFORMAT();
					int length = 0;
					IntPtr dataptr = Audio.GetStreamData(ref Stream, ref WaveFormat, ref length);
					System.Runtime.InteropServices.Marshal.Copy(dataptr + SamplesPerFrame * Audio.CountChannels * 2 * FrameNr, rawaudio, 0, SamplesPerFrame * Audio.CountChannels * 2);
					System.Runtime.InteropServices.Marshal.FreeHGlobal(dataptr);
					AudioFrames[0] = new THPAudioFrame(rawaudio, Audio);
				}
				Header = new THPFrameHeader(Size, (Audio != null ? 80 + AudioFrames[0].Data.Length : 0));
			}
			public void Write(EndianBinaryWriter er)
			{
				Header.Write(er);
				er.Write(ImageData, 0, ImageData.Length);
				if (AudioFrames != null)
				{
					AudioFrames[0].Write(er);
					uint size = (UInt32)(ImageData.Length + 16 + 80 + AudioFrames[0].Data.Length);
					er.Write(new byte[size % 4], 0, (int)size % 4);
				}
				else
				{
					uint size = (UInt32)(ImageData.Length + 12);
					er.Write(new byte[size % 4], 0, (int)size % 4);
				}
			}
			public THPFrameHeader Header;
			public byte[] ImageData;
			public THPAudioFrame[] AudioFrames;
			public class THPFrameHeader
			{
				public THPFrameHeader(EndianBinaryReader er, bool ContainsAudio)
				{
					NextTotalSize = er.ReadUInt32();
					PrevTotalSize = er.ReadUInt32();
					ImageSize = er.ReadUInt32();
					if (ContainsAudio) AudioSize = er.ReadUInt32();
				}
				public THPFrameHeader(int ImageSize, int AudioSize)
				{
					NextTotalSize = 0;
					PrevTotalSize = 0;
					this.ImageSize = (uint)ImageSize;
					if (AudioSize != 0) this.AudioSize = (uint)AudioSize;
				}
				public void Write(EndianBinaryWriter er)
				{
					er.Write(NextTotalSize);
					er.Write(PrevTotalSize);
					er.Write(ImageSize);
					if (AudioSize != 0) er.Write(AudioSize);
				}
				public UInt32 NextTotalSize;
				public UInt32 PrevTotalSize;
				public UInt32 ImageSize;
				public UInt32 AudioSize;
			}
			public class THPAudioFrame
			{
				public THPAudioFrame(EndianBinaryReader er, int NrAudioChannels)
				{
					Header = new THPAudioFrameHeader(er);
					Data = new byte[(int)Header.ChannelSize * NrAudioChannels];
					er.BaseStream.Read(Data, 0, (int)Header.ChannelSize * NrAudioChannels);
					//Data = er.ReadBytes((int)Header.ChannelSize * NrAudioChannels);
					//if (NrAudioChannels == 2)
					//{
					//Data2 = er.ReadBytes((int)Header.ChannelSize);
					//}
				}
				public THPAudioFrame(byte[] waveFile, AviFile.AudioStream Audio)
				{
					Header = new THPAudioFrameHeader();
					Header.NrSamples = (uint)(waveFile.Length / 2 / Audio.CountChannels);
					Data = Encode(waveFile, Audio);

					Header.ChannelSize = (uint)(Data.Length / Audio.CountChannels);
					Header.Table1 = defTbl;
					Header.Table2 = defTbl;
					Header.Channel1Prev1 = 0;
					Header.Channel1Prev2 = 0;
					Header.Channel2Prev1 = 0;
					Header.Channel2Prev2 = 0;
				}
				public void Write(EndianBinaryWriter er)
				{
					Header.Write(er);
					er.Write(Data, 0, Data.Length);
				}
				private short[] defTbl = new short[] { 1820, -856, 3238, -1514, 2333, -550, 3336, -1376, 2444, -949, 3666, -1764, 2654, -701, 3420, -1398 };
				private byte[] Encode(byte[] inputFrames, AviFile.AudioStream Audio)
				{
					int offset = 0;
					int[] sampleBuffer = new int[14];

					int tempSampleCount = inputFrames.Length / (Audio.CountChannels == 2 ? 4 : 2);
					int modLength = (inputFrames.Length / (Audio.CountChannels == 2 ? 4 : 2)) % 14;

					Array.Resize(ref inputFrames, inputFrames.Length + ((14 - modLength) * (Audio.CountChannels == 2 ? 4 : 2)));

					int sampleCount = inputFrames.Length / (Audio.CountChannels == 2 ? 4 : 2);

					int blocks = (sampleCount + 13) / 14;

					List<int> soundDataLeft = new List<int>();
					List<int> soundDataRight = new List<int>();

					int co = offset;

					for (int j = 0; j < sampleCount; j++)
					{
						soundDataLeft.Add(BitConverter.ToInt16(inputFrames, co));
						co += 2;

						if (Audio.CountChannels == 2)
						{
							soundDataRight.Add(BitConverter.ToInt16(inputFrames, co));
							co += 2;
						}
					}

					byte[] data = new byte[(Audio.CountChannels == 2 ? (blocks * 16) : (blocks * 8))];

					int data1Offset = 0;
					int data2Offset = blocks * 8;

					//this.bnsInfo.Channel2Start = (Audio.CountChannels == 2 ? (uint)data2Offset : 0);

					int[] leftSoundData = soundDataLeft.ToArray();
					int[] rightSoundData = soundDataRight.ToArray();

					for (int y = 0; y < blocks; y++)
					{
						/*try
						{
							if (y % (int)(blocks / 100) == 0 || (y + 1) == blocks)
								ChangeProgress((y + 1) * 100 / blocks);
						}
						catch { }*/

						for (int a = 0; a < 14; a++)
							sampleBuffer[a] = leftSoundData[y * 14 + a];

						byte[] outBuffer = RepackAdpcm(0, this.defTbl, sampleBuffer);

						for (int a = 0; a < 8; a++)
							data[data1Offset + a] = outBuffer[a];

						data1Offset += 8;

						if (Audio.CountChannels == 2)
						{
							for (int a = 0; a < 14; a++)
								sampleBuffer[a] = rightSoundData[y * 14 + a];

							outBuffer = RepackAdpcm(1, this.defTbl, sampleBuffer);

							for (int a = 0; a < 8; a++)
								data[data2Offset + a] = outBuffer[a];

							data2Offset += 8;
						}
					}

					//this.bnsInfo.LoopEnd = (uint)(blocks * 7);

					return data;
				}
				private int[] tlSamples = new int[2];
				private int[,] rlSamples = new int[2, 2];
				private byte[] RepackAdpcm(int index, short[] table, int[] inputBuffer)
				{
					byte[] data = new byte[8];
					int[] blSamples = new int[2];
					int bestIndex = -1;
					double bestError = 999999999.0;
					double error;

					for (int tableIndex = 0; tableIndex < 8; tableIndex++)
					{
						byte[] testData = CompressAdpcm(index, table, tableIndex, inputBuffer, out error);

						if (error < bestError)
						{
							bestError = error;

							for (int i = 0; i < 8; i++)
								data[i] = testData[i];
							for (int i = 0; i < 2; i++)
								blSamples[i] = this.tlSamples[i];

							bestIndex = tableIndex;
						}
					}

					for (int i = 0; i < 2; i++)
						this.rlSamples[index, i] = blSamples[i];

					return data;
				}

				private byte[] CompressAdpcm(int index, short[] table, int tableIndex, int[] inputBuffer, out double outError)
				{
					byte[] data = new byte[8];
					int error = 0;
					int factor1 = table[2 * tableIndex + 0];
					int factor2 = table[2 * tableIndex + 1];

					int exponent = DetermineStdExponent(index, table, tableIndex, inputBuffer);

					while (exponent <= 15)
					{
						bool breakIt = false;
						error = 0;
						data[0] = (byte)(exponent | (tableIndex << 4));

						for (int i = 0; i < 2; i++)
							this.tlSamples[i] = this.rlSamples[index, i];

						int j = 0;

						for (int i = 0; i < 14; i++)
						{
							int predictor = (int)((this.tlSamples[1] * factor1 + this.tlSamples[0] * factor2) >> 11);
							int residual = (inputBuffer[i] - predictor) >> exponent;

							if (residual > 7 || residual < -8)
							{
								exponent++;
								breakIt = true;
								break;
							}

							int nibble = Clamp(residual, -8, 7);

							if ((i & 1) != 0)
								data[i / 2 + 1] = (byte)(data[i / 2 + 1] | (nibble & 0xf));
							else
								data[i / 2 + 1] = (byte)(nibble << 4);

							predictor += nibble << exponent;

							this.tlSamples[0] = this.tlSamples[1];
							this.tlSamples[1] = Clamp(predictor, -32768, 32767);

							error += (int)(Math.Pow((double)(this.tlSamples[1] - inputBuffer[i]), 2));
						}

						if (!breakIt) j = 14;

						if (j == 14) break;
					}

					outError = error;
					return data;
				}

				private int DetermineStdExponent(int index, short[] table, int tableIndex, int[] inputBuffer)
				{
					int[] elSamples = new int[2];
					int maxResidual = 0;
					int factor1 = table[2 * tableIndex + 0];
					int factor2 = table[2 * tableIndex + 1];

					for (int i = 0; i < 2; i++)
						elSamples[i] = this.rlSamples[index, i];

					for (int i = 0; i < 14; i++)
					{
						int predictor = (elSamples[1] * factor1 + elSamples[0] * factor2) >> 11;
						int residual = inputBuffer[i] - predictor;

						if (residual > maxResidual)
							maxResidual = residual;

						elSamples[0] = elSamples[1];
						elSamples[1] = inputBuffer[i];
					}

					return FindExponent(maxResidual);
				}

				private int FindExponent(double residual)
				{
					int exponent = 0;

					while (residual > 7.5 || residual < -8.5)
					{
						exponent++;
						residual /= 2.0;
					}

					return exponent;
				}

				private int Clamp(int input, int min, int max)
				{
					if (input < min) return min;
					if (input > max) return max;
					return input;
				}
				public THPAudioFrameHeader Header;
				public class THPAudioFrameHeader
				{
					public THPAudioFrameHeader(EndianBinaryReader er)
					{
						ChannelSize = er.ReadUInt32();
						NrSamples = er.ReadUInt32();
						Table1 = er.ReadInt16s(16);
						Table2 = er.ReadInt16s(16);
						Channel1Prev1 = er.ReadInt16();
						Channel1Prev2 = er.ReadInt16();
						Channel2Prev1 = er.ReadInt16();
						Channel2Prev2 = er.ReadInt16();
					}
					public THPAudioFrameHeader() { }
					public UInt32 ChannelSize;
					public UInt32 NrSamples;
					public short[] Table1;
					public short[] Table2;
					public Int16 Channel1Prev1;
					public Int16 Channel1Prev2;
					public Int16 Channel2Prev1;
					public Int16 Channel2Prev2;
					public void Write(EndianBinaryWriter er)
					{
						er.Write(ChannelSize);
						er.Write(NrSamples);
						er.Write(Table1, 0, 16);
						er.Write(Table2, 0, 16);
						er.Write(Channel1Prev1);
						er.Write(Channel1Prev2);
						er.Write(Channel2Prev1);
						er.Write(Channel2Prev2);
					}
				}
				public byte[] Data;
			}
			public Bitmap ToBitmap()
			{
				byte[] Result;
				int start = 0, end = 0;
				unsafe
				{
					byte* data = stackalloc byte[ImageData.Length];
					/*for (int i = 0; i < ImageData.Length; i++)
					{
						data[i] = ImageData[i];
					}*/
					System.Runtime.InteropServices.Marshal.Copy(ImageData, 0, (IntPtr)data, ImageData.Length);
					int newSize = countRequiredSize(data, ImageData.Length, ref start, ref end);
					byte* buff = stackalloc byte[newSize];
					convertToRealJpeg(buff, data, ImageData.Length, start, end);
					Result = new byte[newSize];
					System.Runtime.InteropServices.Marshal.Copy((IntPtr)buff, Result, 0, newSize);
				}
				return new Bitmap(new MemoryStream(Result));
			}
			private unsafe int countRequiredSize(byte* data, int size, ref int start, ref int end)
			{
				start = 2 * size;
				int count = 0;

				int j;
				for (j = size - 1; data[j] == 0; --j)
					; //search end of data

				if (data[j] == 0xd9) //thp file
					end = j - 1;
				else if (data[j] == 0xff) //mth file
					end = j - 2;

				for (int i = 0; i < end; ++i)
				{
					if (data[i] == 0xff)
					{
						//if i == srcSize - 1, then this would normally overrun src - that's why 4 padding
						//bytes are included at the end of src
						if (data[i + 1] == 0xda && start == 2 * size)
							start = i;
						if (i > start)
							++count;
					}
				}
				return size + count;
			}

			private unsafe void convertToRealJpeg(byte* dest, byte* src, int srcSize, int start, int end)
			{
				int di = 0;
				for (int i = 0; i < srcSize; ++i, ++di)
				{
					dest[di] = src[i];
					//if i == srcSize - 1, then this would normally overrun src - that's why 4 padding
					//bytes are included at the end of src
					if (src[i] == 0xff && i > start && i < end)
					{
						++di;
						dest[di] = 0;
					}
				}
			}
			private int countRequiredThpSize(byte[] data, int size, ref int start, ref int end)
			{
				start = 2 * size;
				int count = 0;

				int j;
				for (j = size - 1; data[j] == 0; --j)
					; //search end of data

				if (data[j] == 0xd9) //thp file
					end = j - 1;
				else if (data[j] == 0xff) //mth file
					end = j - 2;

				for (int i = 0; i < end; ++i)
				{
					if (data[i] == 0xff)
					{
						//if i == srcSize - 1, then this would normally overrun src - that's why 4 padding
						//bytes are included at the end of src
						if (data[i + 1] == 0xda && start == 2 * size)
							start = i;
						if (i > start)
							--count;
					}
				}
				return size + count;
			}
			private void convertToThpJpeg(byte[] dest, byte[] src, int srcSize, int start, int end)
			{
				int di = 0;
				for (int i = 0; i < srcSize; ++i, ++di)
				{
					dest[di] = src[i];
					//if i == srcSize - 1, then this would normally overrun src - that's why 4 padding
					//bytes are included at the end of src
					if (src[i] == 0xff && i > start && i < end)
					{
						++i;
					}
				}
			}
			public void ToPCM16(out byte[] Output)
			{
				//List<byte> PCM = new List<byte>();
				for (int i = 0; i < 1; i++)
				{
					unsafe
					{
						short* dst = stackalloc short[(int)AudioFrames[i].Header.NrSamples * (AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2 ? 2 : 1)];
						thpAudioDecode(dst, AudioFrames[i], false, AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2);
						//thpAudioDecode(ref dst, i, false, AudioFrames[i].Data2 != null);
						//Output = //(IntPtr)dst;
						Output = new byte[(int)AudioFrames[i].Header.NrSamples * (AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2 ? 2 : 1) * 2];
						System.Runtime.InteropServices.Marshal.Copy((IntPtr)dst, Output, 0, Output.Length);
						return;
						//return (int)AudioFrames[i].Header.NrSamples * (AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2 ? 2 : 1) * 2 * 2;
						//byte[] Result = new byte[AudioFrames[i].Header.NrSamples * (AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2 ? 2 : 1) * 2];
						//System.Runtime.InteropServices.Marshal.Copy((IntPtr)dst, Result, 0, Result.Length);
						/*for (int j = 0; j < AudioFrames[i].Header.NrSamples * (AudioFrames[i].Data.Length == AudioFrames[i].Header.ChannelSize * 2 ? 2 : 1); j++)
						{
							PCM.AddRange(BitConverter.GetBytes(dst[j]));
						}*/
						//PCM.AddRange(Result);
					}
				}
				Output = null;
				//Output = IntPtr.Zero;
				//return 0;
				//return PCM.ToArray();
			}

			private unsafe struct DecStruct
			{
				public byte* currSrcByte;
				public UInt32 blockCount;
				public byte index;
				public byte shift;
			}

			private unsafe void thpAudioInitialize(ref DecStruct s, byte* srcStart)
			{
				s.currSrcByte = srcStart;
				s.blockCount = 2;
				s.index = (byte)((*s.currSrcByte >> 4) & 0x7);
				s.shift = (byte)(*s.currSrcByte & 0xf);
				++s.currSrcByte;
			}
			private unsafe Int32 thpAudioGetNewSample(ref DecStruct s)
			{
				//the following if is executed all 14 calls
				//to thpAudioGetNewSample() (once for each
				//microblock) because mask & 0xf can contain
				//16 different values and starts with 2
				if ((s.blockCount & 0xf) == 0)
				{
					s.index = (byte)((*s.currSrcByte >> 4) & 0x7);
					s.shift = (byte)(*s.currSrcByte & 0xf);
					++s.currSrcByte;
					s.blockCount += 2;
				}

				Int32 ret;
				if ((s.blockCount & 1) != 0)
				{
					Int32 t = (Int32)((*s.currSrcByte << 28) & 0xf0000000);
					ret = t >> 28; //this has to be an arithmetic shift
					++s.currSrcByte;
				}
				else
				{
					Int32 t = (Int32)((*s.currSrcByte << 24) & 0xf0000000);
					ret = t >> 28; //this has to be an arithmetic shift
				}

				++s.blockCount;
				return ret;
			}

			private unsafe int thpAudioDecode(Int16* destBuffer, THPFrame.THPAudioFrame Frame, bool separateChannelsInOutput, bool isInputStereo)
			{
				if (destBuffer == null || Frame == null)
					return 0;

				UInt32 channelInSize = Frame.Header.ChannelSize;
				UInt32 numSamples = Frame.Header.NrSamples;

				byte* srcChannel1 = stackalloc byte[Frame.Data.Length];
				System.Runtime.InteropServices.Marshal.Copy(Frame.Data, 0, (IntPtr)srcChannel1, Frame.Data.Length);

				byte* srcChannel2 = srcChannel1 + channelInSize;

				Int16* table1 = stackalloc Int16[16];
				System.Runtime.InteropServices.Marshal.Copy(Frame.Header.Table1, 0, (IntPtr)table1, 16);

				Int16* table2 = stackalloc Int16[16];
				System.Runtime.InteropServices.Marshal.Copy(Frame.Header.Table2, 0, (IntPtr)table2, 16);

				Int16* destChannel1, destChannel2;

				UInt32 delta;

				if (separateChannelsInOutput)
				{
					//separated channels in output
					destChannel1 = destBuffer;
					destChannel2 = destBuffer + numSamples;
					delta = 1;
				}
				else
				{
					//interleaved channels in output
					destChannel1 = destBuffer;
					destChannel2 = destBuffer + 1;
					delta = 2;
				}

				DecStruct s = new DecStruct();
				if (!isInputStereo)
				{
					//mono channel in input

					thpAudioInitialize(ref s, srcChannel1);

					Int16 prev1 = Frame.Header.Channel1Prev1;// *(Int16*)(srcBuffer + 72);
					Int16 prev2 = Frame.Header.Channel1Prev2;//*(Int16*)(srcBuffer + 74);

					for (int i = 0; i < numSamples; ++i)
					{
						Int64 res = (Int64)thpAudioGetNewSample(ref s);
						res = ((res << s.shift) << 11); //convert to 53.11 fixedpoint

						//these values are 53.11 fixed point numbers
						Int64 val1 = table1[2 * s.index];
						Int64 val2 = table1[2 * s.index + 1];

						//convert to 48.16 fixed point
						res = (val1 * prev1 + val2 * prev2 + res) << 5;

						//rounding:
						UInt16 decimalPlaces = (UInt16)(res & 0xffff);
						if (decimalPlaces > 0x8000) //i.e. > 0.5
							//round up
							++res;
						else if (decimalPlaces == 0x8000) //i.e. == 0.5
							if ((res & 0x10000) != 0)
								//round up every other number
								++res;

						//get nonfractional parts of number, clamp to [-32768, 32767]
						Int32 final = (Int32)(res >> 16);
						if (final > 32767) final = 32767;
						else if (final < -32768) final = -32768;

						prev2 = prev1;
						prev1 = (Int16)final;
						*destChannel1 = (Int16)final;
						*destChannel2 = (Int16)final;
						destChannel1 += delta;
						destChannel2 += delta;
					}
				}
				else
				{
					//two channels in input - nearly the same as for one channel,
					//so no comments here (different lines are marked with XXX)

					thpAudioInitialize(ref s, srcChannel1);
					Int16 prev1 = Frame.Header.Channel1Prev1;// *(Int16*)(srcBuffer + 72);
					Int16 prev2 = Frame.Header.Channel1Prev2;//*(Int16*)(srcBuffer + 74);
					for (int i = 0; i < numSamples; ++i)
					{
						Int64 res = (Int64)thpAudioGetNewSample(ref s);
						res = ((res << s.shift) << 11);
						Int64 val1 = table1[2 * s.index];
						Int64 val2 = table1[2 * s.index + 1];
						res = (val1 * prev1 + val2 * prev2 + res) << 5;
						UInt16 decimalPlaces = (UInt16)(res & 0xffff);
						if (decimalPlaces > 0x8000)
							++res;
						else if (decimalPlaces == 0x8000)
							if ((res & 0x10000) != 0)
								++res;
						Int32 final = (Int32)(res >> 16);
						if (final > 32767) final = 32767;
						else if (final < -32768) final = -32768;
						prev2 = prev1;
						prev1 = (Int16)final;
						*destChannel1 = (Int16)final;
						destChannel1 += delta;
					}

					thpAudioInitialize(ref s, srcChannel2);//XXX
					prev1 = Frame.Header.Channel2Prev1;// *(Int16*)(srcBuffer + 72);
					prev2 = Frame.Header.Channel2Prev2;//*(Int16*)(srcBuffer + 74);
					for (int j = 0; j < numSamples; ++j)
					{
						Int64 res = (Int64)thpAudioGetNewSample(ref s);
						res = ((res << s.shift) << 11);
						Int64 val1 = table2[2 * s.index];//XXX
						Int64 val2 = table2[2 * s.index + 1];//XXX
						res = (val1 * prev1 + val2 * prev2 + res) << 5;
						UInt16 decimalPlaces = (UInt16)(res & 0xffff);
						if (decimalPlaces > 0x8000)
							++res;
						else if (decimalPlaces == 0x8000)
							if ((res & 0x10000) != 0)
								++res;
						Int32 final = (Int32)(res >> 16);
						if (final > 32767) final = 32767;
						else if (final < -32768) final = -32768;
						prev2 = prev1;
						prev1 = (Int16)final;
						*destChannel2 = (Int16)final;
						destChannel2 += delta;
					}
				}
				return (int)numSamples;
			}
		}

		public THPFrame GetFrame(int Frame)
		{
			return Frames[Frame];
			//er.BaseStream.Position = Frameoffset[Frame];
			//return new THPFrame(er, (int)(Components.THPInfos[1] != null ? ((THPComponents.THPAudioInfo)Components.THPInfos[1]).NrData : 0), (int)(Components.THPInfos[1] != null ? ((THPComponents.THPAudioInfo)Components.THPInfos[1]).NrChannels : 0));
		}
		public void Close()
		{
			Frames = new THPFrame[0];
			//er.Close();
			//er.Dispose();
		}
	}
}
