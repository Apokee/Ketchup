﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ketchup.Api.v0;
using Ketchup.Data;
using Ketchup.Extensions;
using Ketchup.Interop;
using Ketchup.IO;
using Tomato;
using UnityEngine;

namespace Ketchup.Modules
{
    [KSPModule("Computer")]
    internal class ModuleKetchupComputer : PartModule
    {
        #region Constants

        private const int ClockFrequency = 100000;
        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyShowWindow = "ShowWindow";
        private const string ConfigKeyDcpu16State = "Dcpu16State";
        private const string ConfigKeyIsPowerOn = "IsPowerOn";
        private const string ConfigNodeConnection = "DEVICE_CONNECTION";

        private const uint ConfigVersion = 1;

        #endregion

        #region Static Fields
        
        private static GUIStyle _styleButtonPressed;
        private static bool _isStyleInit;

        #endregion
        
        #region Instance Fields

        private TomatoDcpu16Adapter _dcpu16;
        private readonly List<IDevice> _connectedDevices = new List<IDevice>();

        private Dcpu16StateManager _dcpu16StateManager;

        private bool _isPowerOn;

        private readonly List<double> _clockRates = new List<double>(60);
        private int _cyclesExecuted;
        private int _clockRateIndex;

        private Rect _windowRect;
        private bool _showWindow;

        private bool _isWindowPositionInit;
        private bool _isWindowSizeInit;

        private string _dcpu16State;

        private List<DeviceConnection> _deviceConnections = new List<DeviceConnection>();

        #endregion

        #region PartModule Methods

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Processor: DCPU-16");
            sb.AppendLine(String.Format(@"Clock Speed: {0}", FormatClockSpeed(ClockFrequency)));
            sb.AppendLine("RAM: 128KB");

            return sb.ToString();
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                InitStylesIfNecessary();
                InitWindowSizeIfNecessary();
                InitWindowPositionIfNecessary();
                RenderingManager.AddToPostDrawQueue(1, OnDraw);

                if (_isPowerOn)
                    TurnOn(coldStart: false);
                else
                    TurnOff();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            uint version;
            if (UInt32.TryParse(node.GetValue(ConfigKeyVersion), out version) && version == 1)
            {
                float windowPositionX;
                if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionX), out windowPositionX))
                {
                    _windowRect.x = windowPositionX;
                }

                float windowPositionY;
                if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionY), out windowPositionY))
                {
                    _windowRect.y = windowPositionY;
                }

                _isWindowPositionInit = true;

                bool showWindow;
                if (Boolean.TryParse(node.GetValue(ConfigKeyShowWindow), out showWindow))
                {
                    _showWindow = showWindow;
                }

                bool isPowerOn;
                if (Boolean.TryParse(node.GetValue(ConfigKeyIsPowerOn), out isPowerOn))
                {
                    _isPowerOn = isPowerOn;
                }

                _dcpu16State = node.GetValue(ConfigKeyDcpu16State);

                foreach (var deviceConnectionNode in node.GetNodes(ConfigNodeConnection))
                {
                    _deviceConnections.Add(new DeviceConnection(deviceConnectionNode));
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);

            node.AddValue(ConfigKeyWindowPositionX, _windowRect.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowRect.y);
            node.AddValue(ConfigKeyShowWindow, _showWindow);
            node.AddValue(ConfigKeyIsPowerOn, _isPowerOn);

            if (_dcpu16StateManager != null)
            {
                var state = _dcpu16StateManager.Save();

                node.AddValue(ConfigKeyDcpu16State, state);
            }

            foreach (var deviceConnection in _deviceConnections)
            {
                var deviceConnectionNode = new ConfigNode(ConfigNodeConnection);

                deviceConnection.Save(deviceConnectionNode);

                node.AddNode(deviceConnectionNode);
            }
        }

        public override void OnUpdate()
        {
            // TODO: this code is gnarly, refactor for clarity
            // Do we really need to keep track of a halt condition in two places?
            if (_isPowerOn && _dcpu16 != null)
            {
                var cyclesToExecute = 0;

                if (_dcpu16.IsPendingWakeUp())
                {
                    cyclesToExecute = (int)Math.Round((TimeWarp.deltaTime / 2.0) * ClockFrequency);
                }
                else if (!_dcpu16.IsHalted())
                {
                    cyclesToExecute = (int)Math.Round(TimeWarp.deltaTime * ClockFrequency);
                }

                if (cyclesToExecute > 0)
                {
                    TimeWarpThrottleIfNecessary();

                    var cyclesExecuted = _dcpu16.Execute(cyclesToExecute);

                    // We use this calculation to get the real-world clock rate, not the in-simulation clock rate
                    var clockRate = _cyclesExecuted / TimeWarp.deltaTime * TimeWarp.CurrentRate;
                    if (_clockRates.Count < 60)
                    {
                        _clockRates.Add(clockRate);
                    }
                    else
                    {
                        _clockRates[_clockRateIndex] = clockRate;
                        _clockRateIndex = ((_clockRateIndex + 1) % 60);
                    }

                    _cyclesExecuted = cyclesExecuted;
                }
            }
        }

        #endregion

        #region KSP Events

        [KSPEvent(guiActive = true, guiName = "Toggle Computer Interface")]
        public void ToggleInterface()
        {
            _showWindow = !_showWindow;
        }

        #endregion

        public void AddDeviceConnection(DeviceConnection deviceConnection)
        {
            _deviceConnections.Add(deviceConnection);
        }

        public void ResetDeviceConnections()
        {
            _deviceConnections.Clear();
        }

        public void UpdateDeviceConnections(Dictionary<Port, Port> updates)
        {
            _deviceConnections = _deviceConnections
                .Select(i => updates.ContainsKey(i.Port) ? new DeviceConnection(i.Type, updates[i.Port]) : i)
                .ToList();
        }

        #region Helper Methods

        private void TimeWarpThrottleIfNecessary()
        {
            if (
                (_dcpu16.IsPendingWakeUp() || !_dcpu16.IsHalted())
                && TimeWarp.WarpMode == TimeWarp.Modes.HIGH
                && TimeWarp.CurrentRate > 1
            )
            {
                var condition = String.Empty;

                if (_dcpu16.IsPendingWakeUp())
                {
                    condition = "DCPU-16 is pending wake up from halt, ";
                }
                else if (_dcpu16.IsHalted())
                {
                    condition = "DCPU-16 is executing, ";
                }

                Debug.Log(String.Format("[Ketchup:Computer] {0} throttling TimeWarp from {1}",
                    condition,
                    TimeWarp.CurrentRate
                ));

                TimeWarp.SetRate(0, false);
            }
        }

        private void OnDraw()
        {
            if (vessel.isActiveVessel && _showWindow)
            {
                GUI.skin = HighLogic.Skin;

                _windowRect = GUILayout.Window(1, _windowRect, OnWindow, part.partInfo.title);
            }
        }
        
        private void OnWindow(int windowId)
        {
            var actualClockSpeedFormatted = FormatClockSpeed(_clockRates.Any() ? _clockRates.Average() : 0);

            GUILayout.BeginHorizontal();
            var pwrButtonPressed = GUILayout.Button("PWR", _isPowerOn ? _styleButtonPressed : GUI.skin.button, GUILayout.ExpandWidth(false));
            if (_isPowerOn)
            {
                if (_dcpu16.IsHalted())
                {
                    GUILayout.Label("Halted", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow }, padding = new RectOffset(0, 0, 8, 0) });
                }
                else
                {
                    GUILayout.Label(actualClockSpeedFormatted, new GUIStyle(GUI.skin.label) { padding = new RectOffset(0, 0, 8, 0) });
                }
            }
            else
            {
                GUILayout.Label("Powered Off", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red }, padding = new RectOffset(0, 0, 8, 0) });
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();

            if (GUI.changed)
            {
                if (pwrButtonPressed) { OnPowerButtonPressed(); }
            }
        }

        private void InitWindowPositionIfNecessary()
        {
            if (!_isWindowPositionInit)
            {
                _windowRect = _windowRect.CenteredOnScreen();

                _isWindowPositionInit = true;
            }
        }

        private void InitWindowSizeIfNecessary()
        {
            if (!_isWindowSizeInit)
            {
                const float defaultWidth = 250f;
                const float defaultHeight = 0f;

                _windowRect = new Rect(_windowRect) { width = defaultWidth, height = defaultHeight };

                _isWindowSizeInit = true;
            }
        }

        private static void InitStylesIfNecessary()
        {
            if (!_isStyleInit)
            {
                _styleButtonPressed = new GUIStyle(HighLogic.Skin.button) { normal = HighLogic.Skin.button.active };

                _isStyleInit = true;
            }
        }

        private void OnPowerButtonPressed()
        {
            if (_isPowerOn)
                TurnOff();
            else
                TurnOn(coldStart: true);
        }

        private void TurnOn(bool coldStart)
        {
            InitializeDcpu16();

            var connectedDevices = DeviceScan().ToList();
            var firmware = connectedDevices.FirstOrDefault(i => i is ModuleKetchupFirmware);

            Connect(firmware);
            Connect(connectedDevices.Where(i => i != firmware));

            if (!coldStart && HasPersistedState())
            {
                _dcpu16StateManager.Load(_dcpu16State);
            }
            else
            {
                if (firmware != null)
                {
                    firmware.OnInterrupt();
                }
            }

            _isPowerOn = true;
        }

        private void TurnOff()
        {
            _isPowerOn = false;

            _dcpu16 = null;
            _dcpu16StateManager = null;

            foreach (var device in _connectedDevices)
            {
                device.OnDisconnect();
            }

            _connectedDevices.Clear();

            _clockRates.RemoveAll(i => true);
            _clockRateIndex = 0;
        }

        private void InitializeDcpu16()
        {
            var dcpu16 = new TomatoDcpu16Adapter(new DCPU());
            var dcpu16StateManager = new Dcpu16StateManager(dcpu16);

            _dcpu16 = dcpu16;
            _dcpu16StateManager = dcpu16StateManager;
        }

        private IEnumerable<IDevice> DeviceScan()
        {
            var connectedGlobalDeviceIds = new HashSet<Port>(_deviceConnections.Select(i => i.Port));
            return vessel
                .Parts
                .SelectMany(i => i.FindModulesImplementing<IDevice>())
                .Where(i => connectedGlobalDeviceIds.Contains(i.Port));
        }

        private void Connect(IEnumerable<IDevice> devices)
        {
            foreach (var device in devices)
            {
                Connect(device);
            }
        }

        private void Connect(IDevice device)
        {
            if (device != null)
            {

                _connectedDevices.Add(device);

                _dcpu16.OnConnect(device);
                device.OnConnect(_dcpu16);

                Debug.Log(String.Format("[Ketchup:Computer] Connected DCPU-16 to {0}", device.FriendlyName));
            }
        }

        private bool HasPersistedState()
        {
            return !String.IsNullOrEmpty(_dcpu16State);
        }

        private static string FormatClockSpeed(double hertz)
        {
            double factor;
            string suffix;

            if (hertz < 1e3)
            {
                factor = 1e0;
                suffix = "Hz";
            }
            else if (hertz < 1e6)
            {
                factor = 1e3;
                suffix = "KHz";
            }
            else if (hertz < 1e9)
            {
                factor = 1e6;
                suffix = "MHz";
            }
            else if (hertz < 1e12)
            {
                factor = 1e9;
                suffix = "Ghz";
            }
            else if (hertz < 1e15)
            {
                factor = 1e12;
                suffix = "THz";
            }
            else if (hertz < 1e18)
            {
                factor = 1e15;
                suffix = "PHz";
            }
            else if (hertz < 1e21)
            {
                factor = 1e18;
                suffix = "EHz";
            }
            else if (hertz < 1e24)
            {
                factor = 1e21;
                suffix = "ZHz";
            }
            else
            {
                factor = 1e24;
                suffix = "YHz";
            }

            return String.Format("{0:G6}{1}", hertz / factor, suffix);
        }

        #endregion
    }
}
