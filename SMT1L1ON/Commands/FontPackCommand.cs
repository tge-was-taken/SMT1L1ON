using System;
using System.IO;
using SMT1L1ON.Common.IO;
using SMT1L1ON.Compression;
using SMT1L1ON.Text.Font;

namespace SMT1L1ON.Commands
{
    [Command( "fontpack" )]
    internal static class FontPackCommand
    {
        public static bool Execute( string inPath )
        {
            if ( !Directory.Exists( inPath ) )
            {
                Console.WriteLine( "Specified directory doesn't exist" );
                return false;
            }

            // Build font pack
            var fontPack = new FontPack();

            int i = 0;
            foreach ( var file in Directory.EnumerateFiles(inPath, "font*.tim") )
            {
                var fontStream = new MemoryStream( File.ReadAllBytes( file ) );
                fontPack.Fonts[i++] = fontStream;
            }

            var directory = Path.GetDirectoryName( inPath );
            var outFileName = Path.Combine( directory, "0010.bin" );

            var fontPackStream = RLECompression.Compress( fontPack.Save() );
            fontPackStream.WriteToFile( outFileName );

            return true;
        }
    }
}