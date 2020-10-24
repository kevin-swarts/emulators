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

        [TestCleanup]
        public void Cleanup()
        {
            _cpu.Dispose();
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x10, (byte)0x10, false, false, false, false, false, false, false, DisplayName = "LDA_I Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x0, (byte)0x0, false, true, false, false, false, false, false, DisplayName = "LDA_I With Zero Bit")]
        public void LoadA_Immediate(byte instruction, byte value, byte expectedAccumulator, bool expectedCarry,
            bool expectedZero, bool expectedInterrupt, bool expectedDecimal, bool expectedBreak,
            bool expectedOverflow, bool expectedNegative)
        {
            var expectedProgramCounterOffset = 2;
            var originalPC = _cpu.PC;
            var memory = new byte[0xffff];
            memory[0x100] = instruction;
            memory[0x101] = value;
            _cpu.Debug();
            _cpu.Reset(memory);
            while (!_cpu.IsPaused) { };

            Assert.AreEqual(expectedAccumulator, _cpu.A, "The Accumulator register does not match the expected value");
            Assert.AreEqual(originalPC + expectedProgramCounterOffset, _cpu.PC, "The Program Counter does not match the expected value");
            loadA_Status(expectedCarry, expectedZero, expectedInterrupt, expectedDecimal, expectedBreak, expectedOverflow, expectedNegative);
        }

        [DataRow(InstructionSet.INS_LDA_ZP, (byte)0x10, (byte)0x01, false, false, false, false, false, false, false, DisplayName = "LDA_ZP Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_ZP, (byte)0x01, (byte)0x00, false, true, false, false, false, false, false, DisplayName = "LDA_ZP With Zero Bit")]
        public void LoadA_Immediate_Zero_Page(byte instruction, byte address, byte expectedAccumulator, bool expectedCarry,
            bool expectedZero, bool expectedInterrupt, bool expectedDecimal, bool expectedBreak,
            bool expectedOverflow, bool expectedNegative)
        {
            var memory = new byte[0xffff];
            memory[0x10] = 0x01;
            memory[0x100] = instruction;
            memory[0x101] = address;
            _cpu.Debug();
            _cpu.Reset(memory);
            while (!_cpu.IsPaused) { };

            Assert.AreEqual(expectedAccumulator, _cpu.A, "The Accumulator register does not match the expected value");
            //Assert.AreEqual(originalPC + expectedProgramCounterOffset, _cpu.PC, "The Program Counter does not match the expected value");
            loadA_Status(expectedCarry, expectedZero, expectedInterrupt, expectedDecimal, expectedBreak, expectedOverflow, expectedNegative);
        }

        //[DataRow(InstructionSet.INS_LDA_ZPX, (byte)0x10, (byte)0x20, (byte)0x01, false, false, false, false, false, false, false, DisplayName = "LDA_ZP Without Zero Bit")]
        //[DataRow(InstructionSet.INS_LDA_ZPX, (byte)0x01, (byte)0x00, false, true, false, false, false, false, false, DisplayName = "LDA_ZP With Zero Bit")]
        //public void LoadA_Immediate_Zero_Page_X(byte instruction, byte address, byte x, byte expectedAccumulator, bool expectedCarry,
        //    bool expectedZero, bool expectedInterrupt, bool expectedDecimal, bool expectedBreak,
        //    bool expectedOverflow, bool expectedNegative)
        //{
        //    var expectedProgramCounterOffset = 4;
        //    var memory = new byte[0xffff];
        //    // memory[]
        //    memory[0x10] = 0x01;
        //    memory[0x100] = instruction;
        //    memory[0x101] = address;
        //    _cpu.X = x;
        //    var originalPC = _cpu.PC;
        //    _cpu.Reset(memory);
        //    Assert.AreEqual(expectedAccumulator, _cpu.A, "The Accumulator register does not match the expected value");
        //    Assert.AreEqual(originalPC + expectedProgramCounterOffset, _cpu.PC, "The Program Counter does not match the expected value");
        //    loadA_Status(expectedCarry, expectedZero, expectedInterrupt, expectedDecimal, expectedBreak, expectedOverflow, expectedNegative);
        //}

        private void loadA_Status(bool expectedCarry,
            bool expectedZero, bool expectedInterrupt, bool expectedDecimal, bool expectedBreak,
            bool expectedOverflow, bool expectedNegative)
        {
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