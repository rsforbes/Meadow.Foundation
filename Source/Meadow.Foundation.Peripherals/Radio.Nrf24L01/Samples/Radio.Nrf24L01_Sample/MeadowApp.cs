using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;

namespace Radio.Nrf24L01_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        public MeadowApp()
        {
            Console.WriteLine("Initializing...");
        }
    }
}