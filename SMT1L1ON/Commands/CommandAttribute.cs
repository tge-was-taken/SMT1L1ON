using System;

namespace SMT1L1ON.Commands
{
    internal class CommandAttribute : Attribute
    {
        public string Name { get; }

        public CommandAttribute( string name )
        {
            Name = name;
        }
    }
}