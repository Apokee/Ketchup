using System.Collections.Generic;

namespace Ketchup.Api.v0
{
    /// <summary>
    /// DCPU-16 Processor.
    /// </summary>
    public interface IDcpu16
    {
        /// <summary>
        /// Register A.
        /// </summary>
        ushort A { get; set; }
        
        /// <summary>
        /// Register B.
        /// </summary>
        ushort B { get; set; }

        /// <summary>
        /// Register C.
        /// </summary>
        ushort C { get; set; }

        /// <summary>
        /// Register X.
        /// </summary>
        ushort X { get; set; }

        /// <summary>
        /// Register Y.
        /// </summary>
        ushort Y { get; set; }

        /// <summary>
        /// Register Z.
        /// </summary>
        ushort Z { get; set; }

        /// <summary>
        /// Register I.
        /// </summary>
        ushort I { get; set; }

        /// <summary>
        /// Register J.
        /// </summary>
        ushort J { get; set; }

        /// <summary>
        /// Program Counter.
        /// </summary>
        ushort PC { get; set; }

        /// <summary>
        /// Stack Pointer.
        /// </summary>
        ushort SP { get; set; }

        /// <summary>
        /// Extra/Excess.
        /// </summary>
        ushort EX { get; set; }

        /// <summary>
        /// Interrupt Address.
        /// </summary>
        ushort IA { get; set; }

        /// <summary>
        /// 65536 words of RAM.
        /// </summary>
        ushort[] Memory { get; }

        /// <summary>
        /// Indicates if the CPU is currently on fire. As per the DCPU-16 specifications, this occurs if the interrupt
        /// queue grows beyond 256 interrupts. Exact failure mode is determined by the implementation.
        /// </summary>
        bool IsOnFire { get; set; }

        /// <summary>
        /// Indicates if the interrupt queue is enabled.
        /// </summary>
        bool IsInterruptQueueEnabled { get; set; }

        /// <summary>
        /// Queue of interrupt messages.
        /// </summary>
        Queue<ushort> InterruptQueue { get; } 
        
        /// <summary>
        /// Called when a device is connected to the CPU. Device should be notified of connection separately. Behavior
        /// is undefined if performed during execution.
        /// </summary>
        /// <param name="device">The device to connect to the CPU.</param>
        /// <returns>The new device index.</returns>
        ushort OnConnect(IDevice device);
        
        /// <summary>
        /// Called when a device is disconnected from the CPU. Device should be notified of disconnection separately.
        /// Behavior is undefined if performed during execution.
        /// </summary>
        /// <param name="deviceIndex">The index of the device to disconnect from the CPU.</param>
        void OnDisconnect(ushort deviceIndex);

        /// <summary>
        /// Execute the next instruction.
        /// </summary>
        /// <returns>The number of cycles the instruction took to complete.</returns>
        int Execute();
        
        /// <summary>
        /// Fire an interrupt to the CPU.
        /// </summary>
        /// <param name="message">The interrupt message to send to the CPU.</param>
        void Interrupt(ushort message);
    }
}
