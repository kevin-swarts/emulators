using System;
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
        private Task<bool> _thread;

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

        public bool IsPaused { get; private set; }

        public CPU(long memoryLimit = 0xFFFF, float frequency = 1.023F)
        {
            MemoryLimit = memoryLimit;
            PC = 0x100; // 255
        }

        public void Reset()
        {
            PC = 0x100; // 255
            SP = 0x00;
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();
            Memory = new byte[MemoryLimit];
        }

        public void Reset(byte[] memory)
        {
            if (_thread != null)
            {
                _thread.Dispose();
            }
            PC = 0x100; // 255
            SP = 0x00;
            A = 0x00;
            X = 0x00;
            Y = 0x00;
            Status = new ProcessorStatus();

            // Pad the memory passed in to fill the memory to MaxLimit
            for(var i = memory.Length; i < MemoryLimit; i++)
            {
                memory[i] = 0x00;
            }

            Memory = memory;
            _threadCancellationTokenSource = new CancellationTokenSource();
            _threadCancellationToken = _threadCancellationTokenSource.Token;
            _thread = new Task<bool>(() =>
            {
                while (!_threadCancellationToken.IsCancellationRequested)
                {
                    this.execute();
                    while (_debug)
                    {
                        if (!_threadCancellationToken.IsCancellationRequested)
                        {
                            this.IsPaused = true;
                            Task.Delay(100, _threadCancellationToken);
                        }
                        else
                        {
                            _debug = false;
                            IsPaused = false;
                            return false;
                        }
                    }

                    this.IsPaused = false;
                }

                return false;
            }, _threadCancellationToken);

            _thread.Start();
        }

        public void Debug()
        {
            this._debug = !_debug;
        }

        private void execute()
        {
            var instruction = Memory[PC];
            PC++;
            switch (instruction)
            {
                case InstructionSet.INS_LDA_I:
                    loadAccumulatorImmediate();
                    break;
                case InstructionSet.INS_LDA_ZP:
                    loadAccumulatorZeroPage();
                    break;
                case InstructionSet.INS_LDA_ZPX:
                case InstructionSet.INS_LDA_A:
                case InstructionSet.INS_LDA_AX:
                case InstructionSet.INS_LDA_AY:
                case InstructionSet.INS_LDA_IX:
                case InstructionSet.INS_LDA_IY:

                    break;
            }
        }

        private ushort fetchWord(ushort address)
        {
            //if (BitConverter.IsLittleEndian)
            //{
                var newByte = new byte[2];
                return BitConverter.ToUInt16(Memory, address);
            //}
            
        }

        /// <summary>
        /// Returns the byte at the address requested
        /// </summary>
        /// <param name="address">The location in memory</param>
        /// <returns>The byte in the requested address</returns>
        private byte fetchByte(ushort address)
        {
            return Memory[address];
        }


        /// <summary>
        /// Returns the next byte based on the current PC, and increments PC by one
        /// </summary>
        /// <returns>The next byte in memory</returns>
        private byte fetchByte()
        {
            return fetchByte(PC++);
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

        public void Dispose()
        {
            _threadCancellationTokenSource.Cancel();
            _thread.Wait();
            _thread.Dispose();
            Memory = null;
            Status = null;
        }
    }
}
