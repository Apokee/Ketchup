using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ketchup.Modules;
using Ketchup.Services;
using UnityEngine;

namespace Ketchup.Behaviors
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal sealed class ButtonManager : MonoBehaviour
    {
        private const ApplicationLauncher.AppScenes ButtonScenes =
            ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;

        private static Texture2D _texture;
        private ApplicationLauncherButton _button;
        private bool _canShowButton;

        private EditorLogic _editorLogic;
        private int _editorPartCount;

        public void Awake()
        {
            Log(LogLevel.Debug, "Awake()");

            if (_texture == null)
            {
                Log(LogLevel.Debug, "Loading button texture");

                var texture = new Texture2D(38, 38, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(Path.Combine(
                    GetBaseDirectory().FullName, "Textures/AppLauncher.png"
                )));

                _texture = texture;
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
        }

        public void Update()
        {
            EditorCheckForFirstPart();
        }

        public void OnDestroy()
        {
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiApplicationLauncherReady);
        }

        private void OnGuiApplicationLauncherReady()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                Log(LogLevel.Debug, "Adding AppLauncher button");

                _canShowButton = true;

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

        private void OnButtonTrue()
        {
            Log(LogLevel.Debug, "OnButtonTrue()");
        }

        private void OnButtonFalse()
        {
            Log(LogLevel.Debug, "OnButtonFalse()");
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

                if (_editorLogic != null)
                {
                    parts = _editorLogic.ship.Parts;
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
                                _texture
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
