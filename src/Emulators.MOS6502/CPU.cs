using System;
using System.Collections;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Word = System.UInt16;
using Bit = System.Boolean;

namespace Emulators.MOS6502
{
    public class CPU : IDisposable
    {
        /// <summary>
        /// Represents the CPU's only thread.
        /// </summary>
        private Task _thread;

        /// <summary>
        /// Determines if the CPU is in debug mode
        /// </summary>
        private Bit _debug = false;

        private Byte _cycles = 0;

        /// <summary>
        /// The cancellation token used to control the thread task
        /// </summary>
        private CancellationTokenSource _threadCancellationTokenSource;
        private CancellationToken _threadCancellationToken;
        private Byte _a;
        private Byte _x;
        private Byte _y;
        private Word _pc;
        private Word _sp;

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

        /// <summary>
        /// The frequency in MHz of the CPU
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// The memory map
        /// </summary>
        public Byte[] Memory { get; private set; }
        public BitArray MemoryMap { get; set; }

        /// <summary>
        /// The maximum amount of memory the process can address
        /// </summary>
        public long MemoryLimit { get; set; } = 0xFFFF;

        /// <summary>
        /// Determines is the CPU is currently paused as Reset cannot guanantee instant pausing
        /// </summary>
        public Bit IsPaused { get; private set; }

        public float CalculationsPerMilisecond { get; set; }

        public Byte Cycles { get { return _cycles; } private set { _cycles = value; } }

        public CPU(long memoryLimit = 0, float frequency = 1.023F)
        {
            if (memoryLimit == 0)
            {
                memoryLimit = Word.MaxValue;
            }

            MemoryLimit = memoryLimit;
            Frequency = frequency;
            _pc = 0x100; // 256
        }

        public void Reset()
        {
            Reset(new Byte[MemoryLimit]);
        }

        public void Reset(Byte[] memory)
        {
            if (_thread != null)
            {
                _thread.Dispose();
            }

            _cycles = 0;

            _pc = 0x100; // 256
            SP = 0x00;
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();

            if (memory.Length < MemoryLimit - 255)
            {
                // Start by padding the zero page memory at the beginning
                var zeroPage = new Byte[256];
                memory = zeroPage.Concat(memory).ToArray();
            }

            if (memory.Length < MemoryLimit)
            {
                memory = memory.Concat(new Byte[MemoryLimit - memory.Length]).ToArray();
            }

            // Pad the memory passed in to fill the memory to MaxLimit
            for (var i = memory.Length; i < MemoryLimit; i++)
            {
                memory[i] = 0x00;
            }

            Memory = memory;
        }

        public void Start()
        {
            _threadCancellationTokenSource = new CancellationTokenSource();
            _threadCancellationToken = _threadCancellationTokenSource.Token;
            _thread = new Task(async () =>
            {
                var stopWatch = new Stopwatch();
                while (!_threadCancellationToken.IsCancellationRequested)
                {
                    stopWatch.Restart();
                    this.execute();

                    while (_debug)
                    {
                        if (!_threadCancellationToken.IsCancellationRequested)
                        {
                            this.IsPaused = true;
                            await Task.Delay(1);
                        }
                        else
                        {
                            _debug = false;
                            IsPaused = false;
                            return;
                        }
                    }

                    stopWatch.Stop();
                    float initElapsed = 0;
                    while (initElapsed + stopWatch.ElapsedTicks < Frequency * 1000)
                    {
                        initElapsed += stopWatch.ElapsedTicks + 1;
                        stopWatch.Reset();
                        await Task.Delay(0);
                        stopWatch.Stop();
                    }

                    CalculationsPerMilisecond = stopWatch.ElapsedTicks + initElapsed;
                }

                return;
            }, _threadCancellationToken);

            _thread.Start();
        }

        public void Debug()
        {
            this._debug = !_debug;
        }

        private void execute()
        {
            _cycles = 0;
#if DEBUG
            // This should only be used while testing as the PC should never wrap
            if (Word.MaxValue == _pc + 1)
            {
                _pc = 0;
            }
#endif
            var instruction = fetchNextByte();

            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    loadRegisterImmediate(ref _a);
                    break;
                case InstructionSet.INS_LDA_ZP:
                    loadRegisterZeroPage(ref _a);
                    break;
                case InstructionSet.INS_LDA_ZPX:
                    loadRegisterZeroPageOffset(X, ref _a);
                    break;
                case InstructionSet.INS_LDA_A:
                    loadRegisterAbsolute(ref _a);
                    break;
                case InstructionSet.INS_LDA_AX:
                    loadRegisterAbsoluteOffset(X, ref _a);
                    break;
                case InstructionSet.INS_LDA_AY:
                    loadRegisterAbsoluteOffset(Y, ref _a);
                    break;
                case InstructionSet.INS_LDA_IX:
                case InstructionSet.INS_LDA_IY:

                    break;
            }
        }

        private Byte fetchWord(Word address)
        {
            _pc++;
            return Memory[address];
        }

        /// <summary>
        /// Returns the Byte at the address requested
        /// </summary>
        /// <param name="address">The location in memory</param>
        /// <returns>The Byte in the requested address</returns>
        private Byte fetchByte(Word address)
        {
            _cycles++;
            if (address > 0x100) _cycles++;
            return Memory[address];
        }

        /// <summary>
        /// Retrieve the X register and increment PC
        /// </summary>
        /// <returns>The current X register</returns>
        private Byte fetchX()
        {
            _cycles++;
            return X;
        }

        /// <summary>
        /// Retrieve the Y register and increment PC
        /// </summary>
        /// <returns>The current Y register</returns>
        private Byte fetchY()
        {
            _cycles++;
            return Y;
        }

        /// <summary>
        /// Returns the same value passed in, but increments the PC by 1
        /// </summary>
        /// <param name="value">The value to return</param>
        /// <returns>The value passed in</returns>
        private Byte fetchRegister(Byte value)
        {
            _cycles++;
            return value;
        }


        /// <summary>
        /// Returns the next Byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next Byte in memory</returns>
        private Byte fetchNextByte()
        {
            _cycles++;
            return Memory[_pc++];
        }

        /// <summary>
        /// Returns the 16 bit value based on the current PC swaps the Bytes, and increments the PC by two
        /// </summary>
        /// <returns>The next word in memory</returns>
        private Word fetchNextWord()
        {
            _cycles++;
#if BIGENDIAN
                var ByteA = Memory[PC++];
                var ByteB = Memory[PC++];
                return swapBytes(ByteA, ByteB);
#else
            var value = BitConverter.ToUInt16(Memory, _pc++);
            _pc++;
            return value;
#endif
        }

        int getBit(Byte b, int bitNumber)
        {
            return ((b >> bitNumber) & 0x01);
        }

        /// <summary>
        /// Swap the Bytes around from little-endian to big-endian, or visa-versa
        /// </summary>
        /// <param name="ByteA">The left most Byte</param>
        /// <param name="ByteB">The right most Byte</param>
        /// <returns></returns>
        private Word swapBytes(Byte ByteA, Byte ByteB)
        {
            return ((Word)(ByteB << 8 | ByteA));
        }

        private void setRegister(ref Byte register, byte value)
        {
            Status.Negative = (sbyte)value < 0;
            Status.Zero = value == 0;
            register = value;
        }

        private void loadRegisterStatus(ref Byte register)
        {
            Status.Zero = register == (Byte)0x00;
            Status.Negative = getBit(register, 7) == 1;
        }

        /// <summary>
        /// Load the next Byte into the register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterImmediate(ref Byte register)
        {
            setRegister(ref register, fetchNextByte());
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 Bytes ino the correct register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterZeroPage(ref Byte register)
        {
            setRegister(ref register, fetchByte(fetchNextByte()));
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 Bytes plus an offset ino the correct register
        /// </summary>
        /// <param name="offset">The amount to offset from the zero page address</param>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterZeroPageOffset(Byte offset, ref Byte register)
        {
            setRegister(ref register, fetchByte(Convert.ToByte(fetchNextByte() + fetchRegister(offset))));
        }

        private void loadRegisterAbsolute(ref Byte register)
        {
            setRegister(ref register, fetchByte(fetchNextWord()));
        }

        private void loadRegisterAbsoluteOffset(Byte offset, ref Byte register)
        {
            var address = fetchNextWord();
            setRegister(ref register, fetchByte(Convert.ToUInt16(address + offset)));
        }

        public void Dispose()
        {
            if (_threadCancellationTokenSource != null)
            {
                _threadCancellationTokenSource.Cancel(true);
                _thread.Wait();
                _thread.Dispose();
                Memory = null;
                Status = null;
            }
        }
    }
}