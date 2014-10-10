using System.Text;
using Ketchup.Api.v0;
using Ketchup.Extensions;
using Ketchup.Utility;
using UnityEngine;

namespace Ketchup.Modules
{
    /// <summary>
    /// Space-Time Orientation & Position (STOP) sensor.
    /// </summary>
    [KSPModule("Device: STOP")]
    internal sealed class ModuleKetchupStop : PartModule, IDevice
    {
        #region Constants

        private enum InterruptOperation
        {
            ActionGetRegisterTaitBryanOrientation = 0x0024,
        }

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "STOP Sensor"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.KerbalSystems; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Stop; }
        }

        public ushort Version
        {
            get { return 0x0001; }
        }

        public Port Port { get; set; }

        #endregion

        #region Instance Members

        private IDcpu16 _dcpu16;

        #endregion

        #region PartModule

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Equipped");

            return sb.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            this.LoadDevicePort(node);
        }

        public override void OnSave(ConfigNode node)
        {
            this.SaveDevicePort(node);
        }

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
                    case InterruptOperation.ActionGetRegisterTaitBryanOrientation:
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

            return 0; // TODO: Return sensible value
        }

        #endregion

        #region Helpers

        private Vector3d GetOrientation()
        {
            var vesselReferenceFrame = vessel.GetVesselReferenceFrame();
            var nedReferenceFrame = vessel.GetNedReferenceFrame();

            return new Vector3d(
                vesselReferenceFrame.GetRoll(nedReferenceFrame),
                vesselReferenceFrame.GetPitch(nedReferenceFrame),
                vesselReferenceFrame.GetHeading(nedReferenceFrame)
            );
        }

        #region Debug

#if DEBUG
        private LineRenderer _northAxisLine;
        private LineRenderer _eastAxisLine;
        private LineRenderer _downAxisLine;

        private LineRenderer _vesselFrontLine;
        private LineRenderer _vesselRightLine;
        private LineRenderer _vesselBottomLine;

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

        #endregion
    }
}
