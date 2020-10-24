using System;
using System.Security.Cryptography.X509Certificates;

namespace Emulators.MOS6502
{
    public class CPU
    {
        public ushort PC { get; set; }
        public ushort SP { get; set; }
        public byte A { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public ProcessorStatus Status { get; set; }
        public float Frequency { get; set; }
        public byte[] Memory { get; private set; }
        public long MemoryLimit { get; set; } = 0xFFFF;


        public CPU(long memoryLimit = 0xFFFF, float frequency = 1.023F)
        {
            MemoryLimit = memoryLimit;
        }

        public void Reset()
        {
            PC = 0x00;
            SP = 0x100; // 256
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();
            Memory = new byte[MemoryLimit];
        }

        public void Reset(byte[] memory)
        {
            PC = 0x100;
            SP = 0x00; // 256
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();

            // Pad the memory passed in to fill the memory to MaxLimit
            for(var i = memory.Length; i < MemoryLimit; i++)
            {
                memory[i] = 0x00;
            }

            Memory = memory;
            this.execute();
        }

        private void execute()
        {
            var instruction = Memory[PC];
            PC++;
            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    loadAccumulatorImmediate();
                    break;
                case InstructionSet.INS_LDA_ZP:
                case InstructionSet.INS_LDA_ZPX:
                case InstructionSet.INS_LDA_A:
                case InstructionSet.INS_LDA_AX:
                case InstructionSet.INS_LDA_AY:
                case InstructionSet.INS_LDA_IX:
                case InstructionSet.INS_LDA_IY:

                    break;
            }
        }

        private ushort fetchWord(ushort address)
        {
            //if (BitConverter.IsLittleEndian)
            //{
                var newByte = new byte[2];
                return BitConverter.ToUInt16(Memory, address);
            //}
            
        }

        /// <summary>
        /// Returns the byte at the address requested
        /// </summary>
        /// <param name="address">The location in memory</param>
        /// <returns>The byte in the requested address</returns>
        private byte fetchByte(ushort address)
        {
            return Memory[address];
        }


        /// <summary>
        /// Returns the next byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next byte in memory</returns>
        private byte fetchByte()
        {
            return fetchByte(PC++);
        }

        private void loadAccumulatorImmediate()
        {
            A = fetchByte();
            Status.Zero = this.A == (byte)0x00;
        }
    }
}
