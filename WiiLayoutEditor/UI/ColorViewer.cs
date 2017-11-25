using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WiiLayoutEditor.UI
{
	public partial class ColorViewer : UserControl
	{
		public ColorViewer()
		{
			InitializeComponent();
		}

		private void ColorViewer_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.FillRectangle(new SolidBrush(ForeColor), e.ClipRectangle);
		}
	}
}
