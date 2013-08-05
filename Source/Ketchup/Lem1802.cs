using System;
using System.Linq;
using System.Threading;
using Ketchup.Api;
using UnityEngine;

namespace Ketchup
{
    public sealed class Lem1802 : PartModule, IDevice
    {
        #region Constants

        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyScreenMap = "ScreenMap";
        private const string ConfigKeyFontMap = "FontMap";
        private const string ConfigKeyPaletteMap = "PaletteMap";
        private const string ConfigKeyBorderColorValue = "BorderColorValue";
        private const string ConfigKeyImageScale = "ImageScale";

        private const uint ConfigVersion = 1;

        private static readonly ushort[] DefaultFont =
        {
            0xb79e, 0x388e, 0x722c, 0x75f4, 0x19bb, 0x7f8f, 0x85f9, 0xb158,
            0x242e, 0x2400, 0x082a, 0x0800, 0x0008, 0x0000, 0x0808, 0x0808,
            0x00ff, 0x0000, 0x00f8, 0x0808, 0x08f8, 0x0000, 0x080f, 0x0000,
            0x000f, 0x0808, 0x00ff, 0x0808, 0x08f8, 0x0808, 0x08ff, 0x0000,
            0x080f, 0x0808, 0x08ff, 0x0808, 0x6633, 0x99cc, 0x9933, 0x66cc,
            0xfef8, 0xe080, 0x7f1f, 0x0701, 0x0107, 0x1f7f, 0x80e0, 0xf8fe,
            0x5500, 0xaa00, 0x55aa, 0x55aa, 0xffaa, 0xff55, 0x0f0f, 0x0f0f,
            0xf0f0, 0xf0f0, 0x0000, 0xffff, 0xffff, 0x0000, 0xffff, 0xffff,
            0x0000, 0x0000, 0x005f, 0x0000, 0x0300, 0x0300, 0x3e14, 0x3e00,
            0x266b, 0x3200, 0x611c, 0x4300, 0x3629, 0x7650, 0x0002, 0x0100,
            0x1c22, 0x4100, 0x4122, 0x1c00, 0x1408, 0x1400, 0x081c, 0x0800,
            0x4020, 0x0000, 0x0808, 0x0800, 0x0040, 0x0000, 0x601c, 0x0300,
            0x3e49, 0x3e00, 0x427f, 0x4000, 0x6259, 0x4600, 0x2249, 0x3600,
            0x0f08, 0x7f00, 0x2745, 0x3900, 0x3e49, 0x3200, 0x6119, 0x0700,
            0x3649, 0x3600, 0x2649, 0x3e00, 0x0024, 0x0000, 0x4024, 0x0000,
            0x0814, 0x2200, 0x1414, 0x1400, 0x2214, 0x0800, 0x0259, 0x0600,
            0x3e59, 0x5e00, 0x7e09, 0x7e00, 0x7f49, 0x3600, 0x3e41, 0x2200,
            0x7f41, 0x3e00, 0x7f49, 0x4100, 0x7f09, 0x0100, 0x3e41, 0x7a00,
            0x7f08, 0x7f00, 0x417f, 0x4100, 0x2040, 0x3f00, 0x7f08, 0x7700,
            0x7f40, 0x4000, 0x7f06, 0x7f00, 0x7f01, 0x7e00, 0x3e41, 0x3e00,
            0x7f09, 0x0600, 0x3e61, 0x7e00, 0x7f09, 0x7600, 0x2649, 0x3200,
            0x017f, 0x0100, 0x3f40, 0x7f00, 0x1f60, 0x1f00, 0x7f30, 0x7f00,
            0x7708, 0x7700, 0x0778, 0x0700, 0x7149, 0x4700, 0x007f, 0x4100,
            0x031c, 0x6000, 0x417f, 0x0000, 0x0201, 0x0200, 0x8080, 0x8000,
            0x0001, 0x0200, 0x2454, 0x7800, 0x7f44, 0x3800, 0x3844, 0x2800,
            0x3844, 0x7f00, 0x3854, 0x5800, 0x087e, 0x0900, 0x4854, 0x3c00,
            0x7f04, 0x7800, 0x047d, 0x0000, 0x2040, 0x3d00, 0x7f10, 0x6c00,
            0x017f, 0x0000, 0x7c18, 0x7c00, 0x7c04, 0x7800, 0x3844, 0x3800,
            0x7c14, 0x0800, 0x0814, 0x7c00, 0x7c04, 0x0800, 0x4854, 0x2400,
            0x043e, 0x4400, 0x3c40, 0x7c00, 0x1c60, 0x1c00, 0x7c30, 0x7c00,
            0x6c10, 0x6c00, 0x4c50, 0x3c00, 0x6454, 0x4c00, 0x0836, 0x4100,
            0x0077, 0x0000, 0x4136, 0x0800, 0x0201, 0x0201, 0x0205, 0x0200
        };

        private static readonly ushort[] DefaultPalette = 
        {
            0x0000, 0x000A, 0x00A0, 0x00AA, 0x0A00, 0x0A0A, 0x0A50, 0x0AAA,
            0x0555, 0x055F, 0x05F5, 0x05FF, 0x0F55, 0x0F5F, 0x0FF5, 0x0FFF
        };

        private const int Width = 128;
        private const int Height = 96;
        private const int CharWidth = 4;
        private const int CharHeight = 8;
        private const int BlinkRate = 1000;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;

        private ushort _screenMap;
        private ushort _fontMap;
        private ushort _paletteMap;
        private ushort _borderColorValue;

        private readonly Texture2D _screenTexture;
        private float _imageScale = 1;
        private Rect _windowPosition;
        private bool _isWindowPositionInit;

        private bool _blinkOn = true;
        // ReSharper disable once NotAccessedField.Local
        private readonly Timer _blinkTimer;

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return "LEM1802 - Low Energy Monitor (compatible)"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.NyaElektriska; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Lem1802Monitor; }
        }

        public ushort Version
        {
            get { return 0x1802; }
        }

        #endregion

        #region Constructors

        public Lem1802()
        {
            _screenTexture = new Texture2D(Width, Height) { filterMode = FilterMode.Point };
            _blinkTimer = new Timer(ToggleBlinker, null, BlinkRate, BlinkRate);
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
            _screenMap = default(ushort);
            _fontMap = default(ushort);
            _paletteMap = default(ushort);
            _borderColorValue = default(ushort);
            _blinkOn = true;
        }

        public int OnInterrupt()
        {
            if (_dcpu16 != null)
            {
                var action = (ActionId)_dcpu16.A;

                switch (action)
                {
                    case ActionId.MemMapScreen:
                        _screenMap = _dcpu16.B;
                        break;
                    case ActionId.MemMapFont:
                        _fontMap = _dcpu16.B;
                        break;
                    case ActionId.MemMapPalette:
                        _paletteMap = _dcpu16.B;
                        break;
                    case ActionId.SetBorderColor:
                        _borderColorValue = (ushort)(_dcpu16.B & 0xF);
                        break;
                    case ActionId.MemDumpFont:
                        Array.Copy(DefaultFont, 0, _dcpu16.Memory, _dcpu16.B, DefaultFont.Length);
                        return 256;
                    case ActionId.MemDumpPalette:
                        Array.Copy(DefaultPalette, 0, _dcpu16.Memory, _dcpu16.B, DefaultPalette.Length);
                        return 16;
                }
            }

            return 0;
        }

        #endregion

        #region PartModule Methods

        public override void OnLoad(ConfigNode node)
        {
            uint version;
            if (UInt32.TryParse(node.GetValue(ConfigKeyVersion), out version) && version == 1)
            {
                float x;
                if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionX), out x))
                {
                    _windowPosition.x = x;
                }

                float y;
                if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionY), out y))
                {
                    _windowPosition.y = y;
                }

                _isWindowPositionInit = true;

                ushort screenMap;
                if (UInt16.TryParse(node.GetValue(ConfigKeyScreenMap), out screenMap))
                {
                    _screenMap = screenMap;
                }

                ushort fontMap;
                if (UInt16.TryParse(node.GetValue(ConfigKeyFontMap), out fontMap))
                {
                    _fontMap = fontMap;
                }

                ushort paletteMap;
                if (UInt16.TryParse(node.GetValue(ConfigKeyPaletteMap), out paletteMap))
                {
                    _paletteMap = paletteMap;
                }

                ushort borderColorValue;
                if (UInt16.TryParse(node.GetValue(ConfigKeyBorderColorValue), out borderColorValue))
                {
                    _borderColorValue = borderColorValue;
                }

                float imageScale;
                if (Single.TryParse(node.GetValue(ConfigKeyImageScale), out imageScale))
                {
                    _imageScale = imageScale;
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);
            node.AddValue(ConfigKeyWindowPositionX, _windowPosition.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowPosition.y);
            node.AddValue(ConfigKeyScreenMap, _screenMap);
            node.AddValue(ConfigKeyFontMap, _fontMap);
            node.AddValue(ConfigKeyPaletteMap, _paletteMap);
            node.AddValue(ConfigKeyBorderColorValue, _borderColorValue);
            node.AddValue(ConfigKeyImageScale, _imageScale);
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                InitWindowPositionIfNecessary();
                RenderingManager.AddToPostDrawQueue(1, OnDraw);
            }
        }

        #endregion

        #region Helper Methods

        private void OnDraw()
        {
            if (vessel.isActiveVessel)
            {
                GUI.skin = HighLogic.Skin;
                
                _windowPosition = GUILayout.Window(3, _windowPosition, OnWindow, "LEM-1802");
            }
        }

        private void OnWindow(int windowId)
        {
            Refresh();

            GUILayout.BeginHorizontal();

            GUILayout.Box(String.Empty, GUILayout.Width((int)(_screenTexture.width * _imageScale)), GUILayout.Height((int)(_screenTexture.height * _imageScale)));
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), _screenTexture, ScaleMode.ScaleToFit);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x")) { SetMonitorScale(1); }
            if (GUILayout.Button("2x")) { SetMonitorScale(2); }
            if (GUILayout.Button("3x")) { SetMonitorScale(3); }
            if (GUILayout.Button("4x")) { SetMonitorScale(4); }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void Refresh()
        {
            if (_dcpu16 == null || _screenMap == 0)
            {
                for (var x = 0; x < _screenTexture.width; x++)
                {
                    for (var y = 0; y < _screenTexture.height; y++)
                    {
                        _screenTexture.SetPixel(x, y, Color.black);
                    }
                }
            }
            else
            {
                ushort address = 0;
                for (var y = 0; y < 12; y++)
                {
                    for (var x = 0; x < 32; x++)
                    {
                        var value = _dcpu16.Memory[_screenMap + address];
                        uint fontValue;
                        if (_fontMap == 0)
                            fontValue = (uint)((DefaultFont[(value & 0x7F) * 2] << 16) | DefaultFont[(value & 0x7F) * 2 + 1]);
                        else
                            fontValue = (uint)((_dcpu16.Memory[_fontMap + ((value & 0x7F) * 2)] << 16) | _dcpu16.Memory[_fontMap + ((value & 0x7F) * 2) + 1]);

                        fontValue = BitConverter.ToUInt32(BitConverter.GetBytes(fontValue).Reverse().ToArray(), 0);

                        var background = GetPaletteColor((byte)((value & 0xF00) >> 8));
                        var foreground = GetPaletteColor((byte)((value & 0xF000) >> 12));
                        for (var i = 0; i < sizeof(uint) * 8; i++)
                        {
                            Color color;

                            var tx = (i / 8 + (x * CharWidth));
                            var ty = Math.Abs((i % 8 + (y * CharHeight)) - (Height - 1));

                            if ((fontValue & 1) == 0 || (((value & 0x80) == 0x80) && !_blinkOn))
                                color = background;
                            else
                                color = foreground;

                            _screenTexture.SetPixel(tx, ty, color);

                            fontValue >>= 1;
                        }
                        address++;
                    }
                }
            }

            _screenTexture.Apply();
        }

        private Color GetPaletteColor(byte value)
        {
            var color = _paletteMap == 0 ?
                DefaultPalette[value & 0xF] :
                _dcpu16.Memory[_paletteMap + (value & 0xF)];

            var b = (byte)(color & 0xF);
            b |= (byte)(b << 4);
            var g = (byte)((color & 0xF0) >> 4);
            g |= (byte)(g << 4);
            var r = (byte)((color & 0xF00) >> 8);
            r |= (byte)(r << 4);

            var bf = (float)b / Byte.MaxValue;
            var gf = (float)g / Byte.MaxValue;
            var rf = (float)r / Byte.MaxValue;

            return new Color(rf, gf, bf, 1);
        }

        private void ToggleBlinker(object o)
        {
            _blinkOn = !_blinkOn;
        }

        private void SetMonitorScale(float scale)
        {
            _imageScale = scale;
            _windowPosition = new Rect(_windowPosition) { width = 0, height = 0 };
        }

        private void InitWindowPositionIfNecessary()
        {
            if (!_isWindowPositionInit)
            {
                const float defaultTop = 400f;

                _windowPosition = new Rect(Screen.width - 200f, defaultTop, 0, 0);

                _isWindowPositionInit = true;
            }
        }

        #endregion

        #region Nested Types

        private enum ActionId : ushort
        {
            MemMapScreen = 0x0000,
            MemMapFont = 0x0001,
            MemMapPalette = 0x0002,
            SetBorderColor = 0x0003,
            MemDumpFont = 0x0004,
            MemDumpPalette = 0x0005,
        }

        #endregion
    }
}
