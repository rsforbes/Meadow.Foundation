using System;
using Meadow.Foundation.Sensors.Spatial;
using Meadow.Hardware;

namespace Sensors.Distance
{
    public partial class TFMini : IDistanceSensor
    {
        ISerialPort serialPort;
        int baudRate;

        int maximumBytesBeforeHeader = 30;
        int maximumMeasurementAttempts = 10;


        public float Distance { get; protected set; }
        public float Strength { get; protected set; }


        //public TFMini(ISerialPort serialPort, int baud = 115200)
        //{
        //    this.serialPort = serialPort;
        //    this.baudRate = baud;
        //    Init();
        //}

        public TFMini(IIODevice device, SerialPortName portName)
        {
            this.baudRate = 115200;
            serialPort = device.CreateSerialPort(
                portName,
                this.baudRate,
                8,
                Parity.None,
                StopBits.One);
            Init();
        }

        protected void Init()
        {
            serialPort.Open();

            // set the output format to be TFMini frames
            //SendCommand(TFMini.Commands.Reset);
            //System.Threading.Thread.Sleep(100);
            SendCommand(TFMini.Commands.EnterConfigMode);
            SendCommand(TFMini.Commands.SetBaudRate(baudRate));
            SendCommand(TFMini.Commands.RangeLimit(0));
            SendCommand(TFMini.Commands.StandardOutputFormat);
            SendCommand(TFMini.Commands.ExitConfigMode);
        }

        protected void ReadUntilFrameHeader()
        {
            int current = 0x0;
            int last = 0x0;
            while (true) {
                current = serialPort.ReadByte();
                if (current == -1) { return; }

                if ((byte)last == 0x59 && (byte)current == 0x59) {
                    Console.WriteLine("Found TFMini Header");
                    break;
                } else {
                    Console.WriteLine($"Not there yet: {last.ToString("x")}");

                }
                last = current;
            }
        }

        protected bool SendCommand(byte[] command)
        {
            byte[] response = new byte[7];

            this.serialPort.Write(command);
            Console.WriteLine($"Writing: {BitConverter.ToString(command).Replace("-", " ")}");
            this.ReadUntilFrameHeader();
            //Console.WriteLine($"success would be: {command[3] == Commands.Success}");
            this.serialPort.Read(response, 0, 7);
            Console.WriteLine($"Response: {BitConverter.ToString(response).Replace("-", " ")}");


            //if (response == Commands.Success) { return true; }
            //else if (response == Commands.InstructionError) {
            //    throw new Exception("Instruction not valid."); }
            //else if (response == Commands.ParameterError) {
            //    throw new Exception("Parameter not valid.");
            //}
            //throw new Exception("Unknown failure.");

            return true;
        }

        protected float ReadDistance()
        {

            return 0;
        }

        protected float ReadLastSignalStrength()
        {
            return 0;
        }

        protected DistanceReading DecodeFrame(byte[] frame)
        {
            // Step 3: Interpret frame
            int dist = (frame[1] << 8) + frame[0];
            int strength = (frame[3] << 8) + frame[2];
            int reserved = frame[4];
            int originalSignalQuality = frame[5];

            var reading = new DistanceReading() {
                Distance = dist,
                SignalStrength = strength,
                SignalQuality = originalSignalQuality
            };
            return reading;
        }

        public class Config
        {
            public OperatingMode OperatingMode { get; set; }
        }

        public class DistanceReading
        {
            public float Distance { get; set; }
            public float SignalStrength { get; set; }
            public float SignalQuality { get; set; }
        }
    }
}
