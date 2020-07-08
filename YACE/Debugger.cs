using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace YACE
{
    public class Debugger
    {
        private Emulator _emulator;
        private CPU _cpu;
        private Memory _memory;

        public event Action RegisterValueChanged;
        public event Action Stepped;

        public Debugger(Emulator Emulator, CPU CPU,Memory Memory)
        {
            _emulator = Emulator;
            _cpu = CPU;
            _memory = Memory;
        }

        public ushort GetProgramCounter()
        {
            return _cpu.PC;
        }

        public void SetProgramCounter(ushort Value)
        {
            if (Value > 0xFFF)
                throw new ArgumentException("Program Counter cannot exceed 4095.");
            _cpu.PC = Value;
            RegisterValueChanged?.Invoke();
        }

        public byte GetMainRegister(int RegisterIndex)
        {
            if (RegisterIndex > 15)
                throw new ArgumentException("Register Index cannot be greater than 15.");
            return _cpu.Registers[RegisterIndex];
        }

        public void SetMainRegister(int RegisterIndex, byte Value)
        {
            if(RegisterIndex > 15)
                throw new ArgumentException("Register Index cannot be greater than 15.");
            _cpu.Registers[RegisterIndex] = Value;
            RegisterValueChanged?.Invoke();
        }

        public ushort GetRegisterI()
        {
            return _cpu.RegisterI;
        }

        public void SetRegisterI(ushort Value)
        {
            if (Value > 0xFFF)
                throw new ArgumentException("Register value should not exceed 4095.");
            _cpu.RegisterI = Value;
            RegisterValueChanged?.Invoke();
        }

        public void Step()
        {
            _emulator.Tick();
            Stepped?.Invoke();
        }
        
        public byte[] GetRAM()
        {
            return _memory.RAM;
        }

        public Span<byte> GetROM()
        {
            return _memory.ReadROM();
        }

        public int GetROMBaseAddress()
        {
            return _memory.ROMBaseAddress;
        }

    }
}
