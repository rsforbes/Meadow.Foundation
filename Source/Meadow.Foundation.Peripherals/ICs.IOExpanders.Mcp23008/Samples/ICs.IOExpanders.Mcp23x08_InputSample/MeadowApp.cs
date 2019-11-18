using System;
using System.Collections;
using System.Text;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.ICs.IOExpanders;
using Meadow.Hardware;

namespace ICs.IOExpanders.Mcp23x08_InputSample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        Mcp23x08 _mcp;

        public MeadowApp()
        {
            ConfigurePeripherals();

//            while (true) {
                TestBulkPinReads(10);
                //TestDigitalOutputPorts(2);
//            }

        }

        public void ConfigurePeripherals()
        {
            // create a new mcp with all the address pins pulled low for
            // an address of 0x20/32
            _mcp = new Mcp23x08(Device.CreateI2cBus(), false, false, false);

        }

        void TestBulkPinReads(int loopCount)
        {
            for (int l = 0; l < loopCount; l++) {
                byte mask = _mcp.ReadFromPorts();
                var bits = new BitArray(new byte[] { mask });
                StringBuilder bitsString = new StringBuilder();
                foreach (var bit in bits) {
                    bitsString.Append((bool)bit?"1":"0");
                }

                Console.WriteLine($"Port Values, raw:{mask.ToString("X")}, bits: { bitsString.ToString()}");

                Thread.Sleep(100);
            }
        }
    }
}
