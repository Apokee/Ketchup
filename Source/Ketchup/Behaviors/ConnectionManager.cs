using System.Collections.Generic;
using Ketchup.Api.v0;
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

            foreach (var part in parts)
            {
                Log(LogLevel.Debug, "RecalculateConnections(): {0} {1} the root",
                    part.partInfo.title, part.parent == null ? "is" : "is not"
                );

                var computers = part.FindModulesImplementing<ModuleKetchupComputer>();
                var devices = part.FindModulesImplementing<IDevice>();

                if (computers.Count > 0)
                {
                    Log(LogLevel.Debug, "RecalculateConnections(): {0} computer(s) found", computers.Count);
                }

                if (devices.Count > 0)
                {
                    Log(LogLevel.Debug, "RecalculateConnections(): {0} device(s) found", devices.Count);
                }
            }

            _recalculateConnections = false;
        }

        #endregion

        #region Helper Methods

        private static void Log(LogLevel level, string message, params object[] args)
        {
            Service.Debug.Log("ConnectionManager", level, message, args);
        }

        #endregion
        
    }
}
