using System;

namespace Ketchup.CrashDevice
{
    /// <summary>
    /// Centrally Regulated Avionic Subsystem Haptics (CRASH) controller device.
    /// </summary>
    internal sealed class KetchupCrashModule : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation : ushort
        {
            GetStage        = 0x0001,

            SetMode         = 0x4001,
            SetRotation     = 0x4002,
            SetTranslation  = 0x4003,
            SetThrottle     = 0x4004,
            SetActionGroup  = 0x4005,
        }

        private enum Mode : ushort
        {
            Passive     = 0x0000,
            Exclusive   = 0x0001,
            Cooperative = 0x0002,
        }

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "KSG CRASH Controller"; }
        }

        public uint ManufacturerId
        {
            get { return 0xcae02013; } // TODO: Replace with constant
        }

        public uint DeviceId
        {
            get { return 0xcae10001; }
        }

        public ushort Version
        {
            get { return 0x0001; }
        }

        #endregion

        #region State

        [KSPField(guiName="KSG CRASH Controller Mode", guiActive=true, isPersistant=true)]
        private Mode _mode;

        #endregion

        private IDcpu16 _dcpu16;

        public void OnConnect(IDcpu16 dcpu16)
        {
            _dcpu16 = dcpu16;
        }

        public void OnDisconnect()
        {
            _dcpu16 = null;
        }

        public int OnInterrupt()
        {
            if (_dcpu16 == null) { return 0; }

            switch((InterruptOperation)_dcpu16.A)
            {
                case InterruptOperation.GetStage:
                    break;
                case InterruptOperation.SetMode:
                    if (Enum.IsDefined(typeof(Mode), _dcpu16.B))
                    {
                        _mode = (Mode)_dcpu16.B;
                    }
                    break;
                case InterruptOperation.SetRotation:
                    break;
                case InterruptOperation.SetTranslation:
                    break;
                case InterruptOperation.SetThrottle:
                    break;
                case InterruptOperation.SetActionGroup:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return 0; // TODO: Set to a reasonable value
        }
    }
}
