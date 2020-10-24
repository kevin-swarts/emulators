using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.MOS6502
{
    public class InstructionSet
    {
        public const byte INS_LDA_I = 0xA9;
        public const byte INS_LDA_ZP = 0xA5;
        public const byte INS_LDA_ZPX = 0xB5;
        public const byte INS_LDA_A = 0xAD;
        public const byte INS_LDA_AX = 0xBD;
        public const byte INS_LDA_AY = 0xB9;
        public const byte INS_LDA_IX = 0xA1;
        public const byte INS_LDA_IY = 0xB1;
    }
}
