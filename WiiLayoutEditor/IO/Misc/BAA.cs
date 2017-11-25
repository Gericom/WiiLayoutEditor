using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WiiLayoutEditor.IO.Misc
{
	public class BAA
	{
		public BAA(byte[] file)
		{
			EndianBinaryReader er = new EndianBinaryReader(new MemoryStream(file), Endianness.BigEndian);
			List<AudioArchive> a = new List<AudioArchive>();
			bool OK;
			while (er.BaseStream.Position != er.BaseStream.Length)
			{
				a.Add(new AudioArchive(er, out OK));
				if (!OK) { System.Windows.Forms.MessageBox.Show("Error"); goto end; }
			}
		end:
			er.Close();
		}
		public AudioArchive[] AudioArchives;
		public class AudioArchive
		{
			public const String StartSignature = "AA_<";
			public AudioArchive(EndianBinaryReader er, out bool OK)
			{
				OK = true;
			}
			public String Type;
			public class BAAFileEntry
			{
				public UInt32 StartOffset;
				public byte[] Data;
			}
			public class BAABSTFileEntry : BAAFileEntry
			{
				public UInt32 EndOffset;
			}
			public class BAABSTNFileEntry : BAAFileEntry
			{
				public UInt32 EndOffset;
			}
			public class BAAWSFileEntry : BAAFileEntry
			{
				public UInt32 ID;
				public UInt32 Unknown;
			}
			public class BAABNKFileEntry : BAAFileEntry
			{
				public UInt32 ID;
			}
			public class BAABFCAFileEntry : BAAFileEntry
			{

			}
			public class BAABSFTFileEntry : BAAFileEntry
			{

			}
			public class BAABSCFileEntry : BAAFileEntry
			{
				public UInt32 EndOffset;
			}
			public class BAABMSFileEntry : BAAFileEntry
			{
				public UInt32 ID;
				public UInt32 EndOffset;
			}
			public class BAABAACFileEntry : BAAFileEntry
			{

			}
		}
	}
}
