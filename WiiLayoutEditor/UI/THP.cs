using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WiiLayoutEditor.UI
{
	public partial class THP : Form
	{
		public IO.Misc.THP File;
		public THP(IO.Misc.THP Thp)
		{
			File = Thp;
			InitializeComponent();
			System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
		}
		IntPtr Timer;
		bool audio = false;
		IntPtr WaveOut;
		WaveLib.WaveOutPlayer w;
		private void THP_Load(object sender, EventArgs e)
		{
			pictureBox1.Image = File.GetFrame(0).ToBitmap();
			Width = (int)((IO.Misc.THP.THPComponents.THPVideoInfo)File.Components.THPInfos[0]).Width + 20;
			Height = (int)((IO.Misc.THP.THPComponents.THPVideoInfo)File.Components.THPInfos[0]).Height + 29;
			audio = File.Components.THPInfos[1] != null;
			if (audio)
			{
				bb = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 2));
				ww = new NAudio.Wave.WaveOut();
				bb.DiscardOnBufferOverflow = true;
				ww.Init(bb);
				ww.Play();
				//	WaveLib.WaveNative.waveOutOpen(out WaveOut, 0, new WaveLib.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 16, 2), new WaveLib.WaveNative.WaveDelegate(WaveCallBack), 0, 0);
				//w = new WaveLib.WaveOutBuffer(WaveOut, (int)File.Header.MaxBufferSize);
				//w = new WaveLib.WaveOutPlayer(0, new WaveLib.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 16, 2), (int)File.Header.MaxBufferSize, 1, new WaveLib.BufferFillEventHandler(BufferFiller));
				/*h.dwBytesRecorded = 0;
				h.dwUser = IntPtr.Zero;
				h.dwFlags = 0;
				h.dwLoops = 0;
				h.lpNext = IntPtr.Zero;
				h.reserved = 0;
				
				unsafe
				{
					WaveLib.WaveNative.waveOutPrepareHeader(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
				}*/
			}
			//for (int i = 0; i < 10; i++)
			//{
			//	t.Enqueue(File.GetFrame(frame++));
			//}
			//backgroundWorker1.RunWorkerAsync();
			//timer1.Interval = (int)(1000f / File.Header.FPS - 0.5f);
			//timer1.Enabled = true;
			//	Timer = SetTimer(Handle, IntPtr.Zero, /*(uint)(1000f / File.Header.FPS-0.68336f)*/(uint)(1000f / File.Header.FPS - (1000f / File.Header.FPS / 10f)), IntPtr.Zero);
			backgroundWorker2.RunWorkerAsync();
			if (audio) backgroundWorker1.RunWorkerAsync();
		
			if (audio)
			{

				//backgroundWorker1.RunWorkerAsync();
				//bb = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 2));
				//ww = new NAudio.Wave.WaveOut();
				//bb.DiscardOnBufferOverflow = true;
				//ww.Init(bb);
				//ww.Play();
				//	WaveLib.WaveNative.waveOutOpen(out WaveOut, 0, new WaveLib.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 16, 2), new WaveLib.WaveNative.WaveDelegate(WaveCallBack), 0, 0);
				//w = new WaveLib.WaveOutBuffer(WaveOut, (int)File.Header.MaxBufferSize);
				//w = new WaveLib.WaveOutPlayer(0, new WaveLib.WaveFormat((int)((IO.Misc.THP.THPComponents.THPAudioInfo)File.Components.THPInfos[1]).Frequentie, 16, 2), (int)File.Header.MaxAudioSamples * 2 * 2, 1, new WaveLib.BufferFillEventHandler(BufferFiller));
				/*h.dwBytesRecorded = 0;
				h.dwUser = IntPtr.Zero;
				h.dwFlags = 0;
				h.dwLoops = 0;
				h.lpNext = IntPtr.Zero;
				h.reserved = 0;
				
				unsafe
				{
					WaveLib.WaveNative.waveOutPrepareHeader(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
				}*/
			}
		}
		NAudio.Wave.BufferedWaveProvider bb;
		NAudio.Wave.WaveOut ww;
		System.Collections.Generic.Queue<byte[]> t = new Queue<byte[]>();
		System.Collections.Generic.Queue<int> t2 = new Queue<int>();
		int frame = 0;
		/*internal void BufferFiller(IntPtr data, int size)
		{
			if (t.Count != 0)
			{
				byte[] b = t.Dequeue();
				//size = b.Length;
				System.Runtime.InteropServices.Marshal.Copy(b, 0, data, b.Length);
				//data = t.Dequeue();
				//size = t2.Dequeue();
			}
		}*/
		//WaveLib.WaveNative.WaveHdr h = new WaveLib.WaveNative.WaveHdr();
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x113)
			{
				frame++;
				if (frame >= File.Header.NrFrames) frame = 0;
				IO.Misc.THP.THPFrame f = File.GetFrame(frame);//t.Dequeue();

				if (audio)
				{
					byte[] a;
					f.ToPCM16(out a);
					//t.Enqueue(a);
					//w.Size = f.ToPCM16(out w.Data);

					//unsafe
					//{
					//WaveLib.WaveNative.waveOutPrepareHeader(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
					//WaveLib.WaveNative.waveOutWrite(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
					//}
					//byte[] s = f.ToPCM16();
					bb.AddSamples(a, 0, a.Length);
				}
				pictureBox1.Image = f.ToBitmap();
				//backgroundWorker1.RunWorkerAsync();
			}
			base.WndProc(ref m);
		}
		[DllImport("user32.dll", ExactSpelling = true)]
		static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

		private void THP_FormClosing(object sender, FormClosingEventArgs e)
		{
			backgroundWorker2.CancelAsync();
			//timer1.Enabled = false;
			if (audio)
			{
				backgroundWorker1.CancelAsync();
				//w.Dispose();
				audio = false;
				ww.Stop();
				ww.Dispose();
			}
			File.Close();
		}

		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			int i = 0;
			//System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
			while (!e.Cancel)
			{
				if (i > frame + 200) continue;
				//w.Reset();
				//w.Start();
				i++;
				if (i >= File.Header.NrFrames) i = 0;
				IO.Misc.THP.THPFrame f = File.GetFrame(i);
				byte[] a;
				f.ToPCM16(out a);
				bb.AddSamples(a, 0, a.Length);
				/*t.Enqueue(f);
				if (audio)
				{
					//if (ww.PlaybackState != NAudio.Wave.PlaybackState.Playing) ww.Play();
					//byte[] s = f.ToPCM16();
					//bb.AddSamples(s, 0, s.Length);
				}

				//frame++;
				//if (frame >= File.Header.NrFrames) frame = 0;
				//IO.Misc.THP.THPFrame f = t.Dequeue();//File.GetFrame(frame);

				pictureBox1.Image = f.ToBitmap();
				w.Stop();
				System.Threading.Thread.Sleep(new TimeSpan((long)(((1000f / File.Header.FPS) - 2f) * 10000f) - w.ElapsedTicks));/
				IO.Misc.THP.THPFrame f = File.GetFrame(i);
				t.Enqueue(f);
				if (audio)
				{
					h.dwBufferLength = f.ToPCM16(out h.lpData);

					unsafe
					{
						//WaveLib.WaveNative.waveOutPrepareHeader(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
						WaveLib.WaveNative.waveOutWrite(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
					}
					//byte[] s = f.ToPCM16();
					//bb.AddSamples(s, 0, s.Length);
				}*/

			}
		}

		private void THP_Shown(object sender, EventArgs e)
		{

		}

		private void pictureBox1_Layout(object sender, LayoutEventArgs e)
		{

		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			frame++;
			if (frame >= File.Header.NrFrames) frame = 0;
			IO.Misc.THP.THPFrame f = File.GetFrame(frame);//t.Dequeue();
			pictureBox1.Image = f.ToBitmap();
			/*if (audio)
			{
				byte[] a;
				f.ToPCM16(out a);
				//t.Enqueue(a);
				//w.Size = f.ToPCM16(out w.Data);

				//unsafe
				//{
				//WaveLib.WaveNative.waveOutPrepareHeader(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
				//WaveLib.WaveNative.waveOutWrite(WaveOut, ref h, sizeof(WaveLib.WaveNative.WaveHdr));
				//}
				//byte[] s = f.ToPCM16();
				bb.AddSamples(a, 0, a.Length);
			}*/
		}

		private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
		{
			//System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
			while (!e.Cancel)
			{
				//w.Reset();
				//w.Start();
				System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
				s.Start();
				frame++;
				if (frame >= File.Header.NrFrames) frame = 0;
				IO.Misc.THP.THPFrame f = File.GetFrame(frame);
				pictureBox1.Image = f.ToBitmap();
				s.Stop();
				if ((int)(1000f / File.Header.FPS - (1000f / File.Header.FPS / 20f)) - s.ElapsedMilliseconds > 0)
				{
					System.Threading.Thread.Sleep((int)(1000f / File.Header.FPS - (1000f / File.Header.FPS / 20f)) - (int)s.ElapsedMilliseconds);
				}
			}
		}
	}
}
