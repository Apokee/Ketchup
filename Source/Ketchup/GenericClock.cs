using Ketchup.Api;

namespace Ketchup
{
    public sealed class GenericClock : PartModule, IDevice
    {
        #region Instance Fields

        private IDcpu16 _dcpu16;

        private bool _enabled;
        private float _period;
        private ushort _elapsedTicks;
        private ushort _interruptMessage;
        private float _timeUntilNextTick;

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "Generic Clock (compatible)"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.Unknown; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.GenericClock; }
        }

        public ushort Version
        {
            get { return 0x0001; }
        }

        #endregion

        #region IDevice Methods

        public void OnConnect(IDcpu16 dcpu16)
        {
            _dcpu16 = dcpu16;
        }

        public void OnDisconnect()
        {
            _dcpu16 = default(IDcpu16);
            _enabled = default(bool);
            _period = default(float);
            _elapsedTicks = default(ushort);
            _interruptMessage = default(ushort);
            _timeUntilNextTick = default(float);
        }

        public int OnInterrupt()
        {
            if (_dcpu16 != null)
            {
                switch (_dcpu16.A)
                {
                    case 0:
                        _enabled = _dcpu16.B != 0;
                        _period = _dcpu16.B / 60f;
                        _elapsedTicks = 0;
                        break;
                    case 1:
                        _dcpu16.C = _elapsedTicks;
                        break;
                    case 2:
                        _interruptMessage = _dcpu16.B;
                        break;
                }
            }

            return 0;
        }

        #endregion

        #region PartModule Methods

        public override void OnUpdate()
        {
            if (_enabled)
            {
                var gameTimePassed = TimeWarp.deltaTime;

                if (gameTimePassed > _timeUntilNextTick)
                {
                    _timeUntilNextTick = _period;
                    _elapsedTicks += 1;

                    if (_interruptMessage != 0)
                    {
                        _dcpu16.Interrupt(_interruptMessage);
                    }
                }
                else
                {
                    _timeUntilNextTick -= gameTimePassed;
                }
            }
        }

        #endregion
    }
}
