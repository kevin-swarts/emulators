using Microsoft.VisualStudio.TestTools.UnitTesting;
using Emulators.MOS6502;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Emulators.MOS6502.Tests
{
    [TestClass()]
    public class CPUTests
    {
        private CPU _cpu = null;

        [TestInitialize]
        public void Initialize()
        {
            _cpu = new CPU();
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x10, (byte)0x10, false, false, false, false, false, false, false, DisplayName = "LDA_I Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x0, (byte)0x0, false, true, false, false, false, false, false, DisplayName = "LDA_I With Zero Bit")]
        public void LoadA(byte instruction, byte value, byte expectedValue, bool expectedCarry,
            bool expectedZero, bool expectedInterrupt, bool expectedDecimal, bool expectedBreak,
            bool expectedOverflow, bool expectedNegative)
        {
            var memory = new byte[0xffff];
            memory[0x100] = instruction;
            memory[0x101] = value;
            _cpu.Reset(memory);
            Assert.AreEqual(expectedValue, _cpu.A, "The Accumulator register does not match the expected value");
            Assert.AreEqual(expectedCarry, _cpu.Status.Carry, "The Carry flag does not match the expected value");
            Assert.AreEqual(expectedZero, _cpu.Status.Zero, "The Zero flag does not match the expected value");
            Assert.AreEqual(expectedInterrupt, _cpu.Status.Interrupt, "The Interrupt flag does not match the expected value");
            Assert.AreEqual(expectedDecimal, _cpu.Status.DecimalMode, "The Decimal flag does not match the expected value");
            Assert.AreEqual(expectedBreak, _cpu.Status.Break, "The Break flag does not match the expected value");
            Assert.AreEqual(expectedOverflow, _cpu.Status.Overflow, "The Overflow flag does not match the expected value");
            Assert.AreEqual(expectedNegative, _cpu.Status.Negative, "The Negative flag does not match the expected value");
        }
    }
}