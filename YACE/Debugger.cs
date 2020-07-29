using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace YACE
{
    /// <summary>
    /// Class for accessing emulator debug information
    /// </summary>
    public class Debugger
    {
        private Emulator _emulator;
        private CPU _cpu;
        private Memory _memory;

        /// <summary>
        /// Occurs when a register's value is changed via the debugger.
        /// </summary>
        public event Action RegisterValueChanged;
        /// <summary>
        /// Occurs when the emulator is stepped using the debugger.
        /// </summary>
        public event Action Stepped;

        public Debugger(Emulator Emulator, CPU CPU,Memory Memory)
        {
            _emulator = Emulator;
            _cpu = CPU;
            _memory = Memory;
        }

        /// <summary>
        /// Get the current Program Counter register value
        /// </summary>
        /// <returns></returns>
        public ushort GetProgramCounter()
        {
            return _cpu.PC;
        }

        /// <summary>
        /// Set the current Program Counter register value.
        /// </summary>
        /// <param name="Value"></param>
        public void SetProgramCounter(ushort Value)
        {
            if (Value > 0xFFF)
                throw new ArgumentException("Program Counter cannot exceed 4095.");
            _cpu.PC = Value;
            RegisterValueChanged?.Invoke();
        }

        /// <summary>.
        /// Get the value of the specified register
        /// </summary>
        /// <param name="RegisterIndex">The index of the register (0x0 - 0xF).</param>
        /// <returns>The register value</returns>
        public byte GetMainRegister(int RegisterIndex)
        {
            if (RegisterIndex > 15)
                throw new ArgumentException("Register Index cannot be greater than 15.");
            return _cpu.Registers[RegisterIndex];
        }

        /// <summary>
        /// Sets the value of the specified register
        /// </summary>
        /// <param name="RegisterIndex">The index of the register (0x0 - 0xF).</param>
        /// <param name="Value">The new value</param>
        public void SetMainRegister(int RegisterIndex, byte Value)
        {
            if(RegisterIndex > 15)
                throw new ArgumentException("Register Index cannot be greater than 15.");
            _cpu.Registers[RegisterIndex] = Value;
            RegisterValueChanged?.Invoke();
        }

        /// <summary>
        /// Get the value of register I (address register).
        /// </summary>
        /// <returns></returns>
        public ushort GetRegisterI()
        {
            return _cpu.RegisterI;
        }

        /// <summary>
        /// Set the value of register I (address register).
        /// </summary>
        /// <param name="Value"></param>
        public void SetRegisterI(ushort Value)
        {
            if (Value > 0xFFF)
                throw new ArgumentException("Register value should not exceed 4095.");
            _cpu.RegisterI = Value;
            RegisterValueChanged?.Invoke();
        }

        /// <summary>
        /// Execute a single instruction on the CPU and then raise the stepped event.
        /// </summary>
        public void Step()
        {
            _emulator.Tick();
            Stepped?.Invoke();
        }
        
        /// <summary>
        /// Gets a reference to the current main memory (RAM) of the emulator.
        /// </summary>
        /// <returns></returns>
        public byte[] GetRAM()
        {
            return _memory.RAM;
        }

        /// <summary>
        /// Gets a reference to the currently loaded ROM.
        /// </summary>
        /// <returns></returns>
        public Span<byte> GetROM()
        {
            return _memory.ReadROM();
        }

        /// <summary>
        /// Gets the start address of memory where ROMs are loaded.
        /// </summary>
        /// <returns></returns>
        public int GetROMBaseAddress()
        {
            return _memory.ROMBaseAddress;
        }

    }
}
