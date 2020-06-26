using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.CompilerServices;
using YACE;

namespace YACE_Tests
{
    [TestClass]
    public class CPUTests
    {
        private Emulator _emu;
        private Random _random;

        public CPUTests()
        {
            _emu = new Emulator();
            _random = new Random();
        }

        //Helpers
        private void SetNextInstruction(ushort Opcode)
        {
            _emu.Memory.WriteShort(_emu.Memory.ROMBaseAddress, Opcode);
            _emu.CPU.PC = _emu.Memory.ROMBaseAddress;
        }
        private void SetNextInstruction(int Opcode)
        {
            SetNextInstruction((ushort)Opcode);
        }
        private ushort GetRandomAddress()
        {
            return (ushort)_random.Next(0, 0x1000);
        }
        private ushort GetOpcodeWithXRegister(ushort InstructionBase, byte RegisterID, byte ImmediateByte)
        {
            return (ushort)(InstructionBase | (RegisterID << 8) | ImmediateByte);
        }
        private ushort GetOpcodeWithXYRegister(ushort InstructionBase, byte RegisterID1, byte RegisterID2, byte TrailNibble)
        {
            ushort result = (ushort)(InstructionBase | (RegisterID1 << 8) | (RegisterID2 << 4) | TrailNibble);
            return result;
        }
        private byte GetRandomRegister(int ExcludeRegister = -1)
        {
            int result = _random.Next(0, 0xF);
            if (ExcludeRegister == -1)
                return (byte)result;

            while (result == ExcludeRegister)
            {
                result = _random.Next(0, 0xF);
            }
            return (byte)result;
        }


        //Instructions
        [TestMethod]
        public void ClearScreen_Test()
        {
            byte[,] pBuffer = _emu.Graphics.FrameBuffer;
            for (int y = 0; y < pBuffer.GetUpperBound(1) - 1; y++)
            {
                for (int x = 0; x < pBuffer.GetUpperBound(0) - 1; x++)
                {
                    pBuffer[x, y] = (byte)_random.Next(0, 2);
                }
            }
            SetNextInstruction(0x00E0);
            _emu.Tick();
            for (int y = 0; y < pBuffer.GetUpperBound(1) - 1; y++)
            {
                for (int x = 0; x < pBuffer.GetUpperBound(0) - 1; x++)
                {
                    Assert.AreEqual(pBuffer[x, y], 0);
                }
            }
        }

        [TestMethod]
        public void ReturnFromSubroutine_Test()
        {
            SetNextInstruction(0x00EE);
            ushort returnAddress = GetRandomAddress();
            _emu.Memory.PushToStack(returnAddress);
            _emu.Tick();
            Assert.AreEqual(returnAddress, _emu.CPU.PC);
        }

        [TestMethod]
        public void GoToAddress_Test()
        {
            ushort randomAddress = GetRandomAddress();
            SetNextInstruction(0x1000 | randomAddress);
            _emu.Tick();
            Assert.AreEqual(randomAddress, _emu.CPU.PC);
        }

        [TestMethod]
        public void CallSubroutine_Test()
        {
            ushort functionAddress = GetRandomAddress();
            SetNextInstruction(0x2000 | functionAddress);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(_emu.CPU.PC, functionAddress);
            Assert.AreEqual(oldPC + 2, _emu.Memory.ReadShort((ushort)(_emu.Memory.StackPointer - 2)));
        }

        [TestMethod]
        public void SkipNextIfEqual_Test()
        {
            //Check if the instruction does skip on equal value
            byte randomRegisterID = GetRandomRegister();
            byte randomValue = (byte)_random.Next(0, 0x100);
            _emu.CPU.Registers[randomRegisterID] = randomValue;
            ushort opcode = GetOpcodeWithXRegister(0x3000, randomRegisterID, randomValue);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);

            //Check if the instruction does not skip on not equal value
            randomRegisterID = GetRandomRegister();
            randomValue = (byte)_random.Next(0, 0x100);
            _emu.CPU.Registers[randomRegisterID] = (byte)(randomValue + 2);
            opcode = GetOpcodeWithXRegister(0x3000, randomRegisterID, randomValue);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);
        }

        [TestMethod]
        public void SkipNextIfNotEqual_Test()
        {
            //Check if the instruction does skip on equal value
            byte randomRegisterID = GetRandomRegister();
            byte randomValue = (byte)_random.Next(0, 0x100);
            _emu.CPU.Registers[randomRegisterID] = randomValue;
            ushort opcode = GetOpcodeWithXRegister(0x4000, randomRegisterID, randomValue);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);

            //Check if the instruction does not skip on not equal value
            randomRegisterID = GetRandomRegister();
            randomValue = (byte)_random.Next(0, 0x100);
            _emu.CPU.Registers[randomRegisterID] = (byte)(randomValue + 2);
            opcode = GetOpcodeWithXRegister(0x4000, randomRegisterID, randomValue);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);
        }

        [TestMethod]
        public void SkipNextIfRegisterEqual_Test()
        {
            byte randomRegisterID1 = GetRandomRegister();
            byte randomRegisterID2 = GetRandomRegister(randomRegisterID1);

            byte randomValue1 = (byte)_random.Next(0, 0x100);
            byte randomValue2 = randomValue1;
            while (randomValue1 == randomValue2)
                randomValue2 = (byte)_random.Next(0, 0x100);

            _emu.CPU.Registers[randomRegisterID1] = randomValue1;
            _emu.CPU.Registers[randomRegisterID2] = randomValue2;
            ushort opcode = GetOpcodeWithXYRegister(0x5000, randomRegisterID1, randomRegisterID2, 0);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);

            //Check if the 2 registers are equal that the next instruction is skipped
            randomRegisterID1 = GetRandomRegister();
            randomRegisterID2 = GetRandomRegister(randomRegisterID1);

            randomValue1 = (byte)_random.Next(0, 0x100);

            _emu.CPU.Registers[randomRegisterID1] = randomValue1;
            _emu.CPU.Registers[randomRegisterID2] = randomValue1;
            opcode = GetOpcodeWithXYRegister(0x5000, randomRegisterID1, randomRegisterID2, 0);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);
        }

        [TestMethod]
        public void SetRegisterToValue_Test()
        {
            byte regID = GetRandomRegister();
            byte randomValue = (byte)_random.Next(0, 0x100);
            ushort opcode = GetOpcodeWithXRegister(0x6000, regID, randomValue);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(randomValue, _emu.CPU.Registers[regID]);
        }

        [TestMethod]
        public void AddValueToRegister_Test()
        {
            byte regID = GetRandomRegister();
            byte randomValue = (byte)_random.Next(0, 0x100);
            byte oldRegisterValue = _emu.CPU.Registers[regID];
            byte expectedResult = (byte)(randomValue + oldRegisterValue);
            ushort opcode = GetOpcodeWithXRegister(0x7000, regID, randomValue);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID]);
        }

        [TestMethod]
        public void SetRegisterToRegister_Test()
        {
            byte xRegID = GetRandomRegister();
            byte yRegID = GetRandomRegister(xRegID);
            ushort opcode = GetOpcodeWithXYRegister(0x8000, xRegID, yRegID, 0);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(_emu.CPU.Registers[yRegID], _emu.CPU.Registers[xRegID]);
        }

        [TestMethod]
        public void OrRegisters_Test()
        {
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte value1 = _emu.CPU.Registers[regID1];
            byte value2 = _emu.CPU.Registers[regID2];
            byte result = (byte)(value1 | value2);

            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 1);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(result, _emu.CPU.Registers[regID1]);
        }

        [TestMethod]
        public void AndRegisters_Test()
        {
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte value1 = _emu.CPU.Registers[regID1];
            byte value2 = _emu.CPU.Registers[regID2];
            byte result = (byte)(value1 & value2);

            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 2);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(result, _emu.CPU.Registers[regID1]);
        }

        [TestMethod]
        public void XorRegisters_Test()
        {
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte value1 = _emu.CPU.Registers[regID1];
            byte value2 = _emu.CPU.Registers[regID2];
            byte result = (byte)(value1 ^ value2);

            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 3);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(result, _emu.CPU.Registers[regID1]);
        }

        [TestMethod]
        public void AddRegisterToRegisterWithCarry_Test()
        {
            //Check result < 255
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte val1 = (byte)_random.Next(0, 50);
            byte val2 = (byte)_random.Next(0, 50);
            byte expectedResult = (byte)(val1 + val2);
            byte expectedCarryFlag = (byte)(val1 + val2 > 255 ? 1 : 0);

            _emu.CPU.Registers[regID1] = val1;
            _emu.CPU.Registers[regID2] = val2;
            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 4);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedCarryFlag, _emu.CPU.Registers[0xF]);

            //Check result > 255
            regID1 = GetRandomRegister();
            regID2 = GetRandomRegister(regID1);
            val1 = (byte)_random.Next(200, 0x100);
            val2 = (byte)_random.Next(200, 0x100);
            expectedResult = (byte)(val1 + val2);
            expectedCarryFlag = (byte)(val1 + val2 > 255 ? 1 : 0);

            _emu.CPU.Registers[regID1] = val1;
            _emu.CPU.Registers[regID2] = val2;
            opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 4);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedCarryFlag, _emu.CPU.Registers[0xF]);
        }

        [TestMethod]
        public void SubtractFromRegisterWithCarry_Test()
        {
            //Check without carry flag
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte value1 = (byte)_random.Next(100, 200);
            byte value2 = (byte)_random.Next(0, 100);
            _emu.CPU.Registers[regID1] = value1;
            _emu.CPU.Registers[regID2] = value2;
            byte expectedResult = (byte)(value1 - value2);
            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 5);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(1, _emu.CPU.Registers[0xF]);

            //Check with carry flag
            regID1 = GetRandomRegister();
            regID2 = GetRandomRegister(regID1);
            value1 = (byte)_random.Next(0, 100);
            value2 = (byte)_random.Next(100, 200);
            _emu.CPU.Registers[regID1] = value1;
            _emu.CPU.Registers[regID2] = value2;
            expectedResult = (byte)(value1 - value2);
            opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 5);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(0, _emu.CPU.Registers[0xF]);
        }

        [TestMethod]
        public void RightShift_Test()
        {
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte randomValue = (byte)_random.Next(0, 0x100);
            byte expectedFRegister = (byte)(randomValue & 1);
            byte expectedResult = (byte)(randomValue >> 1);
            _emu.CPU.Registers[regID2] = randomValue;
            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 6);
            SetNextInstruction(opcode);
            _emu.Tick();

            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedFRegister, _emu.CPU.Registers[0xF]);
        }

        [TestMethod]
        public void SetRegisterToSubtractionResult_Test()
        {
            //Test with borrow (Y register < X register)
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte val1 = (byte)_random.Next(100, 200);
            byte val2 = (byte)_random.Next(0, 100);
            byte expectedResult = (byte)(val2 - val1);
            byte expectedCarryFlag = (byte)(val2 < val1 ? 0 : 1);
            _emu.CPU.Registers[regID1] = val1;
            _emu.CPU.Registers[regID2] = val2;
            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 7);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedCarryFlag, _emu.CPU.Registers[0xF]);

            //Test with no borrow (Y register > X register)
            regID1 = GetRandomRegister();
            regID2 = GetRandomRegister(regID1);
            val1 = (byte)_random.Next(0, 100);
            val2 = (byte)_random.Next(100, 200);
            expectedResult = (byte)(val2 - val1);
            expectedCarryFlag = (byte)(val2 < val1 ? 0 : 1);
            _emu.CPU.Registers[regID1] = val1;
            _emu.CPU.Registers[regID2] = val2;
            opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 7);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedCarryFlag, _emu.CPU.Registers[0xF]);
        }

        [TestMethod]
        public void LeftShift_Test()
        {
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte randomValue = (byte)_random.Next(0, 0x100);
            byte expectedFRegister = (byte)(randomValue >> 7);
            byte expectedResult = (byte)(randomValue << 1);
            _emu.CPU.Registers[regID2] = randomValue;
            ushort opcode = GetOpcodeWithXYRegister(0x8000, regID1, regID2, 0xE);
            SetNextInstruction(opcode);
            _emu.Tick();

            Assert.AreEqual(expectedResult, _emu.CPU.Registers[regID1]);
            Assert.AreEqual(expectedFRegister, _emu.CPU.Registers[0xF]);
        }

        [TestMethod]
        public void SkipNextIfRegistersNotEqual_Test()
        {
            //Test with values not equal
            byte regID1 = GetRandomRegister();
            byte regID2 = GetRandomRegister(regID1);
            byte value1 = (byte)_random.Next(0, 0x100);
            byte value2 = (byte)_random.Next(0, 0x100);
            if (value1 == value2)
                value2++;

            _emu.CPU.Registers[regID1] = value1;
            _emu.CPU.Registers[regID2] = value2;
            ushort opcode = GetOpcodeWithXYRegister(0x9000, regID1, regID2, 0);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);

            //Test with values equal
            regID1 = GetRandomRegister();
            regID2 = GetRandomRegister(regID1);
            value1 = (byte)_random.Next(0, 0x100);

            _emu.CPU.Registers[regID1] = value1;
            _emu.CPU.Registers[regID2] = value1;
            opcode = GetOpcodeWithXYRegister(0x9000, regID1, regID2, 0);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);
        }

        [TestMethod]
        public void SetRegisterI_Test()
        {
            ushort address = (ushort)_random.Next(0, 0x1000);
            ushort opcode = (ushort)(0xA000 | address);
            SetNextInstruction(opcode);
            _emu.CPU.Tick();
            Assert.AreEqual(address, _emu.CPU.RegisterI);
        }

        [TestMethod]
        public void JumpToAddressPlusV0_Test()
        {
            byte randomValue = (byte)_random.Next(0, 0x100);
            _emu.CPU.Registers[0] = randomValue;
            ushort randomAddress = (ushort)_random.Next(0, 0x1000);
            ushort opcode = (ushort)(0xB000 | randomAddress);
            ushort expectedResult = (ushort)(randomAddress + randomValue);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.PC);
        }

        [TestMethod]
        public void SetRegisterToRandomAnd_Test()
        {
            //Not really any way to test this because it relies on a random value
            byte randomValue = (byte)_random.Next(0, 0x100);
            byte regID = GetRandomRegister();
            ushort opcode = GetOpcodeWithXRegister(0xC000, regID, randomValue);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);
        }

        [TestMethod]
        public void DrawSprite_Test()
        {
            //Reset frame buffer
            _emu.Graphics.ClearDisplay();

            byte height = (byte)_random.Next(1, 16); //Get random height
            byte regID1 = GetRandomRegister(); //Get random register to store start X
            byte regID2 = GetRandomRegister(regID1); //Get random register to store start Y
            byte xVal = (byte)_random.Next(0, 56); //Generate random start X
            byte yVal = (byte)_random.Next(0, 32 - height); //Generate random start Y
            //Store start X and Y
            _emu.CPU.Registers[regID1] = xVal;
            _emu.CPU.Registers[regID2] = yVal;

            ushort opcode = GetOpcodeWithXYRegister(0xD000, regID1, regID2, height); //Generate opcode
            byte[] spriteBytes = new byte[height]; //Create new array for storing sprite bytes
            _random.NextBytes(spriteBytes); //Generate a series of random bytes, each byte represents 8 pixels (1 bit per pixel)
            ushort spriteAddress = (ushort)_random.Next(0x200, 0x1000); //Start address of the sprite
            Array.Copy(spriteBytes, 0, _emu.Memory.RAM, spriteAddress, height); //Copy sprite bytes to memory

            //Setup registers
            SetNextInstruction(opcode);
            _emu.CPU.RegisterI = spriteAddress;
            _emu.Tick();

            //Validate data
            byte[,] frameBuffer = _emu.Graphics.FrameBuffer;
            for (int y = 0; y < height; y++)
            {
                //The current 8-pixel byte
                byte currentByte = spriteBytes[y];
                for (int x = 0; x < 8; x++)
                {
                    //Reads the current bit
                    byte expectedValue = (byte)((currentByte & (1 << x)) >> x);
                    //Verify the expexted bit is stored in the frame buffer
                    Assert.AreEqual(expectedValue, frameBuffer[xVal + x, yVal + y]);
                }
            }

        }

        [TestMethod]
        public void SkipNextIfKeyPressed_Test()
        {
            //Key is pressed
            byte regID = GetRandomRegister();
            byte expectedKey = (byte)_random.Next(0, 0x10);
            _emu.CPU.Registers[regID] = expectedKey;
            _emu.Input.SetKeyState(expectedKey, true);
            ushort opcode = GetOpcodeWithXRegister(0xE000, regID, 0x9E);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);

            //Key is not pressed
            regID = GetRandomRegister();
            expectedKey = (byte)_random.Next(0, 0x10);
            _emu.CPU.Registers[regID] = expectedKey;
            _emu.Input.SetKeyState(expectedKey, false);
            opcode = GetOpcodeWithXRegister(0xE000, regID, 0x9E);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);
        }

        [TestMethod]
        public void SkipNextIfKeyNotPressed_Test()
        {
            //Key is pressed
            byte regID = GetRandomRegister();
            byte expectedKey = (byte)_random.Next(0, 0x10);
            _emu.CPU.Registers[regID] = expectedKey;
            _emu.Input.SetKeyState(expectedKey, true);
            ushort opcode = GetOpcodeWithXRegister(0xE000, regID, 0xA1);
            SetNextInstruction(opcode);
            ushort oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 2, _emu.CPU.PC);

            //Key is not pressed
            regID = GetRandomRegister();
            expectedKey = (byte)_random.Next(0, 0x10);
            _emu.CPU.Registers[regID] = expectedKey;
            _emu.Input.SetKeyState(expectedKey, false);
            opcode = GetOpcodeWithXRegister(0xE000, regID, 0xA1);
            SetNextInstruction(opcode);
            oldPC = _emu.CPU.PC;
            _emu.Tick();
            Assert.AreEqual(oldPC + 4, _emu.CPU.PC);
        }

        [TestMethod]
        public void SetRegisterToDelayTimer_Test()
        {
            byte regID = GetRandomRegister();
            _emu.CPU.DelayTimer = 100;
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x07);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(_emu.CPU.DelayTimer, _emu.CPU.Registers[regID]);
        }

        [TestMethod]
        public void SetDelayTimerToRegister_Test()
        {
            byte regID = GetRandomRegister();
            _emu.CPU.Registers[regID] = (byte)_random.Next(0, 0x100);
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x15);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(_emu.CPU.Registers[regID], _emu.CPU.DelayTimer);
        }

        [TestMethod]
        public void SetSoundTimerToRegister_Test()
        {
            byte regID = GetRandomRegister();
            _emu.CPU.Registers[regID] = (byte)_random.Next(0, 0x100);
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x18);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(_emu.CPU.Registers[regID], _emu.CPU.SoundTimer);
        }

        [TestMethod]
        public void AddRegisterToI_Test()
        {
            byte regID = GetRandomRegister();
            byte randomValue = (byte)_random.Next(0, 0x100);
            ushort randomAddress = (ushort)_random.Next(0, 0x1000);
            _emu.CPU.Registers[regID] = randomValue;
            _emu.CPU.RegisterI = randomAddress;
            ushort expectedResult = (ushort)(randomAddress + randomValue);
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x1E);
            SetNextInstruction(opcode);
            _emu.Tick();
            Assert.AreEqual(expectedResult, _emu.CPU.RegisterI);
        }

        [TestMethod]
        public void SetIToSpriteLocation_Test()
        {
            byte randomCharacter = GetRandomRegister();
            byte regID = GetRandomRegister();
            _emu.CPU.Registers[regID] = randomCharacter;
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x29);
            SetNextInstruction(opcode);
            _emu.Tick();
            ushort expectedAddress = (ushort)(_emu.Memory.FontDataBaseAddress + (randomCharacter * 5));
            Assert.AreEqual(expectedAddress, _emu.CPU.RegisterI);
        }

        [TestMethod]
        public void StoreBinbaryCodedDecimal_Test()
        {
            byte regID = GetRandomRegister();
            byte value = (byte)_random.Next(0, 0x100);
            byte hundreds = (byte)(value / 100);
            byte tens = (byte)((value % 100) / 10);
            byte ones = (byte)(value % 10);
            _emu.CPU.Registers[regID] = value;
            ushort opcode = GetOpcodeWithXRegister(0xF000, regID, 0x33);
            SetNextInstruction(opcode);
            _emu.CPU.RegisterI = 0x500;
            _emu.Tick();
            Assert.AreEqual(hundreds, _emu.Memory.ReadByte(_emu.CPU.RegisterI));
            Assert.AreEqual(tens, _emu.Memory.ReadByte(_emu.CPU.RegisterI + 1));
            Assert.AreEqual(ones, _emu.Memory.ReadByte(_emu.CPU.RegisterI + 2));
        }

        [TestMethod]
        public void RegisterDump_Test()
        {
            byte maxRegister = GetRandomRegister();
            for (int i = 0; i <= maxRegister; i++)
                _emu.CPU.Registers[i] = (byte)_random.Next(0, 0x100);

            ushort opcode = GetOpcodeWithXRegister(0xF000, maxRegister, 0x55);
            SetNextInstruction(opcode);
            _emu.CPU.RegisterI = 0x500;
            _emu.Tick();

            for (int i = 0; i <= maxRegister; i++)
                Assert.AreEqual(_emu.CPU.Registers[i], _emu.Memory.ReadByte(_emu.CPU.RegisterI + i));
        }

        [TestMethod]
        public void RegisterLoad_Test()
        {
            byte maxRegister = GetRandomRegister();
            _emu.CPU.RegisterI = 0x500;
            byte[] randomBytes = new byte[maxRegister + 1];
            _random.NextBytes(randomBytes);
            Array.Copy(randomBytes, 0, _emu.Memory.RAM, _emu.CPU.RegisterI, randomBytes.Length);
            ushort opcode = GetOpcodeWithXRegister(0xF000, maxRegister, 0x65);
            SetNextInstruction(opcode);
            _emu.Tick();

            for (int i = 0; i < maxRegister; i++)
                Assert.AreEqual(randomBytes[i], _emu.CPU.Registers[i]);
        }
    }
}
