using Emulators.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MOS6502
{
    public class Commodore64Board : IMainboard
    {
        public IProcessor Processor { get; private set; }
        public IMemory< Memory { get; private set; }

        public byte AddressSize { get; private set; }

        public byte DataBusSize { get; set; }

        public IEnumerable<IOutput> Outputs { get; set; }
        public IEnumerable<IInput> Inputs { get; set; }

        public Commodore64Board()
        {
            this.Processor = new Processor(this);
        }

        public void Reset(byte[] memoryMap)
        {
            this.Processor.Reset();

            if (memoryMap.Length < this.Processor.MemoryLimit - 255)
            {
                // Start by padding the zero page memory at the beginning
                var zeroPage = new Byte[256];
                memoryMap = zeroPage.Concat(memoryMap).ToArray();
            }

            if (memoryMap.Length < this.Processor.MemoryLimit)
            {
                memoryMap = memoryMap.Concat(new Byte[this.Processor.MemoryLimit - memoryMap.Length]).ToArray();
            }

            // Pad the memory passed in to fill the memory to MaxLimit
            for (var i = memoryMap.Length; i < this.Processor.MemoryLimit; i++)
            {
                memoryMap[i] = 0x00;
            }

            this.Memory = new Memory(this, memoryMap);
        }
    }
};