using System;
using System.Collections.Generic;
using System.Linq;
using Ketchup.Api.v0;
using Ketchup.Data;
using Ketchup.Modules;
using UnityEngine;
using Ketchup.Services;

namespace Ketchup.Behaviors
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal sealed class ConnectionManager : MonoBehaviour
    {
        #region Constants

        private enum Mode
        {
            Editor,
            Flight,
        }

        #endregion

        #region Fields

        /// <summary>
        /// The mode the <see cref="ConnectionManager"/> should operate in.
        /// </summary>
        /// <remarks>
        /// <c>null</c> if the ConnectionManager is uninitialized.
        /// </remarks>
        private Mode? _mode;

        /// <summary>
        /// Whether or not connections should be recalculated on next Update().
        /// </summary>
        private bool _recalculateConnections;

        /// <summary>
        /// Reference to EditorLogic instance.
        /// </summary>
        /// <remarks>
        /// Always do a <c>null</c> check as it may not be set for some time after the editor loads.
        /// </remarks>
        private EditorLogic _editorLogic;

        /// <summary>
        /// The number of parts we last saw in the editor.
        /// </summary>
        private int _editorPartCount;

        #endregion

        #region MonoBehaviour

        public void Awake()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                Log(LogLevel.Debug, "Awake(): In Editor");
                _mode = Mode.Editor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                Log(LogLevel.Debug, "Awake(): In Flight");
                _mode = Mode.Flight;
            }
        }

        public void Start()
        {
            if (_mode != null)
            {
                Log(LogLevel.Debug, "Start()");

                if (_mode == Mode.Editor)
                {
                    _editorLogic = EditorLogic.fetch;
                }

                PrepareConnectionRecalculation();

                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.OnVesselRollout.Add(OnVesselRollout);
                GameEvents.onStageSeparation.Add(OnStageSeperation);
                GameEvents.onJointBreak.Add(OnJointBreak);
                GameEvents.onPartDestroyed.Add(OnPartDestroyed);
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            }
        }

        public void Update()
        {
            if (_mode != null)
            {
                EditorCheckForFirstPart();
                RecalculateIfNecessary();
            }
        }

        public void OnDestroy()
        {
            if (_mode != null)
            {
                Log(LogLevel.Debug, "OnDestroy()");

                GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
                GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
                GameEvents.onJointBreak.Remove(OnJointBreak);
                GameEvents.onStageSeparation.Remove(OnStageSeperation);
                GameEvents.OnVesselRollout.Remove(OnVesselRollout);
                GameEvents.onPartRemove.Remove(OnPartRemove);
                GameEvents.onPartAttach.Remove(OnPartAttach);
            }
        }

        #endregion

        #region Methods

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            Log(LogLevel.Debug, "OnPartAttach()");

            PrepareConnectionRecalculation();
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            Log(LogLevel.Debug, "OnPartRemove()");

            PrepareConnectionRecalculation();
        }

        private void OnVesselRollout(ShipConstruct data)
        {
            Log(LogLevel.Debug, "OnVesselRollout()");

            if (_mode == Mode.Flight)
            {
                var computers = data.Parts.SelectMany(i => i.FindModulesImplementing<ModuleKetchupComputer>());
                var devices = data.Parts.SelectMany(i => i.FindModulesImplementing<IDevice>());

                var updates = new Dictionary<Port, Port>();

                foreach (var device in devices.Where(i => i.Port.Scope == PortScope.Craft))
                {
                    var oldPort = device.Port;
                    var newPort = new Port(PortScope.Persistence, Guid.NewGuid());

                    device.Port = newPort;
                    updates[oldPort] = newPort;
                }

                foreach (var computer in computers)
                {
                    computer.UpdateDeviceConnections(updates);
                }
            }
        }

        private void OnStageSeperation(EventReport data)
        {
            Log(LogLevel.Debug, "OnStageSeperation(): {0}", data.origin.partInfo.title);

            EnsureConnectivityOfUnpackedVessels();
        }

        private void OnJointBreak(EventReport data)
        {
            Log(LogLevel.Debug, "OnJointBreak(): {0}", data.origin.partInfo.title);

            EnsureConnectivityOfUnpackedVessels();
        }

        private void OnPartDestroyed(Part data)
        {
            Log(LogLevel.Debug, "OnPartDestroyed()");

            EnsureConnectivityOfUnpackedVessels();
        }

        private void OnVesselWasModified(Vessel data)
        {
            Log(LogLevel.Debug, "OnVesselWasModified()");

            EnsureConnectivityOfUnpackedVessels();
        }

        private void EnsureConnectivityOfUnpackedVessels()
        {
            if (_mode == Mode.Flight)
            {
                foreach (var vessel in FlightGlobals.Vessels.Where(i => !i.packed))
                {
                    Log(LogLevel.Debug, "Found unpacked vessel: {0}", vessel.RevealName());

                    EnsureConnectivity(vessel.Parts);
                }
            }
        }

        private void EditorCheckForFirstPart()
        {
            if (_mode == Mode.Editor)
            {
                if (_editorLogic != null)
                {
                    var oldPartCount = _editorPartCount;
                    var newPartCount = _editorLogic.ship.Count;

                    if (oldPartCount == 0 && newPartCount > 0)
                    {
                        Log(LogLevel.Debug, "First Part(s) Added");

                        PrepareConnectionRecalculation();
                    }

                    if (oldPartCount > 0 && newPartCount == 0)
                    {
                        Log(LogLevel.Debug, "Last Part(s) Removed");

                        PrepareConnectionRecalculation();
                    }

                    _editorPartCount = newPartCount;
                }
            }
        }

        private void RecalculateIfNecessary()
        {
            if (_recalculateConnections)
            {
                if (_mode == Mode.Editor)
                {
                    if (_editorLogic != null)
                    {
                        RecalculateConnections(_editorLogic.ship.Parts);
                    }
                }
            }
        }

        private void PrepareConnectionRecalculation()
        {
            Log(LogLevel.Debug, "PrepareConnectionRecalculation()");

            _recalculateConnections = true;
        }

        private void RecalculateConnections(ICollection<Part> parts)
        {
            Log(LogLevel.Debug, "RecalculateConnections(): Vessel contains {0} parts", parts.Count);

            switch (_mode)
            {
                case Mode.Editor:
                    Log(LogLevel.Debug, "RecalculateConnections(): In Editor, proceeding");

                    var computers = GetComputers(parts).ToList();
                    var devices = GetDevices(parts);

                    var computer = computers[0];

                    computer.ResetDeviceConnections();
                    foreach (var device in devices)
                    {
                        if (device.Port == null)
                        {   // TODO: When there is a common DeviceModule base class, this should be moved
                            device.Port = new Port(PortScope.Craft, Guid.NewGuid());
                        }

                        computer.AddDeviceConnection(
                            new DeviceConnection(DeviceConnectionType.Automatic, device.Port, null)
                        );
                    }
                    break;
                case Mode.Flight:
                    Log(LogLevel.Debug, "RecalculateConnections(): In Flight, doing nothing");
                    break;
            }

            _recalculateConnections = false;
        }

        private static void EnsureConnectivity(IEnumerable<Part> parts)
        {
            var computers = GetComputers(parts);

            foreach (var computer in computers)
            {
                computer.EnsureDeviceConnections();
            }
        }

        #endregion

        #region Helper Methods

        private static void Log(LogLevel level, string message, params object[] args)
        {
            Service.Debug.Log("ConnectionManager", level, message, args);
        }

        private static IEnumerable<ModuleKetchupComputer> GetComputers(IEnumerable<Part> parts)
        {
            return parts.SelectMany(i => i.FindModulesImplementing<ModuleKetchupComputer>());
        }

        private static IEnumerable<IDevice> GetDevices(IEnumerable<Part> parts)
        {
            return parts.SelectMany(i => i.FindModulesImplementing<IDevice>());
        }

        #endregion
        
    }
}
