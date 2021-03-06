﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace YACE
{
    /// <summary>
    /// Executes the CPU instructions from the ROM in memory.
    /// </summary>
    public class CPU
    {
        private Memory _memory;
        private Graphics _graphics;
        private Input _input;
        private Random _random;

        /// <summary>
        /// Program Counter. Memory location of the next instruction to be executed.
        /// </summary>
        public ushort PC { get; set; }
        /// <summary>
        /// Registers 0x0 - 0xF (0-15).
        /// </summary>
        public byte[] Registers { get; private set; }
        /// <summary>
        /// Address Register. Used by instructions that require a 2 byte address to be supplied.
        /// </summary>
        public ushort RegisterI { get; set; }
        /// <summary>
        /// The delay timer counts down at a rate of 60Hz and can be used for timing.
        /// </summary>
        public byte DelayTimer { get; set; }
        /// <summary>
        /// The sound timer counts down at a rate of 60Hz and emits a noise when the value is non-zero.
        /// </summary>
        public byte SoundTimer { get; set; }

        //The currently decoded opcode.
        private ushort _opcode;

        //The time the timers ticked last. This is used to roughly calculate when they should decrement.
        private DateTime _delayTimerLastTick;
        private DateTime _soundTimerLastTick;

        /// <summary>
        /// Indicates that the program is currently blocked and waiting for user input.
        /// </summary>
        public bool WaitingForInput { get; set; } = false;
        /// <summary>
        /// The register to store the pressed key value in.
        /// </summary>
        private byte _waitingForInputTargetRegister;

        /// <summary>
        ///Occurs when a key is pressed while the CPU is blocked and awaiting a key press (Opcode 0xFX0A)
        /// </summary>
        public event Action UnblockedByKeypress;
        /// <summary>
        /// Raised when the value of SoundTimer is non-zero.
        /// </summary>
        public event Action Beeped;

        public CPU(Memory Memory, Graphics Graphics, Input Input)
        {
            _memory = Memory;
            _graphics = Graphics;
            _input = Input;
            _input.KeyPressed += KeyPressEventHandler;
            _random = new Random();

            _delayTimerLastTick = new DateTime(0);
            _soundTimerLastTick = new DateTime(0);

            PC = _memory.ROMBaseAddress;
            Registers = new byte[16];
        }

        /// <summary>
        /// Resets the value of all registers.
        /// </summary>
        public void Reset()
        {
            PC = _memory.ROMBaseAddress;
            for (int i = 0; i < 16; i++)
                Registers[i] = 0;
            RegisterI = 0;
            DelayTimer = 0;
            SoundTimer = 0;
            WaitingForInput = false;
        }

        /// <summary>
        /// Executes the next instruction in memory and updates any timers as needed.
        /// </summary>
        public void Tick()
        {
            if (DelayTimer != 0 && (DateTime.Now - _delayTimerLastTick).TotalMilliseconds >= 1 / 60)
            {
                DelayTimer--;
                _delayTimerLastTick = DateTime.Now;
            }
            if (SoundTimer != 0 && (DateTime.Now - _soundTimerLastTick).TotalMilliseconds >= 1 / 60)
            {
                Beeped?.Invoke();
                SoundTimer--;
                _soundTimerLastTick = DateTime.Now;
            }

            if (WaitingForInput == false)
            {
                _opcode = Fetch();
                Func<ushort> func = Decode(_opcode);
                Execute(func);
            }


        }

        /// <summary>
        /// Retrieves the next opcode from memory.
        /// </summary>
        /// <returns>The opcode to be executed next.</returns>
        private ushort Fetch()
        {
            return (ushort)_memory.ReadShort(PC);
        }

        /// <summary>
        /// Determines which function to call next based on the current opcode.
        /// </summary>
        /// <param name="Opcode">The opcode to be decoded.</param>
        /// <returns>A delegate pointing to the next function which should be called.</returns>
        private Func<ushort> Decode(ushort Opcode)
        {
            Func<ushort> result = null;

            switch (Opcode)
            {
                case ushort code when code >= 0x0000 && code < 0x1000 && code != 0x00E0 && code != 0x00EE:
                    result = CallRCAProgram;
                    break;
                case 0x00E0:
                    result = ClearScreen;
                    break;
                case 0x00EE:
                    result = ReturnFromSubroutine;
                    break;
                case ushort code when code >= 0x1000 && code < 0x2000:
                    result = JumpToAddress;
                    break;
                case ushort code when code >= 0x2000 && code < 0x3000:
                    result = CallSubroutine;
                    break;
                case ushort code when code >= 0x3000 && code < 0x4000:
                    result = SkipNextIfEqual;
                    break;
                case ushort code when code >= 0x4000 && code < 0x5000:
                    result = SkipNextIfNotEqual;
                    break;
                case ushort code when code >= 0x5000 && code < 0x6000:
                    result = SkipNextIfRegistersEqual;
                    break;
                case ushort code when code >= 0x6000 && code < 0x7000:
                    result = SetRegisterToImmediate;
                    break;
                case ushort code when code >= 0x7000 && code < 0x8000:
                    result = AddImmediateToRegister;
                    break;
                case ushort code when code >= 0x8000 && code < 0x9000:
                    byte trailNibble = Helpers.ReadNibble(code, 0);
                    switch (trailNibble)
                    {
                        case 0:
                            result = SetRegisterToRegister;
                            break;
                        case 1:
                            result = OrRegisters;
                            break;
                        case 2:
                            result = AndRegisters;
                            break;
                        case 3:
                            result = XorRegisters;
                            break;
                        case 4:
                            result = AddRegistersWithCarry;
                            break;
                        case 5:
                            result = SubtractFromRegisterWithCarry;
                            break;
                        case 6:
                            result = RightShift;
                            break;
                        case 7:
                            result = SetRegisterToSubtractionResult;
                            break;
                        case 0xE:
                            result = LeftShift;
                            break;
                        default:
                            throw new NotImplementedException($"Opcode {Opcode} is not implemented");
                    }
                    break;
                case ushort code when code >= 0x9000 && code < 0xA000 && Helpers.ReadNibble(code, 0) == 0:
                    result = SkipNextIfRegistersNotEqual;
                    break;
                case ushort code when code >= 0xA000 && code < 0xB000:
                    result = SetRegisterI;
                    break;
                case ushort code when code >= 0xB000 && code < 0xC000:
                    result = JumpToAddressPlusV0;
                    break;
                case ushort code when code >= 0xC000 && code < 0xD000:
                    result = SetRegisterToRandomAnd;
                    break;
                case ushort code when code >= 0xD000 && code < 0xE000:
                    result = DrawSprite;
                    break;
                case ushort code when code >= 0xE000 && code < 0xF000:
                    if (Helpers.ReadNibble(code, 0) == 0xE)
                        result = SkipNextIfKeyPressed;
                    else if (Helpers.ReadNibble(code, 0) == 0x1)
                        result = SkipNextIfKeyNotPressed;
                    else
                        throw new NotImplementedException($"Opcode {Opcode} is not implemented");
                    break;
                case ushort code when code >= 0xF000:
                    switch (Helpers.ReadNibble(code, 0))
                    {
                        case 0x7:
                            result = SetRegisterToDelayTimer;
                            break;
                        case 0xA:
                            result = AwaitKeyPress;
                            break;
                        case 0x5:
                            if (Helpers.ReadNibble(code, 1) == 1)
                                result = SetDelayTimer;
                            else if (Helpers.ReadNibble(code, 1) == 5)
                                result = RegisterDump;
                            else if (Helpers.ReadNibble(code, 1) == 6)
                                result = RegisterLoad;
                            else
                                throw new NotImplementedException($"Opcode {Opcode} is not implemented");
                            break;
                        case 0x8:
                            result = SetSoundTimer;
                            break;
                        case 0xE:
                            result = AddRegisterToI;
                            break;
                        case 0x9:
                            result = SetIToSpriteAddress;
                            break;
                        case 0x3:
                            result = StoreBinaryCodedDecimal;
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException($"Opcode {Opcode} is not implemented");

            }

            return result;
        }

        /// <summary>
        /// Executes the next instruction and increments the program counter.
        /// </summary>
        /// <param name="Func">The function to call.</param>
        private void Execute(Func<ushort> Func)
        {
            ushort pcIncrementCount = Func();
            PC += pcIncrementCount;
        }

        /// <summary>
        /// Handles a key being pressed while the program is blocked.
        /// </summary>
        /// <param name="Keycode">The keycode of the pressed key (0x0 - 0xF).</param>
        private void KeyPressEventHandler(byte Keycode)
        {
            if (WaitingForInput == true)
            {
                Registers[_waitingForInputTargetRegister] = Keycode;
                WaitingForInput = false;
                PC += 2;
                UnblockedByKeypress?.Invoke();
            }
        }

        //Opcode functions
        private ushort CallRCAProgram()
        {
            throw new NotImplementedException();
        }

        private ushort ClearScreen()
        {
            _graphics.ClearDisplay();
            return 2;
        }

        private ushort ReturnFromSubroutine()
        {
            ushort returnAddress = _memory.PopFromStack();
            PC = returnAddress;
            return 0;
        }

        private ushort JumpToAddress()
        {
            ushort address = _memory.Read12BitAddress(PC);
            PC = address;
            return 0;
        }

        private ushort CallSubroutine()
        {
            ushort address = _memory.Read12BitAddress(PC);
            _memory.PushToStack((ushort)(PC + 2));
            PC = address;
            return 0;
        }

        private ushort SkipNextIfEqual()
        {
            byte registerID = Helpers.ReadNibble(_opcode, 2);
            byte value = _memory.ReadByte(PC + 1);
            if (Registers[registerID] == value)
                return 4;
            else
                return 2;
        }

        private ushort SkipNextIfNotEqual()
        {
            byte registerID = Helpers.ReadNibble(_opcode, 2);
            byte value = _memory.ReadByte(PC + 1);
            if (Registers[registerID] != value)
                return 4;
            else
                return 2;
        }

        private ushort SkipNextIfRegistersEqual()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            if (Registers[registerID1] == Registers[registerID2])
                return 4;
            else return 2;
        }

        private ushort SetRegisterToImmediate()
        {
            byte registerID = Helpers.ReadNibble(_opcode, 2);
            Registers[registerID] = _memory.ReadByte(PC + 1);
            return 2;
        }

        private ushort AddImmediateToRegister()
        {
            byte registerID = Helpers.ReadNibble(_opcode, 2);
            Registers[registerID] += _memory.ReadByte(PC + 1);
            return 2;
        }

        private ushort SetRegisterToRegister()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            Registers[registerID1] = Registers[registerID2];
            return 2;
        }

        private ushort OrRegisters()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            Registers[registerID1] = (byte)(Registers[registerID1] | Registers[registerID2]);
            return 2;
        }

        private ushort AndRegisters()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            Registers[registerID1] = (byte)(Registers[registerID1] & Registers[registerID2]);
            return 2;
        }

        private ushort XorRegisters()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            Registers[registerID1] = (byte)(Registers[registerID1] ^ Registers[registerID2]);
            return 2;
        }

        private ushort AddRegistersWithCarry()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            if (Registers[registerID1] + Registers[registerID2] > 255)
                Registers[0xF] = 1;
            else
                Registers[0xF] = 0;
            Registers[registerID1] += Registers[registerID2];
            return 2;
        }
        private ushort SubtractFromRegisterWithCarry()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            if (Registers[registerID1] < Registers[registerID2])
                Registers[0xF] = 0;
            else
                Registers[0xF] = 1;
            Registers[registerID1] -= Registers[registerID2];
            return 2;
        }

        private ushort RightShift()
        {
            byte regID1 = Helpers.ReadNibble(_opcode, 2);
            byte regID2 = Helpers.ReadNibble(_opcode, 1);
            byte value = Registers[regID2];
            byte leastSignificantBit = (byte)(value & 1);
            Registers[0xF] = leastSignificantBit;
            Registers[regID1] = (byte)(value >> 1);
            return 2;
        }

        private ushort SetRegisterToSubtractionResult()
        {
            byte regID1 = Helpers.ReadNibble(_opcode, 2);
            byte regID2 = Helpers.ReadNibble(_opcode, 1);

            byte value1 = Registers[regID1];
            byte value2 = Registers[regID2];

            Registers[0xF] = (byte)(value2 < value1 ? 0 : 1);

            byte result = (byte)(value2 - value1);
            Registers[regID1] = result;

            return 2;
        }

        private ushort LeftShift()
        {
            byte regID1 = Helpers.ReadNibble(_opcode, 2);
            byte regID2 = Helpers.ReadNibble(_opcode, 1);
            byte value = Registers[regID2];
            byte msb = (byte)(value >> 7);
            Registers[0xF] = msb;
            byte result = (byte)(value << 1);
            Registers[regID1] = result;
            return 2;

        }

        private ushort SkipNextIfRegistersNotEqual()
        {
            byte registerID1 = Helpers.ReadNibble(_opcode, 2);
            byte registerID2 = Helpers.ReadNibble(_opcode, 1);
            if (Registers[registerID1] != Registers[registerID2])
                return 4;
            else
                return 2;
        }

        private ushort SetRegisterI()
        {
            ushort address = _memory.Read12BitAddress(PC);
            RegisterI = address;
            return 2;
        }

        private ushort JumpToAddressPlusV0()
        {
            ushort address = _memory.Read12BitAddress(PC);
            address += Registers[0];
            PC = address;
            return 0;
        }

        private ushort SetRegisterToRandomAnd()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            byte value = _memory.ReadByte(PC + 1);
            byte random = (byte)_random.Next(0, 256);
            Registers[regID] = (byte)(random & value);
            return 2;
        }

        private ushort DrawSprite()
        {
            byte regID1 = Helpers.ReadNibble(_opcode, 2);
            byte regID2 = Helpers.ReadNibble(_opcode, 1);
            byte x = Registers[regID1];
            byte y = Registers[regID2];
            byte height = Helpers.ReadNibble(_opcode, 0);
            bool pixelsFlippedResult = _graphics.DrawSprite(x, y, height, RegisterI);
            Registers[0xF] = (byte)(pixelsFlippedResult ? 1 : 0);
            return 2;
        }

        private ushort SkipNextIfKeyPressed()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            byte key = Registers[regID];
            if (_input.GetKeyState(key) == true)
                return 4;
            else
                return 2;
        }

        private ushort SkipNextIfKeyNotPressed()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            byte key = Registers[regID];
            if (_input.GetKeyState(key) == false)
                return 4;
            else
                return 2;
        }

        private ushort SetRegisterToDelayTimer()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            Registers[regID] = DelayTimer;
            return 2;
        }

        private ushort AwaitKeyPress()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            WaitingForInput = true;
            _waitingForInputTargetRegister = regID;
            return 0;
        }

        private ushort SetDelayTimer()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            DelayTimer = Registers[regID];
            return 2;
        }

        private ushort SetSoundTimer()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            SoundTimer = Registers[regID];
            return 2;
        }

        private ushort AddRegisterToI()
        {
            Registers[0xF] = 0;
            byte regID = Helpers.ReadNibble(_opcode, 2);
            ushort result = (ushort)(RegisterI + Registers[regID]);
            if (result > 0xFFF)
            {
                result = (ushort)(result - 0xFFF);
                Registers[0xF] = 1;
            }
            RegisterI = result;
            return 2;
        }

        private ushort SetIToSpriteAddress()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            byte character = Registers[regID];
            RegisterI = (ushort)(_memory.FontDataBaseAddress + (character * 5));
            return 2;
        }
        private ushort StoreBinaryCodedDecimal()
        {
            byte regID = Helpers.ReadNibble(_opcode, 2);
            byte value = Registers[regID];
            byte hundreds = (byte)(value / 100);
            byte tens = (byte)((value % 100) / 10);
            byte ones = (byte)(value % 10);
            _memory.WriteByte(RegisterI, hundreds);
            _memory.WriteByte(RegisterI + 1, tens);
            _memory.WriteByte(RegisterI + 2, ones);
            return 2;
        }
        private ushort RegisterDump()
        {
            byte maxRegID = Helpers.ReadNibble(_opcode, 2);
            ushort currentIAddress = RegisterI;
            for (byte regID = 0; regID <= maxRegID; regID++)
            {
                _memory.WriteByte(currentIAddress, Registers[regID]);
                currentIAddress++;
            }
            return 2;
        }
        private ushort RegisterLoad()
        {
            byte maxRegID = Helpers.ReadNibble(_opcode, 2);
            ushort currentIAddress = RegisterI;
            for (byte regID = 0; regID <= maxRegID; regID++)
            {
                Registers[regID] = _memory.ReadByte(currentIAddress);
                currentIAddress++;
            }
            return 2;
        }
    }
}
