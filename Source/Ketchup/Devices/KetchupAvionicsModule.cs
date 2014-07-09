using Ketchup.Extensions;
using Ketchup.Utility;
using UnityEngine;

namespace Ketchup.Devices
{
    internal sealed class KetchupAvionicsModule : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation
        {
            GetRotation         = 0x0001,
            GetTranslation      = 0x0002,
            GetThrottle         = 0x0003,

            GetOrientation      = 0x000C,
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

        private IDcpu16 _dcpu16;

        #endregion

        #region PartModule Members

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
#if DEBUG
                DebugSetupAxes();
#endif
            }
        }

        public override void OnUpdate()
        {
#if DEBUG
            DebugUpdateAxes();
#endif
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
    }
}
