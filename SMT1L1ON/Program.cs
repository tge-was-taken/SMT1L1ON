using System;
using System.Linq;
using System.Reflection;
using SMT1L1ON.Commands;


namespace SMT1L1ON
{
    internal static class Program
    {
        private static void DisplayUsage()
        {
            Console.WriteLine( "Usage: " +
                               "    SMT1L1ON <command> <args>\n" +
                               "\n" +
                               "Commands:\n" +
                               "\n" +
                               "        dec" +
                               "        Decompresses a file that uses the Huffman-esque RLE compression\n" +
                               "        Argument(s): <path to file>\n" +
                               "\n" +
                               "\n" +
                               "        com" +
                               "        Compresses a file that uses the Huffman-esque RLE compression\n" +
                               "        Argument(s): <path to file>\n" +
                               "\n" +
                               "        fontunpack" +
                               "        Unpacks a font pack file into multiple TIM textures\n" +
                               "        Argument(s): <path to font pack file>\n" +
                               "\n" +
                               "        fontpack" +
                               "        Packs a directory containing font tim file into a font pack file (0010.bin)\n" +
                               "        Argument(s): <path to directory containing font tim files>" );
        }

        private static void Main( string[] args )
        {
            if ( args.Length == 0 )
            {
                DisplayUsage();
                return;
            }

            if ( !ExecuteCommand( args ) )
            {
                Console.WriteLine( "Command failed!" );
            }
            else
            {
                Console.WriteLine( "Command executed successfully" );
            }
        }

        private static bool ExecuteCommand( string[] args )
        {
            var command = args[ 0 ];
            var commandTypes = Assembly.GetExecutingAssembly()
                                       .GetTypes()
                                       .Where( x => x.GetCustomAttribute< CommandAttribute >() != null )
                                       .ToDictionary( x => x.GetCustomAttribute< CommandAttribute >().Name,
                                                      StringComparer.InvariantCultureIgnoreCase );

            if ( !commandTypes.TryGetValue( command, out var commandType ) )
            {
                Console.WriteLine( "Unknown command" );
                return false;
            }

            // Get execute method
            var executeMethod = commandType.GetMethod( "Execute" );
            if ( executeMethod == null )
                throw new MissingMethodException( commandType.Name, "Execute" );

            // Set up argument list
            var executeMethodParameters = executeMethod.GetParameters();
            if ( ( args.Length - 1 ) < executeMethodParameters.Length )
            {
                Console.WriteLine( $"Expected {executeMethodParameters.Length} argument(s)" );
                return false;
            }

            var executeMethodCallArgs = new string[executeMethodParameters.Length];
            for ( int i = 0; i < executeMethodCallArgs.Length; i++ )
                executeMethodCallArgs[ i ] = args[ 1 + i ];

            // Call method
            return ( bool ) executeMethod.Invoke( null, executeMethodCallArgs );
        }
    }
}
