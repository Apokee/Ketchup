using System;
using System.Collections.Generic;
using System.Linq;
using Ketchup.Api;
using Ketchup.Extensions;
using Ketchup.Interop;
using Ketchup.IO;
using KSP.IO;
using Tomato;
using UnityEngine;

namespace Ketchup
{
    public class Computer : PartModule
    {
        #region Constants

        private const int ClockFrequency = 100000;
        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyDcpu16State = "Dcpu16State";
        private const string ConfigKeyIsPowerOn = "IsPowerOn";
        private const string ConfigKeyMemoryImage = "MemoryImage";

        private const uint ConfigVersion = 1;

        #endregion

        #region Static Fields
        
        private static GUIStyle _styleButtonPressed;
        private static bool _isStyleInit;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;
        private readonly List<IDevice> _devices = new List<IDevice>();

        private Dcpu16StateManager _dcpu16StateManager;

        private bool _isPowerOn;
        private bool _isHalted;

        private ushort? _pcAtHalt;
        private int? _warpIndexBeforeWake;

        private string _memoryImage = String.Empty;

        private readonly List<double> _clockRates = new List<double>(60);
        private int _cyclesExecuted;
        private int _clockRateIndex;

        private TimeWarp _timeWarp;

        private Rect _windowRect;
        private bool _isWindowPositionInit;
        private bool _isWindowSizeInit;

        private string _dcpu16State;

        #endregion

        #region PartModule Methods

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                _timeWarp = TimeWarp.fetch;

                InitStylesIfNecessary();
                InitWindowPositionIfNecessary();
                InitWindowSizeIfNecessary();
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

                bool isPowerOn;
                if (Boolean.TryParse(node.GetValue(ConfigKeyIsPowerOn), out isPowerOn))
                {
                    _isPowerOn = isPowerOn;
                }

                _memoryImage = node.GetValue(ConfigKeyMemoryImage) ?? String.Empty;
                _dcpu16State = node.GetValue(ConfigKeyDcpu16State);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);

            node.AddValue(ConfigKeyWindowPositionX, _windowRect.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowRect.y);
            node.AddValue(ConfigKeyIsPowerOn, _isPowerOn);

            if (!String.IsNullOrEmpty(_memoryImage))
            {
                node.AddValue(ConfigKeyMemoryImage, _memoryImage);
            }

            if (_dcpu16StateManager != null)
            {
                var state = _dcpu16StateManager.SaveAsBase64();

                node.AddValue(ConfigKeyDcpu16State, state);
            }
        }

        public override void OnUpdate()
        {
            if (_isPowerOn && _dcpu16 != null)
            {
                if (_isHalted)
                {
                    if (_dcpu16.PC != _pcAtHalt)
                    {
                        _warpIndexBeforeWake = TimeWarp.CurrentRateIndex;

                        _pcAtHalt = null;
                        _isHalted = false;
                    }
                }
                else
                {
                    if (_dcpu16.IsHalted())
                    {
                        _pcAtHalt = _dcpu16.PC;
                        _isHalted = true;

                        if (_warpIndexBeforeWake != null)
                        {
                            var index = _warpIndexBeforeWake.Value;
                            _warpIndexBeforeWake = null;
                            TimeWarp.SetRate(index, true);
                        }
                    }
                    else
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
                }
            }
        }

        #endregion

        #region Helper Methods

        private void OnDraw()
        {
            if (vessel.isActiveVessel)
            {
                GUI.skin = HighLogic.Skin;

                _windowRect = GUILayout.Window(1, _windowRect, OnWindow, part.partInfo.title);
            }
        }
        
        private void OnWindow(int windowId)
        {
            var actualClockSpeed = (_clockRates.Any() ? _clockRates.Average() / 1000 : 0);

            var actualClockSpeedFormatted = actualClockSpeed.ToString("F3") + " KHz";

            GUILayout.BeginHorizontal();
            GUILayout.Label("Memory Image:", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (_isPowerOn)
            {
                GUILayout.Label(_memoryImage, GUILayout.ExpandWidth(true));
            }
            else
            {
                _memoryImage = GUILayout.TextField(_memoryImage);
            }
            GUILayout.EndHorizontal();

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
                const float defaultTop = 200f;

                _windowRect = new Rect(_windowRect) { x = Screen.width - 250f, y = defaultTop }; 

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

        private void InitializeDcpu16(string state)
        {
            _dcpu16 = new TomatoDcpu16Adapter(new DCPU());
            _dcpu16StateManager = new Dcpu16StateManager(_dcpu16);

            if (String.IsNullOrEmpty(state))
            {
                var memoryImage = LoadMemoryImage();

                Array.Copy(memoryImage, _dcpu16.Memory, memoryImage.Length);
            }
            else
            {
                _dcpu16StateManager.LoadFromBase64(state);
            }
        }

        private ushort[] LoadMemoryImage()
        {
            var memoryImageBytes = File.ReadAllBytes<Computer>(_memoryImage);
            var memoryImageUShorts = new ushort[memoryImageBytes.Length / 2];

            for (var i = 0; i < memoryImageBytes.Length; i += 2)
            {
                var a = memoryImageBytes[i];
                var b = memoryImageBytes[i + 1];

                memoryImageUShorts[i / 2] = (ushort)((a << 8) | b);
            }

            return memoryImageUShorts;
        }

        private void TurnOn(bool useState)
        {
            if (useState && !String.IsNullOrEmpty(_dcpu16State))
            {
                InitializeDcpu16(_dcpu16State);
            }
            else
            {
                InitializeDcpu16(null);
            }

            foreach (var device in vessel.Parts.SelectMany(i => i.Modules.OfType<IDevice>()))
            {
                _devices.Add(device);
                Connect(_dcpu16, device);

                Debug.Log(String.Format("[Ketchup] Connected CPU to {0}", device.FriendlyName));
            }

            _isPowerOn = true;
        }

        private void TurnOff()
        {
            _isPowerOn = false;

            _isHalted = false;
            _pcAtHalt = 0;

            _dcpu16 = null;
            _dcpu16StateManager = null;

            foreach (var device in _devices)
            {
                device.OnDisconnect();
            }

            _devices.Clear();

            _clockRates.RemoveAll(i => true);
            _clockRateIndex = 0;
        }

        private static void Connect(IDcpu16 dcpu16, IDevice device)
        {
            dcpu16.OnConnect(device);
            device.OnConnect(dcpu16);
        }

        #endregion
    }
}
