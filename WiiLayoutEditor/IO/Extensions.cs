using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace System
{
	public static class Extensions
	{
		public static Color ReadColor16(this EndianBinaryReader er)
		{
			int r = er.ReadInt16();
			int g = er.ReadInt16();
			int b = er.ReadInt16();
			int a = er.ReadInt16();
			return Color.FromArgb(a, r, g, b);
		}
		public static Color ReadColor8(this EndianBinaryReader er)
		{
			int r = er.ReadByte();
			int g = er.ReadByte();
			int b = er.ReadByte();
			int a = er.ReadByte();
			return Color.FromArgb(a, r, g, b);
		}

		public static void Write(this EndianBinaryWriter er, Color c, bool Color16 = false)
		{
			if (Color16)
			{
				er.Write((Int16)c.R);
				er.Write((Int16)c.G);
				er.Write((Int16)c.B);
				er.Write((Int16)c.A);
			}
			else
			{
				er.Write((Byte)c.R);
				er.Write((Byte)c.G);
				er.Write((Byte)c.B);
				er.Write((Byte)c.A);
			}
		}
	}
}
