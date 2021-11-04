using System;
using System.Threading.Tasks;

namespace Emulators.Platform
{
    public interface IProcessor : IDisposable
    {
        Task StartAsync();
        Task DebugAsync();
        IMemory Memory { get; }
        long MemoryLimit { get; }
        int AddressSize { get; }
        byte DataBusSize { get; }
        Endianness Endianness { get; }
        IMainboard Mainboard { get; }
        //byte Cycles { get; set; }

        void Reset();
    }
}