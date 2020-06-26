using System;
using System.Collections.Generic;
using System.Text;

namespace YACE
{
    public class Input
    {
        private bool[] _keysPressed;

        public event Action<byte> KeyPressed;

        public Input()
        {
            _keysPressed = new bool[16];
        }

        public bool GetKeyState(byte Keycode)
        {
            return _keysPressed[Keycode];
        }

        public void SetKeyState(byte Keycode, bool State)
        {
            if (Keycode > 0xF)
                return;
            if (State == true)
            {
                KeyPressed?.Invoke(Keycode);
            }
            _keysPressed[Keycode] = State;
        }

    }
}
