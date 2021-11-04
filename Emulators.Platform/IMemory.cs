using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.Platform
{
    public interface IMemory : IEnumerable<byte>
    {
        IMainboard Mainboard { get; }
    }
}