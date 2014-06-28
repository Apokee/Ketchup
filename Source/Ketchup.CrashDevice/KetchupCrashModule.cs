using System;

namespace Ketchup.CrashDevice
{
    /// <summary>
    /// Centrally Regulated Avionic Subsystem Haptics (CRASH) controller device.
    /// </summary>
    internal sealed class KetchupCrashModule : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation
        {
            SetMode         = 0x0001,
            SetRotation     = 0x0002,
            SetTranslation  = 0x0003,
            SetThrottle     = 0x0004,
            SetActionGroup  = 0x0005,
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
                case InterruptOperation.SetMode:
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
