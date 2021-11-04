using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.Platform
{
    public interface ITimer
    {
        float Frequency { get; }
        event EventHandler Tick;
    }
}
