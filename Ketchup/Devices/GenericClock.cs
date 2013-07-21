using Ketchup.Constants;
using Tomato.Hardware;

namespace Ketchup.Devices
{
    public sealed class GenericClock : Device
    {
        public override string FriendlyName
        {
            get { return "Generic Clock (compatible)"; }
        }

        public override uint ManufacturerID
        {
            get { return (uint)ManufacturerId.Unknown; }
        }

        public override uint DeviceID
        {
            get { return (uint)DeviceId.GenericClock; }
        }

        public override ushort Version
        {
            get { return 0x0001; }
        }

        private ushort _frequency;
        private ushort _elapsedTicks;
        private ushort _interruptMessage;
        private int _elapstedHardwareTicks;

        public override int HandleInterrupt()
        {
            switch (AttachedCPU.A)
            {
                case 0:
                    _frequency = AttachedCPU.B;
                    _elapsedTicks = 0;
                    break;
                case 1:
                    AttachedCPU.C = _elapsedTicks;
                    break;
                case 2:
                    _interruptMessage = AttachedCPU.B;
                    break;
            }
            return 0;
        }

        public override void Tick()
        {
            _elapstedHardwareTicks++;
            if (_elapstedHardwareTicks >= _frequency)
            {
                _elapstedHardwareTicks = 0;
                _elapsedTicks++;
                if (_interruptMessage != 0)
                    AttachedCPU.FireInterrupt(_interruptMessage);
            }
            base.Tick();
        }

        public override void Reset()
        {
            _elapsedTicks = _interruptMessage = _frequency = 0;
        }
    }
}
