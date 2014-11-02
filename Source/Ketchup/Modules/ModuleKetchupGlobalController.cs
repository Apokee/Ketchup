using System;
using Ketchup.Api.v0;

namespace Ketchup.Modules
{
    [KSPModule("Device: Global Controller")]
    internal sealed class ModuleKetchupGlobalController : PartModule, IDevice
    {
        #region Device Identifiers

        public string FriendlyName { get { return "Global Controller"; } }
        public uint ManufacturerId { get { return (uint)Constants.ManufacturerId.Apokee; } }
        public uint DeviceId { get { return 0x73B0AD20; } } // FIXME: Original DeviceId was too long
        public ushort Version { get { return 0x0001; } }
        public Port Port { get; set; }

        #endregion

        private IDcpu16 _dcpu16;

        #region IDevice

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
            throw new NotImplementedException();
        }

        #endregion
    }
}
