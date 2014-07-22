using System.IO;
using System.Reflection;
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
            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiApplicationLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
        }

        public void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiApplicationLauncherReady);
        }

        private void OnGuiApplicationLauncherReady()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                Log(LogLevel.Debug, "Adding AppLauncher button");

                _button = ApplicationLauncher.Instance.AddModApplication(
                    OnButtonTrue,
                    OnButtonFalse,
                    OnButtonHover,
                    OnButtonHoverOut,
                    OnEnable,
                    OnDisable,
                    ButtonScenes,
                    _texture
                );
            }
        }

        private void OnGameSceneLoadRequested(GameScenes data)
        {
            if (_button != null)
            {
                Log(LogLevel.Debug, "Removing AppLauncher button");

                ApplicationLauncher.Instance.RemoveModApplication(_button);
            }
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

        private void OnEnable()
        {
            Log(LogLevel.Debug, "OnEnable()");
        }

        private void OnDisable()
        {
            Log(LogLevel.Debug, "OnDisable()");
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
