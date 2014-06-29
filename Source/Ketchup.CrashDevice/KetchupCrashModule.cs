using System;
using System.Collections.Generic;
using System.Linq;
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

            EventStageSpent = 0x8001,
        }

        private enum Mode : ushort
        {
            // ReSharper disable once UnusedMember.Local
            Passive     = 0x0000,
            Exclusive   = 0x0001,
            // ReSharper disable once UnusedMember.Local
            Cooperative = 0x0002, // TODO: Implement cooperative mode
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

        [KSPField(guiName = "CRASH Mode", guiActive = true, isPersistant = true)]
        private Mode _mode;

        [KSPField(guiName = "CRASH Roll", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _roll;

        [KSPField(guiName = "CRASH Pitch", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _pitch;

        [KSPField(guiName = "CRASH Yaw", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _yaw;

        [KSPField(guiName = "CRASH Translation X", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationX;

        [KSPField(guiName = "CRASH Translation Y", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationY;

        [KSPField(guiName = "CRASH Translation Z", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _translationZ;

        [KSPField(guiName = "CRASH Throttle", guiActive = true, guiFormat = "F3", isPersistant = true)]
        private float _throttle;

        [KSPField(isPersistant = true)]
        private ushort _stageSpentInterruptMessage;

        [KSPField(isPersistant = true)]
        private int _lastSpentStageInterrupted = -1;

        [KSPField(isPersistant = true)]
        private uint _stagesPendingActivation;

        #endregion

        #region PartModule

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                // TODO: Need to unregister this at some point
                vessel.OnFlyByWire += OnFlyByWire;
            }
        }

        public override void OnUpdate()
        {
            if (_dcpu16 != null)
            {
                ActivateStageIfNecessary();
                InterruptStageSpentIfNecessary();
            }
        }

        private void InterruptStageSpentIfNecessary()
        {
            if (_stageSpentInterruptMessage != 0)
            {
                if (_lastSpentStageInterrupted != vessel.currentStage && IsStageSpent())
                {
                    _dcpu16.Interrupt(_stageSpentInterruptMessage);

                    _lastSpentStageInterrupted = vessel.currentStage;
                }
            }
        }

        private void ActivateStageIfNecessary()
        {
            if (_stagesPendingActivation > 0 && vessel.currentStage > 0)
            {
                var originalStage = vessel.currentStage;

                // HACK: This is only good for the active vessel, which this vessel might not be
                Staging.ActivateNextStage();

                if (vessel.currentStage != originalStage)
                {
                    _stagesPendingActivation--;
                }
            }
        }

        private void OnFlyByWire(FlightCtrlState flightCtrlState)
        {
            if (_dcpu16 != null && _mode == Mode.Exclusive)
            {
                flightCtrlState.roll = _roll;
                flightCtrlState.pitch = _pitch;
                flightCtrlState.yaw = _yaw;

                flightCtrlState.X = _translationX;
                flightCtrlState.Y = _translationY;
                flightCtrlState.Z = _translationZ;

                flightCtrlState.mainThrottle = _throttle;
            }
        }

        #endregion

        #region IDevice

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
                        if (actionGroup == KSPActionGroup.Stage)
                        {
                            _stagesPendingActivation++;
                        }
                        else
                        {
                            // TODO: Determine what setting true/false does for "trigger" action groups like Abort
                            vessel.ActionGroups[actionGroup] = MachineWord.ToBoolean(_dcpu16.C);
                        }
                    }
                    break;
                case InterruptOperation.EventStageSpent:
                    _stageSpentInterruptMessage = _dcpu16.B;
                    break;
            }

            return 0; // TODO: Set to a reasonable value
        }

        #endregion

        #region Helpers

        private bool IsStageSpent()
        {
            var engines = vessel.Parts.Where(IsInCurrentStage).SelectMany(GetEngines).ToArray();

            return engines.Any() && engines.All(IsEngineSpent);
        }

        private bool IsInCurrentStage(Part p)
        {
            return p.inverseStage == vessel.currentStage;
        }

        private static IEnumerable<IEngineStatus> GetEngines(Part p)
        {
            return p.Modules.OfType<IEngineStatus>();
        }

        private static bool IsEngineSpent(IEngineStatus engine)
        {
            return !engine.isOperational;
        }

        #endregion
    }
}
