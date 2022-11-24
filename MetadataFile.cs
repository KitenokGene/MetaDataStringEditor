using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MetaDataStringEditor {
    class MetadataFile : IDisposable {
        public BinaryReader reader;

        private uint stringLiteralOffset;
        private uint stringLiteralCount;
        private long DataInfoPosition;
        private uint stringLiteralDataOffset;
        private uint stringLiteralDataCount;
        private List<StringLiteral> stringLiterals = new List<StringLiteral>();
        public List<byte[]> strBytes = new List<byte[]>();

        public MetadataFile(string fullName) {
            reader = new BinaryReader(File.OpenRead(fullName));

            // 读取文件
            ReadHeader();

            // 读取字符串
            ReadLiteral();
            ReadStrByte();

            Logger.I("Basic read complete");
        }

        private void ReadHeader() {
            Logger.I("Reading header");
            uint vansity = reader.ReadUInt32();
            if (vansity != 0xFAB11BAF) {
                throw new Exception("Flag check failed");
            }
            int version = reader.ReadInt32();
            stringLiteralOffset = reader.ReadUInt32();      // The position of the list area will not be changed later
            stringLiteralCount = reader.ReadUInt32();       // The size of the list area will not be changed later
            DataInfoPosition = reader.BaseStream.Position;  // Remember the current location, it will be used later
            stringLiteralDataOffset = reader.ReadUInt32();  // The location of the data area may need to be changed
            stringLiteralDataCount = reader.ReadUInt32();   // The length of the data area may need to be changed
        }

        private void ReadLiteral() {
            Logger.I("Reading Literal");
            ProgressBar.SetMax((int)stringLiteralCount / 8);

            reader.BaseStream.Position = stringLiteralOffset;
            for (int i = 0; i < stringLiteralCount / 8; i++) {
                stringLiterals.Add(new StringLiteral {
                    Length = reader.ReadUInt32(),
                    Offset = reader.ReadUInt32()
                });
                ProgressBar.Report();
            }
        }

        private void ReadStrByte() {
            Logger.I("Reading Str Bytes");
            ProgressBar.SetMax(stringLiterals.Count);

            for (int i = 0; i < stringLiterals.Count; i++) {
                reader.BaseStream.Position = stringLiteralDataOffset + stringLiterals[i].Offset;
                strBytes.Add(reader.ReadBytes((int)stringLiterals[i].Length));
                ProgressBar.Report();
            }
        }

        public void WriteToNewFile(string fileName) {
            BinaryWriter writer = new BinaryWriter(File.Create(fileName));

            // copy all over first
            reader.BaseStream.Position = 0;
            reader.BaseStream.CopyTo(writer.BaseStream);

            // Renew Literal
            Logger.I("Renewing Literal");
            ProgressBar.SetMax(stringLiterals.Count);
            writer.BaseStream.Position = stringLiteralOffset;
            uint count = 0;
            for (int i = 0; i < stringLiterals.Count; i++) {

                stringLiterals[i].Offset = count;
                stringLiterals[i].Length = (uint)strBytes[i].Length;

                writer.Write(stringLiterals[i].Length);
                writer.Write(stringLiterals[i].Offset);
                count += stringLiterals[i].Length;

                ProgressBar.Report();
            }

            // Perform an alignment, not sure if it is necessary, but Unity has done it, so it is better to make up
            var tmp = (stringLiteralDataOffset + count) % 4;
            if (tmp != 0) count += 4 - tmp;

            // Check if there is enough space to place
            if (count > stringLiteralDataCount) {
                // Check if there is any other data behind the data area, if not, you can directly extend the data area
                if (stringLiteralDataOffset + stringLiteralDataCount < writer.BaseStream.Length) {
                    // The original space is not enough, and it cannot be directly extended, so the whole is moved to the end of the file
                    stringLiteralDataOffset = (uint)writer.BaseStream.Length;
                }
            }
            stringLiteralDataCount = count;

            // Renewing string
            Logger.I("RenewString");
            ProgressBar.SetMax(strBytes.Count);
            writer.BaseStream.Position = stringLiteralDataOffset;
            for (int i = 0; i < strBytes.Count; i++) {
                writer.Write(strBytes[i]);
                ProgressBar.Report();
            }

            // RenewHeader
            Logger.I("RenewHeader");
            writer.BaseStream.Position = DataInfoPosition;
            writer.Write(stringLiteralDataOffset);
            writer.Write(stringLiteralDataCount);

            Logger.I("Renewing finished");
            writer.Close();
        }
        
        public void Dispose() {
            reader?.Dispose();
        }
        
        public class StringLiteral {
            public uint Length;
            public uint Offset;
        }
    }
}
