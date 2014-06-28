using System;
using Ketchup.Utility;

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

        [KSPField(guiName="KSG CRASH Mode", guiActive=true, isPersistant=true)]
        private Mode _mode;

        [KSPField(guiName="KSG CRASH Roll", guiActive=true, guiFormat="F3", isPersistant=true)]
        private float _roll;

        [KSPField(guiName = "KSG CRASH Pitch", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _pitch;

        [KSPField(guiName = "KSG CRASH Yaw", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _yaw;

        [KSPField(guiName = "KSG CRASH Translation X", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationX;

        [KSPField(guiName = "KSG CRASH Translation Y", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationY;

        [KSPField(guiName = "KSG CRASH Translation Z", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationZ;

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
                    if (vessel.currentStage >= UInt16.MinValue && vessel.currentStage <= UInt16.MaxValue)
                    {
                        _dcpu16.B = MachineWord.FromUInt16((ushort)vessel.currentStage);
                    }
                    break;
                case InterruptOperation.SetMode:
                    if (Enum.IsDefined(typeof(Mode), _dcpu16.B))
                    {
                        _mode = (Mode)_dcpu16.B;
                    }
                    break;
                case InterruptOperation.SetRotation:
                    _roll = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                    _pitch = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                    _yaw = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                    break;
                case InterruptOperation.SetTranslation:
                    _translationX = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                    _translationY = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                    _translationZ = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
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
