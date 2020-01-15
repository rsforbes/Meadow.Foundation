using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.ICs.ADCs;

namespace ICs.IOExpanders.HT16K33_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        protected Ads1115 ads1115;

        public MeadowApp()
        {
            Initialize();
        }

        void Initialize()
        {
            Console.WriteLine("Initialize...");
            ads1115 = new Ads1115(Device.CreateI2cBus());
        }
    }
}