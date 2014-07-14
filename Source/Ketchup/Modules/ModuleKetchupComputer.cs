using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ketchup.Api.v0;
using Ketchup.Extensions;
using Ketchup.Interop;
using Ketchup.IO;
using Tomato;
using UnityEngine;

namespace Ketchup.Modules
{
    [KSPModule("Computer")]
    internal class KetchupComputerModule : PartModule
    {
        #region Constants

        private const int ClockFrequency = 100000;
        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyShowWindow = "ShowWindow";
        private const string ConfigKeyDcpu16State = "Dcpu16State";
        private const string ConfigKeyIsPowerOn = "IsPowerOn";

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
        private bool _isHalted;

        private int? _warpIndexBeforeWake;

        private readonly List<double> _clockRates = new List<double>(60);
        private int _cyclesExecuted;
        private int _clockRateIndex;

        private TimeWarp _timeWarp;

        private Rect _windowRect;
        private bool _showWindow;

        private bool _isWindowPositionInit;
        private bool _isWindowSizeInit;
        

        private string _dcpu16State;

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
                _timeWarp = TimeWarp.fetch;

                InitStylesIfNecessary();
                InitWindowSizeIfNecessary();
                InitWindowPositionIfNecessary();
                RenderingManager.AddToPostDrawQueue(1, OnDraw);

                if (_isPowerOn)
                    TurnOn(useState: true);
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
        }

        public override void OnUpdate()
        {
            // TODO: this code is gnarly, refactor for clarity
            // Do we really need to keep track of a halt condition in two places?
            if (_isPowerOn && _dcpu16 != null)
            {
                if (_isHalted)
                {
                    if (_dcpu16.IsPendingWakeUp())
                    {
                        _warpIndexBeforeWake = TimeWarp.CurrentRateIndex;

                        _isHalted = false;
                    }
                }
                else
                {
                    if (_dcpu16.IsPendingWakeUp() || !_dcpu16.IsHalted())
                    {
                        var maxPhysicsWarpIndex = _timeWarp.physicsWarpRates.Length - 1;

                        if (_timeWarp.physicsWarpRates[maxPhysicsWarpIndex] < TimeWarp.CurrentRate)
                        {
                            TimeWarp.SetRate(TimeWarp.WarpMode == TimeWarp.Modes.LOW ? maxPhysicsWarpIndex : 0, true);
                        }

                        var cyclesToExecute = (int)Math.Round(Time.deltaTime * ClockFrequency * TimeWarp.CurrentRate);


                        var cyclesExecuted = 0;
                        while (cyclesExecuted < cyclesToExecute)
                        {
                            cyclesExecuted += _dcpu16.Execute();
                        }

                        var clockRate = _cyclesExecuted / Time.deltaTime;
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
                    else
                    {
                        _isHalted = true;

                        if (_warpIndexBeforeWake != null)
                        {
                            var index = _warpIndexBeforeWake.Value;
                            _warpIndexBeforeWake = null;
                            TimeWarp.SetRate(index, true);
                        }
                    }
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

        #region Helper Methods

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
                if (_isHalted)
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
                TurnOn(useState: false);
        }

        private void TurnOn(bool useState)
        {
            InitializeDcpu16();

            var firmware = GetFirmware();

            Connect(firmware);
            Connect(DeviceScan());

            if (useState && HasPersistedState())
            {
                _dcpu16StateManager.Load(_dcpu16State);
            }
            else
            {
                firmware.OnInterrupt();
            }

            _isPowerOn = true;
        }

        private void TurnOff()
        {
            _isPowerOn = false;

            _isHalted = false;

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

        private ModuleKetchupFirmware GetFirmware()
        {
            return part.Modules.OfType<ModuleKetchupFirmware>().Single();
        }

        private IEnumerable<IDevice> DeviceScan()
        {
            return vessel.Parts.SelectMany(i => i.Modules.OfType<IDevice>()).Where(i => !(i is ModuleKetchupFirmware)).ToList();
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
            _connectedDevices.Add(device);

            _dcpu16.OnConnect(device);
            device.OnConnect(_dcpu16);

            Debug.Log(String.Format("[Ketchup:Computer] Connected DCPU-16 to {0}", device.FriendlyName));
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
