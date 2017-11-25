﻿// CTools library - Library functions for CTools
// Copyright (C) 2010 Chadderz

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Text;

namespace System.IO
{
    public sealed class EndianBinaryWriter : IDisposable
    {
        private bool disposed;
        private byte[] buffer;

        public Stream BaseStream { get; private set; }
        public Endianness Endianness { get; set; }
        public Endianness SystemEndianness { get { return BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian; } }

        private bool Reverse { get { return SystemEndianness != Endianness; } }

        public EndianBinaryWriter(Stream baseStream)
            : this(baseStream, Endianness.BigEndian)
        { }

        public EndianBinaryWriter(Stream baseStream, Endianness endianness)
        {
            if (baseStream == null) throw new ArgumentNullException("baseStream");
            if (!baseStream.CanWrite) throw new ArgumentException("baseStream");

            BaseStream = baseStream;
            Endianness = endianness;
        }

        ~EndianBinaryWriter()
        {
            Dispose(false);
        }

        private void WriteBuffer(int bytes, int stride)
        {
            if (Reverse)
                for (int i = 0; i < bytes; i += stride)
                {
                    Array.Reverse(buffer, i, stride);
                }

            BaseStream.Write(buffer, 0, bytes);
        }

        private void CreateBuffer(int size)
        {
            if (buffer == null || buffer.Length < size)
                buffer = new byte[size];
        }

        public void Write(byte value)
        {
            CreateBuffer(1);
            buffer[0] = value;
            WriteBuffer(1, 1);
        }

        public void Write(byte[] value, int offset, int count)
        {
            CreateBuffer(count);
            Array.Copy(value, offset, buffer, 0, count);
            WriteBuffer(count, 1);
        }

        public void Write(sbyte value)
        {
            CreateBuffer(1);
            unchecked
            {
                buffer[0] = (byte)value;
            }
            WriteBuffer(1, 1);
        }

        public void Write(sbyte[] value, int offset, int count)
        {
            CreateBuffer(count);

            unchecked
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[i] = (byte)value[i + offset];
                }
            }

            WriteBuffer(count, 1);
        }

        public void Write(char value, Encoding encoding)
        {
            int size;

            size = GetEncodingSize(encoding);
            CreateBuffer(size);
            Array.Copy(encoding.GetBytes(new string(value, 1)), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(char[] value, int offset, int count, Encoding encoding)
        {
            int size;

            size = GetEncodingSize(encoding);
            CreateBuffer(size * count);
            Array.Copy(encoding.GetBytes(value, offset, count), 0, buffer, 0, count * size);
            WriteBuffer(size * count, size);
        }

        private static int GetEncodingSize(Encoding encoding)
        {
            if (encoding == Encoding.UTF8 || encoding == Encoding.ASCII)
                return 1;
            else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
                return 2;

            return 1;
        }

        public void Write(string value,Encoding encoding,  bool nullTerminated)
        {
            Write(value.ToCharArray(), 0, value.Length, encoding);
            if (nullTerminated)
                Write('\0', encoding);
        }

        public void Write(double value)
        {
            const int size = sizeof(double);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(double[] value, int offset, int count)
        {
            const int size = sizeof(double);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(Single value)
        {
            const int size = sizeof(Single);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(Single[] value, int offset, int count)
        {
            const int size = sizeof(Single);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(Int32 value)
        {
            const int size = sizeof(Int32);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(Int32[] value, int offset, int count)
        {
            const int size = sizeof(Int32);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(Int64 value)
        {
            const int size = sizeof(Int64);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(Int64[] value, int offset, int count)
        {
            const int size = sizeof(Int64);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(Int16 value)
        {
            const int size = sizeof(Int16);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(Int16[] value, int offset, int count)
        {
            const int size = sizeof(Int16);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(UInt16 value)
        {
            const int size = sizeof(UInt16);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(UInt16[] value, int offset, int count)
        {
            const int size = sizeof(UInt16);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(UInt32 value)
        {
            const int size = sizeof(UInt32);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(UInt32[] value, int offset, int count)
        {
            const int size = sizeof(UInt32);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void Write(UInt64 value)
        {
            const int size = sizeof(UInt64);

            CreateBuffer(size);
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, 0, size);
            WriteBuffer(size, size);
        }

        public void Write(UInt64[] value, int offset, int count)
        {
            const int size = sizeof(UInt64);

            CreateBuffer(size * count);
            for (int i = 0; i < count; i++)
            {
                Array.Copy(BitConverter.GetBytes(value[i + offset]), 0, buffer, i * size, size);
            }

            WriteBuffer(size * count, size);
        }

        public void WritePadding(int multiple, byte padding)
        {
            int length = (int)(BaseStream.Position % multiple);

            if (length != 0)
                while (length != multiple)
                {
                    BaseStream.WriteByte(padding);
                    length++;
                }
        }

        public void WritePadding(int multiple, byte padding, long from, int offset)
        {
            int length = (int)((BaseStream.Position - from) % multiple);
            length = (length + offset) % multiple;

            if (length != 0)
                while (length != multiple)
                {
                    BaseStream.WriteByte(padding);
                    length++;
                }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (BaseStream != null)
                    {
                        BaseStream.Close();
                    }
                }

                buffer = null;

                disposed = true;
            }
        }
    }
}
