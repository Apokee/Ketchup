﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ketchup.Devices;
using Ketchup.Extensions;
using KSP.IO;
using Tomato;
using UnityEngine;

namespace Ketchup
{
    public class Dcpu16Computer : PartModule
    {
        #region Fields

        #region GUI

        private const string KeyboardInputName = "KetchupDcpu16ComputerKeyboardInput";

        private static Rect _windowPosition;
        private static GUIStyle _styleBox;
        private static GUIStyle _styleButton;
        private static GUIStyle _styleLabel;
        private static GUIStyle _styleTextField;
        private static GUIStyle _styleWindow;
        private static bool _hasInitPosition;
        private static bool _hasInitStyles;

        #endregion

        #region Devices

        private DCPU _dcpu;
        private Lem1802 _lem1802;
        private GenericKeyboard _genericKeyboard;
        private GenericClock _genericClock;

        #endregion

        #region Emulation

        private bool _isPowerOn;
        private bool _isKeyboardAttached;

        private string _program = String.Empty;

        private readonly List<double> _clockRates = new List<double>(60);
        private int _cyclesExecuted;
        private int _clockRateIndex;

        #endregion

        #region Game

        private TimeWarp _timeWarp;

        #endregion

        #endregion

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                _dcpu = new DCPU();
                _lem1802 = new Lem1802();
                _genericKeyboard = new GenericKeyboard();
                _genericClock = new GenericClock();

                _dcpu.ConnectDevice(_lem1802);
                _dcpu.ConnectDevice(_genericKeyboard);
                _dcpu.ConnectDevice(_genericClock);

                _timeWarp = TimeWarp.fetch;

                InitStylesIfNecessary();

                RenderingManager.AddToPostDrawQueue(3, OnDraw);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            var config = PluginConfiguration.CreateForType<Dcpu16Computer>();
            config.load();

            var savedRect = config.GetValue<Rect>("_windowPosition");
            _windowPosition = new Rect(savedRect.x, savedRect.y, 0, 0);
        }

        public override void OnSave(ConfigNode node)
        {
            var config = PluginConfiguration.CreateForType<Dcpu16Computer>();

            config.SetValue("_windowPosition", _windowPosition);
            config.save();
        }

        public override void OnUpdate()
        {
            if (_isPowerOn)
            {
                var maxPhysicsWarpIndex = _timeWarp.physicsWarpRates.Length - 1;

                if (_timeWarp.physicsWarpRates[maxPhysicsWarpIndex] < TimeWarp.CurrentRate)
                {
                    TimeWarp.SetRate(TimeWarp.WarpMode == TimeWarp.Modes.LOW ? maxPhysicsWarpIndex : 0, true);
                }

                var cyclesToExecute = (int)Math.Round(Time.deltaTime * _dcpu.ClockSpeed * TimeWarp.CurrentRate);

                _dcpu.Execute(cyclesToExecute);

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

                _cyclesExecuted = cyclesToExecute;
            }
        }

        private void OnDraw()
        {
            if (this.vessel.isActiveVessel && this.IsPrimary())
            {
                _windowPosition = GUILayout.Window(1, _windowPosition, OnWindow, this.part.partInfo.title, _styleWindow);

                InitPositionIfNecessary();
            }
        }
        
        private void OnWindow(int windowId)
        {
            if (_isKeyboardAttached && this.vessel.isActiveVessel)
            {
                GUI.FocusControl(KeyboardInputName);

                var currentEvent = Event.current;

                if (currentEvent.isKey)
                {
                    var eventType = currentEvent.type;

                    if (currentEvent.alt)
                        _genericKeyboard.KeyDown(KeyCode.LeftAlt);
                    else
                        _genericKeyboard.KeyUp(KeyCode.LeftAlt);

                    if (currentEvent.control)
                        _genericKeyboard.KeyDown(KeyCode.LeftControl);
                    else
                        _genericKeyboard.KeyUp(KeyCode.LeftControl);

                    if (currentEvent.shift)
                        _genericKeyboard.KeyDown(KeyCode.LeftShift);
                    else
                        _genericKeyboard.KeyUp(KeyCode.LeftShift);

                    switch (eventType)
                    {
                        case EventType.KeyDown:
                            _genericKeyboard.KeyDown(currentEvent.keyCode);
                            currentEvent.Use();
                            break;
                        case EventType.KeyUp:
                            _genericKeyboard.KeyUp(currentEvent.keyCode);
                            currentEvent.Use();
                            break;
                    }
                }
            }

            var monitorImage = _lem1802.GetScreenImage(scale: 4);

            var pressedStyle = new GUIStyle(_styleButton) { normal = _styleButton.active };

            var actualClockSpeed = (_clockRates.Any() ? _clockRates.Average() / 1000 : 0);

            var actualClockSpeedFormatted = actualClockSpeed.ToString("F3") + " KHz";

            GUILayout.BeginHorizontal();
            var pwrButtonPressed = GUILayout.Button("PWR", _isPowerOn ? pressedStyle : _styleButton, GUILayout.ExpandWidth(false));
            var kbdButtonPressed = GUILayout.Button("KBD", _isKeyboardAttached ? pressedStyle : _styleButton, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Memory Image:", _styleLabel, GUILayout.Width(100));
            if (_isPowerOn)
            {
                GUILayout.Label(_program, _styleLabel, GUILayout.Width(400));
            }
            else
            {
                _program = GUILayout.TextField(_program, _styleTextField);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Box(monitorImage, _styleBox, GUILayout.Width(monitorImage.width + 8), GUILayout.Height(monitorImage.height + 8));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(actualClockSpeedFormatted, _styleLabel);
            GUILayout.EndHorizontal();

            GUI.SetNextControlName(KeyboardInputName);
            GUI.TextField(new Rect(0, 0, 0, 0), String.Empty, GUIStyle.none);

            GUI.DragWindow();

            if (GUI.changed)
            {
                if (pwrButtonPressed) { OnPowerButtonPressed(); }
                if (kbdButtonPressed) { OnKeyboardButtonPressed(); }
            }
        }

        private static void InitPositionIfNecessary()
        {
            if (!_hasInitPosition)
            {
                _windowPosition = _windowPosition.CenterScreen();

                _hasInitPosition = true;
            }
        }

        private static void InitStylesIfNecessary()
        {
            if (!_hasInitStyles)
            {
                _styleBox = new GUIStyle(HighLogic.Skin.box);
                _styleButton = new GUIStyle(HighLogic.Skin.button);
                _styleLabel = new GUIStyle(HighLogic.Skin.label);
                _styleTextField = new GUIStyle(HighLogic.Skin.textField);
                _styleWindow = new GUIStyle(HighLogic.Skin.window);

                _hasInitStyles = true;
            }
        }

        private void OnKeyboardButtonPressed()
        {
            _isKeyboardAttached = !_isKeyboardAttached;
        }

        private void OnPowerButtonPressed()
        {
            if (_isPowerOn)
            {
                _isPowerOn = false;

                _dcpu.Reset();
                _dcpu.FlashMemory(new ushort[_dcpu.Memory.Length]);
                _clockRates.RemoveAll(i => true);
                _clockRateIndex = 0;
            }
            else
            {
                var programBytes = File.ReadAllBytes<Dcpu16Computer>(_program);
                var programUShorts = new ushort[programBytes.Length / 2];

                for (var i = 0; i < programBytes.Length; i += 2)
                {
                    var a = programBytes[i];
                    var b = programBytes[i + 1];

                    programUShorts[i / 2] = (ushort)((a << 8) | b);
                }

                _dcpu.FlashMemory(programUShorts);

                _isPowerOn = true;
            }
        }
    }
}
