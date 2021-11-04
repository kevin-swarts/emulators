using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Emulators.Platform;

namespace Emulators.MOS6502
{
    public abstract class Processor : IProcessor, IDisposable
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

        protected int _pc;

        /// <summary>
        /// The frequency in MHz of the CPU
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// The memory map
        /// </summary>
        public IMemory Memory { get => Mainboard.Memory; }

        /// <summary>
        /// The maximum amount of memory the process can address
        /// </summary>
        public long MemoryLimit { get; set; } = 0xFFFF;

        /// <summary>
        /// Determines is the CPU is currently paused as Reset cannot guanantee instant pausing
        /// </summary>
        public bool IsPaused { get; private set; }

        public float CalculationsPerMilisecond { get; set; }

        public int AddressSize { get; private set; }

        public byte DataBusSize { get; private set; } = 8;

        public Endianness Endianness { get; private set; } = Endianness.Big;

        public IMainboard Mainboard { get; private set; }
        public IList<InstructionDetail> InstructionSet { get; set; } = new List<InstructionDetail>();

        public Processor(IMainboard mainboard, long memoryLimit = 0, float frequency = 1.023F)
        {
            Mainboard = mainboard;

            if (memoryLimit == 0)
            {
                memoryLimit = Int32.MaxValue;
            }

            MemoryLimit = memoryLimit;
            Frequency = frequency;
        }

        public virtual void Reset()
        {
            if (_thread != null)
            {
                _thread.Dispose();
            }
        }

        public async Task StartAsync()
        {
            _threadCancellationTokenSource = new CancellationTokenSource();
            _threadCancellationToken = _threadCancellationTokenSource.Token;
            _thread = new Task(async () =>
            {
                var stopWatch = new Stopwatch();
                while (!_threadCancellationToken.IsCancellationRequested)
                {
                    stopWatch.Restart();
                    this.Execute();

                    while (_debug)
                    {
                        if (!_threadCancellationToken.IsCancellationRequested)
                        {
                            this.IsPaused = true;
                            await Task.Delay(10);
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

            var _ = await Task.FromResult<bool>(true);
        }

        public async Task DebugAsync()
        {
            var state = this._debug;
            this._debug = !_debug;

            while(this._debug == state)
            {
                await Task.Delay(1);
            }
        }

        protected abstract void Execute();

        public virtual void Dispose()
        {
            if (_threadCancellationTokenSource != null)
            {
                _threadCancellationTokenSource.Cancel(true);
                _thread.Wait();
                _thread.Dispose();
            }
        }
    }

    public class InstructionDetail
    {
        public string Mnemonic { get; set; }
        public byte OpCode { get; set; }
        public byte Bytes { get; set; }
        public byte Cycles { get; set; }
        public byte MaxOperands { get; set; }
        public byte MinOperands { get; set; }
        public string Description { get; set; }
    }
}