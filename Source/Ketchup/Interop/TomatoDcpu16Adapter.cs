using System.Collections.Generic;
using Tomato;

namespace Ketchup.Interop
{
    internal sealed class TomatoDcpu16Adapter : IDcpu16
    {
        private readonly DCPU _dcpu16;

        /// <summary>
        /// Is true if Interrupt() has been called, but no call to Execute() has occurred since.
        /// </summary>
        /// <remarks>
        /// This is necessary to tell if the DCPU needs to wake up after a halt to process an interrupt.
        /// </remarks>
        private bool _interruptWakeFlag;

        public ushort A
        {
            get { return _dcpu16.A; }
            set { _dcpu16.A = value; }
        }

        public ushort B
        {
            get { return _dcpu16.B; }
            set { _dcpu16.B = value; }
        }

        public ushort C
        {
            get { return _dcpu16.C; }
            set { _dcpu16.C = value; }
        }

        public ushort X
        {
            get { return _dcpu16.X; }
            set { _dcpu16.X = value; }
        }

        public ushort Y
        {
            get { return _dcpu16.Y; }
            set { _dcpu16.Y = value; }
        }

        public ushort Z
        {
            get { return _dcpu16.Z; }
            set { _dcpu16.Z = value; }
        }

        public ushort I
        {
            get { return _dcpu16.I; }
            set { _dcpu16.I = value; }
        }

        public ushort J
        {
            get { return _dcpu16.J; }
            set { _dcpu16.J = value; }
        }

        public ushort PC
        {
            get { return _dcpu16.PC; }
            set { _dcpu16.PC = value; }
        }

        public ushort SP
        {
            get { return _dcpu16.SP; }
            set { _dcpu16.SP = value; }
        }

        public ushort EX
        {
            get { return _dcpu16.EX; }
            set { _dcpu16.EX = value; }
        }

        public ushort IA
        {
            get { return _dcpu16.IA; }
            set { _dcpu16.IA = value; }
        }

        public ushort[] Memory
        {
            get { return _dcpu16.Memory; }
        }

        public bool IsOnFire
        {
            get { return _dcpu16.IsOnFire; }
            set { _dcpu16.IsOnFire = value; }
        }

        public bool IsInterruptQueueEnabled
        {
            get { return _dcpu16.InterruptQueueEnabled; }
            set { _dcpu16.InterruptQueueEnabled = value; }
        }

        public Queue<ushort> InterruptQueue
        {
            get { return _dcpu16.InterruptQueue; }
        }

        public TomatoDcpu16Adapter(DCPU dcpu16)
        {
            _dcpu16 = dcpu16;
            _interruptWakeFlag = false;
        }

        public ushort OnConnect(IDevice device)
        {
            _dcpu16.ConnectDevice(new TomatoDeviceAdapter(device));

            return (ushort)(_dcpu16.Devices.Count - 1);
        }

        public void OnDisconnect(ushort deviceIndex)
        {
            _dcpu16.Devices[deviceIndex] = null;
        }

        public int Execute()
        {
            _interruptWakeFlag = false;

            var cyclesBefore = _dcpu16.TotalCycles;
            _dcpu16.Execute(1);
            var cyclesAfter = _dcpu16.TotalCycles;

            return cyclesAfter - cyclesBefore;
        }

        public void Interrupt(ushort message)
        {
            _interruptWakeFlag = true;

            _dcpu16.FireInterrupt(message);
        }

        public bool IsHalted()
        {
            return _dcpu16.Memory[_dcpu16.PC] == 0x8382; // ADD PC, -1
        }

        public bool IsPendingWakeUp()
        {
            return IsHalted() && _interruptWakeFlag;
        }
    }
}
