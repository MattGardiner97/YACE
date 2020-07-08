using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YACE_Tests")]

namespace YACE
{
    public class Emulator
    {

        internal Memory Memory { get; private set; }
        public Graphics Graphics { get; private set; }
        public Input Input { get; private set; }
        internal CPU CPU { get; private set; }
        public Debugger Debugger { get; private set; }

        public bool IsPaused { get; private set; } = false;
        public bool ROMIsLoaded { get; private set; } = false;

        public event Action Paused;
        public event Action Resumed;
        //We use LateResumed only to begin the loop again on the main form. This ensures any other functions subscribed to 'Resumed' get to run.
        public event Action LateResumed;
        public event Action Beeped;
        public delegate void ROMLoadedDelegate(Span<byte> ROM);
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

        public void Tick()
        {
            CPU.Tick();
        }

        public void Pause()
        {
            if (ROMIsLoaded == false)
                return;

            IsPaused = true;
            Paused?.Invoke();
        }

        public void Resume()
        {
            if (ROMIsLoaded == false)
                return;

            IsPaused = false;
            Resumed?.Invoke();

            LateResumed?.Invoke();
        }

        public void LoadROM(byte[] ROM)
        {
            Reset();
            Memory.LoadROM(ROM);

            ROMIsLoaded = true;

            Span<byte> romSpan = Memory.RAM.AsSpan<byte>(Memory.ROMBaseAddress,ROM.Length);
            ROMLoaded?.Invoke(romSpan);
        }

        public void Reset()
        {
            Memory.Reset();
            Graphics.Reset();
            CPU.Reset();
        }
    }
}
