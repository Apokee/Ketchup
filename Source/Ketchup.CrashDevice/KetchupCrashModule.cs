using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<ushort, KSPActionGroup> ActionGroupMapping =
            new Dictionary<ushort, KSPActionGroup>
            {
                {0x0001, KSPActionGroup.Custom01},
                {0x0002, KSPActionGroup.Custom02},
                {0x0003, KSPActionGroup.Custom03},
                {0x0004, KSPActionGroup.Custom04},
                {0x0005, KSPActionGroup.Custom05},
                {0x0006, KSPActionGroup.Custom06},
                {0x0007, KSPActionGroup.Custom07},
                {0x0008, KSPActionGroup.Custom08},
                {0x0009, KSPActionGroup.Custom09},
                {0x000A, KSPActionGroup.Custom10},
                {0x8001, KSPActionGroup.Stage},
                {0x8002, KSPActionGroup.Gear},
                {0x8003, KSPActionGroup.Light},
                {0x8004, KSPActionGroup.RCS},
                {0x8005, KSPActionGroup.SAS},
                {0x8006, KSPActionGroup.Brakes},
                {0x8007, KSPActionGroup.Abort}
            };

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

        [KSPField(guiName = "KSG CRASH Mode", guiActive = true, isPersistant = true)]
        private Mode _mode;

        [KSPField(guiName = "KSG CRASH Roll", guiActive = true, guiFormat = "F3", isPersistant = true)]
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

        [KSPField(guiName = "KSG CRASH Throttle", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _throttle;

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
                    _throttle = Range.ScaleUnsignedInt16ToUnsignedUnary(MachineWord.ToUInt16(_dcpu16.B));
                    break;
                case InterruptOperation.SetActionGroup:
                    KSPActionGroup actionGroup;
                    if (ActionGroupMapping.TryGetValue(_dcpu16.B, out actionGroup))
                    {
                        // TODO: Determine what setting true/false does for "trigger" action groups like Abort
                        vessel.ActionGroups[actionGroup] = MachineWord.ToBoolean(_dcpu16.C);
                    }
                    break;
            }

            return 0; // TODO: Set to a reasonable value
        }
    }
}
