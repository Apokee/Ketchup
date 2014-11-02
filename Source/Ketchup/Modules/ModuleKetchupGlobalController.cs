using System;
using Ketchup.Api.v0;

namespace Ketchup.Modules
{
    [KSPModule("Device: Global Controller")]
    internal sealed class ModuleKetchupGlobalController : PartModule, IDevice
    {
        #region Device Identifiers

        public string FriendlyName
        {
            get { throw new NotImplementedException(); }
        }

        public uint ManufacturerId
        {
            get { throw new NotImplementedException(); }
        }

        public uint DeviceId
        {
            get { throw new NotImplementedException(); }
        }

        public ushort Version
        {
            get { throw new NotImplementedException(); }
        }

        public Port Port
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IDevice

        public void OnConnect(IDcpu16 dcpu16)
        {
            throw new NotImplementedException();
        }

        public void OnDisconnect()
        {
            throw new NotImplementedException();
        }

        public int OnInterrupt()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
