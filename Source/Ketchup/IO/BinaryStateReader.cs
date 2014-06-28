using System;
using System.IO;
using SharpCompress.Compressor;
using SharpCompress.Compressor.Deflate;

namespace Ketchup.IO
{
    internal sealed class BinaryStateReader : BinaryReader
    {
        public BinaryStateReader(string state)
            : base(GetStateStream(state)) { }

        private static Stream GetStateStream(string state)
        {
            // Strip empty spaces to reverse the escaping performed by BinaryStateWriter.ToString()
            return new GZipStream(
                new MemoryStream(Convert.FromBase64String(state.Replace(" ", String.Empty))),
                CompressionMode.Decompress
            );
        }
    }
}
