using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YACE_Tests")]

namespace YACE
{
    /// <summary>
    /// Main class responsible for emulation
    /// </summary>
    public class Emulator
    {

        /// <summary>
        /// Represents the device's volatile memory (RAM)
        /// </summary>
        internal Memory Memory { get; private set; }
        /// <summary>
        /// Handles drawing operations for the device
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Handles user input for the device
        /// </summary>
        public Input Input { get; private set; }
        /// <summary>
        /// Performs the core of the emulation work
        /// </summary>
        internal CPU CPU { get; private set; }
        /// <summary>
        /// Bridge between library consumers and emulator internals
        /// </summary>
        public Debugger Debugger { get; private set; }

        /// <summary>
        /// Represents whether the device is currently paused
        /// </summary>
        public bool IsPaused { get; private set; } = false;
        /// <summary>
        /// Represents whether a ROM is currently loaded
        /// </summary>
        public bool ROMIsLoaded { get; private set; } = false;

        /// <summary>
        /// Raised when the emulator is paused.
        /// </summary>
        public event Action Paused;
        /// <summary>
        /// Raised when the emulator is resumed.
        /// </summary>
        public event Action Resumed;
        /// <summary>
        /// Raised after the <see cref="Resumed"/> event is raised.
        /// </summary>
        //We use LateResumed only to begin the loop again on the main form. This ensures any other functions subscribed to 'Resumed' get to run.
        public event Action LateResumed;
        /// <summary>
        /// Raised when the emulator needs to play a sound effect. Playing the sound is performed by the frontend subscribed to this event.
        /// </summary>
        public event Action Beeped;

        public delegate void ROMLoadedDelegate(Span<byte> ROM);
        /// <summary>
        /// Raised when a ROM is loaded into memory.
        /// </summary>
        public event ROMLoadedDelegate ROMLoaded;

        public Emulator()
        {
            Memory = new Memory();
            Graphics = new Graphics(Memory);
            Input = new Input();
            CPU = new CPU(Memory, Graphics, Input);
            Debugger = new Debugger(this, CPU,Memory);

            CPU.Beeped += () => { this.Beeped?.Invoke(); };
        }

        /// <summary>
        /// Executes a single instruction on the CPU
        /// </summary>
        public void Tick()
        {
            CPU.Tick();
        }

        /// <summary>
        /// Pauses emulation.
        /// </summary>
        public void Pause()
        {
            if (ROMIsLoaded == false)
                return;

            IsPaused = true;
            Paused?.Invoke();
        }

        /// <summary>
        /// Resumes emulation.
        /// </summary>
        public void Resume()
        {
            if (ROMIsLoaded == false)
                return;

            IsPaused = false;
            Resumed?.Invoke();

            LateResumed?.Invoke();
        }

        /// <summary>
        /// Resets the state of the machine and loads a new ROM into main memory.
        /// </summary>
        /// <param name="ROM"></param>
        public void LoadROM(byte[] ROM)
        {
            Reset();
            Memory.LoadROM(ROM);

            ROMIsLoaded = true;

            Span<byte> romSpan = Memory.RAM.AsSpan<byte>(Memory.ROMBaseAddress,ROM.Length);
            ROMLoaded?.Invoke(romSpan);
        }

        /// <summary>
        /// Resets the state of the machine.
        /// </summary>
        public void Reset()
        {
            Memory.Reset();
            Graphics.Reset();
            CPU.Reset();
        }
    }
}
