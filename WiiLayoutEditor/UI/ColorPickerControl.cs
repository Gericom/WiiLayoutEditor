using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WiiLayoutEditor.UI
{
	public partial class ColorPickerControl : UserControl
	{
		public delegate void ColorChangedEventhandler(Color c);
		public event ColorChangedEventhandler ColorChanged;
		private double hue = 0;
		public Double Hue
		{
			get { return hue; }
			set
			{
				hue = value; 
				Invalidate();
			}
		}

		public Color GetSelectedColor()
		{
			double sat = Saturation;
			double hue2 = Hue;
			return IO.Util.HSL2RGB(hue2, sat * Value / ((hue2 = (2 - sat) * Value) < 1 ? hue2 : 2 - hue2), hue2 / 2);
		}
		public ColorPickerControl()
		{
			InitializeComponent();
		}

		private void ColorPickerControl_Load(object sender, EventArgs e)
		{

		}

		private void ColorPickerControl_Paint(object sender, PaintEventArgs e)
		{
			Color c = IO.Util.HSL2RGB(Hue, 1, 0.5f);
			for (int y = 0; y < Height; y++)
			{
				int y2 = Height - y;
				LinearGradientBrush b = new LinearGradientBrush(new Point(0, y), new Point(Height, y),
					Color.FromArgb((int)(y2 / (float)Height * 255), (int)(y2 / (float)Height * 255), (int)(y2 / (float)Height * 255))
					, Color.FromArgb((int)(y2 / (float)Height * c.R), (int)(y2 / (float)Height * c.G), (int)(y2 / (float)Height * c.B)));
				e.Graphics.FillRectangle(b, new Rectangle(0, y, Height, 1));
			}

			e.Graphics.DrawEllipse(new Pen((Saturation < 0.2 && Value > 0.8 ? Color.Black : Color.White)), new Rectangle((int)(Saturation * Height) - 4, (int)(Height - Value * Height) - 4, 8, 8));
		}

		private void ColorPickerControl_Resize(object sender, EventArgs e)
		{
			//if (Height > Width) Width = Height;
			//else Height = Width;
		}
		public double Saturation;
		public double Value;
		private void ColorPickerControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				int x = 0;
				int y = 0;
				if (e.X < 0) x = 0;
				else if (e.X > Height) x = Height;
				else x = e.X;
				if (e.Y < 0) y = 0;
				else if (e.Y> Height) y= Height;
				else y = e.Y;
				//Saturation = x / (float)Height;
				//Luminisity = (Height-y) / (float)Height;
				Saturation = x / (float)Height;
				Value = (Height - y) / (float)Height;
				Invalidate();
				if (ColorChanged != null)
				{
					ColorChanged.Invoke(GetSelectedColor());
				}
			}
		}
	}
}
