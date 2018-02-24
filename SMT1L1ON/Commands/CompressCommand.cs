using System;
using System.IO;
using SMT1L1ON.Common.IO;
using SMT1L1ON.Compression;

namespace SMT1L1ON.Commands
{
    [Command( "com" )]
    internal static class CompressCommand
    {
        public static bool Execute( string inPath )
        {
            if ( !File.Exists( inPath ) )
            {
                Console.WriteLine( "Specified file doesn't exist" );
                return false;
            }

            // Compress
            Stream compressed;
            using ( var inStream = File.OpenRead( inPath ) )
                compressed = RLECompression.Compress( inStream );

            compressed.WriteToFile( inPath + ".com" );

            return true;
        }
    }
}