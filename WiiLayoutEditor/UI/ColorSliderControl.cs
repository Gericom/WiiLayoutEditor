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
	public partial class ColorSliderControl : UserControl
	{
		public delegate void HueChangedEventhandler(double Hue);
		public event HueChangedEventhandler HueChanged;
		public ColorSliderControl()
		{
			InitializeComponent();
		}

		private void ColorSliderControl_Load(object sender, EventArgs e)
		{

		}

		private void ColorSliderControl_Paint(object sender, PaintEventArgs e)
		{
			for (int y = 0; y < Height; y++)
			{
				e.Graphics.DrawLine(new Pen(IO.Util.HSL2RGB((y==0?0:(Height - y) / (float)Height), 1, 0.5f)), new Point(0, y), new Point(Width, y));
			}
			e.Graphics.DrawLine(new Pen(Color.Black, 1), 0, (int)(Height - Hue * Height), Width, (int)(Height - Hue * Height));
		}
		public double Hue = 0;
		private void ColorSliderControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				int y = 0;
				if (e.Y < 0) y = 0;
				else if (e.Y > Height) y = Height;
				else y = e.Y;
				Hue = (Height-y) / (float)Height;
				if (Hue == 1) Hue = 0;
				Invalidate();
				if (HueChanged != null)
				{
					HueChanged.Invoke(Hue);
				}
			}
		}


	}
}
