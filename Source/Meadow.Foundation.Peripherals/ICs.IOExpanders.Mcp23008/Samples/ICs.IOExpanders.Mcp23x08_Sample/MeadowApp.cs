using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Foundation.ICs.IOExpanders;
using System.Collections.Generic;

namespace ICs.IOExpanders.Mcp23x08_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        Mcp23x08 _mcp;


        public MeadowApp()
        {
            ConfigurePeripherals();

            var out00 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP0);
            var out01 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP1);
            var out02 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP2);
            var out03 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP3);
            var out04 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP4);
            var out05 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP5);
            var out06 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP6);
            var out07 = _mcp.CreateDigitalOutputPort(_mcp.Pins.GP7);

            var outs = new List<IDigitalOutputPort>() {
                out00, out01, out02, out03, out04, out05, out06, out07
            };

           
            while (true) {
                for (int i = 0; i < outs.Count; i++) {
                    Console.WriteLine($"i: {i}");
                    // turn them all off, except whatever we're on
                    for (int j = 0; j < outs.Count; j++) {
                        Console.WriteLine($"j: {j}");
                        outs[i].State = (i == j);
                    }
                    Thread.Sleep(500);
                }
            }
        }

        public void ConfigurePeripherals()
        {
            // create a new mcp with all the address pins pulled high for
            // an address of 0x27/39
            _mcp = new Mcp23x08(Device.CreateI2cBus(), true, true, true);


        }

        
    }
}
