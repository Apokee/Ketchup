using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ketchup.Api.v0;
using UnityEngine;

namespace Ketchup.Modules
{
    [KSPModule("Device: SPED-3")]
    internal sealed class ModuleKetchupSped3 : PartModule, IDevice
    {
        #region Constants

        private const float EdgeWidthMeters = 0.01f;
        private const float ProjectedWidthXMeters = 1f;
        private const float ProjectedWidthYMeters = 1f;
        private const float ProjectedWidthZMeters = 1f;
        private const float RotationRateDegreesPerSecond = 50f;

        private readonly Vector3 _scaleVector = new Vector3(ProjectedWidthXMeters / 255f, ProjectedWidthYMeters / 255f, ProjectedWidthZMeters / 255f);
        private readonly Vector3 _translateVector = new Vector3(-0.5f, -0.5f, 0.5f);

        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyState = "State";
        private const string ConfigKeyMemoryMap = "MemoryMap";
        private const string ConfigKeyVertexCount = "VertexCount";
        private const string ConfigKeyTargetRotation = "TargetRotation";
        private const string ConfigKeyCurrentRotation = "CurrentRotation";

        private const uint ConfigVersion = 1;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;
        private ushort[] _lastDrawnVertices = new ushort[0];
        private readonly List<GameObject> _lineObjects = new List<GameObject>();
        private readonly List<LineRenderer> _lineRenderers = new List<LineRenderer>();

        private StateCode _state;
        private ushort _memoryMap;
        private ushort _vertexCount;
        private ushort _targetRotation;
        private float _currentRotation;

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "Mackapar Suspended Particle Exciter Display, Rev 3 (SPED-3)"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.Mackapar; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Sped3; }
        }

        public ushort Version
        {
            get { return 0x0003; }
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
            _lastDrawnVertices = new ushort[0];
            _state = default(StateCode);
            _memoryMap = default(ushort);
            _vertexCount = default(ushort);
            _targetRotation = default(ushort);
            _currentRotation = default(float);
            ClearLines();
            
        }

        public int OnInterrupt()
        {
            if (_dcpu16 != null)
            {
                switch ((InterruptOperation)_dcpu16.A)
                {
                    case InterruptOperation.PollDevice:
                        _dcpu16.B = (ushort)_state;
                        _dcpu16.C = (ushort)ErrorCode.None;
                        break;

                    case InterruptOperation.MapRegion:
                        _memoryMap = _dcpu16.X;
                        _vertexCount = (ushort)(_dcpu16.Y % 128);

                        _state = _vertexCount == 0 ? StateCode.NoData : StateCode.Running;

                        break;

                    case InterruptOperation.RotateDevice:
                        _targetRotation = (ushort)(_dcpu16.X % 360);

                        if (Math.Abs(_targetRotation - _currentRotation) > 1f)
                        {
                            _state = StateCode.Turning;
                        }
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
                ushort state;
                if (UInt16.TryParse(node.GetValue(ConfigKeyState), out state))
                {
                    _state = (StateCode)state;
                }

                ushort memoryMap;
                if (UInt16.TryParse(node.GetValue(ConfigKeyMemoryMap), out memoryMap))
                {
                    _memoryMap = memoryMap;
                }

                ushort vertexCount;
                if (UInt16.TryParse(node.GetValue(ConfigKeyVertexCount), out vertexCount))
                {
                    _vertexCount = vertexCount;
                }

                ushort targetRotation;
                if (UInt16.TryParse(node.GetValue(ConfigKeyTargetRotation), out targetRotation))
                {
                    _targetRotation = targetRotation;
                }

                float currentRotation;
                if (Single.TryParse(node.GetValue(ConfigKeyCurrentRotation), out  currentRotation))
                {
                    _currentRotation = currentRotation;
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);
            node.AddValue(ConfigKeyState, (ushort)_state);
            node.AddValue(ConfigKeyMemoryMap, _memoryMap);
            node.AddValue(ConfigKeyVertexCount, _vertexCount);
            node.AddValue(ConfigKeyTargetRotation, _targetRotation);
            node.AddValue(ConfigKeyCurrentRotation, _currentRotation);
        }

        public override void OnUpdate()
        {
            if (_dcpu16 == null)
            {
                ClearLines();
            }
            else
            {
                var vertexDataLength = _vertexCount * 2;

                var currentVertices = new ushort[vertexDataLength];
                Array.Copy(_dcpu16.Memory, _memoryMap, currentVertices, 0, vertexDataLength);

                if (!currentVertices.SequenceEqual(_lastDrawnVertices))
                {
                    DrawVertices(GetVertices(currentVertices));

                    _lastDrawnVertices = currentVertices;
                }

                var maxRotation = TimeWarp.deltaTime * RotationRateDegreesPerSecond;
                var rotationDifference = Math.Abs(_targetRotation - _currentRotation);

                if (rotationDifference > 0.1f)
                {
                    float rotationAdjustment;
                    if (rotationDifference > maxRotation)
                    {
                        _currentRotation = (_currentRotation + maxRotation) % 360f;
                        rotationAdjustment = maxRotation;
                    }
                    else
                    {
                        _currentRotation = _targetRotation;
                        rotationAdjustment = rotationDifference;
                    }

                    foreach (var lineRenderer in _lineRenderers)
                    {
                        lineRenderer.transform.Translate(new Vector3(0.5f, 0.5f, 0f));
                        lineRenderer.transform.Rotate(Vector3.forward, rotationAdjustment);
                        lineRenderer.transform.Translate(new Vector3(-0.5f, -0.5f, 0f));
                    }
                }
                else
                {
                    _state = _vertexCount == 0 ? StateCode.NoData : StateCode.Running;
                }
            }
        }

        #endregion

        #region Helper Methods

        private void ClearLines()
        {
            if (_lineObjects.Count > 0)
            {
                _lineRenderers.Clear();

                foreach (var lineObject in _lineObjects)
                {
                    Destroy(lineObject);
                }

                _lineObjects.Clear();
            }
        }

        private Vertex[] GetVertices(IList<ushort> vertexData)
        {
            var vertices = new Vertex[vertexData.Count / 2];

            if (_dcpu16 != null)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    var offset = i * 2;

                    vertices[i] = new Vertex(vertexData[offset], vertexData[offset + 1]);
                }
            }

            return vertices;
        }

        private void DrawVertices(IList<Vertex> vertices)
        {
            ClearLines();

            if (vertices.Count >= 2)
            {
                for (var i = 0; i < vertices.Count - 1; i++)
                {
                    DrawLine(vertices[i], vertices[i + 1]);
                }
            }
        }

        private void DrawLine(Vertex start, Vertex end)
        {
            var color = start.ToColor();

            var obj = new GameObject("Line");

            var lineRenderer = obj.AddComponent<LineRenderer>();
            lineRenderer.transform.parent = transform;
            lineRenderer.useWorldSpace = false;
            lineRenderer.transform.localPosition = Vector3.zero;
            lineRenderer.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);

            lineRenderer.transform.localScale = _scaleVector;
            lineRenderer.transform.Translate(new Vector3(0.5f, 0.5f, 0f));
            lineRenderer.transform.Rotate(Vector3.forward, _currentRotation);
            lineRenderer.transform.Translate(new Vector3(-0.5f, -0.5f, 0f));
            lineRenderer.transform.Translate(_translateVector);

            lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            lineRenderer.SetColors(color, color);
            lineRenderer.SetWidth(EdgeWidthMeters, EdgeWidthMeters);
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, start.ToVector3());
            lineRenderer.SetPosition(1, end.ToVector3());

            _lineObjects.Add(obj);
            _lineRenderers.Add(lineRenderer);
        }

        #endregion

        #region NestedTypes

        private enum InterruptOperation : ushort
        {
            PollDevice      = 0x0000,
            MapRegion       = 0x0001,
            RotateDevice    = 0x0002,
        }

        private enum ErrorCode : ushort
        {
            None    = 0,
            // ReSharper disable once UnusedMember.Local
            Broken  = 1,
        }

        private enum StateCode : ushort
        {
            NoData  = 0x0000,
            Running = 0x0001,
            Turning = 0x0002,
        }

        private enum VertexColor : byte
        {
            Black   = 0,
            Red     = 1,
            Green   = 2,
            Blue    = 3,
        }

        private struct Vertex
        {
            #region Constants

            private static readonly Color ColorBlackNormal  = new Color(0.125f, 0.125f, 0.125f);
            private static readonly Color ColorBlackIntense = new Color(0.25f, 0.25f, 0.25f);
            private static readonly Color ColorRedNormal    = new Color(0.5f, 0f, 0f);
            private static readonly Color ColorRedIntense   = new Color(1f, 0f, 0f);
            private static readonly Color ColorGreenNormal  = new Color(0f, 0.5f, 0f);
            private static readonly Color ColorGreenIntense = new Color(0f, 1f, 0f);
            private static readonly Color ColorBlueNormal   = new Color(0f, 0f, 0.5f);
            private static readonly Color ColorBlueIntense  = new Color(0f, 0f, 1f);

            #endregion

            #region Instance Fields

            private readonly byte _x;
            private readonly byte _y;
            private readonly byte _z;
            private readonly VertexColor _color;
            private readonly bool _intense;

            #endregion

            public Vertex(ushort word1, ushort word2)
            {
                _x = (byte)(word1 & 0xFF);
                _y = (byte)((word1 >> 8) & 0xFF);
                _z = (byte)(word2 & 0xFF);
                _color = (VertexColor)((word2 >> 8) & 3);
                _intense = ((word2 >> 10) & 1) == 1;
            }

            public Vector3 ToVector3()
            {
                return new Vector3(_x, _y, _z);
            }

            public Color ToColor()
            {
                switch(_color)
                {
                    case VertexColor.Black:
                        return _intense ? ColorBlackIntense : ColorBlackNormal;
                    case VertexColor.Red:
                        return _intense ? ColorRedIntense : ColorRedNormal;
                    case VertexColor.Green:
                        return _intense ? ColorGreenIntense : ColorGreenNormal;
                    case VertexColor.Blue:
                        return _intense ? ColorBlueIntense : ColorBlueNormal;
                    default:
                        throw new Exception("Unexpected value: " + _color);
                }
            }
        }

        #endregion
    }
}
