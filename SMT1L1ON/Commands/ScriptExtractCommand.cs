using System;
using System.IO;
using System.Linq;
using SMT1L1ON.Script;
using SMT1L1ON.Text.Encoding;

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
            //script.ToFile("test.sc02", SMT1Encoding.Instance );

            using ( var writer = File.CreateText( "test.msg") )
            {
                for ( var i = 0; i < script.Dialogs.Count; i++ )
                {
                    var dialog = script.Dialogs[ i ];
                    writer.WriteLine( $"[dlg {i:D4}]" );

                    if ( dialog != null )
                    {
                        foreach ( var token in dialog )
                        {
                            switch ( token.Kind )
                            {
                                case TokenKind.Function:
                                    var functionToken = ( FunctionToken ) token;
                                    writer.Write( $"[f 0x{functionToken.Id:X3}" );
                                    foreach ( var argument in functionToken.Arguments )
                                    {
                                        writer.Write( $" {argument}" );
                                    }
                                    writer.Write( "]" );

                                    // Insert a newline after the line terminator function
                                    if ( functionToken.Id == 0x805 )
                                        writer.WriteLine();

                                    break;
                                case TokenKind.Text:
                                    writer.Write( ( ( TextToken ) token ).Text );
                                    break;
                                case TokenKind.CodePoint:
                                    writer.Write( $"[cp 0x{( ( CodePointToken ) token ).Value:X4}]" );
                                    break;
                                default:
                                    throw new NotImplementedException( token.Kind.ToString() );
                            }
                        }

                        // Insert extra newline if the last token wasn't already a line terminator
                        var lastToken = dialog.LastOrDefault();
                        if ( lastToken == null || lastToken.Kind != TokenKind.Function || ( ( FunctionToken ) lastToken ).Id != 0x805 )
                            writer.WriteLine();
                    }
                    else
                    {
                        // New lines!
                        writer.WriteLine();
                    }

                    writer.WriteLine();
                }
            }

            return true;
        }
    }
}