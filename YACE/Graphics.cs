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

        /// <summary>
        /// The current emulator framebuffer. Each element refers to a single pixel on the screen. Values can be 1 or 0.
        /// </summary>
        public byte[,] FrameBuffer { get; private set; }

        /// <summary>
        /// Raised when the frame buffer is drawn to.
        /// </summary>
        public event Action ScreenRefresh;

        private Memory _memory;

        public Graphics(Memory Memory)
        {
            _memory = Memory;

            FrameBuffer = new byte[64, 32];
        }

        /// <summary>
        /// Resets the state of the object.
        /// </summary>
        public void Reset()
        {
            ClearDisplay();
        }

        /// <summary>
        /// Resets all pixels to 0.
        /// </summary>
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

        /// <summary>
        /// Draws a sprite to the frame buffer. 
        /// </summary>
        /// <param name="StartX">The X position to draw the sprite</param>
        /// <param name="StartY">The Y position to draw the sprite</param>
        /// <param name="Height">How many rows of bytes to draw.</param>
        /// <param name="IAddress">The memory location to load sprite data from.</param>
        /// <remarks>Sprites are represented by bytes starting at IAddress. All sprites are 8 pixels wide.
        /// Each byte represents a row of pixels, each bit represents a single pixel. The height parameter represents how many bytes will be read.
        /// A bit value of 1 instructs to flip the target pixel state. A bit value of 0 does nothing to the target pixel.</remarks>
        /// <returns>Indicates whether a bit was set from 0 to 1. Often used for collision detection.</returns>
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
