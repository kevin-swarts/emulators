using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Emulators.Platform
{
    public interface IMainboard
    {
        IProcessor Processor { get; }
        IMemory Memory { get; }
        byte AddressSize { get; }
        byte DataBusSize { get; }
        IEnumerable<IOutput> Outputs { get; }
        IEnumerable<IInput> Inputs { get; }
        void Reset(byte[] memoryMap);
    }
}
