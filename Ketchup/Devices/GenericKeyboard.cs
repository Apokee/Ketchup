﻿using System.Collections.Generic;
using Ketchup.Constants;
using Tomato.Hardware;
using UnityEngine;

namespace Ketchup.Devices
{
    public sealed class GenericKeyboard : Device
    {
        #region Key Mappings

        private static readonly Dictionary<KeyCode, KeyCodePair> KeyMappings = new Dictionary<KeyCode, KeyCodePair>
        {
            {KeyCode.Backspace,         new KeyCodePair(0x10)},
            {KeyCode.Delete,            new KeyCodePair(0x13)},
            {KeyCode.Insert,            new KeyCodePair(0x12)},
            {KeyCode.Return,            new KeyCodePair(0x11)},

            {KeyCode.UpArrow,           new KeyCodePair(0x80)},
            {KeyCode.DownArrow,         new KeyCodePair(0x81)},
            {KeyCode.LeftArrow,         new KeyCodePair(0x82)},
            {KeyCode.RightArrow,        new KeyCodePair(0x83)},

            {KeyCode.LeftControl,       new KeyCodePair(0x91)},
            {KeyCode.RightControl,      new KeyCodePair(0x91)},

            {KeyCode.LeftShift,         new KeyCodePair(0x90)},
            {KeyCode.RightShift,        new KeyCodePair(0x90)},
            
            {KeyCode.Keypad0,           new KeyCodePair(0x30)},
            {KeyCode.Keypad1,           new KeyCodePair(0x31)},
            {KeyCode.Keypad2,           new KeyCodePair(0x32)},
            {KeyCode.Keypad3,           new KeyCodePair(0x33)},
            {KeyCode.Keypad4,           new KeyCodePair(0x34)},
            {KeyCode.Keypad5,           new KeyCodePair(0x35)},
            {KeyCode.Keypad6,           new KeyCodePair(0x36)},
            {KeyCode.Keypad7,           new KeyCodePair(0x37)},
            {KeyCode.Keypad8,           new KeyCodePair(0x38)},
            {KeyCode.Keypad9,           new KeyCodePair(0x39)},
            {KeyCode.KeypadDivide,      new KeyCodePair(0x2f)},
            {KeyCode.KeypadEnter,       new KeyCodePair(0x11)},
            {KeyCode.KeypadEquals,      new KeyCodePair(0x3d)},
            {KeyCode.KeypadMinus,       new KeyCodePair(0x2d)},
            {KeyCode.KeypadMultiply,    new KeyCodePair(0x2a)},
            {KeyCode.KeypadPeriod,      new KeyCodePair(0x2e)},
            {KeyCode.KeypadPlus,        new KeyCodePair(0x2b)},

            {KeyCode.Space,             new KeyCodePair(0x20)},
            //{KeyCode.Exclaim,         new KeyCodePair(0x21)}, // doesn't work
            //{KeyCode.DoubleQuote,     new KeyCodePair(0x22)}, // doesn't work
            //{KeyCode.Hash,            new KeyCodePair(0x23)}, // doesn't work
            //{KeyCode.Dollar,          new KeyCodePair(0x24)}, // doesn't work
            // Missing: Percent Sign
            //{KeyCode.Ampersand,       new KeyCodePair(0x26)}, // doesn't work
            {KeyCode.Quote,             new KeyCodePair(0x27, 0x22)},
            //{KeyCode.LeftParen,       new KeyCodePair(0x28)}, // doesn't work
            //{KeyCode.RightParen,      new KeyCodePair(0x29)}, // doesn't work
            //{KeyCode.Asterisk,        new KeyCodePair(0x2a)}, // doesn't work
            //{KeyCode.Plus,            new KeyCodePair(0x2b)}, // doesn't work
            {KeyCode.Comma,             new KeyCodePair(0x2c, 0x3c)},
            {KeyCode.Minus,             new KeyCodePair(0x2d, 0x5f)},
            {KeyCode.Period,            new KeyCodePair(0x2e, 0x3e)},
            {KeyCode.Slash,             new KeyCodePair(0x2f, 0x3f)},
            {KeyCode.Alpha0,            new KeyCodePair(0x30, 0x29)},
            {KeyCode.Alpha1,            new KeyCodePair(0x31, 0x21)},
            {KeyCode.Alpha2,            new KeyCodePair(0x32, 0x40)},
            {KeyCode.Alpha3,            new KeyCodePair(0x33, 0x23)},
            {KeyCode.Alpha4,            new KeyCodePair(0x34, 0x24)},
            {KeyCode.Alpha5,            new KeyCodePair(0x35, 0x25)},
            {KeyCode.Alpha6,            new KeyCodePair(0x36, 0x5e)},
            {KeyCode.Alpha7,            new KeyCodePair(0x37, 0x26)},
            {KeyCode.Alpha8,            new KeyCodePair(0x38, 0x2a)},
            {KeyCode.Alpha9,            new KeyCodePair(0x39, 0x28)},
            //{KeyCode.Colon,           new KeyCodePair(0x3a)}, // doesn't work
            {KeyCode.Semicolon,         new KeyCodePair(0x3b, 0x3a)},
            //{KeyCode.Less,            new KeyCodePair(0x3c)}, // doesn't work
            {KeyCode.Equals,            new KeyCodePair(0x3d, 0x2b)},
            //{KeyCode.Greater,         new KeyCodePair(0x3e)}, // doesn't work
            //{KeyCode.Question,        new KeyCodePair(0x3f)}, // doesn't work
            //{KeyCode.At,              new KeyCodePair(0x40)}, // doesn't work
            // Missing: Uppercase Letters
            {KeyCode.LeftBracket,       new KeyCodePair(0x5b, 0x7b)},
            {KeyCode.Backslash,         new KeyCodePair(0x5c, 0x7c)},
            {KeyCode.RightBracket,      new KeyCodePair(0x5d, 0x7d)},
            //{KeyCode.Caret,           new KeyCodePair(0x5e)}, // doesn't work
            //{KeyCode.Underscore,      new KeyCodePair(0x5f)}, // doesn't work
            {KeyCode.BackQuote,         new KeyCodePair(0x60, 0x7e)},
            {KeyCode.A,                 new KeyCodePair(0x61, 0x41)},
            {KeyCode.B,                 new KeyCodePair(0x62, 0x42)},
            {KeyCode.C,                 new KeyCodePair(0x63, 0x43)},
            {KeyCode.D,                 new KeyCodePair(0x64, 0x44)},
            {KeyCode.E,                 new KeyCodePair(0x65, 0x45)},
            {KeyCode.F,                 new KeyCodePair(0x66, 0x46)},
            {KeyCode.G,                 new KeyCodePair(0x67, 0x47)},
            {KeyCode.H,                 new KeyCodePair(0x68, 0x48)},
            {KeyCode.I,                 new KeyCodePair(0x69, 0x49)},
            {KeyCode.J,                 new KeyCodePair(0x6a, 0x4a)},
            {KeyCode.K,                 new KeyCodePair(0x6b, 0x4b)},
            {KeyCode.L,                 new KeyCodePair(0x6c, 0x4c)},
            {KeyCode.M,                 new KeyCodePair(0x6d, 0x4d)},
            {KeyCode.N,                 new KeyCodePair(0x6e, 0x4e)},
            {KeyCode.O,                 new KeyCodePair(0x6f, 0x4f)},
            {KeyCode.P,                 new KeyCodePair(0x70, 0x50)},
            {KeyCode.Q,                 new KeyCodePair(0x71, 0x51)},
            {KeyCode.R,                 new KeyCodePair(0x72, 0x52)},
            {KeyCode.S,                 new KeyCodePair(0x73, 0x53)},
            {KeyCode.T,                 new KeyCodePair(0x74, 0x54)},
            {KeyCode.U,                 new KeyCodePair(0x75, 0x55)},
            {KeyCode.V,                 new KeyCodePair(0x76, 0x56)},
            {KeyCode.W,                 new KeyCodePair(0x77, 0x57)},
            {KeyCode.X,                 new KeyCodePair(0x78, 0x58)},
            {KeyCode.Y,                 new KeyCodePair(0x79, 0x59)},
            {KeyCode.Z,                 new KeyCodePair(0x7a, 0x5a)},
        };

        #endregion

        private readonly Queue<ushort> _buffer = new Queue<ushort>();
        private readonly HashSet<ushort> _pressedKeys = new HashSet<ushort>();

        private ushort _interruptMessage;

        public override string FriendlyName
        {
            get { return "Generic Keyboard (compatible)"; }
        }

        public override uint ManufacturerID
        {
            get { return (uint)ManufacturerId.Unknown; }
        }

        public override uint DeviceID
        {
            get { return (uint)DeviceId.GenericKeyboard; }
        }

        public override ushort Version
        {
            get { return 0x0001; }
        }

        public override int HandleInterrupt()
        {
            var action = (ActionId)AttachedCPU.A;

            switch (action)
            {
                case ActionId.ClearBuffer:
                    _buffer.Clear();
                    break;
                case ActionId.StoreNextKey:
                    AttachedCPU.C = _buffer.Count == 0 ? (ushort)0 : _buffer.Dequeue();
                    break;
                case ActionId.CheckNextKey:
                    AttachedCPU.C = _pressedKeys.Contains(AttachedCPU.B) ? (ushort)1 : (ushort)0;
                    break;
                case ActionId.SetInterruptBehavior:
                    _interruptMessage = AttachedCPU.B;
                    break;
            }

            return 0;
        }

        public override void Reset()
        {
            _interruptMessage = 0;
            _pressedKeys.Clear();
            _buffer.Clear();
        }

        public ushort GetKeyValue(KeyCode keyCode)
        {
            KeyCodePair keyCodePair;
            return KeyMappings.TryGetValue(keyCode, out keyCodePair) ?
                (_pressedKeys.Contains(0x90) ? keyCodePair.Shifted : keyCodePair.Normal) :
                (ushort)0;
        }

        public void KeyDown(KeyCode keyCode)
        {
            var code = GetKeyValue(keyCode);

            if (code != 0)
            {
                _buffer.Enqueue(code);
                _pressedKeys.Add(code);

                if (_interruptMessage != 0)
                {
                    AttachedCPU.FireInterrupt(_interruptMessage);
                }
            }
        }

        public void KeyUp(KeyCode keyCode)
        {
            var code = GetKeyValue(keyCode);

            if (code != 0)
            {
                _pressedKeys.Remove(code);
            }
        }

        private enum ActionId : ushort
        {
            ClearBuffer             = 0x0000,
            StoreNextKey            = 0x0001,
            CheckNextKey            = 0x0002,
            SetInterruptBehavior    = 0x0003,
        }

        private struct KeyCodePair
        {
            public readonly ushort Normal;
            public readonly ushort Shifted;

            public KeyCodePair(ushort normal)
            {
                Normal = normal;
                Shifted = normal;
            }

            public KeyCodePair(ushort normal, ushort shifted)
            {
                Normal = normal;
                Shifted = shifted;
            }
        }
    }
}
