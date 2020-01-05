using System;
using System.Text;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Radio;

namespace Radio.Nrf24L01_Sample
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        Nrf24L01 radio;

        public MeadowApp()
        {
            Console.WriteLine("Initializing...");

            var spi = Device.CreateSpiBus(); //Mode 0

            radio = new Nrf24L01(device: Device,
                spiBus: spi,
                chipEnablePin: Device.Pins.D00,
                chipSelectPin: Device.Pins.D01,
                interuptPin: Device.Pins.D02,
                packetSize: 20);

            Console.WriteLine("radion instance created");

            TestSender();
        }

        void TestSender()
        {
            // Set sender send address, receiver pipe0 address (Optional)
            byte[] receiverAddress = Encoding.UTF8.GetBytes("NRF24");
            radio.Address = receiverAddress;

            // Loop
            while (true)
            {
                Console.WriteLine("Send message");

                radio.Send(Encoding.UTF8.GetBytes("Hello Meadow!"));

                Thread.Sleep(2000);
            }
        }

        void TestReceiver()
        {
            // Set sender send address, receiver pipe0 address (Optional)
            byte[] receiverAddress = Encoding.UTF8.GetBytes("NRF24");
            radio.Pipe0.Address = receiverAddress;

            // Binding DataReceived event
            radio.DataReceived += Radio_DataReceived;
        }

        private void Radio_DataReceived(object sender, Nrf24L01.DataReceivedEventArgs e)
        {
            var raw = e.Data;
            var message = Encoding.UTF8.GetString(raw);

            Console.WriteLine($"Msg: {message}");
        }
    }
}