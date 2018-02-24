using System;
using System.IO;
using SMT1L1ON.Script;

namespace SMT1L1ON.Commands
{
    [Command("scriptextract")]
    internal static class ScriptExtractCommand
    {
        public static bool Execute( string inPath )
        {
            if ( !File.Exists( inPath ) )
            {
                Console.WriteLine( "Specified file doesn't exist" );
                return false;
            }

            // Load script
            var script = SC02Script.FromFile( inPath );

            using ( var writer = File.CreateText( "test.msg") )
            {
                for ( var i = 0; i < script.DialogLines.Count; i++ )
                {
                    var line = script.DialogLines[ i ];
                    writer.WriteLine( $"[ln {i:D4}]" );

                    if ( line != null )
                    {
                        foreach ( var token in line )
                        {
                            switch ( token.Kind )
                            {
                                case TokenKind.Function:
                                    var functionToken = ( FunctionToken )token;
                                    writer.Write( $"[f 0x{functionToken.Id:X3}" );
                                    foreach ( var argument in functionToken.Arguments )
                                    {
                                        writer.Write( $" {argument}" );
                                    }
                                    writer.Write( "]" );
                                    break;
                                case TokenKind.Text:
                                    writer.Write( ( ( TextToken )token ).Text );
                                    break;
                                case TokenKind.CodePoint:
                                    writer.Write( $"[cp 0x{( ( CodePointToken )token ).Value:X4}]" );
                                    break;
                                default:
                                    throw new NotImplementedException( token.Kind.ToString() );
                            }
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }
            }

            return true;
        }
    }
}