using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.MOS6502
{
    public class Memory
    {
        private readonly byte[] _memory;
        public Memory()
        {
            _memory = new byte[0x1000];

            for (var i = 0; i < _memory.Length; i++)
            {
                _memory[i] = 0x00;
            }
        }


        public byte this[ushort index]
        {
            get { return _memory[index]; }
            set { _memory[index] = value; }
        }
    }
}