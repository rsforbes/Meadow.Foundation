using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Foundation.ICs.IOExpanders;

namespace ICs.IOExpanders.Mcp23x08_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        Mcp23x08 _mcp;


        public MeadowApp()
        {
            ConfigurePeripherals();
        }

        public void ConfigurePeripherals()
        {
            _mcp = new Mcp23x08(Device.CreateI2cBus());


        }

        
    }
}
