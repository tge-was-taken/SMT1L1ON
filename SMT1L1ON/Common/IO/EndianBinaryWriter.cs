﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMT1L1ON.Common.IO
{
    public class EndianBinaryWriter : BinaryWriter
    {
        private StringBuilder   mStringBuilder;
        private Endianness      mEndianness;
        private bool            mSwap;
        private Encoding        mEncoding;
        private Queue<long>     mPosQueue;

        public Endianness Endianness
        {
            get { return mEndianness; }
            set
            {
                if (value != EndiannessUtils.SystemEndianness)
                    mSwap = true;
                else
                    mSwap = false;

                mEndianness = value;
            }
        }

        public bool EndiannessNeedsSwapping
        {
            get { return mSwap; }
        }

        public long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public long BaseStreamLength
        {
            get { return BaseStream.Length; }
        }

        public EndianBinaryWriter(Stream input, Endianness endianness)
            : base(input)
        {
            Init(Encoding.Default, endianness);
        }

        public EndianBinaryWriter(Stream input, Encoding encoding, Endianness endianness)
            : base(input, encoding)
        {
            Init(encoding, endianness);
        }

        public EndianBinaryWriter(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness)
            : base(input, encoding, leaveOpen)
        {
            Init(encoding, endianness);
        }

        public EndianBinaryWriter(Stream input, bool leaveOpen, Endianness endianness) : this(input, Encoding.Default, leaveOpen, endianness) { }

        private void Init(Encoding encoding, Endianness endianness)
        {
            Endianness = endianness;
            mStringBuilder = new StringBuilder();
            mEncoding = encoding;
            mPosQueue = new Queue<long>();
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            BaseStream.Seek(offset, origin);
        }

        public void SeekBegin(long offset)
        {
            BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public void SeekCurrent(long offset)
        {
            BaseStream.Seek(offset, SeekOrigin.Current);
        }

        public void SeekEnd(long offset)
        {
            BaseStream.Seek(offset, SeekOrigin.End);
        }

        public void EnqueuePosition()
        {
            mPosQueue.Enqueue(Position);
        }

        public long PeekEnqueuedPosition()
        {
            return mPosQueue.Peek();
        }

        public long DequeuePosition()
        {
            return mPosQueue.Dequeue();
        }

        public void SeekBeginToDequeuedPosition()
        {
            SeekBegin(DequeuePosition());
        }

        public new void Write(byte[] values)
        {
            base.Write(values);
        }

        public void Write(sbyte[] values)
        {
            for (int i = 0; i < values.Length; i++)
                Write(values[i]);
        }

        public void Write(bool[] values)
        {
            for (int i = 0; i < values.Length; i++)
                Write(values[i]);
        }

        public override void Write(short value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(short[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(ushort value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(ushort[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(int value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(int[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(uint value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(uint[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(long value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(long[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(ulong value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(ulong[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(float value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(float[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public override void Write(decimal value)
        {
            base.Write(mSwap ? EndiannessUtils.Swap(value) : value);
        }

        public void Write(decimal[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public void Write(string value, StringBinaryFormat format, int fixedLength = -1)
        {
            switch (format)
            {
                case StringBinaryFormat.NullTerminated:
                    {
                        Write(mEncoding.GetBytes(value));

                        for (int i = 0; i < mEncoding.GetMaxByteCount(1); i++)
                            Write((byte)0);
                    }
                    break;
                case StringBinaryFormat.FixedLength:
                    {
                        if (fixedLength == -1)
                        {
                            throw new ArgumentException("Fixed length must be provided if format is set to fixed length", nameof(fixedLength));
                        }

                        var bytes = mEncoding.GetBytes(value);
                        if (bytes.Length > fixedLength)
                        {
                            throw new ArgumentException("Provided string is longer than fixed length", nameof(value));
                        }

                        Write(bytes);
                        fixedLength -= bytes.Length;

                        while (fixedLength-- > 0)
                            Write((byte)0);
                    }
                    break;

                case StringBinaryFormat.PrefixedLength8:
                    {
                        Write((byte)value.Length);
                        Write(mEncoding.GetBytes(value));
                    }
                    break;

                case StringBinaryFormat.PrefixedLength16:
                    {
                        Write((ushort)value.Length);
                        Write(mEncoding.GetBytes(value));
                    }
                    break;

                case StringBinaryFormat.PrefixedLength32:
                    {
                        Write((uint)value.Length);
                        Write(mEncoding.GetBytes(value));
                    }
                    break;

                default:
                    throw new ArgumentException("Invalid format specified", nameof(format));
            }
        }

        public void WritePadding( int count )
        {
            for ( int i = 0; i < count / 8; i++ )
                Write( 0L );

            for ( int i = 0; i < count % 8; i++ )
                Write( ( byte ) 0 );
        }

        public void WriteAlignmentPadding( int alignment )
        {
            WritePadding( AlignmentUtils.GetAlignedDifference( Position, alignment ) );
        }
    }
}
