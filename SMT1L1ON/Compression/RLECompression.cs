using System;
using System.IO;
using System.Linq;
using SMT1L1ON.Common.IO;

namespace SMT1L1ON.Compression
{
    /// <summary>
    /// Compression and decompression utilities for files that use RLE compression.
    /// </summary>
    public static class RLECompression
    {
        private const int REPEAT_BYTE_COMMAND_SIZE = 0x3;

        /// <summary>
        /// Decompresses the given input stream and returns a stream containing the decompressed data.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Stream Decompress( Stream input )
        {
            var output = new MemoryStream();

            var decompressedLength = input.ReadByte() | input.ReadByte() << 8 | input.ReadByte() << 16 | input.ReadByte() << 24;
            var repeatByteCommand = input.ReadByte();
            input.Seek( -1, SeekOrigin.Current );

            while ( output.Length < decompressedLength )
            {
                var b = input.ReadByte();
                if ( b == repeatByteCommand )
                {
                    // Repeat byte literal
                    var value = input.ReadByte();
                    var count = input.ReadByte();
                    for ( int i = 0; i < count; i++ )
                    {
                        output.WriteByte( ( byte )value );
                    }           
                }
                else if ( b == 0xFF )
                {
                    break;
                }
                else
                {
                    // Copy byte literal
                    output.WriteByte( ( byte )b );
                }
            }

            output.Position = 0;
            return output;
        }

        /// <summary>
        /// Compresses the given input stream and returns a stream containing the compressed data.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Stream Compress( Stream input )
        {
            var output = new MemoryStream();

            // Find suitable command byte
            byte[] byteUseCountLookup = new byte[255];
            while ( !input.EndOfStream() )
                ++byteUseCountLookup[ input.ReadByte() ];

            int minUseCount = byteUseCountLookup.Min( x => x );
            byte repeatByteCommand = 0;

            for ( int i = 0; i < byteUseCountLookup.Length; i++ )
            {
                if ( byteUseCountLookup[ i ] == minUseCount )
                {
                    repeatByteCommand = ( byte )i;
                    break;
                }                
            }

            // Write decompressed length
            output.WriteByte( ( byte ) ( input.Length & 0xFF ) );
            output.WriteByte( ( byte )( ( input.Length >> 8 ) & 0xFF ) );
            output.WriteByte( ( byte )( ( input.Length >> 16 ) & 0xFF ) );
            output.WriteByte( ( byte )( ( input.Length >> 24 ) & 0xFF ) );

            input.Position = 0;
            bool isFirst = true;
            while ( !input.EndOfStream() )
            {
                var value = input.ReadByte();
                var pos = input.Position;

                int repeatCount = 1;
                if ( value != repeatByteCommand )
                {
                    // Check for repeating sequence if value is not equal to the repeat byte value
                    while ( !input.EndOfStream() && input.ReadByte() == value )
                        ++repeatCount;

                    // Seek back because we read one byte that isn't equal to the current value
                    --input.Position;
                }

                if ( !isFirst && repeatCount <= REPEAT_BYTE_COMMAND_SIZE )
                {
                    // Just output it the way it is
                    input.Position = pos;
                    output.WriteByte( ( byte )value );
                }
                else
                {
                    do
                    {
                        // Clamp the number of repeated bytes to the max, which is 0xFF
                        int curRepeatCount = Math.Min( repeatCount, byte.MaxValue );

                        // Output repeat byte literal sequence
                        output.WriteByte( repeatByteCommand );
                        output.WriteByte( ( byte )value );
                        output.WriteByte( ( byte )curRepeatCount );

                        repeatCount -= curRepeatCount;
                    }
                    while ( repeatCount > REPEAT_BYTE_COMMAND_SIZE );
                }

                isFirst = false;
            }

            output.WriteByte( 0xFF );

            output.Position = 0;
            return output;
        }
    }
}
