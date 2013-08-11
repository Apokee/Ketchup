namespace Ketchup.Api
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
        /// Called when the device is connected to the CPU. CPU should be notified of connection seperately. Behavior
        /// is undefined if performed during execution.
        /// </summary>
        /// <param name="dcpu16">The CPU the device is connected to.</param>
        void OnConnect(IDcpu16 dcpu16);

        /// <summary>
        /// Called when the device is disconnected from the CPU. The device should no longer modify the state of the
        /// CPU or fire interrupts. CPU should be notified of disconnection seperately. Behavior is undefined if
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
