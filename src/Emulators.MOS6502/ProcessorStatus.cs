using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.MOS6502
{
    public class ProcessorStatus
    {
        public bool Carry { get; set; }
        public bool Zero { get; set; }
        public bool Interrupt { get; set; }
        public bool DecimalMode { get; set; }
        public bool Break { get; set; }
        public bool Overflow { get; set; }
        public bool Negative { get; set; }
    }
}
