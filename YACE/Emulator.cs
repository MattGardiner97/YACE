using System;

namespace YACE
{
    public class Emulator
    {
        public Memory Memory { get; private set; }
        public Graphics Graphics { get; private set; }
        public Input Input { get; private set; }
        public CPU CPU { get; private set; }

        private bool _isPaused = false;

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                if (value == true)
                    Paused?.Invoke();
                else
                    Resumed?.Invoke();
            }
        }

        public event Action Ticked;
        public event Action Paused;
        public event Action Resumed;

        public Emulator()
        {
            Memory = new Memory();
            Graphics = new Graphics(Memory);
            Input = new Input();
            CPU = new CPU(Memory, Graphics, Input);

            CPU.KeypressUnblocked += () => { if (_isPaused == true) Ticked?.Invoke(); };
        }

        public void Tick()
        {
            CPU.Tick();
            if (_isPaused == true)
                Ticked?.Invoke();
        }

    }
}
