using System.IO;

namespace SMT1L1ON.Common.IO
{
    public static class StreamExtensions
    {
        public static void CopyToFully( this Stream @this, Stream stream )
        {
            @this.Position = 0;
            @this.CopyTo( stream );
        }

        public static bool EndOfStream( this Stream @this )
        {
            return @this.Position == @this.Length;
        }

        public static void WriteToFile( this Stream @this, string filepath )
        {
            using ( var stream = FileUtils.Create( filepath ) )
                @this.CopyToFully( stream );
        }
    }
}
