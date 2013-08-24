using System;
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

        public void Load(string state)
        {
            try
            {
                using (var reader = new BinaryStateReader(state))
                {
                    // Header
                    var magicNumber = reader.ReadUInt32();
                    if (magicNumber != MagicNumber)
                    {
                        throw new LoadStateException("DCPU-16", String.Format("Magic number is incorrect. Expected: {0:X}. Found: {1:X}.", MagicNumber, magicNumber));
                    }

                    var versionNumber = reader.ReadUInt32();
                    if (versionNumber != VersionNumber)
                    {
                        throw new LoadStateException("DCPU-16", String.Format("Unsupported version number: {0}", versionNumber));
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

        public string Save()
        {
            using (var writer = new BinaryStateWriter())
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

                return writer.ToString();
            }
        }

        #endregion
    }
}
