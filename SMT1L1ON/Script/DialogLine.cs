using System.Collections;
using System.Collections.Generic;

namespace SMT1L1ON.Script
{
    public class DialogLine : IEnumerable<IToken>
    {
        public List<IToken> Tokens { get; }

        public DialogLine()
        {
            Tokens = new List< IToken >();
        }

        // IEnumerable implementation
        public IEnumerator<IToken> GetEnumerator()
        {
            return Tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}