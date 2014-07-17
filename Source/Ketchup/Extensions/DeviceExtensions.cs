using System;
using Ketchup.Api.v0;

namespace Ketchup.Extensions
{
    public static class DeviceExtensions
    {
        private const string ConfigKeyGlobalDeviceId = "GlobalDeviceId";

        public static void LoadGlobalDeviceId(this IDevice device, ConfigNode node)
        {
            var connectionId = node.GetValue(ConfigKeyGlobalDeviceId);
            if (!String.IsNullOrEmpty(connectionId))
            {
                device.GlobalDeviceId = new Kuid(connectionId);
            }
        }

        public static void SaveGlobalDeviceId(this IDevice device, ConfigNode node)
        {
            if (device.GlobalDeviceId != null)
            {
                node.AddValue(ConfigKeyGlobalDeviceId, device.GlobalDeviceId.ToString());
            }
        }
    }
}
