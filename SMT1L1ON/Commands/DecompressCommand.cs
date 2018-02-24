using System;
using System.IO;
using SMT1L1ON.Common.IO;
using SMT1L1ON.Compression;

namespace SMT1L1ON.Commands
{
    [Command( "dec" )]
    internal static class DecompressCommand
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

            decompressed.WriteToFile( inPath + ".dec" );

            return true;
        }
    }
}