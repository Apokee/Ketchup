using System;
using System.Text;
using Ketchup.Api.v0;

namespace Ketchup.Devices
{
    [KSPModule("Device: Clock")]
    public sealed class KetchupGenericClockModule : PartModule, IDevice
    {
        #region Constants

        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyIsClockEnabled = "IsClockEnabled";
        private const string ConfigKeyPeriod = "Period";
        private const string ConfigKeyElapsedTicks = "ElapsedTicks";
        private const string ConfigKeyInterruptMessage = "InterruptMessage";
        private const string ConfigKeyTimeUntilNextTick = "TimeUntilNextTick";

        private const uint ConfigVersion = 1;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;

        private bool _isClockEnabled;
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
            _isClockEnabled = default(bool);
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
                        _isClockEnabled = _dcpu16.B != 0;
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

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Equipped");

            return sb.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            uint version;
            if (UInt32.TryParse(node.GetValue(ConfigKeyVersion), out version) && version == 1)
            {
                bool isClockEnabled;
                if (Boolean.TryParse(node.GetValue(ConfigKeyIsClockEnabled), out isClockEnabled))
                {
                    _isClockEnabled = isClockEnabled;
                }

                float period;
                if (Single.TryParse(node.GetValue(ConfigKeyPeriod), out period))
                {
                    _period = period;
                }

                ushort elapsedTicks;
                if (UInt16.TryParse(node.GetValue(ConfigKeyElapsedTicks), out elapsedTicks))
                {
                    _elapsedTicks = elapsedTicks;
                }

                ushort interruptMessage;
                if (UInt16.TryParse(node.GetValue(ConfigKeyInterruptMessage), out interruptMessage))
                {
                    _interruptMessage = interruptMessage;
                }

                float timeUntilNextTick;
                if (Single.TryParse(node.GetValue(ConfigKeyTimeUntilNextTick), out timeUntilNextTick))
                {
                    _timeUntilNextTick = timeUntilNextTick;
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);
            node.AddValue(ConfigKeyIsClockEnabled, _isClockEnabled);
            node.AddValue(ConfigKeyPeriod, _period);
            node.AddValue(ConfigKeyElapsedTicks, _elapsedTicks);
            node.AddValue(ConfigKeyInterruptMessage, _interruptMessage);
            node.AddValue(ConfigKeyTimeUntilNextTick, _timeUntilNextTick);
        }

        public override void OnUpdate()
        {
            if (_isClockEnabled)
            {
                var gameTimePassed = TimeWarp.deltaTime;

                if (gameTimePassed >= _timeUntilNextTick)
                {
                    var correction = gameTimePassed - _timeUntilNextTick;

                    _timeUntilNextTick = _period - correction;
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
