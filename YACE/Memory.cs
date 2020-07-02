using System;
using System.Collections.Generic;
using System.Text;

namespace YACE
{
    public class Memory
    {
        private const ushort RAM_SIZE = 0x1000;
        private const ushort STACK_BASE = 0xEA0;
        private const ushort STACK_MAX = STACK_BASE + 48;
        private const ushort FONT_DATA_BASE_ADDRESS = 0;
        private const ushort ROM_BASE_ADDRESS = 0X200;

        private int _romSize = 0;

        public ushort FontDataBaseAddress { get { return FONT_DATA_BASE_ADDRESS; } }
        public ushort ROMBaseAddress { get { return ROM_BASE_ADDRESS; } }

        public byte[] RAM { get; private set; }

        public ushort StackPointer { get; set; } = 0xEA0;


        public Memory()
        {
            RAM = new byte[RAM_SIZE];
            byte[] fontData = new byte[]
            {
                0xF0,0x90,0x90,0x90,0xF0,
                0x20,0x60,0x20,0x20,0x70,
                0xF0,0x10,0xF0,0x80,0xF0,
                0xF0,0x10,0xF0,0x10,0xF0,
                0x90,0x90,0xF0,0x10,0x10,
                0xF0,0x80,0xF0,0x10,0xF0,
                0xF0,0x80,0xF0,0x90,0xF0,
                0xF0,0x10,0x20,0x40,0x40,
                0xF0,0x90,0xF0,0x90,0xF0,
                0xF0,0x90,0xF0,0x10,0xF0,
                0xF0,0x90,0xF0,0x90,0x90,
                0xE0,0x90,0xE0,0x90,0xE0,
                0xF0,0x80,0x80,0x80,0xF0,
                0xE0,0x90,0x90,0x90,0xE0,
                0xF0,0x80,0xF0,0x80,0xF0,
                0xF0,0x80,0xF0,0x80,0x80
            };
            Array.Copy(fontData, RAM, fontData.Length);
        }

        public void LoadROM(byte[] ROM)
        {
            _romSize = ROM.Length;
            Array.Copy(ROM, 0, RAM, ROM_BASE_ADDRESS, ROM.Length);

            Span<byte> romSpan = RAM.AsSpan<byte>(ROM_BASE_ADDRESS, ROM.Length);
        }

        public byte ReadByte(ushort Address)
        {
            return RAM[Address];

        }
        public byte ReadByte(int Address)
        {
            return ReadByte((ushort)Address);
        }

        public void WriteByte(ushort Address, byte Value)
        {
            RAM[Address] = Value;
        }
        public void WriteByte(int Address, byte Value)
        {
            WriteByte((ushort)Address, Value);
        }

        public ushort ReadShort(ushort Address)
        {
            byte b1 = RAM[Address];
            byte b2 = RAM[Address + 1];
            ushort result = (ushort)(b1 << 8);
            result = (ushort)(result | b2);
            return result;
        }

        public ushort Read12BitAddress(ushort BaseAddress)
        {
            ushort result = ReadShort(BaseAddress);
            result = (ushort)(result & 0x0FFF);
            return result;
        }

        public void WriteShort(ushort Address,ushort Value)
        {
            byte v1 = (byte)(Value >> 8);
            byte v2 = (byte)(Value & 0xFF);
            WriteByte(Address, v1);
            WriteByte(Address + 1, v2);
        }

        public void PushToStack(ushort Value)
        {
            if (StackPointer >= STACK_MAX)
                throw new StackOverflowException();

            WriteShort(StackPointer, Value);

            StackPointer += 2;
        }

        public ushort PopFromStack()
        {
            if (StackPointer <= STACK_BASE)
                throw new StackOverflowException();

            StackPointer -= 2;
            ushort result = ReadShort(StackPointer);
            return result;
        }

        public byte[] ReadROM()
        {
            byte[] result = new byte[_romSize];
            Array.Copy(RAM, ROMBaseAddress, result, 0, _romSize);
            return result;
        }

        public void Reset()
        {
            StackPointer = 0xEA0;
        }
    }
}
