using System;
using System.IO;
using System.IO.Compression;
using Ketchup.Api;
using Ketchup.Exceptions;

namespace Ketchup.IO
{
    internal sealed class Dcpu16StateManager
    {
        #region Constants

        private const uint MagicNumber      = 0xdbb0cae0;
        private const uint VersionNumber    = 0x00000001;
        private const uint MemorySize       = 0x00010000;

        #endregion

        #region Instance Fields

        private readonly IDcpu16 _dcpu16;

        #endregion

        #region Constructors

        public Dcpu16StateManager(IDcpu16 dcpu16)
        {
            _dcpu16 = dcpu16;
        }

        #endregion

        #region Methods

        public void LoadFromBase64(string state)
        {
            LoadFromByteArray(Convert.FromBase64String(state));
        }

        public void LoadFromByteArray(byte[] state)
        {
            using (var stream = new MemoryStream(state))
            {
                LoadFromStream(stream);
            }
        }

        public void LoadFromStream(Stream stream)
        {
            try
            {
                using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                using (var reader = new BinaryReader(gzipStream))
                {
                    // Header
                    var magicNumber = reader.ReadUInt32();
                    if (magicNumber != MagicNumber)
                    {
                        throw new LoadStateException(String.Format("Magic number is incorrect. Expected: {0:X}. Found: {1:X}.", MagicNumber, magicNumber));
                    }

                    var versionNumber = reader.ReadUInt32();
                    if (versionNumber != VersionNumber)
                    {
                        throw new LoadStateException(String.Format("Unsupported version number: {0}", versionNumber));
                    }

                    // Registers
                    _dcpu16.A = reader.ReadUInt16();
                    _dcpu16.B = reader.ReadUInt16();
                    _dcpu16.C = reader.ReadUInt16();
                    _dcpu16.X = reader.ReadUInt16();
                    _dcpu16.Y = reader.ReadUInt16();
                    _dcpu16.Z = reader.ReadUInt16();
                    _dcpu16.I = reader.ReadUInt16();
                    _dcpu16.J = reader.ReadUInt16();
                    _dcpu16.PC = reader.ReadUInt16();
                    _dcpu16.SP = reader.ReadUInt16();
                    _dcpu16.EX = reader.ReadUInt16();
                    _dcpu16.IA = reader.ReadUInt16();

                    // Memory
                    for (var i = 0; i < MemorySize; i++)
                    {
                        _dcpu16.Memory[i] = reader.ReadUInt16();
                    }

                    // Flags
                    _dcpu16.IsOnFire = reader.ReadBoolean();
                    _dcpu16.IsInterruptQueueEnabled = reader.ReadBoolean();

                    // Interrupts
                    var interruptCount = reader.ReadInt32();
                    _dcpu16.InterruptQueue.Clear();
                    for (var i = 0; i < interruptCount; i++)
                    {
                        _dcpu16.InterruptQueue.Enqueue(reader.ReadUInt16());
                    }
                }
            }
            catch (Exception e)
            {
                throw new LoadStateException(e);
            }
        }

        public string SaveAsBase64()
        {
            return Convert.ToBase64String(SaveAsByteArray());
        }

        public byte[] SaveAsByteArray()
        {
            using (var stream = new MemoryStream())
            {
                SaveToStream(stream);

                return stream.ToArray();
            }
        }

        public void SaveToStream(Stream stream)
        {
            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress))
            using (var writer = new BinaryWriter(gzipStream))
            {
                // Header
                writer.Write(MagicNumber);
                writer.Write(VersionNumber);

                // Registers
                writer.Write(_dcpu16.A);
                writer.Write(_dcpu16.B);
                writer.Write(_dcpu16.C);
                writer.Write(_dcpu16.X);
                writer.Write(_dcpu16.Y);
                writer.Write(_dcpu16.Z);
                writer.Write(_dcpu16.I);
                writer.Write(_dcpu16.J);
                writer.Write(_dcpu16.PC);
                writer.Write(_dcpu16.SP);
                writer.Write(_dcpu16.EX);
                writer.Write(_dcpu16.IA);

                // Memory
                for (var i = 0; i < MemorySize; i++)
                {
                    writer.Write(_dcpu16.Memory[i]);
                }

                // Flags
                writer.Write(_dcpu16.IsOnFire);
                writer.Write(_dcpu16.IsInterruptQueueEnabled);

                // Interrupts
                writer.Write(_dcpu16.InterruptQueue.Count);
                foreach (var interrupt in _dcpu16.InterruptQueue)
                {
                    writer.Write(interrupt);
                }
            }
        }

        #endregion
    }
}
