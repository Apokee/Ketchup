using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ksg.Ketchup.Exceptions;
using Ksg.Ketchup.Extensions;
using Ksg.Ketchup.IO;
using UnityEngine;

namespace Ksg.Ketchup.Devices
{
    internal sealed class KetchupFirmwareModule : PartModule, IDevice
    {
        #region Constants

        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyShowWindow = "ShowWindow";
        private const string ConfigKeyFirmwareName = "FirmwareName";
        private const string ConfigKeyFirmwareData = "FirmwareData";

        private const uint ConfigVersion = 1;

        private const int MaxFirmwareWords = 512;

        private static readonly ushort[] DefaultFirmware =
        {
            0xa8c1, 0x7ce1, 0x0200, 0x7cd2, 0x0200, 0x7f81, 0x0200, 0x39fe,
            0x9381, 0x1a00, 0x88c3, 0x80d2, 0x7f81, 0x0214, 0x1a20, 0x7c12,
            0x24c5, 0x7c32, 0x4fd5, 0x1bc1, 0x02a7, 0x7c12, 0xf615, 0x7c32,
            0x7349, 0x1bc1, 0x02a8, 0x7f81, 0x0201, 0x83d2, 0x02a7, 0x7f81,
            0x022e, 0x8401, 0x7a40, 0x02a7, 0x8432, 0x7f81, 0x0238, 0x8c01,
            0x8461, 0x8481, 0x7a40, 0x02a7, 0x8833, 0x7f81, 0x0242, 0x8401,
            0x7a40, 0x02a7, 0x8832, 0x7f81, 0x025e, 0x7f81, 0x0226, 0x83d2,
            0x02a8, 0x7f81, 0x025c, 0x7c20, 0x024b, 0x7cc1, 0x0267, 0x7f81,
            0x0251, 0x83d2, 0x02a8, 0x7f81, 0x025c, 0x7c20, 0x024b, 0x7cc1,
            0x027f, 0x7f81, 0x0251, 0x83d2, 0x02a8, 0x8b83, 0x7c20, 0x024b,
            0x7cc1, 0x0296, 0x7f81, 0x0251, 0x8401, 0x7c21, 0x8000, 0x7a40,
            0x02a8, 0x6381, 0x7ce1, 0x8000, 0x85d2, 0x7f81, 0x025c, 0x3841,
            0x7c4b, 0xf000, 0x09fe, 0x7f81, 0x0253, 0x7f81, 0x025c, 0x8401,
            0x8421, 0x8441, 0x8461, 0x8481, 0x84c1, 0x84e1, 0x8761, 0x8781,
            0x0041, 0x0054, 0x0054, 0x0041, 0x0043, 0x0048, 0x0020, 0x004d,
            0x0033, 0x0035, 0x0046, 0x0044, 0x0020, 0x0041, 0x004e, 0x0044,
            0x0020, 0x0052, 0x0045, 0x0042, 0x004f, 0x004f, 0x0054, 0x0000,
            0x0049, 0x004e, 0x0053, 0x0045, 0x0052, 0x0054, 0x0020, 0x0044,
            0x0049, 0x0053, 0x004b, 0x0020, 0x0041, 0x004e, 0x0044, 0x0020,
            0x0052, 0x0045, 0x0042, 0x004f, 0x004f, 0x0054, 0x0000, 0x0044,
            0x0052, 0x0049, 0x0056, 0x0045, 0x0020, 0x0052, 0x0045, 0x0041,
            0x0044, 0x0020, 0x0045, 0x0052, 0x0052, 0x004f, 0x0052, 0x0000,
            0xffff, 0xffff 
        };

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;

        private FirmwareRom _firmware;

        private Rect _windowRect;
        private bool _showWindow;

        private bool _isWindowPositionInit;
        private bool _flashingRom;

        private List<FirmwareRom> _firmwares;

        #endregion

        #region Device Identifiers

        // TODO: The proposed specifications do not mention any specifics for these

        public string FriendlyName
        {
            get { return "DCPU-16 Firmware"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.Unknown; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.Firmware; }
        }

        public ushort Version
        {
            get { return 0x0001; }
        }

        #endregion

        #region Constructors

        public KetchupFirmwareModule()
        {
            _firmware = new FirmwareRom("<Default>", DefaultFirmware);
        }

        #endregion

        #region IDevice Methods

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
                Array.Copy(_firmware.Data, 0, _dcpu16.Memory, _dcpu16.B, _firmware.Data.Length);
            }

            return 0;
        }

        #endregion

        #region PartModule Methods

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                InitWindowPositionIfNecessary();
                RenderingManager.AddToPostDrawQueue(1, OnDraw);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            uint version;
            if (UInt32.TryParse(node.GetValue(ConfigKeyVersion), out version) && version == 1)
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

                bool showWindow;
                if (Boolean.TryParse(node.GetValue(ConfigKeyShowWindow), out showWindow))
                {
                    _showWindow = showWindow;
                }

                if (node.HasValue(ConfigKeyFirmwareName) && node.HasValue(ConfigKeyFirmwareData))
                {
                    var firmwareName = node.GetValue(ConfigKeyFirmwareName);
                    var firmwareData = node.GetValue(ConfigKeyFirmwareData);

                    _firmware = new FirmwareRom(firmwareName, firmwareData);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);
            node.AddValue(ConfigKeyWindowPositionX, _windowRect.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowRect.y);
            node.AddValue(ConfigKeyShowWindow, _showWindow);
            node.AddValue(ConfigKeyFirmwareName, _firmware.Name);
            node.AddValue(ConfigKeyFirmwareData, _firmware.Serialize());
        }

        #endregion

        #region KSP Events

        [KSPEvent(guiActive = true, guiName = "Toggle Firmware Interface")]
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

                _windowRect = GUILayout.Window(5, _windowRect, OnWindow, "Firmware");
            }
        }

        private void OnWindow(int windowId)
        {
            GUILayout.BeginHorizontal();
            var flashRomButtonPressed = GUILayout.Button("Flash");
            GUILayout.Label(_firmware.Name, GUILayout.Width(125));
            GUILayout.EndHorizontal();

            if (_flashingRom)
            {
                foreach (var firmware in _firmwares)
                {
                    if (GUILayout.Button(firmware.Name))
                    {
                        _firmware = firmware;
                        _flashingRom = false;
                    }
                }
            }

            GUI.DragWindow();

            if (GUI.changed)
            {
                if (flashRomButtonPressed) { OnFlashRomButtonPressed(); }

                _windowRect = new Rect(_windowRect) { width = 0, height = 0 };
            }
        }

        private void OnFlashRomButtonPressed()
        {
            _flashingRom = !_flashingRom;

            if (_flashingRom)
            {
                _firmwares = GetFirmwareRoms().ToList();
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

        private static IEnumerable<FirmwareRom> GetFirmwareRoms()
        {
            yield return new FirmwareRom("<Default>", DefaultFirmware);

            foreach (var file in IoUtility.GetFirmwareFiles())
            {
                if (file.Length / 2 <= MaxFirmwareWords)
                {
                    var firmwareBytes = File.ReadAllBytes(file.FullName);
                    var firmwareUShorts = new ushort[firmwareBytes.Length / 2];

                    for (var i = 0; i < firmwareBytes.Length; i += 2)
                    {
                        var a = firmwareBytes[i];
                        var b = firmwareBytes[i + 1];

                        firmwareUShorts[i / 2] = (ushort)((a << 8) | b);
                    }

                    yield return new FirmwareRom(file.Name.Substring(0, file.Name.Length - 4), firmwareUShorts);
                }
            }
        }


        #endregion

        #region Nested Types

        private sealed class FirmwareRom
        {
            private const uint MagicNumber = 0xdbb0cae2;
            private const uint VersionNumber = 0x00000001;

            public string Name { get; private set; }
            public ushort[] Data { get; private set; }

            public FirmwareRom(string name, ushort[] data)
            {
                Name = name;
                Data = data;
            }

            public FirmwareRom(string name, string serializedData)
            {
                Name = name;

                using (var reader = new BinaryStateReader(serializedData))
                {
                    // Header
                    var magicNumber = reader.ReadUInt32();
                    if (magicNumber != MagicNumber)
                    {
                        throw new LoadStateException("Firmware ROM", String.Format("Magic number is incorrect. Expected: {0:X}. Found: {1:X}.", MagicNumber, magicNumber));
                    }

                    var versionNumber = reader.ReadUInt32();
                    if (versionNumber != VersionNumber)
                    {
                        throw new LoadStateException("Firmware ROM", String.Format("Unsupported version number: {0}", versionNumber));
                    }

                    // Data
                    var wordCount = reader.ReadInt32();
                    Data = new ushort[wordCount];
                    for (var i = 0; i < wordCount; i++)
                    {
                        Data[i] = reader.ReadUInt16();
                    }
                }
            }

            public string Serialize()
            {
                using (var writer = new BinaryStateWriter())
                {
                    // Header
                    writer.Write(MagicNumber);
                    writer.Write(VersionNumber);

                    // Data
                    writer.Write(Data.Length);
                    foreach (var word in Data)
                    {
                        writer.Write(word);
                    }

                    return writer.ToString();
                }
            }
        }

        #endregion
    }
}
