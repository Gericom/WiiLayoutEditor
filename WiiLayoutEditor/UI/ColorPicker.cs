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
	public partial class ColorPicker :Form
	{
		public ColorPicker()
		{
			InitializeComponent();
		}
		public ColorPicker(Color c)
		{
			InitializeComponent();
			trackBar1.Value = c.A;
			double h;
			double s;
			double l;
			IO.Util.RGB2HSL(colorViewer1.ForeColor = c, out h, out s, out l);
			if (h == 1) h = 0;
			s *= l < .5 ? l : 1 - l;
			colorSliderControl1.Hue = h;
			colorSliderControl1.Invalidate();
			if (l + s != 0) colorPickerControl1.Saturation = 2 * s / (l + s);
			else colorPickerControl1.Saturation = 0;
			colorPickerControl1.Value = l + s;
			colorPickerControl1.Hue = h;
		}

		private void ColorPicker_Load(object sender, EventArgs e)
		{

		}

		private void colorSliderControl1_HueChanged(double Hue)
		{
			colorPickerControl1.Hue = Hue;
			colorViewer1.ForeColor= Color.FromArgb((byte)trackBar1.Value,colorPickerControl1.GetSelectedColor());
		}

		private void colorPickerControl1_ColorChanged(Color c)
		{
			colorViewer1.ForeColor = Color.FromArgb((byte)trackBar1.Value, c);
		}

		public Color GetColor()
		{
			return colorViewer1.ForeColor;
		}

		private void trackBar1_Scroll(object sender, EventArgs e)
		{
			//numericUpDown4.Value = trackBar1.Value;
			colorViewer1.ForeColor = Color.FromArgb((byte)trackBar1.Value, colorViewer1.ForeColor);
		}
		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (!ignore)
			{
				//panel1.BackColor = Color.FromArgb(trackBar1.Value = (int)numericUpDown4.Value, (int)numericUpDown1.Value,(int)numericUpDown2.Value,(int)numericUpDown3.Value);
				double h;
				double s;
				double l;
				ignore2 = true;
				IO.Util.RGB2HSL(colorViewer1.ForeColor = Color.FromArgb(trackBar1.Value = (int)numericUpDown4.Value, (int)numericUpDown1.Value, (int)numericUpDown2.Value, (int)numericUpDown3.Value), out h, out s, out l);
				if (h == 1) h = 0;
				s *= l < .5 ? l : 1 - l;
				colorSliderControl1.Hue = h;
				colorSliderControl1.Invalidate();
				if (l + s != 0)colorPickerControl1.Saturation = 2 * s / (l + s);
				else colorPickerControl1.Saturation = 0;
				colorPickerControl1.Value = l + s;
				colorPickerControl1.Hue = h;
			}
			else
			{
				ignore = false;
			}
		}
		bool ignore = false;
		bool ignore2 = false;
		private void panel1_BackColorChanged(object sender, EventArgs e)
		{
			if (!ignore2)
			{
				ignore = true;
				numericUpDown1.Value = colorViewer1.ForeColor.R;
				ignore = true;
				numericUpDown2.Value = colorViewer1.ForeColor.G;
				ignore = true;
				numericUpDown3.Value = colorViewer1.ForeColor.B;
				ignore = true;
				numericUpDown4.Value = colorViewer1.ForeColor.A;
			}
			else
			{
				ignore2 = false;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Close();
		}
	}
}
