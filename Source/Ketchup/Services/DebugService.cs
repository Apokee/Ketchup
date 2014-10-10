using System;
using UnityEngine;

namespace Ketchup.Services
{
    public sealed class DebugService : IDebugService
    {
        public LogLevel Level { get; set; }

        public DebugService()
        {
#if DEBUG
            Level = LogLevel.Debug;
#else
            Level = LogLevel.Info;
#endif
        }

        public void Log(string context, LogLevel level, string message, params object[] args)
        {
            if (level > Level) { return; }

            var fullMessage = String.Format("[Ketchup:{0}] {1}", context, String.Format(message, args));

            switch(level)
            {
                case LogLevel.Error:
                    Debug.LogError(fullMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    Debug.Log(fullMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
