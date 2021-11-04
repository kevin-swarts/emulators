using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators.Platform
{
    public class Timer : ITimer
    {
        public Timer(float frequency)
        {
            Frequency = frequency;
        }

        public float Frequency { get; private set; }

        public event EventHandler Tick;
    }
}
