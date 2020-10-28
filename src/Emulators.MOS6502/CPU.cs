using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

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
        private bool _debug = false;

        private byte _cycles = 0;

        /// <summary>
        /// The cancellation token used to control the thread task
        /// </summary>
        private CancellationTokenSource _threadCancellationTokenSource;
        private CancellationToken _threadCancellationToken;

        /// <summary>
        /// Program Counter
        /// </summary>
        public ushort PC { get; set; }

        /// <summary>
        /// Stack Pointer
        /// </summary>
        public ushort SP { get; set; }

        /// <summary>
        /// Accumulator
        /// </summary>
        public byte A { get; set; }

        /// <summary>
        /// X Register
        /// </summary>
        public byte X { get; set; }

        /// <summary>
        /// Y Register
        /// </summary>
        public byte Y { get; set; }

        /// <summary>
        /// Processor Status Flags (7 used bit byte)
        /// </summary>
        public ProcessorStatus Status { get; set; }

        /// <summary>
        /// The frequency in MHz of the CPU
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// The memory map
        /// </summary>
        public byte[] Memory { get; private set; }

        /// <summary>
        /// The maximum amount of memory the process can address
        /// </summary>
        public long MemoryLimit { get; set; } = 0xFFFF;

        /// <summary>
        /// Determines is the CPU is currently paused as Reset cannot guanantee instant pausing
        /// </summary>
        public bool IsPaused { get; private set; }

        public float CalculationsPerMilisecond { get; set; }

        public byte Cycles { get { return _cycles; } private set { _cycles = value; } }

        public CPU(long memoryLimit = 0, float frequency = 1.023F)
        {
            if (memoryLimit == 0)
            {
                memoryLimit = ushort.MaxValue;
            }

            MemoryLimit = memoryLimit;
            Frequency = frequency;
            PC = 0x100; // 256
        }

        public void Reset()
        {
            Reset(new byte[MemoryLimit]);
        }

        public void Reset(byte[] memory)
        {
            if (_thread != null)
            {
                _thread.Dispose();
            }

            _cycles = 0;

            PC = 0x100; // 256
            SP = 0x00;
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();

            if (memory.Length < MemoryLimit - 255)
            {
                // Start by padding the zero page memory at the beginning
                var zeroPage = new byte[256];
                memory = zeroPage.Concat(memory).ToArray();
            }

            if (memory.Length < MemoryLimit)
            {
                memory = memory.Concat(new byte[MemoryLimit - memory.Length]).ToArray();
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
            if (ushort.MaxValue == PC + 1)
            {
                PC = 0;
            }
#endif
            var instruction = fetchNextByte();

            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    loadRegisterImmediate(reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_ZP:
                    loadRegisterZeroPage(reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_ZPX:
                    loadRegisterZeroPageOffset(X, reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_A:
                    loadRegisterAbsolute(reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_AX:
                    loadRegisterAbsoluteOffset(X, reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_AY:
                    loadRegisterAbsoluteOffset(Y, reg => A = reg);
                    break;
                case InstructionSet.INS_LDA_IX:
                case InstructionSet.INS_LDA_IY:

                    break;
            }
        }

        private byte fetchWord(ushort address)
        {
            PC++;
            return Memory[address];
        }

        /// <summary>
        /// Returns the byte at the address requested
        /// </summary>
        /// <param name="address">The location in memory</param>
        /// <returns>The byte in the requested address</returns>
        private byte fetchByte(ushort address)
        {
            _cycles++;
            if (address > 0x100) _cycles++;
            return Memory[address];
        }

        /// <summary>
        /// Retrieve the X register and increment PC
        /// </summary>
        /// <returns>The current X register</returns>
        private byte fetchX()
        {
            _cycles++;
            return X;
        }

        /// <summary>
        /// Retrieve the Y register and increment PC
        /// </summary>
        /// <returns>The current Y register</returns>
        private byte fetchY()
        {
            _cycles++;
            return Y;
        }

        /// <summary>
        /// Returns the same value passed in, but increments the PC by 1
        /// </summary>
        /// <param name="value">The value to return</param>
        /// <returns>The value passed in</returns>
        private byte fetchRegister(byte value)
        {
            _cycles++;
            return value;
        }


        /// <summary>
        /// Returns the next byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next byte in memory</returns>
        private byte fetchNextByte()
        {
            _cycles++;
            return Memory[PC++];
        }

        /// <summary>
        /// Returns the 16 bit value based on the current PC swaps the bytes, and increments the PC by two
        /// </summary>
        /// <returns>The next word in memory</returns>
        private ushort fetchNextWord()
        {
            _cycles++;
            var byteA = Memory[PC++];
            var byteB = Memory[PC++];
            return swapBytes(byteA, byteB);
        }

        /// <summary>
        /// Swap the bytes around from little-endian to big-endian, or visa-versa
        /// </summary>
        /// <param name="byteA">The left most byte</param>
        /// <param name="byteB">The right most byte</param>
        /// <returns></returns>
        private ushort swapBytes(byte byteA, byte byteB)
        {
            return ((ushort)(byteB << 8 | byteA));
        }

        private void loadAccumulatorStatus()
        {
            Status.Zero = this.A == (byte)0x00;
        }

        /// <summary>
        /// Load the next byte into the register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterImmediate(Action<byte> action)
        {
            action(fetchNextByte());
            loadAccumulatorStatus();
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 bytes ino the correct register
        /// </summary>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterZeroPage(Action<byte> action)
        {
            action(fetchByte(fetchNextByte()));
            loadAccumulatorStatus();
        }

        /// <summary>
        /// Load the value at the position in zero page memory < 256 bytes plus an offset ino the correct register
        /// </summary>
        /// <param name="offset">The amount to offset from the zero page address</param>
        /// <param name="action">The action that will take the value and apply it to the correct register</param>
        private void loadRegisterZeroPageOffset(byte offset, Action<byte> action)
        {
            action(fetchByte(Convert.ToByte(fetchNextByte() + fetchRegister(offset))));
            loadAccumulatorStatus();
        }

        private void loadRegisterAbsolute(Action<byte> action)
        {
            var address = fetchNextWord();
            action(fetchByte(address));
            loadAccumulatorStatus();
        }

        private void loadRegisterAbsoluteOffset(byte offset, Action<byte> action)
        {
            var address = fetchNextWord();
            var value = fetchByte(Convert.ToUInt16(address + offset));
            action(value);
            loadAccumulatorStatus();
            //var byteA = fetchNextByte();
            //var byteB = fetchNextByte();
            //var address = Convert.ToUInt16((byteB << 8 | byteA) + fetchRegister(offset));
            ////if (address > 0x100) PC++; // > 255
            //action(fetchByte(address));

            //loadAccumulatorStatus();
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