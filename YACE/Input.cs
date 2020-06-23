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
            _keysPressed = new bool[0xF];
        }

        public bool GetKeyState(byte Keycode)
        {
            return _keysPressed[Keycode];
        }

        public void SetKeyState(byte Keycode, bool State)
        {
            if(State == true)
            {
                KeyPressed?.DynamicInvoke(new object[] { Keycode });
            }
            _keysPressed[Keycode] = State;
        }

    }
}
