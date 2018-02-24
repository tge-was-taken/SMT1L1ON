using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMT1L1ON.Script
{
    public struct FunctionToken : IToken
    {
        public TokenKind Kind => TokenKind.Function;

        public short Id { get; }

        public List<short> Arguments { get; }

        public int Size => GetSize( Id );

        public FunctionToken( short id )
        {
            Id = id;
            Arguments = new List< short >();
        }

        public FunctionToken( short id, params short[] arguments )
        {
            Id = id;
            Arguments = arguments.ToList();
        }

        public static int GetSize( int functionId )
        {
            switch ( functionId )
            {
                case 0x801:
                case 0x802:
                case 0x816:
                case 0x81F:
                case 0x820:
                case 0x821:
                case 0x822:
                case 0x823:
                case 0x824:
                    return 4;

                default:
                    return 2;
            }
        }

        public static int GetArgumentCount( int functionId )
        {
            return ( ( GetSize( functionId ) - 2 ) / 2 );
        }

        public static bool IsFunctionId( short value )
        {
            switch ( value )
            {
                case 0x800:
                case 0x801:
                case 0x802:
                case 0x803:
                case 0x804:
                case 0x805:
                case 0x806:
                case 0x807:
                case 0x808:
                case 0x809:
                case 0x80A:
                case 0x80B:
                case 0x80C:
                case 0x80D:
                case 0x80E:
                case 0x80F:
                case 0x810:
                case 0x811:
                case 0x812:
                case 0x814:
                case 0x815:
                case 0x816:
                case 0x817:
                case 0x818:
                case 0x81F:
                case 0x820:
                case 0x821:
                case 0x822:
                case 0x823:
                case 0x824:
                    return true;
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder($"f 0x{Id:X3}");
            if ( Arguments.Count > 0 )
            {
                int i = 0;
                do
                {
                    builder.Append( " " );
                    builder.Append( Arguments[ i++ ] );
                }
                while ( i < Arguments.Count );
            }

            return builder.ToString();
        }
    }
}