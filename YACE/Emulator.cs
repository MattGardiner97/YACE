using System;

namespace YACE
{
    public class Emulator
    {
        public Memory Memory { get; private set; }
        public Graphics Graphics { get; private set; }
        public Input Input { get; private set; }
        public CPU CPU { get; private set; }

        public event Action Ticked;

        public Emulator()
        {
            Memory = new Memory();
            Graphics = new Graphics(Memory);
            Input = new Input();
            CPU = new CPU(Memory,Graphics,Input);
        }

        public void Tick()
        {
            CPU.Tick();
            this?.Ticked.Invoke();
        }

    }
}
