using Microsoft.VisualStudio.TestTools.UnitTesting;
using Emulators.MOS6502;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using Emulators.Platform;

namespace Emulators.MOS6502.Tests
{
    [TestClass()]
    public class LDATests
    {
        private IMainboard _mainboard = null;
        private Processor _cpu = null;
        private ushort _originalPC;

        [TestInitialize]
        public void Initialize()
        {
            _mainboard = new Commodore64Board();
            _cpu = (Processor)_mainboard.Processor;
            _originalPC = _cpu.PC; ;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cpu.Dispose();
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x10, (byte)0x10, (byte)0, (byte)0, (byte)2, (ushort)2, (ushort)0, false, false, false, false, false, false, false, DisplayName = "LDA_I Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0x0, (byte)0, (byte)0, (byte)0, (byte)2, (ushort)2, (ushort)0, false, true, false, false, false, false, false, DisplayName = "LDA_I With Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_I, (byte)0b10000001, (byte)129, (byte)0, (byte)0, (byte)2, (ushort)2, (ushort)0, false, false, false, false, false, false, true, DisplayName = "LDA_I With Negative Bit")]
        public async Task LoadA_Immediate(byte instruction, byte address, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
            bool z, bool i, bool d, bool b,
            bool o, bool n)
        {
            var memory = new byte[] { instruction, address };
            _mainboard.Reset(memory);
            await _cpu.DebugAsync();
            await _cpu.StartAsync();

            while (!_cpu.IsPaused) { };

            Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_ZP, (byte)0x10, (byte)0x01, (byte)0, (byte)0, (byte)3, (ushort)2, (ushort)0,  false, false, false, false, false, false, false, DisplayName = "LDA_ZP Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_ZP, (byte)0x10, (byte)0, (byte)0, (byte)0, (byte)3, (ushort)2, (ushort)0, false, true, false, false, false, false, false, DisplayName = "LDA_ZP With Zero Bit")]
        public async Task LoadA_Zero_Page(byte instruction, byte address, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
            bool z, bool i, bool d, bool b,
            bool o, bool n)
        {
            var memory = new byte[0xffff];
            var originalPC = _cpu.PC;

            memory[0x100] = instruction;
            memory[0x101] = address;
            memory[address] = a;

            _mainboard.Reset(memory);
            await _cpu.DebugAsync();
            await _cpu.StartAsync();

            while (!_cpu.IsPaused) { };

            Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_ZPX, (byte)0x10, (byte)0x01, (byte)0x01, (byte)0, (byte)4, (ushort)2, (ushort)0, false, false, false, false, false, false, false, DisplayName = "LDA_ZP Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_ZPX, (byte)0x10, (byte)0x00, (byte)0x10, (byte)0, (byte)4, (ushort)2, (ushort)0, false, true, false, false, false, false, false, DisplayName = "LDA_ZP With Zero Bit")]
        public async Task LoadA_Zero_Page_X(byte instruction, byte address, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
            bool z, bool i, bool d, bool b,
            bool o, bool n)
        {
            var memory = new byte[0xffff];
            // memory[]
            memory[0x11] = 0x01;
            memory[0x100] = instruction;
            memory[0x101] = address;

            _mainboard.Reset(memory);
            await _cpu.DebugAsync();

            _cpu.X = x;

            await _cpu.StartAsync();

            while (!_cpu.IsPaused) { };

            Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_A, (byte)0x10, (byte)0x01, (byte)0x01, (byte)0, (byte)0, (byte)4, (ushort)3, (ushort)0, false, false, false, false, false, false, false, DisplayName = "LDA_ZP Without Zero Bit")]
        [DataRow(InstructionSet.INS_LDA_A, (byte)0x10, (byte)0x02, (byte)0x00, (byte)0, (byte)0, (byte)4, (ushort)3, (ushort)0, false, true, false, false, false, false, false, DisplayName = "LDA_ZP With Zero Bit")]
        public async Task LoadA_Absolute(byte instruction, byte addressLow, byte addressHigh, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
            bool z, bool i, bool d, bool b,
            bool o, bool n)
        {
            var memory = new byte[0xffff];
            // memory[]
            memory[0x0110] = 0x01;
            memory[0x100] = instruction;
            memory[0x101] = addressLow;
            memory[0x102] = addressHigh;

            _mainboard.Reset(memory);
            _cpu.X = x;
            await _cpu.DebugAsync();
            await _cpu.StartAsync();

            while (!_cpu.IsPaused) { };

            Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        }

        [DataTestMethod]
        [DataRow(InstructionSet.INS_LDA_AX, (ushort)0x0110, (byte)0, (byte)4, (byte)0, (byte)4, (ushort)0, (ushort)0, false, false, false, false, false, false, false, DisplayName = "INS_LDA_AX Without Zero Bit")]
        //[DataRow(InstructionSet.INS_LDA_AX, (ushort)0x0002, (byte)0, (byte)4, (byte)0, (ushort)4, (ushort)3, (ushort)0, false, true, false, false, false, false, false, DisplayName = "INS_LDA_AX With Zero Bit")]
        public async Task LoadA_AbsoluteOffsetX(byte instruction, ushort address, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
            bool z, bool i, bool d, bool b,
            bool o, bool n)
        {
            var memory = new byte[0xffff];
            // memory[]
            memory[0x0110] = 0x01;
            memory[0x100] = instruction;
            memory[0x101] = BitConverter.GetBytes(address)[0];
            memory[0x102] = BitConverter.GetBytes(address)[1];

            _mainboard.Reset(memory);
            _cpu.X = x;
            await _cpu.DebugAsync();

            await _cpu.StartAsync();

            while (!_cpu.IsPaused) { };

            Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        }

        //[DataTestMethod]
        //[DataRow(InstructionSet.INS_LDA_AY, (byte)0x10, (byte)0x01, (byte)0x01, (byte)0, (byte)0, (byte)4, (ushort)3, (ushort)0, false, false, false, false, false, false, false, DisplayName = "INS_LDA_AY Without Zero Bit")]
        //[DataRow(InstructionSet.INS_LDA_AY, (byte)0x10, (byte)0x02, (byte)0x00, (byte)0, (byte)0, (byte)4, (ushort)3, (ushort)0, false, true, false, false, false, false, false, DisplayName = "INS_LDA_AY With Zero Bit")]
        //public void LoadA_AbsoluteOffsetY(byte instruction, byte addressLow, byte addressHigh, byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c,
        //    bool z, bool i, bool d, bool b,
        //    bool o, bool n)
        //{
        //    var memory = new byte[0xffff];
        //    // memory[]
        //    memory[0x0110] = 0x01;
        //    memory[0x100] = instruction;
        //    memory[0x101] = addressLow;
        //    memory[0x102] = addressHigh;

        //    _cpu.Reset(memory);
        //    _cpu.Y = y;
        //    _cpu.Debug();

        //    _cpu.Start();

        //    while (!_cpu.IsPaused) { };

        //    Load_A_Asserts(a, x, y, cycles, pc, sp, c, z, i, d, b, o, n);
        //}

        private void Load_A_Asserts(byte a, byte x, byte y, byte cycles, ushort pc, ushort sp, bool c, bool z, bool i, bool d, bool b, bool o, bool n)
        {
            Assert.AreEqual(a, _cpu.A, "The A register does not match the expected value");
            Assert.AreEqual(x, _cpu.X, "The X register does not match the expected value");
            Assert.AreEqual(y, _cpu.Y, "The Y register does not match the expected value");

            Assert.AreEqual(cycles, _cpu.Cycles, "The number of Cycles does not match the expected value");

            Assert.AreEqual(pc + _originalPC, _cpu.PC, "The PC register does not match the expected value");
            Assert.AreEqual(sp, _cpu.SP, "The SP register does not match the expected value");

            Assert.AreEqual(c, _cpu.Status.Carry, "The Carry flag does not match the expected value");
            Assert.AreEqual(z, _cpu.Status.Zero, "The Zero flag does not match the expected value");
            Assert.AreEqual(i, _cpu.Status.Interrupt, "The Interrupt flag does not match the expected value");
            Assert.AreEqual(d, _cpu.Status.DecimalMode, "The Decimal flag does not match the expected value");
            Assert.AreEqual(b, _cpu.Status.Break, "The Break flag does not match the expected value");
            Assert.AreEqual(o, _cpu.Status.Overflow, "The Overflow flag does not match the expected value");
            Assert.AreEqual(n, _cpu.Status.Negative, "The Negative flag does not match the expected value");
        }
    }
}