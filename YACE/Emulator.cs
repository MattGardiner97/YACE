using System;

namespace YACE
{
    public class Emulator
    {
        public Memory Memory { get; private set; }
        public Graphics Graphics { get; private set; }
        public Input Input { get; private set; }
        public CPU CPU { get; private set; }

        public bool IsPaused { get; private set; } = false;
        public bool ROMIsLoaded { get; private set; } = false;

        public event Action Ticked;
        public event Action Paused;
        public event Action Resumed;
        //We use LateResumed only to begin the loop again on the main form. This ensures any other functions subscribed to 'Resumed' get to run.
        public event Action LateResumed; 

        public delegate void ROMLoadedDelegate(Span<byte> ROM);
        public event ROMLoadedDelegate ROMLoaded;

        public Emulator()
        {
            Memory = new Memory();
            Graphics = new Graphics(Memory);
            Input = new Input();
            CPU = new CPU(Memory, Graphics, Input);

            CPU.UnblockedByKeypress += () => { if (IsPaused == true) Ticked?.Invoke(); };
        }

        public void Tick()
        {
            CPU.Tick();
            if (IsPaused == true)
                Ticked?.Invoke();
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
