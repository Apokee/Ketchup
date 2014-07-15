using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ketchup.Api.v0;
using Ketchup.Utility;

namespace Ketchup.Modules
{
    /// <summary>
    /// Centrally Regulated Avionic Subsystem Handler (CRASH) device.
    /// </summary>
    [KSPModule("Device: CRASH")]
    internal sealed class ModuleKetchupCrash : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation : ushort
        {
            ControlGetRegRotation       = 0x0001,
            ControlGetRegTranslation    = 0x0002,
            ControlGetRegThrottle       = 0x0003,
            ControlGetRegTrim           = 0x0004,
            ControlGetRegStage          = 0x0005,
            ControlGetRegGear           = 0x0006,
            ControlGetRegLight          = 0x0007,
            ControlGetRegRcs            = 0x0008,
            ControlGetRegSas            = 0x0009,
            ControlGetRegBrakes         = 0x000A,
            ControlGetRegAbort          = 0x000B,
            ControlGetRegActionGroup    = 0x000C,

            ControlGetMemRotation       = 0x0201,
            ControlGetMemTranslation    = 0x0202,
            ControlGetMemThrottle       = 0x0203,
            ControlGetMemTrim           = 0x0204,
            ControlGetMemStage          = 0x0205,
            ControlGetMemGear           = 0x0206,
            ControlGetMemLight          = 0x0207,
            ControlGetMemRcs            = 0x0208,
            ControlGetMemSas            = 0x0209,
            ControlGetMemBrakes         = 0x020A,
            ControlGetMemAbort          = 0x020B,
            ControlGetMemActionGroup    = 0x020C,

            ControlSetRegRotation       = 0x0801,
            ControlSetRegTranslation    = 0x0802,
            ControlSetRegThrottle       = 0x0803,
            ControlSetRegTrim           = 0x0804,
            ControlSetRegStage          = 0x0805,
            ControlSetRegGear           = 0x0806,
            ControlSetRegLight          = 0x0807,
            ControlSetRegRcs            = 0x0808,
            ControlSetRegSas            = 0x0809,
            ControlSetRegBrake          = 0x080A,
            ControlSetRegAbort          = 0x080B,
            ControlSetRegActionGroup    = 0x080C,

            ControlSetMemRotation       = 0x0A01,
            ControlSetMemTranslation    = 0x0A02,
            ControlSetMemThrottle       = 0x0A03,
            ControlSetMemTrim           = 0x0A04,
            ControlSetMemStage          = 0x0A05,
            ControlSetMemGear           = 0x0A06,
            ControlSetMemLight          = 0x0A07,
            ControlSetMemRcs            = 0x0A08,
            ControlSetMemSas            = 0x0A09,
            ControlSetMemBrakes         = 0x0A0A,
            ControlSetMemAbort          = 0x0A0B,

            ControlSetMemActionGroup    = 0x0A0C,

            EventStageSpent             = 0x2001,

            ConfGetControlMask          = 0xE001,
            ConfSetControlMask          = 0xE401,

        }

        private enum ActionGroupState : ushort
        {
            Inactive    = 0x0000,
            Active      = 0x0001,
            Toggle      = 0x0002,
            Ignore      = 0xFFFF,
        }

        [Flags]
        private enum ControlFlag : ushort
        {
            Roll            = 1,
            Pitch           = 2,
            Yaw             = 4,
            TranslationX    = 8,
            TranslationY    = 16,
            TranslationZ    = 32,
            Throttle        = 64,
            RollTrim        = 128,
            PitchTrim       = 256,
            YawTrim         = 512,
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
            };

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "KSG CRASH Controller"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.KerbalSystems; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Crash; }
        }

        public ushort Version
        {
            get { return 0x0001; }
        }

        #endregion

        #region State

        [KSPField(isPersistant = true)]
        private ushort _controlMask;

        [KSPField(isPersistant = true)]
        private float _roll;

        [KSPField(isPersistant = true)]
        private float _pitch;

        [KSPField(isPersistant = true)]
        private float _yaw;

        [KSPField(isPersistant = true)]
        private float _translationX;

        [KSPField(isPersistant = true)]
        private float _translationY;

        [KSPField(isPersistant = true)]
        private float _translationZ;

        [KSPField(isPersistant = true)]
        private float _throttle;

        [KSPField(isPersistant = true)]
        private float _rollTrim;

        [KSPField(isPersistant = true)]
        private float _pitchTrim;

        [KSPField(isPersistant = true)]
        private float _yawTrim;

        [KSPField(isPersistant = true)]
        private ushort _stageSpentInterruptMessage;

        [KSPField(isPersistant = true)]
        private int _lastSpentStageInterrupted = -1;

        [KSPField(isPersistant = true)]
        private uint _stagesPendingActivation;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteRotation;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteTranslation;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteThrottle;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteTrim;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteStage;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteGear;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteLight;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteRcs;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteSas;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteBrakes;

        [KSPField(isPersistant = true)]
        private uint _memAddressWriteAbort;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadRotation;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadTranslation;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadThrottle;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadTrim;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadStage;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadGear;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadLight;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadRcs;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadSas;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadBrakes;

        [KSPField(isPersistant = true)]
        private uint _memAddressReadAbort;

        // ReSharper disable FieldCanBeMadeReadOnly.Local
        [KSPField(isPersistant = true)]
        private ConfigDictionary _memAddressesWriteActionGroups = new ConfigDictionary();
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        // ReSharper disable FieldCanBeMadeReadOnly.Local
        [KSPField(isPersistant = true)]
        private readonly ConfigDictionary _memAddressReadActionGroups = new ConfigDictionary();
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        // ReSharper disable FieldCanBeMadeReadOnly.Local
        [KSPField(isPersistant = true)]
        private FlightCtrlState _lastFlightCtrlState = new FlightCtrlState();
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        #endregion

        #region PartModule

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Equipped");

            return sb.ToString();
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                vessel.OnFlyByWire += OnFlyByWire;
            }
        }

        public void OnDestroy()
        {
            if (vessel != null)
            {
                // ReSharper disable once DelegateSubtraction
                vessel.OnFlyByWire -= OnFlyByWire;
            }
        }

        public override void OnUpdate()
        {
            if (_dcpu16 != null)
            {
                ActivateStageIfNecessary();
                InterruptStageSpentIfNecessary();

                WriteRotationToMemory();
                WriteTranslationToMemory();
                WriteThrottleToMemory();
                WriteTrimToMemory();
                WriteStageToMemory();
                WriteGearToMemory();
                WriteLightToMemory();
                WriteRcsToMemory();
                WriteSasToMemory();
                WriteBrakesToMemory();
                WriteAbortToMemory();
                WriteActionGroupsToMemory();

                ReadRotationFromMemory();
                ReadTranslationFromMemory();
                ReadThrottleFromMemory();
                ReadTrimFromMemory();
                ReadStageFromMemory();
                ReadGearFromMemory();
                ReadLightFromMemory();
                ReadRcsFromMemory();
                ReadSasFromMemory();
                ReadBrakesFromMemory();
                ReadAbortFromMemory();
                ReadActionGroupsFromMemory();
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
            if (_dcpu16 != null)
            {
                if ((_controlMask & (ushort)ControlFlag.Roll) == (ushort)ControlFlag.Roll)
                {
                    flightCtrlState.roll = _roll;
                }

                if ((_controlMask & (ushort)ControlFlag.Pitch) == (ushort)ControlFlag.Pitch)
                {
                    flightCtrlState.pitch = _pitch;
                }

                if ((_controlMask & (ushort)ControlFlag.Yaw) == (ushort)ControlFlag.Yaw)
                {
                    flightCtrlState.yaw = _yaw;
                }

                if ((_controlMask & (ushort)ControlFlag.TranslationX) == (ushort)ControlFlag.TranslationX)
                {
                    flightCtrlState.X = _translationX;
                }

                if ((_controlMask & (ushort)ControlFlag.TranslationY) == (ushort)ControlFlag.TranslationY)
                {
                    flightCtrlState.Y = _translationY;
                }

                if ((_controlMask & (ushort)ControlFlag.TranslationZ) == (ushort)ControlFlag.TranslationZ)
                {
                    flightCtrlState.Z = _translationZ;
                }

                if ((_controlMask & (ushort)ControlFlag.Throttle) == (ushort)ControlFlag.Throttle)
                {
                    flightCtrlState.mainThrottle = _throttle;
                }

                if ((_controlMask & (ushort)ControlFlag.RollTrim) == (ushort)ControlFlag.RollTrim)
                {
                    flightCtrlState.rollTrim = _rollTrim;
                }

                if ((_controlMask & (ushort)ControlFlag.PitchTrim) == (ushort)ControlFlag.PitchTrim)
                {
                    flightCtrlState.pitchTrim = _pitchTrim;
                }

                if ((_controlMask & (ushort)ControlFlag.YawTrim) == (ushort)ControlFlag.YawTrim)
                {
                    flightCtrlState.yawTrim = _yawTrim;
                }
            }

            _lastFlightCtrlState.CopyFrom(flightCtrlState);
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
                case InterruptOperation.ControlGetRegRotation:
                    _dcpu16.X = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.roll));
                    _dcpu16.Y = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.pitch));
                    _dcpu16.Z = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.yaw));
                    break;
                case InterruptOperation.ControlGetRegTranslation:
                    _dcpu16.X = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.X));
                    _dcpu16.Y = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.Y));
                    _dcpu16.Z = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.Z));
                    break;
                case InterruptOperation.ControlGetRegThrottle:
                    _dcpu16.X = MachineWord.FromUInt16(Range.ScaleUnsignedUnaryToUnsignedInt16(
                        _lastFlightCtrlState.mainThrottle
                    ));
                    break;
                case InterruptOperation.ControlGetRegTrim:
                    _dcpu16.X = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(
                        _lastFlightCtrlState.rollTrim
                    ));
                    _dcpu16.Y = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(
                        _lastFlightCtrlState.pitchTrim
                    ));
                    _dcpu16.Z = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(
                        _lastFlightCtrlState.yawTrim
                    ));
                    break;
                case InterruptOperation.ControlGetRegStage:
                    if (vessel.currentStage >= UInt16.MinValue && vessel.currentStage <= UInt16.MaxValue)
                    {
                        _dcpu16.X = MachineWord.FromUInt16((ushort)vessel.currentStage);
                    }
                    break;
                case InterruptOperation.ControlGetRegGear:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Gear]);
                    break;
                case InterruptOperation.ControlGetRegLight:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Light]);
                    break;
                case InterruptOperation.ControlGetRegRcs:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.RCS]);
                    break;
                case InterruptOperation.ControlGetRegSas:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.SAS]);
                    break;
                case InterruptOperation.ControlGetRegBrakes:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Brakes]);
                    break;
                case InterruptOperation.ControlGetRegAbort:
                    _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Abort]);
                    break;
                case InterruptOperation.ControlGetRegActionGroup:
                    {
                        KSPActionGroup actionGroup;
                        if (ActionGroupMapping.TryGetValue(_dcpu16.B, out actionGroup))
                        {
                            _dcpu16.X = MachineWord.FromBoolean(vessel.ActionGroups[actionGroup]);
                        }
                    }
                    break;
                case InterruptOperation.ControlGetMemRotation:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressWriteRotation = _dcpu16.B;
                        WriteRotationToMemory();
                    }
                    break;
                case InterruptOperation.ControlGetMemTranslation:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressWriteTranslation = _dcpu16.B;
                        WriteTranslationToMemory();
                    }
                    break;
                case InterruptOperation.ControlGetMemThrottle:
                    _memAddressWriteThrottle = _dcpu16.B;
                    WriteThrottleToMemory();
                    break;
                case InterruptOperation.ControlGetMemTrim:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressWriteTrim = _dcpu16.B;
                        WriteTrimToMemory();
                    }
                    break;
                case InterruptOperation.ControlGetMemStage:
                    _memAddressWriteStage = _dcpu16.B;
                    WriteStageToMemory();
                    break;
                case InterruptOperation.ControlGetMemGear:
                    _memAddressWriteGear = _dcpu16.B;
                    WriteGearToMemory();
                    break;
                case InterruptOperation.ControlGetMemLight:
                    _memAddressWriteLight = _dcpu16.B;
                    WriteLightToMemory();
                    break;
                case InterruptOperation.ControlGetMemRcs:
                    _memAddressWriteRcs = _dcpu16.B;
                    WriteRcsToMemory();
                    break;
                case InterruptOperation.ControlGetMemSas:
                    _memAddressWriteSas = _dcpu16.B;
                    WriteSasToMemory();
                    break;
                case InterruptOperation.ControlGetMemBrakes:
                    _memAddressWriteBrakes = _dcpu16.B;
                    WriteBrakesToMemory();
                    break;
                case InterruptOperation.ControlGetMemAbort:
                    _memAddressWriteAbort = _dcpu16.B;
                    WriteAbortToMemory();
                    break;
                case InterruptOperation.ControlGetMemActionGroup:
                    {
                        KSPActionGroup actionGroup;
                        if (ActionGroupMapping.TryGetValue(_dcpu16.C, out actionGroup))
                        {
                            if (_dcpu16.B == 0x0000)
                            {
                                if (_memAddressesWriteActionGroups.ContainsKey(actionGroup.ToString()))
                                {
                                    _memAddressesWriteActionGroups.Remove(actionGroup.ToString());
                                }
                            }
                            else
                            {
                                _memAddressesWriteActionGroups[actionGroup.ToString()] =
                                    _dcpu16.B.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        WriteActionGroupsToMemory();
                    }
                    break;
                case InterruptOperation.ControlSetRegRotation:
                    _roll = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                    _pitch = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                    _yaw = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                    break;
                case InterruptOperation.ControlSetRegTranslation:
                    _translationX = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                    _translationY = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                    _translationZ = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                    break;
                case InterruptOperation.ControlSetRegThrottle:
                    _throttle = Range.ScaleUnsignedInt16ToUnsignedUnary(MachineWord.ToUInt16(_dcpu16.B));
                    break;
                case InterruptOperation.ControlSetRegTrim:
                    _rollTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                    _pitchTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                    _yawTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                    break;
                case InterruptOperation.ControlSetRegStage:
                    _stagesPendingActivation++;
                    break;
                case InterruptOperation.ControlSetRegGear:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.Gear);
                    break;
                case InterruptOperation.ControlSetRegLight:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.Light);
                    break;
                case InterruptOperation.ControlSetRegRcs:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.RCS);
                    break;
                case InterruptOperation.ControlSetRegSas:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.SAS);
                    break;
                case InterruptOperation.ControlSetRegBrake:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.Brakes);
                    break;
                case InterruptOperation.ControlSetRegAbort:
                    HandleSetActionGroup(_dcpu16.B, KSPActionGroup.Abort);
                    break;
                case InterruptOperation.ControlSetRegActionGroup:
                    {
                        KSPActionGroup actionGroup;
                        if (ActionGroupMapping.TryGetValue(_dcpu16.C, out actionGroup))
                        {
                            HandleSetActionGroup(_dcpu16.B, actionGroup);

                        }
                    }
                    break;
                case InterruptOperation.ControlSetMemRotation:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressReadRotation = _dcpu16.B;
                        ReadRotationFromMemory();
                    }
                    break;
                case InterruptOperation.ControlSetMemTranslation:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressReadTranslation = _dcpu16.B;
                        ReadTranslationFromMemory();
                    }
                    break;
                case InterruptOperation.ControlSetMemThrottle:
                    _memAddressReadThrottle = _dcpu16.B;
                    ReadThrottleFromMemory();
                    break;
                case InterruptOperation.ControlSetMemTrim:
                    if (IsMemoryRangeValid(_dcpu16.B, 3))
                    {
                        _memAddressReadTrim = _dcpu16.B;
                        ReadTrimFromMemory();
                    }
                    break;
                case InterruptOperation.ControlSetMemStage:
                    _memAddressReadStage = _dcpu16.B;
                    ReadStageFromMemory();
                    break;
                case InterruptOperation.ControlSetMemGear:
                    _memAddressReadGear = _dcpu16.B;
                    ReadGearFromMemory();
                    break;
                case InterruptOperation.ControlSetMemLight:
                    _memAddressReadLight = _dcpu16.B;
                    ReadLightFromMemory();
                    break;
                case InterruptOperation.ControlSetMemRcs:
                    _memAddressReadRcs = _dcpu16.B;
                    ReadRcsFromMemory();
                    break;
                case InterruptOperation.ControlSetMemSas:
                    _memAddressReadSas = _dcpu16.B;
                    ReadSasFromMemory();
                    break;
                case InterruptOperation.ControlSetMemBrakes:
                    _memAddressReadBrakes = _dcpu16.B;
                    ReadBrakesFromMemory();
                    break;
                case InterruptOperation.ControlSetMemAbort:
                    _memAddressReadAbort = _dcpu16.B;
                    ReadAbortFromMemory();
                    break;
                case InterruptOperation.ControlSetMemActionGroup:
                    {
                        KSPActionGroup actionGroup;
                        if (ActionGroupMapping.TryGetValue(_dcpu16.C, out actionGroup))
                        {
                            if (_dcpu16.B == 0x0000)
                            {
                                if (_memAddressReadActionGroups.ContainsKey(actionGroup.ToString()))
                                {
                                    _memAddressReadActionGroups.Remove(actionGroup.ToString());
                                }
                            }
                            else
                            {
                                _memAddressReadActionGroups[actionGroup.ToString()] =
                                    _dcpu16.B.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        ReadActionGroupsFromMemory();
                    }
                    break;
                case InterruptOperation.EventStageSpent:
                    _stageSpentInterruptMessage = _dcpu16.B;
                    break;
                case InterruptOperation.ConfGetControlMask:
                    _dcpu16.B = _controlMask;
                    break;
                case InterruptOperation.ConfSetControlMask:
                    _controlMask = _dcpu16.B;
                    break;
            }

            return 0; // TODO: Set to a reasonable value
        }

        private void WriteRotationToMemory()
        {
            if (_memAddressWriteRotation != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteRotation + 0] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.roll));
                _dcpu16.Memory[_memAddressWriteRotation + 1] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.pitch));
                _dcpu16.Memory[_memAddressWriteRotation + 2] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.yaw));
            }
        }

        private void WriteTranslationToMemory()
        {
            if (_memAddressWriteTranslation != 0x000)
            {
                _dcpu16.Memory[_memAddressWriteTranslation + 0] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.X));
                _dcpu16.Memory[_memAddressWriteTranslation + 1] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.Y));
                _dcpu16.Memory[_memAddressWriteTranslation + 2] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.Z));
            }
        }

        private void WriteThrottleToMemory()
        {
            if (_memAddressWriteThrottle != 0x000)
            {
                _dcpu16.Memory[_memAddressWriteThrottle] =
                    MachineWord.FromUInt16(Range.ScaleUnsignedUnaryToUnsignedInt16(_lastFlightCtrlState.mainThrottle));
            }
        }

        private void WriteTrimToMemory()
        {
            if (_memAddressWriteTrim != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteTrim + 0] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.rollTrim));
                _dcpu16.Memory[_memAddressWriteTrim + 1] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.pitchTrim));
                _dcpu16.Memory[_memAddressWriteTrim + 2] =
                    MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(_lastFlightCtrlState.yawTrim));
            }
        }

        private void WriteStageToMemory()
        {
            if (_memAddressWriteStage != 0x0000)
            {
                if (vessel.currentStage >= UInt16.MinValue && vessel.currentStage <= UInt16.MaxValue)
                {
                    _dcpu16.Memory[_memAddressWriteStage] = MachineWord.FromUInt16((ushort)vessel.currentStage);
                }
            }
        }

        private void WriteGearToMemory()
        {
            if (_memAddressWriteGear != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteGear] =
                    MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Gear]);
            }
        }

        private void WriteLightToMemory()
        {
            if (_memAddressWriteLight != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteLight] =
                    MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Light]);
            }
        }

        private void WriteRcsToMemory()
        {
            if (_memAddressWriteRcs != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteRcs] =
                    MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.RCS]);
            }
        }

        private void WriteSasToMemory()
        {
            if (_memAddressWriteSas != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteSas] =
                     MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.SAS]);
            }
        }

        private void WriteBrakesToMemory()
        {
            if (_memAddressWriteBrakes != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteBrakes] =
                     MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Brakes]);
            }
        }

        private void WriteAbortToMemory()
        {
            if (_memAddressWriteAbort != 0x0000)
            {
                _dcpu16.Memory[_memAddressWriteAbort] =
                     MachineWord.FromBoolean(vessel.ActionGroups[KSPActionGroup.Brakes]);
            }
        }

        private void WriteActionGroupsToMemory()
        {
            foreach(var actionGroupAddress in _memAddressesWriteActionGroups)
            {
                if (Enum.IsDefined(typeof(KSPActionGroup), actionGroupAddress.Key))
                {
                    var actionGroup = (KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), actionGroupAddress.Key);
                    ushort address;

                    if (UInt16.TryParse(actionGroupAddress.Value, out address))
                    {
                        _dcpu16.Memory[address] = MachineWord.FromBoolean(vessel.ActionGroups[actionGroup]);
                    }
                }
            }
        }

        private void ReadRotationFromMemory()
        {
            if (_memAddressReadRotation != 0x0000)
            {
                _roll = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 0]
                ));
                _pitch = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 1]
                ));
                _yaw = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 2]
                ));
            }
        }

        private void ReadTranslationFromMemory()
        {
            if (_memAddressReadTranslation != 0x0000)
            {
                _translationX = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 0]
                ));
                _translationY = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 1]
                ));
                _translationZ = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadRotation + 2]
                ));
            }
        }

        private void ReadThrottleFromMemory()
        {
            if (_memAddressReadThrottle != 0x0000)
            {
                _throttle = Range.ScaleUnsignedInt16ToUnsignedUnary(MachineWord.ToUInt16(
                    _dcpu16.Memory[_memAddressReadThrottle]
                ));
            }
        }

        private void ReadTrimFromMemory()
        {
            if (_memAddressReadTrim != 0x0000)
            {
                _rollTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadTrim + 0]
                ));
                _pitchTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadTrim + 1]
                ));
                _yawTrim = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(
                    _dcpu16.Memory[_memAddressReadTrim + 2]
                ));
            }
        }

        private void ReadStageFromMemory()
        {
            if (_memAddressReadStage != 0x0000)
            {
                if (_dcpu16.Memory[_memAddressReadStage] == 0x0001)
                {
                    _stagesPendingActivation++;
                    _dcpu16.Memory[_memAddressReadStage] = 0x0000;
                }
            }
        }

        private void ReadGearFromMemory()
        {
            if (_memAddressReadGear != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadGear], KSPActionGroup.Gear);

                _dcpu16.Memory[_memAddressReadGear] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadLightFromMemory()
        {
            if (_memAddressReadLight != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadLight], KSPActionGroup.Light);

                _dcpu16.Memory[_memAddressReadLight] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadRcsFromMemory()
        {
            if (_memAddressReadRcs != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadRcs], KSPActionGroup.RCS);

                _dcpu16.Memory[_memAddressReadRcs] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadSasFromMemory()
        {
            if (_memAddressReadSas != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadSas], KSPActionGroup.SAS);

                _dcpu16.Memory[_memAddressReadSas] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadBrakesFromMemory()
        {
            if (_memAddressReadBrakes != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadBrakes], KSPActionGroup.Brakes);

                _dcpu16.Memory[_memAddressReadBrakes] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadAbortFromMemory()
        {
            if (_memAddressReadAbort != 0x0000)
            {
                HandleSetActionGroup(_dcpu16.Memory[_memAddressReadAbort], KSPActionGroup.Abort);

                _dcpu16.Memory[_memAddressReadAbort] = (ushort)ActionGroupState.Ignore;
            }
        }

        private void ReadActionGroupsFromMemory()
        {
            foreach (var actionGroupAddress in _memAddressReadActionGroups)
            {
                if (Enum.IsDefined(typeof(KSPActionGroup), actionGroupAddress.Key))
                {
                    var actionGroup = (KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), actionGroupAddress.Key);
                    ushort address;

                    if (UInt16.TryParse(actionGroupAddress.Value, out address))
                    {
                        HandleSetActionGroup(_dcpu16.Memory[address], actionGroup);
                        _dcpu16.Memory[address] = (ushort)ActionGroupState.Ignore;
                    }
                }
            }
        }

        private void HandleSetActionGroup(ushort state, KSPActionGroup actionGroup)
        {
            switch ((ActionGroupState)state)
            {
                case ActionGroupState.Inactive:
                    vessel.ActionGroups.SetGroup(actionGroup, false);
                    break;
                case ActionGroupState.Active:
                    vessel.ActionGroups.SetGroup(actionGroup, true);
                    break;
                case ActionGroupState.Toggle:
                    vessel.ActionGroups.ToggleGroup(actionGroup);
                    break;
            }
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

        private bool IsMemoryRangeValid(ushort baseAddress, ushort length)
        {
            return (baseAddress + length - 1) <= UInt16.MaxValue;
        }

        #endregion
    }
}
