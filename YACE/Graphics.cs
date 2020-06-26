using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace YACE
{
    public class Graphics
    {
        private const int FRAME_WIDTH = 64;
        private const int FRAME_HEIGHT = 32;

        public byte[,] FrameBuffer { get; private set; }

        public event Action ScreenRefresh;

        private Memory _memory;

        public Graphics(Memory Memory)
        {
            _memory = Memory;

            FrameBuffer = new byte[64, 32];
        }

        public void ClearDisplay()
        {
            for(int y = 0;y<FrameBuffer.GetUpperBound(1) + 1; y++)
            {
                for(int x = 0; x < FrameBuffer.GetUpperBound(0) + 1; x++)
                {
                    FrameBuffer[x, y] = 0;
                }
            }
            ScreenRefresh?.Invoke();
        }

        public bool DrawSprite(byte StartX, byte StartY, byte Height, ushort IAddress)
        {
            bool returnValue = false;
            for(int y = 0; y < Height; y++)
            {
                byte currentByte = _memory.ReadByte(IAddress);
                for(int x = 0; x < 8; x++)
                {
                    byte currentBit = (byte)((currentByte >> (7-x)) & 1);
                    if(currentBit == 1)
                    {
                        if (StartX + x >= FRAME_WIDTH || StartY + y >= FRAME_HEIGHT)
                            continue;

                        if (FrameBuffer[StartX +  x , StartY +  y] == 0)
                            FrameBuffer[StartX + x,StartY +  y] = 1;
                        else
                        {
                            FrameBuffer[StartX + x,StartY + y] = 0;
                            //Set if pixel flipped from 0 to 1
                            returnValue = true;
                        }
                    }
                }
                IAddress++;
            }

            ScreenRefresh?.Invoke();

            return returnValue;
        }

    }
}
