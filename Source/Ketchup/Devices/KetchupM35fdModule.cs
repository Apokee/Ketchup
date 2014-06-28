using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ketchup.Exceptions;
using Ketchup.Extensions;
using Ketchup.IO;
using UnityEngine;

namespace Ketchup.Devices
{
    internal sealed class KetchupM35FdModule : PartModule, IDevice
    {
        #region Constants

        private const string ConfigKeyVersion = "Version";
        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyShowWindow = "ShowWindow";
        private const string ConfigKeyCurrentStateCode = "CurrentStateCode";
        private const string ConfigKeyLastErrorCode = "LastErrorCode";
        private const string ConfigKeyCurrentTrack = "CurrentTrack";
        private const string ConfigKeyInsertedDiskIndex = "InsertedDiskIndex";
        private const string ConfigKeyDiskPrefix = "Disk";
        private const string ConfigKeyLabel = "Label";
        private const string ConfigKeyIsWriteProtected = "IsWriteProtected";
        private const string ConfigKeyData = "Data";

        private const uint ConfigVersion = 1;

        private const int WordsPerSector = 512;
        private const int SectorsPerTrack = 18;
        private const int TracksPerDisk = 80;

        private const float WordsPerSecond = 30700f;
        private const float TracksPerSecond = 0.0024f;

        private const int SectorsPerDisk = SectorsPerTrack * TracksPerDisk;
        private const int WordsPerDisk = WordsPerSector * SectorsPerDisk;

        private const float SecondsPerSector = WordsPerSector / WordsPerSecond;

        #endregion

        #region Static Fields

        private static GUIStyle _styleButtonPressed;
        private static bool _isStyleInit;

        #endregion

        #region Instance Fields

        private IDcpu16 _dcpu16;
        private FloppyDisk _disk;

        private List<FloppyDisk> _allLoadedDisks = new List<FloppyDisk>();
        private List<FloppyDisk> _allAvailableDisks = new List<FloppyDisk>();

        private ushort _interruptMessage;

        private StateCode _currentStateCode;
        private ErrorCode _lastErrorCode;

        private int _currentTrack;

        private Rect _windowPosition;
        private bool _showWindow;
        private bool _isWindowPositionInit;
        private GuiMode _guiMode;
        private readonly Dictionary<FloppyDisk, string> _disksBeingLabeled = new Dictionary<FloppyDisk, string>();

        #endregion

        #region Device Identifiers

        public string FriendlyName
        {
            get { return @"Mackapar 3.5"" Floppy Drive (M35FD)"; }
        }

        public uint ManufacturerId
        {
            get { return (uint)Constants.ManufacturerId.Mackapar; }
        }

        public uint DeviceId
        {
            get { return (uint)Constants.DeviceId.M35FdFloppyDrive; }
        }

        public ushort Version
        {
            get { return 0x000b; }
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
            _currentStateCode = default(StateCode);
            _lastErrorCode = default(ErrorCode);
            _currentTrack = default(int);
        }

        public int OnInterrupt()
        {
            if (_dcpu16 != null)
            {
                switch ((InterruptOperation)_dcpu16.A)
                {
                    case InterruptOperation.PollDevice:
                        HandlePollDevice();
                        break;

                    case InterruptOperation.SetInterrupt:
                        HandleSetInterrupt(_dcpu16.X);
                        break;

                    case InterruptOperation.ReadSector:
                        HandleReadSector(_dcpu16.X, _dcpu16.Y);
                        break;

                    case InterruptOperation.WriteSector:
                        HandleWriteSector(_dcpu16.X, _dcpu16.Y);
                        break;
                }
            }

            return 0;
        }

        #endregion

        #region PartModule Methods

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                InitStylesIfNecessary();
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
                    _windowPosition.x = x;
                }

                float y;
                if (Single.TryParse(node.GetValue(ConfigKeyWindowPositionY), out y))
                {
                    _windowPosition.y = y;
                }

                _isWindowPositionInit = true;

                bool showWindow;
                if (Boolean.TryParse(node.GetValue(ConfigKeyShowWindow), out showWindow))
                {
                    _showWindow = showWindow;
                }

                ushort currentStateCode;
                if (UInt16.TryParse(node.GetValue(ConfigKeyCurrentStateCode), out currentStateCode))
                {
                    _currentStateCode = (StateCode)currentStateCode;
                }

                ushort lastErrorCode;
                if (UInt16.TryParse(node.GetValue(ConfigKeyLastErrorCode), out lastErrorCode))
                {
                    _lastErrorCode = (ErrorCode)lastErrorCode;
                }

                int currentTrack;
                if (Int32.TryParse(node.GetValue(ConfigKeyCurrentTrack), out currentTrack))
                {
                    _currentTrack = currentTrack;
                }

                var i = 0;
                while (node.HasNode(ConfigKeyDiskPrefix + i))
                {
                    var diskNode = node.GetNode(ConfigKeyDiskPrefix + i);

                    var label = diskNode.GetValue(ConfigKeyLabel);
                    bool isWriteProtected; Boolean.TryParse(diskNode.GetValue(ConfigKeyIsWriteProtected), out isWriteProtected);
                    var data = diskNode.GetValue(ConfigKeyData);

                    _allLoadedDisks.Add(new FloppyDisk(data) { Label = label, IsWriteProtected = isWriteProtected });

                    i++;
                }

                int diskIndex;
                if (Int32.TryParse(node.GetValue(ConfigKeyInsertedDiskIndex), out diskIndex))
                {
                    if (diskIndex >= 0)
                    {
                        _disk = _allLoadedDisks[diskIndex];
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue(ConfigKeyVersion, ConfigVersion);
            node.AddValue(ConfigKeyWindowPositionX, _windowPosition.x);
            node.AddValue(ConfigKeyWindowPositionY, _windowPosition.y);
            node.AddValue(ConfigKeyShowWindow, _showWindow);
            node.AddValue(ConfigKeyCurrentStateCode, (ushort)_currentStateCode);
            node.AddValue(ConfigKeyLastErrorCode, (ushort)_lastErrorCode);
            node.AddValue(ConfigKeyCurrentTrack, _currentTrack);
            node.AddValue(ConfigKeyInsertedDiskIndex, _allLoadedDisks.IndexOf(_disk));
            for (var i = 0; i < _allLoadedDisks.Count; i++)
            {
                var diskNode = node.AddNode(ConfigKeyDiskPrefix + i);
                diskNode.AddValue(ConfigKeyLabel, _allLoadedDisks[i].Label);
                diskNode.AddValue(ConfigKeyIsWriteProtected, _allLoadedDisks[i].IsWriteProtected);
                diskNode.AddValue(ConfigKeyData, _allLoadedDisks[i].Serialize());
            }
        }

        #endregion

        #region KSP Events

        [KSPEvent(guiActive = true, guiName = "Toggle M35FD Interface")]
        public void ToggleInterface()
        {
            _showWindow = !_showWindow;
        }

        #endregion

        #region Helper Methods

        private void EjectDisk()
        {
            _disk = null;

            SetErrorOrState(state: StateCode.NoMedia);
        }

        private void InsertDisk(FloppyDisk disk)
        {
            _disk = disk;

            SetErrorOrState(state: disk.IsWriteProtected ? StateCode.ReadyWp : StateCode.Ready);
        }

        private void SetErrorOrState(ErrorCode? error = null, StateCode? state = null)
        {
            var doInterrupt = _dcpu16 != null && _interruptMessage != 0 && (error != _lastErrorCode || state != _currentStateCode);

            if (error != null)
            {
                _lastErrorCode = error.Value;
            }

            if (state != null)
            {
                _currentStateCode = state.Value;
            }

            if (doInterrupt)
            {
                _dcpu16.Interrupt(_interruptMessage);
            }
        }

        private void HandlePollDevice()
        {
            _dcpu16.B = (ushort)_currentStateCode;
            _dcpu16.C = (ushort)_lastErrorCode;

            SetErrorOrState(error: ErrorCode.None);
        }

        private void HandleSetInterrupt(ushort interruptMessage)
        {
            _interruptMessage = interruptMessage;
        }

        private void HandleReadSector(ushort sector, ushort address)
        {
            switch (_currentStateCode)
            {
                case StateCode.Ready:
                case StateCode.ReadyWp:
                    _dcpu16.B = 1;

                    StartCoroutine(TransferCoroutine(sector, address, TransferOpreration.Read));

                    break;

                case StateCode.NoMedia:
                    _dcpu16.B = 0;
                    SetErrorOrState(error: ErrorCode.NoMedia);
                    break;

                case StateCode.Busy:
                    _dcpu16.B = 0;
                    SetErrorOrState(error: ErrorCode.Busy);
                    break;
            }
        }

        private void HandleWriteSector(ushort sector, ushort address)
        {
            switch (_currentStateCode)
            {
                case StateCode.Ready:
                    _dcpu16.B = 1;
                    StartCoroutine(TransferCoroutine(sector, address, TransferOpreration.Write));
                    break;

                case StateCode.ReadyWp:
                    _lastErrorCode = ErrorCode.Protected;
                    _dcpu16.B = 0;
                    break;

                case StateCode.NoMedia:
                    _lastErrorCode = ErrorCode.NoMedia;
                    _dcpu16.B = 0;
                    break;

                case StateCode.Busy:
                    _lastErrorCode = ErrorCode.Busy;
                    _dcpu16.B = 0;
                    break;
            }
        }

        private IEnumerator TransferCoroutine(ushort sector, ushort address, TransferOpreration operation)
        {
            SetErrorOrState(state: StateCode.Busy);

            var targetTrack = TrackForSector(sector);
            var trackDifference = Math.Abs(targetTrack - _currentTrack);

            if (trackDifference != 0)
            {
                yield return new WaitForSeconds(trackDifference * TracksPerSecond);
            }

            if (TransferOkToContinue(sector))
            {
                _currentTrack = targetTrack;

                yield return new WaitForSeconds(SecondsPerSector);

                if (TransferOkToContinue(sector))
                {
                    ushort[] source, destination;
                    int sourceIndex, destinationIndex;

                    switch (operation)
                    {
                        case TransferOpreration.Read:
                            source = _disk.GetSector(sector);
                            sourceIndex = 0;
                            destination = _dcpu16.Memory;
                            destinationIndex = address;
                            break;
                        case TransferOpreration.Write:
                            source = _dcpu16.Memory;
                            sourceIndex = address;
                            destination = _disk.GetSector(sector);
                            destinationIndex = 0;
                            break;
                        default:
                            throw new Exception(String.Format("Unexpected operation: {0}.", operation));
                    }

                    Array.Copy(source, sourceIndex, destination, destinationIndex, WordsPerSector);
                }
            }

            if (_currentStateCode == StateCode.Busy)
            {
                SetErrorOrState(state: _disk.IsWriteProtected ? StateCode.ReadyWp : StateCode.Ready);
            }

            yield return null;
        }

        private bool TransferOkToContinue(ushort sector)
        {
            if (_currentStateCode == StateCode.NoMedia)
            {
                SetErrorOrState(error: ErrorCode.Eject);
                return false;
            }

            if (_disk.IsBadSector(sector))
            {
                SetErrorOrState(error: ErrorCode.BadSector);
                return false;
            }

            return true;
        }

        private void OnDraw()
        {
            if (vessel.isActiveVessel && _showWindow)
            {
                GUI.skin = HighLogic.Skin;

                _windowPosition = GUILayout.Window(4, _windowPosition, OnM35FdWindow, "M35FD");
            }
        }

        private void OnM35FdWindow(int windowId)
        {
            GUI.skin = HighLogic.Skin;

            var insertEjectButtonPressed = false;
            var cancelInsertButtonPressed = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(_currentStateCode == StateCode.NoMedia ? "<Empty>" : _disk.Label);

            switch(_guiMode)
            {
                case GuiMode.Normal:
                    if (_allLoadedDisks.Any())
                    {
                        insertEjectButtonPressed = GUILayout.Button(_currentStateCode == StateCode.NoMedia ? "Insert" : "Eject");
                    }
                    break;

                case GuiMode.Insert:
                    cancelInsertButtonPressed = GUILayout.Button("Insert", _styleButtonPressed);
                    break;
            }
            
            GUILayout.EndHorizontal();

            var disksToDestroy = new List<FloppyDisk>();

            var availableDisks = _allLoadedDisks.Where(i => i != _disk).ToList();

            if (availableDisks.Any())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Disks:");
                GUILayout.EndHorizontal();

                foreach (var disk in availableDisks)
                {
                    GUILayout.BeginHorizontal();

                    switch (_guiMode)
                    {
                        case GuiMode.Normal:
                        case GuiMode.Get:
                            if (_disksBeingLabeled.ContainsKey(disk))
                            {
                                _disksBeingLabeled[disk] = GUILayout.TextField(_disksBeingLabeled[disk], GUILayout.Width(125));
                            }
                            else
                            {
                                GUILayout.Label(disk.Label, GUILayout.Width(125));
                            }

                            if (GUILayout.Button("Label"))
                            {
                                if (_disksBeingLabeled.ContainsKey(disk))
                                {
                                    var label = _disksBeingLabeled[disk];

                                    if (!String.IsNullOrEmpty(label) && !String.IsNullOrEmpty(label.Trim()))
                                    {
                                        disk.Label = label;
                                    }

                                    _disksBeingLabeled.Remove(disk);
                                }
                                else
                                {
                                    _disksBeingLabeled.Add(disk, disk.Label);
                                }
                            }

                            if (disk.IsWriteProtected)
                            {
                                if (GUILayout.Button("Protect", _styleButtonPressed))
                                {
                                    disk.IsWriteProtected = !disk.IsWriteProtected;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Protect"))
                                {
                                    disk.IsWriteProtected = !disk.IsWriteProtected;
                                }
                            }

                            if (GUILayout.Button("Destroy"))
                            {
                                disksToDestroy.Add(disk);
                            }

                            break;

                        case GuiMode.Insert:
                            if (GUILayout.Button(disk.Label))
                            {
                                _guiMode = GuiMode.Normal;
                                InsertDisk(disk);
                            }

                            break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No Available Disks");
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            var getDiskButtonPressed = GUILayout.Button("Get Disk");
            GUILayout.EndHorizontal();

            if (_guiMode == GuiMode.Get)
            {
                if (_allAvailableDisks == null)
                {
                    _allAvailableDisks = GetDiskImages().ToList();
                }

                GUILayout.Label("Disk Images:");

                foreach (var disk in _allAvailableDisks)
                {
                    if (GUILayout.Button(disk.Label))
                    {
                        _allLoadedDisks.Add(disk);
                        _allLoadedDisks = _allLoadedDisks.OrderBy(i => i.Label).ToList();

                        _guiMode = GuiMode.Normal;
                    }
                }
            }
            else
            {
                _allAvailableDisks = null;
            }

            GUI.DragWindow();

            if (GUI.changed)
            {
                if (insertEjectButtonPressed)
                {
                    if (_currentStateCode == StateCode.NoMedia)
                    {
                        _guiMode = GuiMode.Insert;
                    }
                    else
                    {
                        EjectDisk();
                    }
                }

                if (getDiskButtonPressed)
                {
                    _guiMode = GuiMode.Get;
                }

                if (cancelInsertButtonPressed)
                {
                    _guiMode = GuiMode.Normal;
                }

                _windowPosition = new Rect(_windowPosition) { width = 300, height = 0 };
            }

            foreach (var disk in disksToDestroy)
            {
                _allLoadedDisks.Remove(disk);
            }
        }

        private void InitWindowPositionIfNecessary()
        {
            if (!_isWindowPositionInit)
            {
                _windowPosition = _windowPosition.CenteredOnScreen();

                _isWindowPositionInit = true;
            }
        }

        private static IEnumerable<FloppyDisk> GetDiskImages()
        {
            yield return new FloppyDisk("<Blank Disk>", new ushort[0]);

            foreach (var file in IoUtility.GetFloppyFiles())
            {
                if (file.Length / 2 <= WordsPerDisk)
                {
                    var diksImageBytes = File.ReadAllBytes(file.FullName);
                    var diskImageUShorts = new ushort[diksImageBytes.Length / 2];

                    for (var i = 0; i < diksImageBytes.Length; i += 2)
                    {
                        var a = diksImageBytes[i];
                        var b = diksImageBytes[i + 1];

                        diskImageUShorts[i / 2] = (ushort)((a << 8) | b);
                    }

                    yield return new FloppyDisk(file.Name.Substring(0, file.Name.Length - 4), diskImageUShorts);
                }
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

        private static int TrackForSector(ushort sector)
        {
            return sector / SectorsPerTrack;
        }

        private static bool IsValidSector(ushort sector)
        {
            return sector < SectorsPerDisk;
        }

        #endregion

        #region Nested Types

        private enum InterruptOperation : ushort
        {
            PollDevice      = 0x0000,
            SetInterrupt    = 0x0001,
            ReadSector      = 0x0002,
            WriteSector     = 0x0003,
        }

        private enum ErrorCode : ushort
        {
            None        = 0x0000,
            Busy        = 0x0001,
            NoMedia     = 0x0002,
            Protected   = 0x0003,
            Eject       = 0x0004,
            BadSector   = 0x0005,
            // ReSharper disable once UnusedMember.Local
            Broken      = 0xffff,
        }

        private enum StateCode : ushort
        {
            NoMedia = 0x0000,
            Ready   = 0x0001,
            ReadyWp = 0x0002,
            Busy    = 0x0003,
        }

        private enum TransferOpreration : byte
        {
            Read    = 1,
            Write   = 2,
        }

        private enum GuiMode : byte
        {
            Normal  = 0,
            Insert  = 1,
            Get     = 2,
        }

        private sealed class FloppyDisk
        {
            #region Constants

            private const uint MagicNumber = 0xdbb0cae1;
            private const uint VersionNumber = 0x00000001;

            #endregion

            #region Instance Fields

            private readonly Dictionary<ushort, ushort[]> _sectors;

            private bool _isWriteProtected;
            private string _label;

            #endregion

            #region Properties

            // ReSharper disable once ConvertToAutoProperty
            public bool IsWriteProtected
            {
                get { return _isWriteProtected; }
                set { _isWriteProtected = value; }
            }

            // ReSharper disable once ConvertToAutoProperty
            public string Label
            {
                get { return _label; }
                set { _label = value; }
            }

            #endregion

            #region Constructors

            public FloppyDisk(string label, ushort[] diskImage)
            {
                _label = label;
                _sectors = ConvertDiskImage(diskImage);
            }

            public FloppyDisk(string serializedData)
            {
                _sectors = new Dictionary<ushort,ushort[]>();

                using (var reader = new BinaryStateReader(serializedData))
                {
                    // Header
                    var magicNumber = reader.ReadUInt32();
                    if (magicNumber != MagicNumber)
                    {
                        throw new LoadStateException("M35FD Floppy Disk", String.Format("Magic number is incorrect. Expected: {0:X}. Found: {1:X}.", MagicNumber, magicNumber));
                    }

                    var versionNumber = reader.ReadUInt32();
                    if (versionNumber != VersionNumber)
                    {
                        throw new LoadStateException("M35FD Floppy Disk", String.Format("Unsupported version number: {0}", versionNumber));
                    }

                    // Data
                    var sectorCount = reader.ReadInt32();
                    for (var i = 0; i < sectorCount; i++)
                    {
                        var sectorNumber = reader.ReadUInt16();
                        var sectorData = new ushort[WordsPerSector];
                        for (var j = 0; j < WordsPerSector; j++)
                        {
                            sectorData[j] = reader.ReadUInt16();
                        }

                        _sectors.Add(sectorNumber, sectorData);
                    }
                }
            }

            #endregion

            #region Methods

            public bool IsBadSector(ushort sector)
            {
                if (!IsValidSector(sector))
                {
                    throw new Exception(String.Format("Bad sector number: {0}.", sector));
                }

                return false;
            }

            public ushort[] GetSector(ushort sector)
            {
                if (!IsValidSector(sector))
                {
                    throw new Exception(String.Format("Bad sector number: {0}.", sector));
                }

                if (!_sectors.ContainsKey(sector))
                {
                    _sectors[sector] = new ushort[WordsPerSector];
                }

                return _sectors[sector];
            }

            public string Serialize()
            {
                using (var writer = new BinaryStateWriter())
                {
                    // Header
                    writer.Write(MagicNumber);
                    writer.Write(VersionNumber);

                    // Data
                    writer.Write(_sectors.Count);
                    foreach (var sectorKeyValue in _sectors)
                    {
                        var sectorNumber = sectorKeyValue.Key;
                        var sectorData = sectorKeyValue.Value;
                        writer.Write(sectorNumber);
                        for (var i = 0; i < WordsPerSector; i++)
                        {
                            writer.Write(sectorData[i]);
                        }
                    }

                    return writer.ToString();
                }
            }

            #endregion

            #region Helpers Methods

            private static Dictionary<ushort, ushort[]> ConvertDiskImage(ushort[] diskImage)
            {
                if (diskImage.Length > WordsPerDisk)
                {
                    throw new Exception("Disk too large."); // TODO: better exception
                }

                var sectors = new Dictionary<ushort, ushort[]>();

                var numberOfSectors = (diskImage.Length / WordsPerSector) + 1;
                for (ushort i = 0; i < numberOfSectors; i++)
                {
                    var sector = new ushort[WordsPerSector];

                    var start = i * WordsPerSector;
                    var end = Math.Min(start + WordsPerSector, diskImage.Length);

                    Array.Copy(diskImage, start, sector, 0, end - start);

                    sectors[i] = sector;
                }

                return sectors;
            }

            #endregion
        }

        #endregion
    }
}
