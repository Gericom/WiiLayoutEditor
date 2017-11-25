using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tao.OpenGl;
using System.Drawing.Imaging;
using System.IO;

namespace WiiLayoutEditor
{
	public partial class Form1 : Form
	{
		IO.BRLYT Layout = null;
		public Form1()
		{
			TypeDescriptor.AddAttributes(typeof(Color),
	new EditorAttribute(typeof(UI.UITypeEditors.PSColorPicker), typeof(System.Drawing.Design.UITypeEditor)),
	new TypeConverterAttribute(typeof(UI.UITypeEditors.MyColorConverter)));
			InitializeComponent();
			simpleOpenGlControl1.InitializeContexts();
			imageList1.Images.Add(Properties.Resources.zone);
			imageList1.Images.Add(Properties.Resources.image);
			imageList1.Images.Add(Properties.Resources.edit);
			imageList1.Images.Add(Properties.Resources.slide);
			imageList1.Images.Add(Properties.Resources.slides_stack);
			imageList1.Images.Add(Properties.Resources.blog_blue);
			imageList1.Images.Add(Properties.Resources.images_stack);
			imageList1.Images.Add(Properties.Resources.edit_language);
			imageList1.Images.Add(Properties.Resources.film);
			imageList1.Images.Add(Properties.Resources.film_timeline);
			imageList1.Images.Add(Properties.Resources.application_text_image);
		}
		private void Form1_Load(object sender, EventArgs e)
		{
			InitOpenGl();
		}
		IO.BasicShader BasicShaderb = new IO.BasicShader();
		byte[] pic;
		public void Render(bool picking = false, Point MousePoint = new Point())
		{
			if (Layout != null)
			{
				Gl.glMatrixMode(Gl.GL_PROJECTION);
				Gl.glLoadIdentity();
				Gl.glViewport(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);

				Gl.glOrtho(-Layout.LYT1.Width / 2.0f, Layout.LYT1.Width / 2.0f, -Layout.LYT1.Height / 2.0f, Layout.LYT1.Height / 2.0f, -1000, 1000);
				//Glu.gluPerspective(90, aspect, 0.02f, 1000.0f);//0.02f, 32.0f);
				//Gl.glTranslatef(0, 0, -100);


				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glLoadIdentity();

				if (!picking) Gl.glClearColor(1, 1, 1, 1);//BGColor.R / 255f, BGColor.G / 255f, BGColor.B / 255f, 1f);
				else Gl.glClearColor(0, 0, 0, 1);
				Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

				Gl.glColor4f(1, 1, 1, 1);
				Gl.glEnable(Gl.GL_TEXTURE_2D);
				Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
				Gl.glColor4f(1, 1, 1, 1);
				Gl.glDisable(Gl.GL_CULL_FACE);
				Gl.glEnable(Gl.GL_ALPHA_TEST);
				Gl.glEnable(Gl.GL_BLEND);
				Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

				Gl.glAlphaFunc(Gl.GL_ALWAYS, 0f);

				Gl.glLoadIdentity();


				if (!picking)
				{
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
					BasicShaderb.Enable();
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
					Gl.glColor4f(204 / 255f, 204 / 255f, 204 / 255f, 1);
					int xbase = 0;
					for (int y = 0; y < simpleOpenGlControl1.Height; y += 8)
					{
						for (int x = xbase; x < simpleOpenGlControl1.Width; x += 16)
						{
							Gl.glRectf(x - simpleOpenGlControl1.Width / 2f, y - simpleOpenGlControl1.Height / 2f, x - simpleOpenGlControl1.Width / 2f + 8, y - simpleOpenGlControl1.Height / 2f + 8);
						}
						if (xbase == 0) xbase = 8;
						else xbase = 0;
					}
				}
				if (picking)
				{
					BasicShaderb.Enable();
					int idx = 0;
					Layout.PAN1.Render(Layout, ref idx, 255, picking);
					pic = new byte[4];
					Bitmap b = IO.Util.ScreenShot(simpleOpenGlControl1);
					Gl.glReadPixels(MousePoint.X, (int)simpleOpenGlControl1.Height - MousePoint.Y, 1, 1, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pic);
					Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
					Render();
					//simpleOpenGlControl1.Refresh();
					//Render();
				}
				else
				{
					int idx = 0;
					List<IO.BRLAN.AnimatedMat1> m = new List<IO.BRLAN.AnimatedMat1>();
					if (toolStripButton2.Checked)
					{
						foreach (IO.BRLYT.mat1.MAT1Entry mm in Layout.MAT1.Entries)
						{
							m.Add(Animation.GetMatValue(FrameNr, mm));
						}
					}
					Layout.PAN1.Render(Layout, ref idx, 255, false, (toolStripButton2.Checked ? Animation : null), FrameNr, null, m.ToArray());
					simpleOpenGlControl1.Refresh();
				}
			}
		}
		public void InitOpenGl()
		{
			Gl.glEnable(Gl.GL_COLOR_MATERIAL);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			//Gl.glDepthFunc(Gl.GL_ALWAYS);
			Gl.glEnable(Gl.GL_LOGIC_OP);
			Gl.glDisable(Gl.GL_CULL_FACE);
			Gl.glEnable(Gl.GL_TEXTURE_2D);

			//Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_BLEND);

			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

			BasicShaderb.Compile(false);
		}
		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& openFileDialog1.FileName.Length > 0)
			{
				timer1.Enabled = false;
				simpleOpenGlControl1.DestroyContexts();
				simpleOpenGlControl1.InitializeContexts();
				InitOpenGl();
				Animation = null;
				FrameNr = 0;
				toolStripButton1.Enabled = true;
				toolStripButton2.Enabled = false;
				toolStripButton2.Checked = false;
				if (openFileDialog1.FileName.ToLower().EndsWith(".brlyt"))
				{
					Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
					if (Directory.Exists(Path.GetDirectoryName(openFileDialog1.FileName).Replace("blyt", "timg")))
					{
						foreach (FileInfo f in new DirectoryInfo(Path.GetDirectoryName(openFileDialog1.FileName).Replace("blyt", "timg")).GetFiles())
						{
							files.Add(f.Name, File.ReadAllBytes(f.FullName));
						}
					}
					foreach (FileInfo f in new DirectoryInfo(Path.GetDirectoryName(openFileDialog1.FileName)).GetFiles())
					{
						if (f.Extension == ".brfnt")
						{
							files.Add(f.Name, File.ReadAllBytes(f.FullName));
						}
					}
					Layout = new IO.BRLYT(File.ReadAllBytes(openFileDialog1.FileName), files);
				}
				else if (openFileDialog1.FileName.ToLower().EndsWith("banner.bin"))
				{
					byte[] file = File.ReadAllBytes(openFileDialog1.FileName);
					if (libWiiSharp.Lz77.IsLz77Compressed(file))
					{
						file = new libWiiSharp.Lz77().Decompress(file);
					}
					libWiiSharp.U8 u = libWiiSharp.U8.Load(file);
					Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
					int i = 0;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brfnt") || u.StringTable[i].ToLower().EndsWith(".tpl")) files.Add(u.StringTable[i], u.Data[i]);
						i++;
					}
					i = 0;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brlyt"))
						{
							Layout = new IO.BRLYT(u.Data[i], files);
							break;
						}
						i++;
					}
				}
				else if (openFileDialog1.FileName.ToLower().EndsWith(".wad"))
				{
					byte[] file = File.ReadAllBytes(openFileDialog1.FileName);
					if (libWiiSharp.Lz77.IsLz77Compressed(file))
					{
						file = new libWiiSharp.Lz77().Decompress(file);
					}
					libWiiSharp.WAD w = libWiiSharp.WAD.Load(file);
					libWiiSharp.U8 u = null;
					int i = 0;
					foreach (libWiiSharp.U8_Node n in w.BannerApp.Nodes)
					{
						if (w.BannerApp.StringTable[i].ToLower().EndsWith("banner.bin"))
						{
							u = libWiiSharp.U8.Load(w.BannerApp.Data[i]);
							break;
						}
						i++;
					}

					Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
					i = 0;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brfnt") || u.StringTable[i].ToLower().EndsWith(".tpl")) files.Add(u.StringTable[i], u.Data[i]);
						i++;
					}
					i = 0;
					bool layout = false;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brlyt") && !layout)
						{
							Layout = new IO.BRLYT(u.Data[i], files);
							layout = true;
						}
						else if (u.StringTable[i].ToLower().EndsWith("banner_start.brlan"))
						{
							Animation = new IO.BRLAN(u.Data[i], files);
							FrameNr = 0;
							toolStripButton2.Enabled = true;
						}
						else if (u.StringTable[i].ToLower().EndsWith("banner.brlan"))
						{
							Animation = new IO.BRLAN(u.Data[i], files);
							FrameNr = 0;
							toolStripButton2.Enabled = true;
						}
						i++;
					}
				}
				else if (openFileDialog1.FileName.ToLower().EndsWith(".bnr"))
				{
					byte[] file = File.ReadAllBytes(openFileDialog1.FileName);
					if (libWiiSharp.Lz77.IsLz77Compressed(file))
					{
						file = new libWiiSharp.Lz77().Decompress(file);
					}
					libWiiSharp.U8 w = libWiiSharp.U8.Load(file);
					libWiiSharp.U8 u = null;
					int i = 0;
					foreach (libWiiSharp.U8_Node n in w.Nodes)
					{
						if (w.StringTable[i].ToLower().EndsWith("banner.bin"))
						{
							u = libWiiSharp.U8.Load(w.Data[i]);
							break;
						}
						i++;
					}

					Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
					i = 0;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brfnt") || u.StringTable[i].ToLower().EndsWith(".tpl")) files.Add(u.StringTable[i], u.Data[i]);
						i++;
					}
					i = 0;
					bool layout = false;
					foreach (libWiiSharp.U8_Node n in u.Nodes)
					{
						if (u.StringTable[i].ToLower().EndsWith(".brlyt") && !layout)
						{
							Layout = new IO.BRLYT(u.Data[i], files);
							layout = true;
						}
						else if (u.StringTable[i].ToLower().EndsWith("banner_start.brlan"))
						{
							Animation = new IO.BRLAN(u.Data[i], files);
							FrameNr = 0;
							toolStripButton2.Enabled = true;
						}
						i++;
					}
				}
				propertyGrid1.SelectedObject = null;
				treeView1.BeginUpdate();
				treeView1.Nodes.Clear();
				treeView1.Nodes.Add("Brlyt");
				treeView1.Nodes[0].ImageIndex = 5;
				treeView1.Nodes[0].SelectedImageIndex = 5;
				TreeNode c = treeView1.Nodes[0].Nodes.Add("Txl1");
				c.ImageIndex = 6;
				c.SelectedImageIndex = 6;
				if (Layout.FNL1 != null)
				{
					c = treeView1.Nodes[0].Nodes.Add("Fnl1");
					c.ImageIndex = 7;
					c.SelectedImageIndex = 7;
				}
				c = treeView1.Nodes[0].Nodes.Add("Mat1");
				c.ImageIndex = 4;
				c.SelectedImageIndex = 4;
				foreach (IO.BRLYT.mat1.MAT1Entry m in Layout.MAT1.Entries)
				{
					m.GetTreeNode(c.Nodes);
				}
				Layout.PAN1.GetTreeNodes(treeView1.Nodes[0].Nodes);
				treeView1.EndUpdate();
				Layout.BindTextures();
				simpleOpenGlControl1.Width = (int)Layout.LYT1.Width;
				simpleOpenGlControl1.Height = (int)Layout.LYT1.Height;
				Render();
			}
		}

		private void simpleOpenGlControl1_Resize(object sender, EventArgs e)
		{
			Render();
		}

		private void simpleOpenGlControl1_MouseUp(object sender, MouseEventArgs e)
		{
			if (Layout != null && !toolStripButton2.Checked)
			{
				Render(true, e.Location);
				int raw = Color.FromArgb(pic[2], pic[1], pic[0]).ToArgb();
				raw &= 0xFFFFFF;
				raw--;
				if (raw >= 0)
				{
					int idx = 0;
					IO.BRLYT.pan1 p = Layout.PAN1.GetByID(raw, ref idx);
					TreeNode n = treeView1.Nodes.Find(p.Name, true)[0];
					n.EnsureVisible();
					treeView1.SelectedNode = n;
				}
				else
				{
					treeView1.SelectedNode = null;
					propertyGrid1.SelectedObject = null;
				}
			}
			//else if (toolStripButton2.Checked) MessageBox.Show("Disable animation for picking.");
		}

		private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			//propertyGrid1.SelectedObject = Layout.PAN1.GetByName(e.Node.Text);
		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			if (propertyGrid1.SelectedObject is IO.BRLYT.txt1)
			{
				((IO.BRLYT.txt1)propertyGrid1.SelectedObject).texbind = false;
			}
			else if (propertyGrid1.SelectedObject is IO.BRLYT.mat1.MAT1Entry)
			{
				((IO.BRLYT.mat1.MAT1Entry)propertyGrid1.SelectedObject).GlShader = new IO.Shader(((IO.BRLYT.mat1.MAT1Entry)propertyGrid1.SelectedObject), ((IO.BRLYT.mat1.MAT1Entry)propertyGrid1.SelectedObject).GlShader.Textures);
				((IO.BRLYT.mat1.MAT1Entry)propertyGrid1.SelectedObject).GlShader.Compile();
			}
			//if(e.ChangedItem.Label == "Name")
			//{
			//treeView1.SelectedNode.Text = (String)e.ChangedItem.Value;
			//}
			Render();
		}

		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (((String)e.Node.Tag) != "mat1")
			{
				propertyGrid1.SelectedObject = Layout.PAN1.GetByName(e.Node.Text);
			}
			else
			{
				foreach (IO.BRLYT.mat1.MAT1Entry m in Layout.MAT1.Entries)
				{
					if (m.Name == e.Node.Text) { propertyGrid1.SelectedObject = m; break; }
				}
			}
		}

		private void menuItem8_Click(object sender, EventArgs e)
		{
			UI.ColorPicker c = new UI.ColorPicker(Color.Red);
			c.ShowDialog();
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& openFileDialog2.FileName.Length > 0)
			{
				Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
				if (Directory.Exists(Path.GetDirectoryName(openFileDialog1.FileName).Replace("blyt", "timg")))
				{
					foreach (FileInfo f in new DirectoryInfo(Path.GetDirectoryName(openFileDialog1.FileName).Replace("blyt", "timg")).GetFiles())
					{
						files.Add(f.Name, File.ReadAllBytes(f.FullName));
					}
				}
				Animation = new IO.BRLAN(File.ReadAllBytes(openFileDialog2.FileName), files);
				treeView1.BeginUpdate();
				if (treeView1.Nodes.Count == 1)
				{
					treeView1.Nodes.Add("Brlan");
					treeView1.Nodes[1].ImageIndex = 8;
					treeView1.Nodes[1].SelectedImageIndex = 8;
				}
				treeView1.Nodes[1].Nodes.Clear();
				treeView1.Nodes[1].Nodes.Add("Pai1");
				treeView1.Nodes[1].Nodes[0].ImageIndex = 9;
				treeView1.Nodes[1].Nodes[0].SelectedImageIndex = 9;
				treeView1.EndUpdate();
				FrameNr = 0;
				toolStripButton2.Enabled = true;
				Render();
			}
		}
		public IO.BRLAN Animation = null;
		public float FrameNr = 0;

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			if (toolStripButton2.Checked)
			{
				FrameNr = 0;
				timer1.Enabled = true;
			}
			else
			{
				timer1.Enabled = false;
				Render();
			}
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			FrameNr += 0.001f * 60f;
			if (FrameNr >= Animation.PAI1.NrFrames) FrameNr = 0;
			System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
			w.Start();
			Render();
			w.Stop();
			FrameNr += (float)(w.Elapsed.TotalSeconds * 60f);
		}

		private void saveToolStripButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.FileName.EndsWith(".brlyt"))
			{
				File.Create(openFileDialog1.FileName).Close();
				File.WriteAllBytes(openFileDialog1.FileName, Layout.Write());
			}
		}

		private void menuItem9_Click(object sender, EventArgs e)
		{
			if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& openFileDialog2.FileName.Length > 0)
			{
				new UI.TPLViewer(openFileDialog2.FileName).ShowDialog();
			}
		}

		private void menuItem10_Click(object sender, EventArgs e)
		{
			if (openFileDialog3.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& openFileDialog3.FileName.Length > 0)
			{
				UI.THP t = new UI.THP(new IO.Misc.THP(File.ReadAllBytes(openFileDialog3.FileName)));
				t.ShowDialog();
				t.Dispose();
			}
		}

		private void menuItem12_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void menuItem14_Click(object sender, EventArgs e)
		{
			if (openFileDialog4.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& openFileDialog4.FileName.Length > 0)
			{
				if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK
				&& saveFileDialog1.FileName.Length > 0)
				{
					IO.Misc.THP t = new IO.Misc.THP(openFileDialog4.FileName);
					t.Write(saveFileDialog1.FileName);
					//File.Create(saveFileDialog1.FileName).Close();
					//File.WriteAllBytes(saveFileDialog1.FileName, t.Write());
				}
			}
		}
	}
}
