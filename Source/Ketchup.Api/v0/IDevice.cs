using System;

namespace Ketchup.Api.v0
{
    /// <summary>
    /// Hardware device that may be connected to an IDcpu16.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Name of the device that may be displayed to a user.
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// 32-bit manufacturer identifier.
        /// </summary>
        uint ManufacturerId { get; }

        /// <summary>
        /// 32-bit device identifier.
        /// </summary>
        uint DeviceId { get; }

        /// <summary>
        /// 16-bit device version number.
        /// </summary>
        ushort Version { get; }

        /// <summary>
        /// Identifier for a particular inatance of this device.
        /// </summary>
        /// <remarks>
        /// Implementors should save and restore this identifier unless its value is <see cref="Guid.Empty"/>.
        /// </remarks>
        Guid GlobalDeviceId { get; set; }

        /// <summary>
        /// Called when the device is connected to the CPU. CPU should be notified of connection separately. Behavior
        /// is undefined if performed during execution.
        /// </summary>
        /// <param name="dcpu16">The CPU the device is connected to.</param>
        void OnConnect(IDcpu16 dcpu16);

        /// <summary>
        /// Called when the device is disconnected from the CPU. The device should no longer modify the state of the
        /// CPU or fire interrupts. CPU should be notified of disconnection separately. Behavior is undefined if
        /// performed during execution.
        /// </summary>
        void OnDisconnect();

        /// <summary>
        /// Called when the device receives an interrupt.
        /// </summary>
        /// <returns>The number of cycles the device took to handle the interrupt.</returns>
        int OnInterrupt();
    }
}
