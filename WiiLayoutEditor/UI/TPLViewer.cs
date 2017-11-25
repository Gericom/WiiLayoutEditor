using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WiiLayoutEditor.UI
{
	public partial class TPLViewer : Form
	{
		IO.TPL f;
		public TPLViewer(string filename)
		{
			f = new IO.TPL(System.IO.File.ReadAllBytes(filename));
			InitializeComponent();
		}

		private void TPLViewer_Load(object sender, EventArgs e)
		{
			pictureBox1.Image = libWiiSharp.TPL.rgbaToImage(f.Images[0].GetData(f.Palettes[0]), f.Images[0].Width, f.Images[0].Height);
		}
	}
}
