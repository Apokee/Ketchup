using System;
using System.IO;
using SharpCompress.Compressor;
using SharpCompress.Compressor.Deflate;

namespace Ksg.Ketchup.IO
{
    internal sealed class BinaryStateWriter : BinaryWriter
    {
        private readonly MemoryStream _memoryStream;

        public BinaryStateWriter()
        {
            _memoryStream = new MemoryStream();

            OutStream = new GZipStream(_memoryStream, CompressionMode.Compress, true);
        }

        public override string ToString()
        {
            OutStream.Close();

            // Add a blank space after all forward-slashes to work around a bug where
            // forward-slashes are unescaped when written to the save file and and
            // consectuive forward slashes end up being treated as the start of a line
            // comment and the saved file is truncated when read.
            // See: http://bugs.kerbalspaceprogram.com/issues/821
            return Convert.ToBase64String(_memoryStream.ToArray()).Replace("/", "/ ");
        }
    }
}
