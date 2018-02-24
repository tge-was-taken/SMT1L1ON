using System.IO;
using SMT1L1ON.Common.IO;

namespace SMT1L1ON.Text.Font
{
    public class FontPack
    {
        private const int HEADER_SIZE = 9;

        // seems to actually by 9, but has to be subtracted because the decompressor strips 
        // the file length at the start of the file
        private const int OFFSET_BASE = 9 - 4; 

        /// <summary>
        /// Number of fonts in a font pack.
        /// </summary>
        public const int FONT_COUNT = 3;

        /// <summary>
        /// Meaning is unknown.
        /// </summary>
        public byte[] HeaderBytes { get; }

        /// <summary>
        /// Array of fonts in the pack file.
        /// </summary>
        public Stream[] Fonts { get; }

        public FontPack()
        {
            HeaderBytes = new byte[HEADER_SIZE] { 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x00, 0x03 };
            Fonts = new Stream[FONT_COUNT];
        }

        public FontPack( Stream stream, bool ownsStream )
        {
            using ( var reader = new EndianBinaryReader( stream, ownsStream, Endianness.LittleEndian ) )
            {
                // Read header
                HeaderBytes = reader.ReadBytes( HEADER_SIZE );

                // Read fonts
                Fonts = new Stream[FONT_COUNT];
                var offsets = reader.ReadInt32s( FONT_COUNT + 1 );

                for ( int i = 0; i < Fonts.Length; i++ )
                {
                    reader.SeekBegin( OFFSET_BASE + offsets[ i ] );
                    var length = ( int ) ( offsets[ i + 1 ] - offsets[ i ] );
                    Fonts[ i ] = new MemoryStream( reader.ReadBytes( length ) );
                }
            }
        }

        public void Save( Stream stream )
        {
            using ( var writer = new EndianBinaryWriter( stream, true, Endianness.LittleEndian ) )
            {
                // Write header
                writer.Write( HeaderBytes );

                // Write offsets
                var curOffset = ( writer.BaseStream.Position + ( (FONT_COUNT + 1) * 4 ) ) - OFFSET_BASE;
                foreach ( var font in Fonts )
                {
                    writer.Write( ( int ) curOffset );
                    curOffset += font.Length;
                }

                writer.Write( ( int ) curOffset );

                foreach ( var font in Fonts )
                {
                    font.CopyToFully( writer.BaseStream );
                }
            }
        }

        public Stream Save()
        {
            var stream = new MemoryStream();
            Save( stream );
            stream.Position = 0;
            return stream;
        }

        public void Save( string filepath )
        {
            using ( var stream = FileUtils.Create( filepath ) )
                Save( stream );
        }
    }
}
