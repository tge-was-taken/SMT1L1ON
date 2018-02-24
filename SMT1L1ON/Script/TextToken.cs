namespace SMT1L1ON.Script
{
    public struct TextToken : IToken
    {
        public TokenKind Kind => TokenKind.Text;

        public string Text { get; }

        public TextToken( string text )
        {
            Text = text;
        }

        public static implicit operator TextToken( string text )
        {
            return new TextToken( text );
        }

        public override string ToString()
        {
            return Text;
        }
    }
}