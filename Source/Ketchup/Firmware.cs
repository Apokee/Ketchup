using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ketchup.Api;
using UnityEngine;

namespace Ketchup
{
    internal sealed class Firmware : PartModule, IDevice
    {
        #region Constants

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

        public Firmware()
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

        private static IEnumerable<FirmwareRom> GetFirmwareRoms()
        {
            yield return new FirmwareRom("<Default>", DefaultFirmware);

            var savesDirectory = Path.Combine(KSPUtil.ApplicationRootPath, "saves");
            var profileDirectory = Path.Combine(savesDirectory, HighLogic.SaveFolder);
            var ketchupDirectory = Path.Combine(profileDirectory, "Ketchup");
            var firmwareDirectory = Path.Combine(ketchupDirectory, "Firmware");

            if (Directory.Exists(firmwareDirectory))
            {
                foreach (var file in Directory.GetFiles(firmwareDirectory))
                {
                    var fileLower = file.ToLowerInvariant();

                    if (fileLower.EndsWith(".bin") || fileLower.EndsWith(".img"))
                    {
                        var fileInfo = new FileInfo(file);

                        if (fileInfo.Length / 2 <= MaxFirmwareWords)
                        {
                            var firmwareBytes = File.ReadAllBytes(file);
                            var firmwareUShorts = new ushort[firmwareBytes.Length / 2];

                            for (var i = 0; i < firmwareBytes.Length; i += 2)
                            {
                                var a = firmwareBytes[i];
                                var b = firmwareBytes[i + 1];

                                firmwareUShorts[i / 2] = (ushort)((a << 8) | b);
                            }

                            yield return new FirmwareRom(fileInfo.Name.Substring(0, fileInfo.Name.Length - 4), firmwareUShorts);
                        }
                    }
                }
            }
        }


        #endregion

        #region Nested Types

        private sealed class FirmwareRom
        {
            public string Name { get; private set; }
            public ushort[] Data { get; private set; }

            public FirmwareRom(string name, ushort[] data)
            {
                Name = name;
                Data = data;
            }
        }

        #endregion
    }
}
