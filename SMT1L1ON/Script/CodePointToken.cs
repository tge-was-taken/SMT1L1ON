namespace SMT1L1ON.Script
{
    public struct CodePointToken : IToken
    {
        public TokenKind Kind => TokenKind.CodePoint;

        public short Value { get; }

        public CodePointToken( short value )
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"cp 0x{Value:X4}";
        }
    }
}