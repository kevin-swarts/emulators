using Emulators.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Word = System.UInt16;
using Bit = System.Boolean;

namespace Emulators.MOS6502
{
    public class Memory : List<byte>, IMemory
    {
        public Memory(IMainboard mainboard, byte[] memoryMap = null)
        {
            this.Mainboard = mainboard;

            this.Capacity = 0xffff;

            if (memoryMap != null)
            {
                this.Clear();
                this.AddRange(memoryMap);

                var index = memoryMap.Length;

                if (index < this.Capacity)
                {
                    while (index < this.Capacity)
                    {
                        this[index] = 0x00;
                        index++;
                    }
                }
            }
            else
            {
                for (var i = 0; i < this.Count; i++)
                {
                    this[i] = 0x00;
                }
            }
        }

        public IMainboard Mainboard { get; set; }

        public byte this[byte address]
        {
            get
            {
                //Mainboard.Processor.Cycles++;
                return base[address];
            }
            set
            {
                base[address] = value;
                //Mainboard.Processor.Cycles++;
            }
        }

        public byte this[ushort address]
        {
            get
            {
                //Mainboard.Processor.Cycles++;
                //if (address > 0x100) Mainboard.Processor.Cycles++;
                return base[address];
            }
            set
            {
                //Mainboard.Processor.Cycles++;
                //if (address > 0x100) Mainboard.Processor.Cycles++;
                base[address] = value;
            }
        }

        public int GetBit(Byte b, int bitNumber)
        {
            return ((b >> bitNumber) & 0x01);
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
    }
}