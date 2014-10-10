using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ketchup.Api.v0;
using Ketchup.Modules;
using Ketchup.Services;
using UnityEngine;

namespace Ketchup.Behaviors
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal sealed class GuiManager : MonoBehaviour
    {
        private const ApplicationLauncher.AppScenes ButtonScenes =
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH |
            ApplicationLauncher.AppScenes.FLIGHT;

        private static Texture2D _appLauncherTexture;

        private ApplicationLauncherButton _button;
        private bool _canShowButton;

        private EditorLogic _editorLogic;
        private int _editorPartCount;

        private bool _showWindow;
        private Rect _windowRect;

        public void Awake()
        {
            Log(LogLevel.Debug, "Awake()");

            if (_appLauncherTexture == null)
            {
                Log(LogLevel.Debug, "Loading AppLauncher texture");

                var texture = new Texture2D(38, 38, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(Path.Combine(
                    GetBaseDirectory().FullName, "Textures/AppLauncher.png"
                )));

                _appLauncherTexture = texture;
            }
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                _editorLogic = EditorLogic.fetch;
            }

            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiApplicationLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            GameEvents.onPartAttach.Add(OnPartAttach);
            GameEvents.onPartRemove.Add(OnPartRemove);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onStageSeparation.Add(OnStageSeperation);
            GameEvents.onJointBreak.Add(OnJointBreak);
            GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            GameEvents.onFlightReady.Add(OnFlightReady);
        }

        public void Update()
        {
            EditorCheckForFirstPart();
        }

        public void OnDestroy()
        {
            GameEvents.onFlightReady.Remove(OnFlightReady);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            GameEvents.onJointBreak.Remove(OnJointBreak);
            GameEvents.onStageSeparation.Remove(OnStageSeperation);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiApplicationLauncherReady);
        }

        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            if (_showWindow && (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
            {
                GUI.skin = HighLogic.Skin;

                if (_windowRect == default(Rect))
                {
                    var windowWidth = Screen.width / 2;
                    var windowHeight = Screen.height / 2;
                    var windowLeft = (Screen.width - windowWidth) / 2;
                    var windowTop = (Screen.height - windowHeight) / 2;

                    _windowRect = new Rect(windowLeft, windowTop, windowWidth, windowHeight);
                }

                _windowRect = GUILayout.Window(50, _windowRect, OnWindow, "Computer");
            }
        }

        private void OnWindow(int windowId)
        {
            List<Part> parts = null;

            if (HighLogic.LoadedSceneIsEditor)
            {
                parts = _editorLogic.ship.Parts;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                parts = FlightGlobals.ActiveVessel.Parts;
            }

            if (parts != null)
            {
                var devices = parts.SelectMany(i => i.FindModulesImplementing<IDevice>());

                foreach (var device in devices)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(device.FriendlyName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                    GUILayout.EndHorizontal();
                }
            }

            GUI.DragWindow();
        }

        private void OnGuiApplicationLauncherReady()
        {
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                Log(LogLevel.Debug, "Adding AppLauncher button");

                // TODO: Enable this when there's a GUI worth using
                //_canShowButton = true;

                UpdateButtonState(checkParts: false);
            }
        }

        private void OnGameSceneLoadRequested(GameScenes data)
        {
            if (_button != null)
            {
                _canShowButton = false;

                UpdateButtonState(checkParts: false);
            }
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            Log(LogLevel.Debug, "OnPartAttach()");

            UpdateButtonState(checkParts: true);
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            Log(LogLevel.Debug, "OnPartRemove()");

            UpdateButtonState(checkParts: true);
        }

        private void OnVesselChange(Vessel data)
        {
            Log(LogLevel.Debug, "OnVesselChange()");

            UpdateButtonState(checkParts: true);
        }

        private void OnStageSeperation(EventReport data)
        {
            Log(LogLevel.Debug, "OnStageSeperation(): {0}", data.origin.partInfo.title);

            UpdateButtonState(checkParts: true);
        }

        private void OnJointBreak(EventReport data)
        {
            Log(LogLevel.Debug, "OnJointBreak(): {0}", data.origin.partInfo.title);

            UpdateButtonState(checkParts: true);
        }

        private void OnPartDestroyed(Part data)
        {
            Log(LogLevel.Debug, "OnPartDestroyed()");

            UpdateButtonState(checkParts: true);
        }

        private void OnVesselWasModified(Vessel data)
        {
            Log(LogLevel.Debug, "OnVesselWasModified()");

            UpdateButtonState(checkParts: true);
        }

        private void OnFlightReady()
        {
            Log(LogLevel.Debug, "OnFlightReady()");

            UpdateButtonState(checkParts: true);
        }

        private void OnButtonTrue()
        {
            Log(LogLevel.Debug, "OnButtonTrue()");

            _showWindow = true;
        }

        private void OnButtonFalse()
        {
            Log(LogLevel.Debug, "OnButtonFalse()");

            _showWindow = false;
        }

        private void OnButtonHover()
        {
            Log(LogLevel.Debug, "OnButtonHover()");
        }

        private void OnButtonHoverOut()
        {
            Log(LogLevel.Debug, "OnButtonHoverOut()");
        }

        private void OnButtonEnable()
        {
            Log(LogLevel.Debug, "OnEnable()");
        }

        private void OnButtonDisable()
        {
            Log(LogLevel.Debug, "OnDisable()");
        }

        private void EditorCheckForFirstPart()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (_editorLogic != null)
                {
                    var oldPartCount = _editorPartCount;
                    var newPartCount = _editorLogic.ship.Count;

                    if (oldPartCount == 0 && newPartCount > 0)
                    {
                        Log(LogLevel.Debug, "First Part(s) Added");

                        UpdateButtonState(checkParts: true);
                    }

                    if (oldPartCount > 0 && newPartCount == 0)
                    {
                        Log(LogLevel.Debug, "Last Part(s) Removed");

                        UpdateButtonState(checkParts: true);
                    }

                    _editorPartCount = newPartCount;
                }
            }
        }

        private void UpdateButtonState(bool checkParts)
        {
            if (_canShowButton && checkParts)
            {
                IEnumerable<Part> parts = null;

                if (HighLogic.LoadedSceneIsEditor && _editorLogic != null)
                {
                    parts = _editorLogic.ship.Parts;
                }
                else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                {
                    parts = FlightGlobals.ActiveVessel.Parts;
                }

                if (parts != null)
                {
                    if (ContainsComputer(parts))
                    {
                        if (_button == null)
                        {
                            _button = ApplicationLauncher.Instance.AddModApplication(
                                OnButtonTrue,
                                OnButtonFalse,
                                OnButtonHover,
                                OnButtonHoverOut,
                                OnButtonEnable,
                                OnButtonDisable,
                                ButtonScenes,
                                _appLauncherTexture
                            );
                        }
                    }
                    else
                    {
                        if (_button != null)
                        {
                            ApplicationLauncher.Instance.RemoveModApplication(_button);
                        }
                    }
                }
            }

            if (_button != null && !_canShowButton)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_button);
            }
        }

        private static bool ContainsComputer(IEnumerable<Part> parts)
        {
            return parts.SelectMany(i => i.FindModulesImplementing<ModuleKetchupComputer>()).Any();
        }

        private static void Log(LogLevel level, string message, params object[] args)
        {
            Service.Debug.Log("ButtonManager", level, message, args);
        }

        // TODO: Move this to a shared utility
        private static DirectoryInfo GetBaseDirectory()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent;
        }
    }
}
