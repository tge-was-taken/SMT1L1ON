using System;
using System.IO;
using SMT1L1ON.Common.IO;
using SMT1L1ON.Compression;
using SMT1L1ON.Text.Font;

namespace SMT1L1ON.Commands
{
    [Command("fontunpack")]
    internal static class FontUnpackCommand
    {
        public static bool Execute( string inPath )
        {
            if ( !File.Exists( inPath ) )
            {
                Console.WriteLine( "Specified file doesn't exist" );
                return false;
            }

            // Decompress
            Stream decompressed;
            using ( var inStream = File.OpenRead( inPath ) )
                decompressed = RLECompression.Decompress( inStream );

            // Load font pack
            var fontPack = new FontPack( decompressed, true );

            // Extract fonts
            var directory = Path.GetDirectoryName( inPath );
            for ( var i = 0; i < fontPack.Fonts.Length; i++ )
            {
                var font = fontPack.Fonts[ i ];
                using ( var outStream = FileUtils.Create( $"{directory}\\fonts\\font{i}.tim" ) )
                    font.CopyToFully( outStream );
            }

            return true;
        }
    }
}