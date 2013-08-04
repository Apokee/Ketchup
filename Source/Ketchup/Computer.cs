using System;
using System.Collections.Generic;
using System.Linq;
using Ketchup.Api;
using Ketchup.Extensions;
using Ketchup.Interop;
using KSP.IO;
using Tomato;
using UnityEngine;

namespace Ketchup
{
    public class Computer : PartModule
    {
        #region Constants

        private const int ClockFrequency = 100000;
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";

        #endregion

        #region Static Fields
        
        private static GUIStyle _styleButtonPressed;
        private static bool _isStyleInit;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;
        private readonly List<IDevice> _devices = new List<IDevice>();

        private bool _isPowerOn;
        private bool _isHalted;

        private ushort? _pcAtHalt;
        private int? _warpIndexBeforeWake;

        private string _program = String.Empty;

        private readonly List<double> _clockRates = new List<double>(60);
        private int _cyclesExecuted;
        private int _clockRateIndex;

        private TimeWarp _timeWarp;

        private Rect _windowRect;
        private bool _isWindowPositionInit;
        private bool _isWindowSizeInit;

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
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            float x;
            if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionX), out x))
            {
                _windowRect.x = x;
            }

            float y;
            if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionY), out y))
            {
                _windowRect.y = y;
            }

            _isWindowPositionInit = true;
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyWindowPositionX, _windowRect.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowRect.y);
        }

        public override void OnUpdate()
        {
            if (_isPowerOn)
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
                GUILayout.Label(_program, GUILayout.ExpandWidth(true));
            }
            else
            {
                _program = GUILayout.TextField(_program);
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

                _windowRect = new Rect(_windowRect) { x = 0f, y = defaultTop }; 

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
            {
                _isPowerOn = false;

                _isHalted = false;
                _pcAtHalt = 0;

                _dcpu16 = null;

                foreach (var device in _devices)
                {
                    device.OnDisconnect();
                }

                _devices.Clear();

                _clockRates.RemoveAll(i => true);
                _clockRateIndex = 0;
            }
            else
            {
                _dcpu16 = new TomatoDcpu16Adapter(new DCPU());

                foreach (var device in vessel.Parts.SelectMany(i => i.Modules.OfType<IDevice>()))
                {
                    _devices.Add(device);
                    Connect(_dcpu16, device);

                    Debug.Log(String.Format("[Ketchup] Connected CPU to {0}", device.FriendlyName)); 
                }

                var programBytes = File.ReadAllBytes<Computer>(_program);
                var programUShorts = new ushort[programBytes.Length / 2];

                for (var i = 0; i < programBytes.Length; i += 2)
                {
                    var a = programBytes[i];
                    var b = programBytes[i + 1];

                    programUShorts[i / 2] = (ushort)((a << 8) | b);
                }

                Array.Copy(programUShorts, _dcpu16.Memory, programUShorts.Length);

                _isPowerOn = true;
            }
        }

        private static void Connect(IDcpu16 dcpu16, IDevice device)
        {
            dcpu16.OnConnect(device);
            device.OnConnect(dcpu16);
        }

        #endregion
    }
}
