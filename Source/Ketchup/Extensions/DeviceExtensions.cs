using Ketchup.Api.v0;

namespace Ketchup.Extensions
{
    public static class DeviceExtensions
    {
        private const string ConfigNodeDevicePort = "DEVICE_PORT";

        public static void LoadDevicePort(this IDevice device, ConfigNode node)
        {
            var devicePortNode = node.GetNode(ConfigNodeDevicePort);
            if (devicePortNode != null)
            {
                device.Port = new Port(devicePortNode);
            }
        }

        public static void SaveDevicePort(this IDevice device, ConfigNode node)
        {
            if (device.Port != null)
            {
                var devicePortNode = new ConfigNode(ConfigNodeDevicePort);
                device.Port.Save(devicePortNode);

                node.AddNode(devicePortNode);
            }
        }
    }
}
