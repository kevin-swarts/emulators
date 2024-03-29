﻿using Emulators.Platform;
using System;
using System.Collections.Generic;
using System.Text;

using Word = System.UInt16;
using Bit = System.Boolean;

namespace Emulators.MOS6502
{
    public class Processor6502 : Processor
    {
        private Byte _cycles = 0;

        private Byte _a;
        private Byte _x;
        private Byte _y;
        private Word _sp;

        public Byte Cycles { get => _cycles; set => _cycles = value; }

        /// <summary>
        /// Program Counter
        /// </summary>
        public Word PC { get => _pc; set => _pc = value; }

        /// <summary>
        /// Stack Pointer
        /// </summary>
        public Word SP { get => _sp; set => _sp = value; }

        /// <summary>
        /// Accumulator
        /// </summary>
        public Byte A { get => _a; set => _a = value; }

        /// <summary>
        /// X Register
        /// </summary>
        public Byte X { get => _x; set => _x = value; }

        /// <summary>
        /// Y Register
        /// </summary>
        public Byte Y { get => _y; set => _y = value; }

        /// <summary>
        /// Processor Status Flags (7 used bit Byte)
        /// </summary>
        public ProcessorStatus Status { get; set; }


        public Processor6502(IMainboard mainboard, long memoryLimit = 0, float frequency = 1.023F) : base(mainboard, memoryLimit, frequency)
        {
            _pc = 0x100; // 256
            InstructionSet = new List<InstructionDetail>
            {
                new InstructionDetail
                {
                    Mnemonic = "ADC",
                    OpCode = 0x69,

                }
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            Status = null;
        }

        public override void Reset()
        {
            base.Reset();
            _cycles = 0;

            _pc = 0x100; // 256
            SP = 0x00;
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();
        }

        protected override void Execute()
        {
            _cycles = 0;
#if DEBUG
            // This should only be used while testing as the PC should never wrap
            if (Word.MaxValue == _pc + 1)
            {
                _pc = 0;
            }
#endif
            var instruction = FetchNextByte();

            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    LoadRegisterImmediate(ref _a);
                    break;
                case InstructionSet.INS_LDA_ZP:
                    LoadRegisterZeroPage(ref _a);
                    break;
                case InstructionSet.INS_LDA_ZPX:
                    loadRegisterZeroPageOffset(X, ref _a);
                    break;
                case InstructionSet.INS_LDA_A:
                    LoadRegisterAbsolute(ref _a);
                    break;
                case InstructionSet.INS_LDA_AX:
                    LoadRegisterAbsoluteOffset(X, ref _a);
                    break;
                case InstructionSet.INS_LDA_AY:
                    LoadRegisterAbsoluteOffset(Y, ref _y);
                    break;
                case InstructionSet.INS_LDA_IX:
                case InstructionSet.INS_LDA_IY:

                    break;
            }
        }

        //private Byte FetchWord(Word address)
        //{
        //    _pc++;
        //    return Memory[address];
        //}

        /// <summary>
        /// Retrieve the X register and increment PC
        /// </summary>
        /// <returns>The current X register</returns>
        private Byte FetchX()
        {
            _cycles++;
            return X;
        }

        /// <summary>
        /// Retrieve the Y register and increment PC
        /// </summary>
        /// <returns>The current Y register</returns>
        private Byte FetchY()
        {
            _cycles++;
            return Y;
        }

        /// <summary>
        /// Returns the same value passed in, but increments the PC by 1
        /// </summary>
        /// <param name="value">The value to return</param>
        /// <returns>The value passed in</returns>
        private Byte FetchRegister(Byte value)
        {
            _cycles++;
            return value;
        }


        /// <summary>
        /// Returns the next Byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next Byte in memory</returns>
        private Byte FetchNextByte()
        {
            _cycles++;
            return Memory[_pc++];
        }

        /// <summary>
        /// Returns the 16 bit value based on the current PC swaps the Bytes, and increments the PC by two
        /// </summary>
        /// <returns>The next word in memory</returns>
        private Word FetchNextWord()
        {
            _cycles++;
#if BIGENDIAN
                var ByteA = Memory[_pc++];
                var ByteB = Memory[_pc++];
                return swapBytes(ByteA, ByteB);
#else
            var value = Memory[_pc++];// BitConverter.ToUInt16(Memory, _pc++);
            _pc++;
            return value;
#endif
        }


        /// <summary>
        /// Swap the Bytes around from little-endian to big-endian, or visa-versa
        /// </summary>
        /// <param name="byteA">The left most Byte</param>
        /// <param name="byteB">The right most Byte</param>
        /// <returns></returns>
        private Word SwapBytes(Byte byteA, Byte byteB)
        {
            return ((Word)(byteB << 8 | byteA));
        }

        private void SetRegister(ref Byte register, byte value)
        {
            Status.Negative = (sbyte)value < 0;
            Status.Zero = value == 0;
            register = value;
        }

        //private void LoadRegisterStatus(ref Byte register)
        //{
        //    Status.Zero = register == (Byte)0x00;
        //    Status.Negative = Memory.GetBit(register, 7) == 1;
        //}

        /// <summary>
        /// Load the next Byte into the register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void LoadRegisterImmediate(ref Byte register)
        {
            SetRegister(ref register, FetchNextByte());
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 Bytes ino the correct register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void LoadRegisterZeroPage(ref Byte register)
        {
            SetRegister(ref register, Memory[FetchNextByte()]);
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 Bytes plus an offset ino the correct register
        /// </summary>
        /// <param name="offset">The amount to offset from the zero page address</param>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterZeroPageOffset(Byte offset, ref Byte register)
        {
            SetRegister(ref register, Memory[Convert.ToByte(FetchNextByte() + FetchRegister(offset))]);
        }

        private void LoadRegisterAbsolute(ref Byte register)
        {
            SetRegister(ref register, Memory[FetchNextWord()]);
        }

        private void LoadRegisterAbsoluteOffset(Byte offset, ref Byte register)
        {
            var address = FetchNextWord();
            SetRegister(ref register, Memory[Convert.ToUInt16(address + offset)]);
        }
    }
}
