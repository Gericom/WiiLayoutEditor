using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.OpenGl;
using System.Drawing.Imaging;
using System.Drawing;

namespace WiiLayoutEditor.IO
{
	public class Util
	{
		public static int AddPadding(int value, int padding)
		{
			if (value % padding != 0)
			{
				value = value + (padding - (value % padding));
			}

			return value;
		}
		public static float MixColors(params float[] c)
		{
			float a = c[0];
			for (int i = 1; i < c.Length; i++)
			{
				a *= c[i];
			}
			for (int i = 1; i < c.Length; i++)
			{
				a /= 255f;
			}
			return a / 255f;
		}
		public static Bitmap ScreenShot(Tao.Platform.Windows.SimpleOpenGlControl simpleOpenGlControl1)
		{
			Bitmap b = new Bitmap(simpleOpenGlControl1.Width, simpleOpenGlControl1.Height);
			BitmapData bd = b.LockBits(new Rectangle(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			Gl.glReadPixels(0, 0, simpleOpenGlControl1.Width, simpleOpenGlControl1.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bd.Scan0);
			b.UnlockBits(bd);
			b.RotateFlip(RotateFlipType.RotateNoneFlipY);
			return b;
		}
		// Given H,S,L in range of 0-1

		// Returns a Color (RGB struct) in range of 0-255

		public static Color HSL2RGB(double h, double sl, double l)
		{

			double v;

			double r, g, b;



			r = l;   // default to gray

			g = l;

			b = l;

			v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);

			if (v > 0)
			{

				double m;

				double sv;

				int sextant;

				double fract, vsf, mid1, mid2;



				m = l + l - v;

				sv = (v - m) / v;

				h *= 6.0;

				sextant = (int)h;

				fract = h - sextant;

				vsf = v * sv * fract;

				mid1 = m + vsf;

				mid2 = v - vsf;

				switch (sextant)
				{

					case 0:

						r = v;

						g = mid1;

						b = m;

						break;

					case 1:

						r = mid2;

						g = v;

						b = m;

						break;

					case 2:

						r = m;

						g = v;

						b = mid1;

						break;

					case 3:

						r = m;

						g = mid2;

						b = v;

						break;

					case 4:

						r = mid1;

						g = m;

						b = v;

						break;

					case 5:

						r = v;

						g = m;

						b = mid2;

						break;

				}

			}

			return Color.FromArgb(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
		}
		public static void RGB2HSL(Color rgb, out double h, out double s, out double l)
		{

			double r = rgb.R / 255.0;

			double g = rgb.G / 255.0;

			double b = rgb.B / 255.0;

			double v;

			double m;

			double vm;

			double r2, g2, b2;



			h = 0; // default to black

			s = 0;

			l = 0;

			v = Math.Max(r, g);

			v = Math.Max(v, b);

			m = Math.Min(r, g);

			m = Math.Min(m, b);

			l = (m + v) / 2.0;

			if (l <= 0.0)
			{

				return;

			}

			vm = v - m;

			s = vm;

			if (s > 0.0)
			{

				s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);

			}

			else
			{

				return;

			}

			r2 = (v - r) / vm;

			g2 = (v - g) / vm;

			b2 = (v - b) / vm;

			if (r == v)
			{

				h = (g == m ? 5.0 + b2 : 1.0 - g2);

			}

			else if (g == v)
			{

				h = (b == m ? 1.0 + r2 : 3.0 - b2);

			}

			else
			{

				h = (r == m ? 3.0 + g2 : 5.0 - r2);

			}

			h /= 6.0;

		}




	}
}
