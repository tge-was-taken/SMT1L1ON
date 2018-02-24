using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SMT1L1ON.Common.IO;
using SMT1L1ON.Text.Encoding;

namespace SMT1L1ON.Script
{
    /// <summary>
    /// Represents an SC02 script file used for scripted events and dialogue.
    /// </summary>
    public class SC02Script
    {
        private static readonly byte[] sMagic = { ( byte ) 'S', ( byte ) 'C', ( byte ) '0', ( byte ) '2' };

        /// <summary>
        /// Gets the list of procedures in the script.
        /// </summary>
        public List<Procedure> Procedures { get; }

        /// <summary>
        /// Gets or sets the Header2 in the script.
        /// </summary>
        public Header2 Header2 { get; set; }

        /// <summary>
        /// Gets or sets the list of the dialogs in the script.
        /// </summary>
        public List<Dialog> Dialogs { get; set; }

        /// <summary>
        /// Construct a new, empty SC02 script.
        /// </summary>
        public SC02Script()
        {
            Procedures = new List< Procedure >();
            Dialogs = new List< Dialog >();
        }

        /// <summary>
        /// Reads an SC02 script from a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static SC02Script FromFile( string path )
        {
            using ( var stream = File.OpenRead( path ) )
                return FromStream( stream, true );
        }

        /// <summary>
        /// Reads an SC02 script from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ownsStream"></param>
        /// <returns></returns>
        public static SC02Script FromStream( Stream stream, bool ownsStream )
        {
            using ( var reader = new EndianBinaryReader( stream, ownsStream, Endianness.LittleEndian ) )
                return FromReader( reader );
        }

        private static SC02Script FromReader( EndianBinaryReader reader )
        {
            var script = new SC02Script();

            var magic = reader.ReadBytes( 4 );
            if ( !magic.SequenceEqual( sMagic ) )
                throw new InvalidDataException( "Header magic value does not match the expected value" );

            var procedureOffsetTableOffset = reader.ReadInt32();
            var header2Offset = reader.ReadInt32();
            var procedureDataOffset = reader.ReadInt32();
            var dialogListOffset = reader.ReadInt32();
            var dialogListLastOffsetOffset = reader.ReadInt32();

            // Files that do not have the header 2 data have the offset set equal to that
            // of the procedure data.
            var hasHeader2 = header2Offset != procedureDataOffset;

            // Files that do not have dialog lines, have both the start and end offset equal to each other
            var hasDialogs = dialogListOffset != dialogListLastOffsetOffset;

            var procedureCount = ( hasHeader2 ? header2Offset - procedureOffsetTableOffset : procedureDataOffset - procedureOffsetTableOffset ) / 2;
            if ( procedureCount > 0 )
            {
                // Read procedure offset table
                reader.SeekBegin( procedureOffsetTableOffset );
                var offsets = reader.ReadUInt16s( procedureCount );

                // Sort & clean up offsets to calculate procedure sizes
                var sortedOffsets = offsets.OrderBy( x => x ).ToList();
                sortedOffsets.Remove( 0xFFFF ); // Remove null (0xFFFF) offsets
                sortedOffsets.Add( ( ushort )( dialogListOffset - procedureDataOffset ) ); // Add highest possible offset for last entry

                for ( int i = 0; i < offsets.Length; i++ )
                {
                    Procedure procedure = null;

                    if ( offsets[i] != 0xFFFF )
                    {
                        // Read procedure
                        reader.SeekBegin( procedureDataOffset + ( offsets[i] ) );
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

            if ( hasDialogs )
            {
                reader.SeekBegin( dialogListOffset );
                var dialogDataOffset = reader.ReadUInt16();
                Debug.Assert( dialogDataOffset > 0, "dialogDataOffset is zero" );

                // Read offsets
                var dialogCount = dialogDataOffset - 1;
                var offsets = reader.ReadUInt16s( dialogCount );
                var dataAbsOffset = reader.BaseStream.Position;

                for ( int i = 0; i < offsets.Length; i++ )
                {
                    Dialog dialog = null;
                    var offset = offsets[i];

                    if ( offset != 0xFFFF )
                    {
                        reader.SeekBegin( dataAbsOffset + ( offset * 2 ) );
                        dialog = new Dialog();

                        while ( true )
                        {
                            var value = reader.ReadInt16();

                            if ( value == 0 )
                                break;

                            if ( FunctionToken.IsFunctionId( value ) )
                            {
                                var arguments = reader.ReadInt16s( FunctionToken.GetArgumentCount( value ) );
                                var functionToken = new FunctionToken( value, arguments );
                                dialog.Tokens.Add( functionToken );
                            }
                            else
                            {
                                if ( value < 442 )
                                {
                                    dialog.Tokens.Add( ( TextToken )SMT1Encoding.Instance.GetString( new[] { ( byte )( value ), ( byte )( ( value >> 8 ) ) } ) );
                                }
                                else if ( value < 883 )
                                {
                                    dialog.Tokens.Add( ( TextToken )SMT1Encoding.Instance.GetString( new[] { ( byte )( value ), ( byte )( ( value >> 8 ) ) } ) );
                                }
                                else if ( value < 1324 )
                                {
                                    dialog.Tokens.Add( ( TextToken )SMT1Encoding.Instance.GetString( new[] { ( byte )( value ), ( byte )( ( value >> 8 ) ) } ) );
                                }
                                else
                                {
                                    // Todo: decode text
                                    var codePointToken = new CodePointToken( value );
                                    dialog.Tokens.Add( codePointToken );
                                }
                            }
                        }

                        var curRelativeOffset = ( reader.Position - dataAbsOffset ) / 2;
                        Debug.Assert( !offsets.Any( x => x > offset && x < curRelativeOffset ), "Dialog line data overlap" );
                    }

                    script.Dialogs.Add( dialog );
                }
            }

            return script;
        }

        /// <summary>
        /// Writes the SC02 script to the specified file.
        /// </summary>
        /// <param name="path"></param>
        public void ToFile( string path, Encoding encoding )
        {
            using ( var stream = FileUtils.Create( path ) )
                ToStream( stream, encoding );
        }

        /// <summary>
        /// Writes the SC02 script to a new empty stream.
        /// </summary>
        /// <returns></returns>
        public Stream ToStream( Encoding encoding )
        {
            var stream = new MemoryStream();
            ToStream( stream, encoding );
            return stream;
        }

        /// <summary>
        /// Writes the SC02 script to the specified stream.
        /// </summary>
        /// <param name="stream"></param>
        public void ToStream( Stream stream, Encoding encoding )
        {
            using ( var writer = new EndianBinaryWriter( stream, true, Endianness.LittleEndian ) )
                Write( writer, encoding );
        }

        private void Write( EndianBinaryWriter writer, Encoding encoding )
        {
            // Calculate header offsets
            var procedureOffsetTableOffset = 0x18;
            var header2Offset = procedureOffsetTableOffset + ( Procedures.Count * 2 );
            var procedureDataOffset = Header2 != null ? header2Offset + 4 : header2Offset;
            var dialogListOffset = procedureDataOffset + Procedures.Sum( x => x.Data.Count ) * 2;
            var dialogListLastOffsetOffset = Dialogs != null ? dialogListOffset + 0x02 + ( ( Dialogs.Count - 1 ) * 2 ) : dialogListOffset;

            // Write header
            writer.Write( sMagic );
            writer.Write( procedureOffsetTableOffset );
            writer.Write( header2Offset );
            writer.Write( procedureDataOffset );
            writer.Write( dialogListOffset );
            writer.Write( dialogListLastOffsetOffset );

            // Write procedure offset table
            writer.SeekBegin( procedureOffsetTableOffset );
            var procedureOffsets = new List< int >();
            var nextProcedureOffset = 0;
            foreach ( var procedure in Procedures )
            {
                if ( procedure != null )
                {
                    writer.Write( ( ushort ) nextProcedureOffset );
                    procedureOffsets.Add( procedureDataOffset + nextProcedureOffset );
                    nextProcedureOffset += ( procedure.Data.Count * 2 );
                }
                else
                {
                    writer.Write( ushort.MaxValue );
                    procedureOffsets.Add( -1 );
                }
            }

            // Write header 2
            if ( Header2 != null )
            {
                writer.SeekBegin( header2Offset );
                writer.Write( Header2.Field00 );
                writer.Write( Header2.Field02 );
            }

            // Write procedures
            for ( var i = 0; i < Procedures.Count; i++ )
            {
                Debug.Assert( !procedureOffsets.Any( x => x > procedureOffsets[i] && x < writer.BaseStream.Position ),
                              "Procedure offset overlaps" );

                writer.SeekBegin( procedureOffsets[ i ] );
                foreach ( short s in Procedures[i].Data )
                    writer.Write( s );
            }

            // Write dialogs
            if ( Dialogs != null )
            {
                writer.SeekBegin( dialogListOffset );
                var dialogDataOffset = 1 + ( Dialogs.Count );
                writer.Write( ( ushort ) dialogDataOffset );

                var dialogOffsets = new List< int >();
                var nextDialogOffset = 0;
                for ( var i = 0; i < Dialogs.Count; i++ )
                {
                    var dialog = Dialogs[ i ];
                    if ( dialog != null )
                    {
                        if ( i + 1 == Dialogs.Count )
                            Debug.Assert( writer.Position == dialogListLastOffsetOffset, "writer.Position != dialogListLastOffsetOffset" );

                        writer.Write( ( ushort ) nextDialogOffset );
                        dialogOffsets.Add( dialogListOffset + ( dialogDataOffset * 2 ) + ( nextDialogOffset * 2 ) );

                        // Calculate size of dialog
                        int dialogSize = 0;
                        foreach ( var token in dialog )
                        {
                            switch ( token.Kind )
                            {
                                case TokenKind.CodePoint:
                                    ++dialogSize;
                                    break;
                                case TokenKind.Text:
                                    dialogSize += ( ( TextToken ) token ).Text.Length;
                                    break;
                                case TokenKind.Function:
                                    dialogSize += ( ( FunctionToken ) token ).Size / 2;
                                    break;
                                default:
                                    throw new NotImplementedException( token.Kind.ToString() );
                            }
                        }

                        // Reserve space for terminator
                        ++dialogSize;
                        nextDialogOffset += dialogSize;
                    }
                    else
                    {
                        writer.Write( ushort.MaxValue );
                        dialogOffsets.Add( -1 );
                    }
                }

                for ( var i = 0; i < Dialogs.Count; i++ )
                {
                    var dialog = Dialogs[ i ];
                    if ( dialog == null )
                        continue;

                    Debug.Assert( !dialogOffsets.Any( x => x > dialogOffsets[i] && x < writer.BaseStream.Position ),
                                  "Dialog offset overlaps" );

                    writer.SeekBegin( dialogOffsets[ i ] );
                    foreach ( var token in dialog )
                    {
                        switch ( token.Kind )
                        {
                            case TokenKind.Function:
                                var functionToken = ( ( FunctionToken ) token );
                                writer.Write( functionToken.Id );
                                foreach ( short argument in functionToken.Arguments )
                                    writer.Write( argument );
                                break;
                            case TokenKind.Text:
                                var textToken = ( ( TextToken ) token );
                                var textBytes = encoding.GetBytes( textToken.Text );
                                writer.Write( textBytes );
                                break;
                            case TokenKind.CodePoint:
                                var cpToken = ( ( CodePointToken ) token );
                                writer.Write( cpToken.Value );
                                break;
                            default:
                                throw new NotImplementedException( token.Kind.ToString() );
                        }
                    }

                    // Terminator
                    writer.Write( ( short ) 0 );
                }
            }
        }
    }
}
