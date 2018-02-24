using System;
using System.IO;

namespace SMT1L1ON.Text.Encoding
{
    public class SMT1Encoding : System.Text.Encoding
    {
        private const int CHAR_PER_ROW = 21;
        private const int CHAR_PER_COLUMN = 21;

        private static bool sInitialized;
        private static char[][] sFont0Table0;
        private static char[][] sFont0Table1;
        private static char[][] sFont0Table2;
        private static char[][] sFont0Table3;
        private static char[][] sFont1Table0;
        private static char[][] sFont1Table1;
        private static char[][] sFont2Table0;
        private static char[][] sFont2Table1;

        public static SMT1Encoding Instance { get; } = new SMT1Encoding();

        public SMT1Encoding()
        {
            if ( !sInitialized )
            {
                sFont0Table0 = LoadTable( "font0_0", 16, 6 );
                sFont0Table1 = LoadTable( "font0_1", 16, 8 );
                sFont0Table2 = LoadTable( "font0_2", 16, 3 );
                sFont0Table3 = LoadTable( "font0_3", 21, 12 );
                sFont1Table0 = LoadTable( "font1_0", 21, 21 );
                sFont1Table1 = LoadTable( "font1_1", 21, 21 );
                sFont2Table0 = LoadTable( "font2_0", 21, 21 );
                sInitialized = true;
            }
        }

        private static char[][] LoadTable( string name, int columnCount, int rowCount )
        {
            var fontMap = File.ReadAllText( $"Text\\Encoding\\{name}.txt" )
                              .Split( new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries );

            var table = new char[columnCount][];
            for ( int y = 0; y < columnCount; y++ )
            {
                table[ y ] = new char[rowCount];

                for ( int x = 0; x < rowCount; x++ )
                    table[ y ][ x ] = fontMap[( y * rowCount ) + x ][ 0 ];
            }

            return table;
        }

        public override int GetByteCount( char[] chars, int index, int count )
        {
            return count * 2;
        }

        public override int GetBytes( char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex )
        {
            throw new NotImplementedException();
        }

        public override int GetCharCount( byte[] bytes, int index, int count )
        {
            return count / 2;
        }

        public override int GetChars( byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex )
        {
            while ( byteCount > 0 )
            {
                var cp = bytes[ byteIndex++ ] | bytes[ byteIndex++ ] << 8;
                char c = ( char )0;

                if ( cp > 0 )
                {
                    --cp;

                    if ( cp < 441 )
                    {
                        // font 1 - 0
                        var columnIndex = cp / 21;
                        var rowIndex = cp % 21; // appears to be correct
                        c = sFont1Table0[ columnIndex ][ rowIndex ];
                    }
                    else if ( cp < 882 )
                    {
                        // font 1 - 1
                        cp -= 441;
                        var columnIndex = cp / 21;
                        var rowIndex = cp % 21; //  appears to be correct
                        c = sFont1Table1[ columnIndex ][ rowIndex ];
                    }
                    else if ( cp < 1323 )
                    {
                        cp -= 882;
                        var columnIndex = cp / 21;
                        var rowIndex = cp % 21; // verify this
                        c = sFont2Table0[ columnIndex ][ rowIndex ];
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                chars[charIndex++] = c;
                byteCount -= 2;
            }

            return byteCount / 2;
        }

        public override int GetMaxByteCount( int charCount )
        {
            return charCount * 2;
        }

        public override int GetMaxCharCount( int byteCount )
        {
            return byteCount / 2;
        }
    }
}
