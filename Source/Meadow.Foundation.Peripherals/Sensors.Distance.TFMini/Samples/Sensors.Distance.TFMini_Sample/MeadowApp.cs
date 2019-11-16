using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Foundation.Sensors.Spatial;

namespace Sensors.Distance.TFMini_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        TFMini tfMini;


        public MeadowApp()
        {
            Console.WriteLine("Initializing serial port");
            //var serial = Device.CreateSerialPort(Device.SerialPortNames.Com4, 115200);

            tfMini = new TFMini(Device, Device.SerialPortNames.Com4);
        }

        protected void Init()
        {

        }

    }
}
