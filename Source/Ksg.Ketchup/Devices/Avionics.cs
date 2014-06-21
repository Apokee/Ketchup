using System.Collections.Generic;
using System.Linq;
using Ksg.Ketchup.Extensions;
using Ksg.Ketchup.Mathematics;
using Ksg.Ketchup.Utility;
using UnityEngine;

namespace Ksg.Ketchup
{
    internal sealed class Avionics : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation
        {
            GetActive           = 0x0000,
            GetRotation         = 0x0001,
            GetTranslation      = 0x0002,
            GetThrottle         = 0x0003,

            GetOrientation      = 0x000C,
            
            SetActive           = 0x4000,
            SetRotation         = 0x4001,
            SetTranslation      = 0x4002,
            SetThrottle         = 0x4003,
            SetSas              = 0x4004,
            SetStage            = 0x4006,

            EventSpentStage     = 0xC001,
            EventAltitudeSea    = 0xC002,
        }

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            // TODO: All friendly names should be 12-characters long, emulating x86 CPUID
            // http://en.wikipedia.org/wiki/CPUID
            get { return "KSG Avionics"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.KerbalSystems; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Avionics; }
        }

        public ushort Version
        {
            get { return 0x0000; }
        }

        #endregion

        #region Instance Members

        private readonly State _state;
        private IDcpu16 _dcpu16;

        #endregion

        #region Constructors

        public Avionics()
        {
            _state = new State();
        }

        #endregion

        #region PartModule Members

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
#if DEBUG
                DebugSetupAxes();
#endif

                // TODO: What happens if we dock and get a new vessel?
                // TODO: Need to unregister this callback
                vessel.OnFlyByWire += OnFlyByWire;
            }
        }

        public override void OnUpdate()
        {
#if DEBUG
            DebugUpdateAxes();
#endif

            if (_dcpu16 != null)
            {
                if (_state.SpentStageMessage != 0)
                {
                    if (_state.LastSpentStageInterrupted != Staging.CurrentStage && IsStageSpent())
                    {
                        _dcpu16.Interrupt(_state.SpentStageMessage);

                        _state.LastSpentStageInterrupted = Staging.CurrentStage;
                    }
                }

                foreach (var altitudeEvent in _state.AltitudeEvents)
                {
                    var currentAltitude = vessel.altitude;
                    var triggerAltitude = altitudeEvent.Altitude;

                    if (
                        (currentAltitude < triggerAltitude && altitudeEvent.LastState == AltitudeState.Above) ||
                        (triggerAltitude < currentAltitude && altitudeEvent.LastState == AltitudeState.Below)
                    )
                    {
                        altitudeEvent.LastState = altitudeEvent.LastState == AltitudeState.Above ?
                            AltitudeState.Below :
                            AltitudeState.Above;

                        _dcpu16.X = altitudeEvent.AltitudeKm;
                        _dcpu16.Y = MachineWord.FromInt16((short)altitudeEvent.LastState);
                        _dcpu16.Interrupt(altitudeEvent.InterruptMessage);
                    }
                }

            }
        }

        private void OnFlyByWire(FlightCtrlState flightCtrlState)
        {
            if (_dcpu16 != null && _state.IsActive)
            {
                flightCtrlState.roll = _state.Roll;
                flightCtrlState.pitch = _state.Pitch;
                flightCtrlState.yaw = _state.Yaw;

                flightCtrlState.X = _state.TranslationX;
                flightCtrlState.Y = _state.TranslationY;
                flightCtrlState.Z = _state.TranslationZ;

                flightCtrlState.mainThrottle = _state.Throttle;

                if (_state.StagesPendingActivation > 0 && Staging.CurrentStage > 0)
                {
                    var originalStage = Staging.CurrentStage;

                    Staging.ActivateNextStage();

                    if (Staging.CurrentStage != originalStage)
                    {
                        _state.StagesPendingActivation--;
                    }
                }
            }
        }

        #endregion

        #region IDevice

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
            if (_dcpu16 != null)
            {
                var operation = (InterruptOperation)_dcpu16.A;

                switch (operation)
                {
                    case InterruptOperation.GetActive:
                        _dcpu16.X = MachineWord.FromBoolean(_state.IsActive);
                        break;
                    case InterruptOperation.GetRotation:
                        _dcpu16.X = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.roll));
                        _dcpu16.Y = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.pitch));
                        _dcpu16.Z = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.yaw));
                        break;
                    case InterruptOperation.GetTranslation:
                        _dcpu16.X = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.X));
                        _dcpu16.Y = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.Y));
                        _dcpu16.Z = MachineWord.FromInt16(Range.ScaleSignedUnaryToSignedInt16(vessel.ctrlState.Z));
                        break;
                    case InterruptOperation.GetThrottle:
                        _dcpu16.X = MachineWord.FromUInt16(Range.ScaleUnsignedUnaryToUnsignedInt16(
                           vessel.ctrlState.mainThrottle
                        ));
                        break;
                    case InterruptOperation.GetOrientation:
                        var orientation = GetOrientation();

                        _dcpu16.X = MachineWord.FromUInt16((ushort)
                            Range.UnsignedDegreesCircle.ScaleTo(Range.UnsignedInt16, orientation.x)
                        );
                        _dcpu16.Y = MachineWord.FromInt16((short)
                            Range.SignedDegreesHalfCircle.ScaleTo(Range.SignedInt16, orientation.y)
                        );
                        _dcpu16.Z = MachineWord.FromInt16((short)
                            Range.SignedDegreesCircle.ScaleTo(Range.SignedInt16, orientation.z)
                        );

                        break;
                    case InterruptOperation.SetActive:
                        _state.IsActive = MachineWord.ToBoolean(_dcpu16.X);
                        break;
                    case InterruptOperation.SetRotation:
                        _state.Roll = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                        _state.Pitch = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                        _state.Yaw = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                        break;
                    case InterruptOperation.SetTranslation:
                        _state.TranslationX = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.X));
                        _state.TranslationY = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Y));
                        _state.TranslationZ = Range.ScaleSignedInt16ToSignedUnary(MachineWord.ToInt16(_dcpu16.Z));
                        break;
                    case InterruptOperation.SetThrottle:
                        _state.Throttle = Range.ScaleUnsignedInt16ToUnsignedUnary(MachineWord.ToUInt16(_dcpu16.X));
                        break;
                    case InterruptOperation.SetSas:
                        vessel.ActionGroups[KSPActionGroup.SAS] = MachineWord.ToBoolean(_dcpu16.X);
                        break;
                    case InterruptOperation.SetStage:
                        _state.StagesPendingActivation++;
                        break;
                    case InterruptOperation.EventSpentStage:
                        _state.SpentStageMessage = _dcpu16.X;
                        break;
                    case InterruptOperation.EventAltitudeSea:
                        var interruptMessage = _dcpu16.X;
                        var altitude = MachineWord.ToUInt16(_dcpu16.Y) * 1000;
                        var lastState = vessel.altitude < altitude ? AltitudeState.Below : AltitudeState.Above;

                        var altitudeEvent = new AltitudeEvent
                        {
                            InterruptMessage = interruptMessage,
                            AltitudeKm = MachineWord.ToUInt16(_dcpu16.Y),
                            Altitude = altitude,
                            LastState = lastState
                        };

                        _state.AltitudeEvents.Add(altitudeEvent);

                        break;
                }
            }

            return 0;
        }

        #endregion

        #region Helpers

        private Vector3d GetOrientation()
        {
            var vesselReferenceFrame = vessel.GetVesselReferenceFrame();
            var nedReferenceFrame = vessel.GetNedReferenceFrame();

            return new Vector3d(
                vesselReferenceFrame.GetHeading(nedReferenceFrame),
                vesselReferenceFrame.GetPitch(nedReferenceFrame),
                vesselReferenceFrame.GetRoll(nedReferenceFrame)
            );
        }

        private bool IsStageSpent()
        {
            var engines = vessel.Parts.Where(IsInCurrentStage).SelectMany(GetEngines).ToArray();

            return engines.Any() && engines.All(IsEngineSpent);
        }

        private static IEnumerable<IEngineStatus> GetEngines(Part part)
        {
            return part.Modules.OfType<IEngineStatus>();
        }

        private static bool IsEngineSpent(IEngineStatus engine)
        {
            return !engine.isOperational;
        }

        private static bool IsInCurrentStage(Part part)
        {
            return part.inverseStage == Staging.CurrentStage;
        }

        private LineRenderer SetupAxisLineRender(Color color, float lengthMeters, float widthMeters)
        {
            var obj = new GameObject("Line");
            var lineRenderer = obj.AddComponent<LineRenderer>();
            lineRenderer.transform.parent = transform;
            lineRenderer.useWorldSpace = false;
            lineRenderer.transform.localPosition = Vector3.zero;
            lineRenderer.transform.localEulerAngles = Vector3.zero;

            lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            lineRenderer.SetColors(color, color);
            lineRenderer.SetWidth(widthMeters, widthMeters);
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.forward * lengthMeters);

            return lineRenderer;
        }

#if DEBUG
        private LineRenderer _northAxisLine;
        private LineRenderer _eastAxisLine;
        private LineRenderer _downAxisLine;

        private LineRenderer _vesselFrontLine;
        private LineRenderer _vesselRightLine;
        private LineRenderer _vesselBottomLine;

        private void DebugSetupAxes()
        {
            _northAxisLine = SetupAxisLineRender(Color.red, 100, 0.5f);
            _eastAxisLine = SetupAxisLineRender(Color.green, 100, 0.5f);
            _downAxisLine = SetupAxisLineRender(Color.blue, 100, 0.5f);

            _vesselFrontLine = SetupAxisLineRender(Color.magenta, 25, 0.25f);
            _vesselRightLine = SetupAxisLineRender(Color.yellow, 25, 0.25f);
            _vesselBottomLine = SetupAxisLineRender(Color.cyan, 25, 0.25f);
        }

        private void DebugUpdateAxes()
        {
            var nedRefFrame = vessel.GetNedReferenceFrame();

            var localCenterOfMass = vessel.transform.InverseTransformPoint(nedRefFrame.Origin);

            _downAxisLine.transform.localPosition = localCenterOfMass;
            _northAxisLine.transform.localPosition = localCenterOfMass;
            _eastAxisLine.transform.localPosition = localCenterOfMass;

            _northAxisLine.transform.rotation = Quaternion.LookRotation(nedRefFrame.North);
            _eastAxisLine.transform.rotation = Quaternion.LookRotation(nedRefFrame.East);
            _downAxisLine.transform.rotation = Quaternion.LookRotation(nedRefFrame.Down);

            _vesselFrontLine.transform.localPosition = localCenterOfMass;
            _vesselRightLine.transform.localPosition = localCenterOfMass;
            _vesselBottomLine.transform.localPosition = localCenterOfMass;

            _vesselFrontLine.transform.rotation = Quaternion.LookRotation(vessel.transform.up.normalized);
            _vesselRightLine.transform.rotation = Quaternion.LookRotation(vessel.transform.right.normalized);
            _vesselBottomLine.transform.rotation = Quaternion.LookRotation(vessel.transform.forward.normalized);
        }
#endif

        #endregion

        #region Nested Classes

        private class State
        {
            public bool IsActive;

            public float Roll;
            public float Pitch;
            public float Yaw;

            public float TranslationX;
            public float TranslationY;
            public float TranslationZ;

            public float Throttle;

            public uint StagesPendingActivation;

            public ushort SpentStageMessage;
            public int LastSpentStageInterrupted = -1;

            public readonly List<AltitudeEvent> AltitudeEvents = new List<AltitudeEvent>();
        }

        private class AltitudeEvent
        {
            public ushort InterruptMessage;
            public ushort AltitudeKm;
            public float Altitude;
            public AltitudeState LastState;
        }

        private enum AltitudeState : short
        {
            Below = -1,
            Above = 1
        }

        #endregion
    }
}
