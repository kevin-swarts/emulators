using System;
using System.Diagnostics;
using System.Linq;
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
#if DEBUG
            // This should only be used while testing as the PC should never wrap
            if (ushort.MaxValue == PC + 1)
            {
                PC = 0;
            }
#endif
            var instruction = Memory[PC++];

            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    loadAccumulatorImmediate();
                    break;
                case InstructionSet.INS_LDA_ZP:
                    loadAccumulatorZeroPage();
                    break;
                case InstructionSet.INS_LDA_ZPX:
                    loadAccumulatorZeroPageXOffset();
                    break;
                case InstructionSet.INS_LDA_A:
                    loadAccumulatorAbsolute();
                    break;
                case InstructionSet.INS_LDA_AX:
                case InstructionSet.INS_LDA_AY:
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
            PC++;
            return Memory[address];
        }

        /// <summary>
        /// Retrieve the X register and increment PC
        /// </summary>
        /// <returns>The current X register</returns>
        private byte fetchX()
        {
            PC++;
            return X;
        }

        /// <summary>
        /// Retrieve the Y register and increment PC
        /// </summary>
        /// <returns>The current Y register</returns>
        private byte fetchY()
        {
            PC++;
            return Y;
        }


        /// <summary>
        /// Returns the next byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next byte in memory</returns>
        private byte fetchByte()
        {
            return fetchByte(PC);
        }

        private void loadAccumulatorStatus()
        {
            Status.Zero = this.A == (byte)0x00;
        }

        private void loadAccumulatorImmediate()
        {
            A = fetchByte();
            loadAccumulatorStatus();
        }

        private void loadAccumulatorZeroPage()
        {
            A = fetchByte(fetchByte());
            loadAccumulatorStatus();
        }

        private void loadAccumulatorZeroPageXOffset()
        {
            A = fetchByte(Convert.ToByte(fetchByte() + fetchX()));
            loadAccumulatorStatus();
        }

        private void loadAccumulatorAbsolute()
        {
            var byteA = fetchByte();
            var byteB = fetchByte();
            A = fetchByte((ushort)(byteB << 8 | byteA));
            loadAccumulatorStatus();
        }

        public void Dispose()
        {
            if (_threadCancellationTokenSource != null)
            {
                _threadCancellationTokenSource.Cancel();
                _thread.Wait();
                _thread.Dispose();
                Memory = null;
                Status = null;
            }
        }
    }
}