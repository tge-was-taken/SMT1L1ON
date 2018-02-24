using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SMT1L1ON.Common.IO;

namespace SMT1L1ON.Script
{
    class SC02Script
    {
        private static readonly byte[] MAGIC = { ( byte ) 'S', ( byte ) 'C', ( byte ) '0', ( byte ) '2' };

        public List<Procedure> Procedures { get; }

        public Header2 Header2 { get; set; }

        public List<DialogLine> DialogLines { get; set; }

        public SC02Script()
        {
            Procedures = new List< Procedure >();
            DialogLines = new List< DialogLine >();
        }

        public static SC02Script FromFile( string path )
        {
            using ( var stream = File.OpenRead( path ) )
                return FromStream( stream, true );
        }

        public static SC02Script FromStream( Stream stream, bool ownsStream )
        {
            var script = new SC02Script();

            using ( var reader = new EndianBinaryReader( stream, ownsStream, Endianness.LittleEndian ) )
            {
                var magic = reader.ReadBytes( 4 );
                if ( !magic.SequenceEqual( MAGIC ) )
                    throw new InvalidDataException( "Header magic value does not match the expected value" );

                var procedureOffsetTableOffset = reader.ReadInt32();
                var header2Offset = reader.ReadInt32();
                var procedureDataOffset = reader.ReadInt32();
                var dialogLineListOffset = reader.ReadInt32();
                var dialogLineListLastOffsetOffset = reader.ReadInt32();

                // Files that do not have the header 2 data have the offset set equal to that
                // of the procedure data.
                var hasHeader2 = header2Offset != procedureDataOffset;

                // Files that do not have dialog lines, have both the start and end offset equal to each other
                var hasDialogLines = dialogLineListOffset != dialogLineListLastOffsetOffset;

                var procedureCount = ( hasHeader2 ? header2Offset - procedureOffsetTableOffset : procedureDataOffset - procedureOffsetTableOffset ) / 2;
                if ( procedureCount > 0 )
                {
                    // Read procedure offset table
                    reader.SeekBegin( procedureOffsetTableOffset );
                    var offsets = reader.ReadUInt16s( procedureCount );
                    
                    // Sort & clean up offsets to calculate procedure sizes
                    var sortedOffsets = offsets.OrderBy( x => x ).ToList();
                    sortedOffsets.Remove( 0xFFFF ); // Remove null (0xFFFF) offsets
                    sortedOffsets.Add( ( ushort ) ( dialogLineListOffset - procedureDataOffset ) ); // Add highest possible offset for last entry

                    for ( int i = 0; i < offsets.Length; i++ )
                    {
                        Procedure procedure = null;

                        if ( offsets[ i ] != 0xFFFF )
                        {
                            // Read procedure
                            reader.SeekBegin( procedureDataOffset + ( offsets[ i ] ) );
                            var size = sortedOffsets[i + 1] - sortedOffsets[i];
                            var data = reader.ReadInt16s( size / 2 );
                            procedure = new Procedure( data.ToList() );
                        }

                        script.Procedures.Add( procedure );
                    }
                }
           
                if ( hasHeader2 )
                {
                    Debug.Assert( ( procedureDataOffset - header2Offset ) == 4, "Header2 data is not 4 bytes in size" );

                    // Read header 2
                    reader.SeekBegin( header2Offset );
                    script.Header2 = new Header2
                    {
                        Field00 = reader.ReadInt16(),
                        Field02 = reader.ReadInt16()
                    };
                }

                if ( hasDialogLines )
                {
                    reader.SeekBegin( dialogLineListOffset );
                    var lineDataOffset = reader.ReadUInt16();
                    Debug.Assert( lineDataOffset > 0, "dialogDataOffset is zero" );

                    // Read offsets
                    var lineCount = lineDataOffset - 1;
                    var offsets = reader.ReadUInt16s( lineCount );
                    var lineDataAbsOffset = reader.BaseStream.Position;

                    for ( int i = 0; i < offsets.Length; i++ )
                    {
                        DialogLine dialogLine = null;
                        var offset = offsets[ i ];

                        if ( offset != 0xFFFF )
                        {
                            reader.SeekBegin( lineDataAbsOffset + ( offset * 2 ) );
                            dialogLine = new DialogLine();

                            while ( true )
                            {
                                var value = reader.ReadInt16();

                                if ( value == 0 )
                                    break;

                                if ( FunctionToken.IsFunctionId( value ) )
                                {
                                    var arguments = reader.ReadInt16s( FunctionToken.GetArgumentCount( value ) );
                                    var functionToken = new FunctionToken( value, arguments );
                                    dialogLine.Tokens.Add( functionToken );

                                    if ( functionToken.Id == 0x800 || functionToken.Id == 0x805 )
                                    {
                                        var returnPos = reader.Position;
                                        var nextValue = reader.ReadInt16();
                                        if ( nextValue == 0 )
                                            break;

                                        reader.SeekBegin( returnPos );
                                    }
                                }
                                else
                                {
                                    // Todo: decode text
                                    var codePointToken = new CodePointToken( value );
                                    dialogLine.Tokens.Add( codePointToken );
                                }
                            }

                            var curRelativeOffset = ( reader.Position - lineDataAbsOffset ) / 2;
                            Debug.Assert( !offsets.Any( x => x > offset && x < curRelativeOffset ), "Dialog line data overlap" );
                        }

                        script.DialogLines.Add( dialogLine );
                    }
                }
            }

            return script;
        }
    }
}
