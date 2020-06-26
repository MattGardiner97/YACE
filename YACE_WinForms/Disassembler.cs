using System;
using System.Collections.Generic;
using System.Text;
using YACE;

namespace YACE_WinForms
{
    public class Disassembler
    {
        private int _baseAddress;
        private byte[] _rom;

        public List<Instruction> Instructions { get; private set; }


        public Disassembler(int ROMBaseAddress)
        {
            _baseAddress = ROMBaseAddress;
            Instructions = new List<Instruction>();
        }

        public void LoadROM(byte[] ROM)
        {
            _rom = new byte[ROM.Length];
            Array.Copy(ROM, _rom, ROM.Length);
        }

        public void Disassemble()
        {
            for (int i = 0; i < _rom.Length; i += 2)
            {
                ushort opcode = (ushort)(_rom[i] << 8 | _rom[i + 1]);
                ushort location = (ushort)(_baseAddress + i);
                string address = Hex((opcode & 0xFFF), 4);
                string value = Hex((opcode & 0xFF), 2);
                string regX = "V" + Hex(ReadNibble(opcode, 2), 1);
                string regY = "V" + Hex(ReadNibble(opcode, 1), 1);

                switch (opcode)
                {
                    case 0x00E0:
                        CreateInstruction(location, opcode, "clear_screen");
                        break;
                    case 0x00EE:
                        CreateInstruction(location, opcode, "return");
                        break;
                    case ushort code when InRange(0x1000, code):
                        CreateInstruction(location, opcode, $"goto {address}");
                        break;
                    case ushort code when InRange(0x2000, code):
                        CreateInstruction(location, opcode, $"call {address}");
                        break;
                    case ushort code when InRange(0x3000, code):
                        CreateInstruction(location, opcode, $"if {regX} != {value}");
                        break;
                    case ushort code when InRange(0x4000, code):
                        CreateInstruction(location, opcode, $"if {regX} == {value}");
                        break;
                    case ushort code when InRange(0x5000, code):
                        CreateInstruction(location, opcode, $"if {regX} != {regY}");
                        break;
                    case ushort code when InRange(0x6000, code):
                        CreateInstruction(location, opcode, $"{regX} = {value}");
                        break;
                    case ushort code when InRange(0x7000, code):
                        CreateInstruction(location, opcode, $"{regX} += {value}");
                        break;
                    case ushort code when InRange(0x8000, code):
                        switch (ReadNibble(code, 0))
                        {
                            case 0:
                                CreateInstruction(location, opcode, $"{regX} = {regY}");
                                break;
                            case 1:
                                CreateInstruction(location, opcode, $"{regX} = {regX} | {regY}");
                                break;
                            case 2:
                                CreateInstruction(location, opcode, $"{regX} = {regX} & {regY}");
                                break;
                            case 3:
                                CreateInstruction(location, opcode, $"{regX} = {regX} ^ {regY}");
                                break;
                            case 4:
                                CreateInstruction(location, opcode, $"{regX} += {regY}");
                                break;
                            case 5:
                                CreateInstruction(location, opcode, $"{regX} -= {regY}");
                                break;
                            case 6:
                                CreateInstruction(location, opcode, $"{regX} >>= 1");
                                break;
                            case 7:
                                CreateInstruction(location, opcode, $"{regX} = {regY} - {regX}");
                                break;
                            case 0xE:
                                CreateInstruction(location, opcode, $"{regX} <<= 1");
                                break;
                            default:
                                CreateInstruction(location, opcode, "unknown");
                                break;
                        }
                        break;
                    case ushort code when InRange(0x9000, code):
                        CreateInstruction(location, opcode, $"if {regX} == {regY}");
                        break;
                    case ushort code when InRange(0xA000, code):
                        CreateInstruction(location, opcode, $"I = {address}");
                        break;
                    case ushort code when InRange(0xB000, code):
                        CreateInstruction(location, opcode, $"goto V0 + {address}");
                        break;
                    case ushort code when InRange(0xC000, code):
                        CreateInstruction(location, opcode, $"{regX} = rand() & {value}");
                        break;
                    case ushort code when InRange(0xD000, code):
                        CreateInstruction(location, opcode, $"draw({regX}, {regY}, {Hex(ReadNibble(opcode, 0), 1)})");
                        break;
                    case ushort code when InRange(0xE000, code):
                        if (ReadNibble(opcode, 0) == 0xE)
                            CreateInstruction(location, opcode, $"if key({regX}) == true");
                        else if (ReadNibble(opcode, 0) == 0x1)
                            CreateInstruction(location, opcode, $"if key({regX}) == false");
                        else
                            CreateInstruction(location, opcode, "unknown");
                        break;
                    case ushort code when InRange(0xF000, code):
                        switch (code & 0xFF)
                        {
                            case 7:
                                CreateInstruction(location, opcode, $"{regX} = delay");
                                break;
                            case 0xA:
                                CreateInstruction(location, opcode, $"{regX} = await_key()");
                                break;
                            case 0x15:
                                CreateInstruction(location, opcode, $"delay = {regX}");
                                break;
                            case 0x18:
                                CreateInstruction(location, opcode, $"sound = {regX}");
                                break;
                            case 0x1E:
                                CreateInstruction(location, opcode, $"I += {regX}");
                                break;
                            case 0x29:
                                CreateInstruction(location, opcode, $"I = sprite({regX})");
                                break;
                            case 0x33:
                                CreateInstruction(location, opcode, $"store_bcd({regX})");
                                break;
                            case 0x55:
                                CreateInstruction(location, opcode, $"reg_dump(V0..{regX})");
                                break;
                            case 0x65:
                                CreateInstruction(location, opcode, $"reg_load(V0..{regX})");
                                break;
                            default:
                                CreateInstruction(location, opcode, "unknown");
                                break;
                        }
                        break;
                    default:
                        CreateInstruction(location, opcode, "unknown");
                        break;
                }
            }
        }

        private string Hex(int Value, int Size)
        {
            return Value.ToString($"X{Size}");
        }

        private bool InRange(ushort LowerBound, ushort Code)
        {
            return Code >= LowerBound && Code < LowerBound + 0x1000;
        }

        private byte ReadNibble(ushort Opcode, int NibbleIndex)
        {
            int shiftCount = (NibbleIndex * 4);
            return (byte)(((0xF << shiftCount) & Opcode) >> shiftCount);
        }

        private void CreateInstruction(int Location, int Opcode, string Assembly)
        {
            Instruction instruction = new Instruction(Location, Opcode, Assembly);
            Instructions.Add(instruction);
        }

    }
}
