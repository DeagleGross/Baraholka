using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public unsafe class DataConverterRewrite
    {
        private const uint MAGIC_HEADER_V0 = 0x09F0C9F0;

        static UTF8Encoding SecureUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private string[] purposes;        

        [GlobalSetup]
        public void Setup()
        {
            purposes =
            [
                "MySample",
                "MyTry",
                "qwe"
            ];
        }

        [Benchmark]
        public byte[] MemoryStream()
        {
            const int MEMORYSTREAM_DEFAULT_CAPACITY = 0x100; // matches MemoryStream.EnsureCapacity
            var ms = new MemoryStream(MEMORYSTREAM_DEFAULT_CAPACITY);

            // additionalAuthenticatedData := { magicHeader (32-bit) || keyId || purposeCount (32-bit) || (purpose)* }
            // purpose := { utf8ByteCount (7-bit encoded) || utf8Text }

            using (var writer = new PurposeBinaryWriter(ms))
            {
                writer.WriteBigEndian(MAGIC_HEADER_V0);
                Debug.Assert(ms.Position == sizeof(uint));
                var posPurposeCount = writer.Seek(sizeof(Guid), SeekOrigin.Current); // skip over where the key id will be stored; we'll fill it in later
                writer.Seek(sizeof(uint), SeekOrigin.Current); // skip over where the purposeCount will be stored; we'll fill it in later

                uint purposeCount = 0;
                foreach (string purpose in purposes)
                {
                    Debug.Assert(purpose != null);
                    writer.Write(purpose); // prepends length as a 7-bit encoded integer
                    purposeCount++;
                }

                // Once we have written all the purposes, go back and fill in 'purposeCount'
                writer.Seek(checked((int)posPurposeCount), SeekOrigin.Begin);
                writer.WriteBigEndian(purposeCount);
            }

            return ms.ToArray();
        }

        [Benchmark]
        public byte[] Manual()
        {
            // additionalAuthenticatedData := { magicHeader (32-bit) || keyId || purposeCount (32-bit) || (purpose)* }
            // purpose := { utf8ByteCount (7-bit encoded) || utf8Text }

            var keySize = sizeof(Guid);
            int totalPurposeLen = 4 + keySize + 4;

            var purposeLengthsPool = ArrayPool<int>.Shared.Rent(purposes.Length);
            for (int i = 0; i < purposes.Length; i++)
            {
                string purpose = purposes[i];

                int purposeLength = SecureUtf8Encoding.GetByteCount(purpose);
                purposeLengthsPool[i] = purposeLength;

                var encoded7BitUIntLength = Measure7BitEncodedUIntLength((uint)purposeLength);
                totalPurposeLen += purposeLength /* length of actual string */ + encoded7BitUIntLength /* length of 'string length' 7-bit encoded int */;
            }

            byte[] targetArr = new byte[totalPurposeLen];
            var targetSpan = targetArr.AsSpan();
            BinaryPrimitives.WriteUInt32BigEndian(targetSpan.Slice(0), MAGIC_HEADER_V0);
            BinaryPrimitives.WriteInt32BigEndian(targetSpan.Slice(4 + keySize), purposes.Length);
            
            int index = 4 + keySize + 4; // starting from first purpose
            for (int i = 0; i < purposes.Length; i++)
            {
                string purpose = purposes[i];

                // writing `utf8ByteCount (7-bit encoded integer) || utf8Text`
                // we have already calculated the lengths of the purpose strings, so just get it from the pool
                index += Write7BitEncodedInt(purposeLengthsPool[i], targetSpan.Slice(index));
                index += SecureUtf8Encoding.GetBytes(purpose.AsSpan(), targetSpan.Slice(index));
            }

            ArrayPool<int>.Shared.Return(purposeLengthsPool);
            Debug.Assert(index == targetArr.Length);
            return targetArr;
        }

        internal static int Measure7BitEncodedUIntLength(uint value)
        {
            return ((31 - System.Numerics.BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

            // does the same as the following code:
            // int count = 1;
            // while ((value >>= 7) != 0)
            // {
            //     count++;
            // }
            // return count;
        }

        static int Write7BitEncodedInt(int value, Span<byte> target)
        {
            uint uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            int index = 0;
            while (uValue > 0x7Fu)
            {
                target[index++] = (byte)(uValue | ~0x7Fu);
                uValue >>= 7;
            }

            target[index++] = (byte)uValue;
            return index;
        }

        private sealed class PurposeBinaryWriter : BinaryWriter
        {
            public PurposeBinaryWriter(MemoryStream stream) : base(stream, SecureUtf8Encoding, leaveOpen: true) { }

            // Writes a big-endian 32-bit integer to the underlying stream.
            public void WriteBigEndian(uint value)
            {
                var outStream = BaseStream; // property accessor also performs a flush
                outStream.WriteByte((byte)(value >> 24));
                outStream.WriteByte((byte)(value >> 16));
                outStream.WriteByte((byte)(value >> 8));
                outStream.WriteByte((byte)(value));
            }
        }
    }
}
