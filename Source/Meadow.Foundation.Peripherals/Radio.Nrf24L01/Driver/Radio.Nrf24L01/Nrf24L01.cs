using System;
using System.Threading;
using Meadow.Hardware;

namespace Meadow.Foundation.Radio
{
    public class Nrf24L01
    {
        #region Properties / Events

        /// <summary>
        /// Delegate representing event that data was received
        /// </summary>
        /// <param name="sender">Object sending the event</param>
        /// <param name="e">Arguments for the event</param>
        public delegate void DataReceivedHandle(object sender, DataReceivedEventArgs e);

        /// <summary>
        /// Triggering when data was received
        /// </summary>
        public event DataReceivedHandle DataReceived;

        public static int DEFAULT_SPEED = 2000000;

        /// <summary>
        /// nRF24L01 Receive Packet Size
        /// </summary>
        public byte PacketSize
        {
            get => _packetSize; 
            set { _packetSize = value < 0 || value > 32 ? throw new ArgumentOutOfRangeException(nameof(value), "PacketSize needs to be in the range of 0 to 32.") : value; }
        }
        private byte _packetSize;

        /// <summary>
        /// nRF24L01 Send Address
        /// </summary>
        public byte[] Address { get => ReadTxAddress(); set => SetTxAddress(value); }

        /// <summary>
        /// nRF24L01 Working Channel
        /// </summary>
        public byte Channel { get => ReadChannel(); set => SetChannel(value); }

        /// <summary>
        /// nRF24L01 Output Power
        /// </summary>
        public OutputPowerType OutputPower { get => ReadOutputPower(); set => SetOutputPower(value); }

        /// <summary>
        /// nRF24L01 Send Rate
        /// </summary>
        public DataRateType DataRate { get => ReadDataRate(); set => SetDataRate(value); }

        /// <summary>
        /// nRF24L01 Power Mode
        /// </summary>
        public PowerModeType PowerMode { get => ReadPowerMode(); set => SetPowerMode(value); }

        /// <summary>
        /// nRF24L01 Working Mode
        /// </summary>
        public WorkingModeType WorkingMode { get => ReadWorkwingMode(); set => SetWorkingMode(value); }

        /// <summary>
        /// nRF24L01 Pipe 0
        /// </summary>
        public Nrf24L01Pipe Pipe0 { get; private set; }

        /// <summary>
        /// nRF24L01 Pipe 1
        /// </summary>
        public Nrf24L01Pipe Pipe1 { get; private set; }

        /// <summary>
        /// nRF24L01 Pipe 2
        /// </summary>
        public Nrf24L01Pipe Pipe2 { get; private set; }

        /// <summary>
        /// nRF24L01 Pipe 3
        /// </summary>
        public Nrf24L01Pipe Pipe3 { get; private set; }

        /// <summary>
        /// nRF24L01 Pipe 4
        /// </summary>
        public Nrf24L01Pipe Pipe4 { get; private set; }

        /// <summary>
        /// nRF24L01 Pipe 5
        /// </summary>
        public Nrf24L01Pipe Pipe5 { get; private set; }

        #endregion Properties

        #region Fields

        protected IDigitalOutputPort chipEnablePort; //controls Rx/Tx mode
        protected IDigitalInputPort interuptPort; //IRQ
        protected ISpiPeripheral _sensor;
        protected SpiBus spi;

        protected byte[] spiBuffer;
        protected readonly byte[] spiReceive;

        private readonly byte[] _empty = Array.Empty<byte>();

        #endregion Fields

        #region Enums

        /// <summary>
        /// nRF24L01 Command
        /// </summary>
        internal enum Command : byte
        {
            NRF_R_REGISTER = 0x00,
            NRF_W_REGISTER = 0x20,
            NRF_R_RX_PAYLOAD = 0x61,
            NRF_W_TX_PAYLOAD = 0xA0,
            NRF_FLUSH_TX = 0xE1,
            NRF_FLUSH_RX = 0xE2,
            NRF_REUSE_TX_PL = 0xE3,
            NRF_NOP = 0xFF,
        }

        /// <summary>
        /// nRF24L01 Send Data Rate
        /// </summary>
        public enum DataRateType
        {
            Rate1Mbps = 0x00,
            Rate2Mbps = 0x01
        }

        /// <summary>
        /// nRF24l01 Output Power
        /// </summary>
        public enum OutputPowerType
        {
            /// <summary>
            /// -18dBm
            /// </summary>
            N18dBm = 0x00,
            /// <summary>
            /// -12dBm
            /// </summary>
            N12dBm = 0x01,
            /// <summary>
            /// -6dBm
            /// </summary>
            N06dBm = 0x02,
            /// <summary>
            /// 0dBm
            /// </summary>
            N00dBm = 0x03
        }

        /// <summary>
        /// nRF24L01 Power Mode
        /// </summary>
        public enum PowerModeType
        {
            UP = 0x01,
            DOWN = 0x00
        }

        /// <summary>
        /// nRF24L01 Working Mode
        /// </summary>
        public enum WorkingModeType
        {
            Receive = 1,
            Transmit = 0
        }

        internal enum Register : byte
        {
            NRF_CONFIG = 0x00,
            NRF_EN_AA = 0x01,
            NRF_EN_RXADDR = 0x02,
            NRF_SETUP_AW = 0x03,
            NRF_SETUP_RETR = 0x04,
            NRF_RF_CH = 0x05,
            NRF_RF_SETUP = 0x06,
            NRF_STATUS = 0x07,
            NRF_OBSERVE_TX = 0x08,
            NRF_RPD = 0x09,
            NRF_RX_ADDR_P0 = 0x0A,
            NRF_RX_ADDR_P1 = 0x0B,
            NRF_RX_ADDR_P2 = 0x0C,
            NRF_RX_ADDR_P3 = 0x0D,
            NRF_RX_ADDR_P4 = 0x0E,
            NRF_RX_ADDR_P5 = 0x0F,
            NRF_TX_ADDR = 0x10,
            NRF_RX_PW_P0 = 0x11,
            NRF_RX_PW_P1 = 0x12,
            NRF_RX_PW_P2 = 0x13,
            NRF_RX_PW_P3 = 0x14,
            NRF_RX_PW_P4 = 0x15,
            NRF_RX_PW_P5 = 0x16,
            NRF_FIFO_STATUS = 0x17,
            NRF_NOOP = 0x00,
        }
    

    #endregion

    #region Constructors

        public Nrf24L01(IIODevice device, ISpiBus spiBus, IPin chipEnablePin, IPin chipSelectPin, IPin interuptPin,
            byte packetSize,
            byte channel = 2,
            OutputPowerType outputPower = OutputPowerType.N00dBm,
            DataRateType dataRate = DataRateType.Rate2Mbps)
        {
        //   spiBuffer = new byte[Width * Height / 8];
        //   spiReceive = new byte[Width * Height / 8];

            chipEnablePort = device.CreateDigitalOutputPort(chipEnablePin, true);
            interuptPort = device.CreateDigitalInputPort(interuptPin, InterruptMode.EdgeFalling);
            interuptPort.Changed += InteruptPort_Changed;

            var chipSelectPort = device.CreateDigitalOutputPort(chipSelectPin, true);

            spi = (SpiBus)spiBus;
            _sensor = new SpiPeripheral(spiBus, chipSelectPort);

            Initialize(outputPower, dataRate, channel);

            InitializePipe();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="data">Send Data</param>
        public void Send(byte[] data)
        {
            // Details in the Datasheet P65
            SetWorkingMode(WorkingModeType.Transmit);
            // wait for some time so that it doesn't go too fast
            Thread.Sleep(1);

            Write(Command.NRF_W_TX_PAYLOAD, Register.NRF_NOOP, data);

            chipEnablePort.State = true;
            Thread.Sleep(1);
            chipEnablePort.State = false;
            Thread.Sleep(1);

            SetWorkingMode(WorkingModeType.Receive);
            Thread.Sleep(1);
        }

        /// <summary>
        /// Receive
        /// </summary>
        /// <param name="length">Packet Size</param>
        /// <returns>Data</returns>
        public Span<byte> Receive(byte length)
        {
            // Details in the Datasheet P65
            chipEnablePort.State = false;

            Span<byte> writeData = stackalloc byte[length];
            Span<byte> readData = WriteRead(Command.NRF_R_RX_PAYLOAD, Register.NRF_NOOP, writeData);

            Span<byte> ret = new byte[readData.Length];
            readData.CopyTo(ret);

            chipEnablePort.State = false;
            // clear RX FIFO interrupt
            Write(Command.NRF_W_REGISTER, Register.NRF_STATUS, 0b_0100_1110);
            chipEnablePort.State = true;

            return ret;
        }

        private void InteruptPort_Changed(object sender, DigitalInputPortEventArgs e)
        {
            DataReceived(sender, new DataReceivedEventArgs(Receive(_packetSize).ToArray()));
        }

        private void Initialize(OutputPowerType outputPower, DataRateType dataRate, byte channel)
        {
            // wait for some time
            Thread.Sleep(10);

            // Details in the datasheet P53
            chipEnablePort.State = false;

            Write(Command.NRF_FLUSH_TX, Register.NRF_NOOP, _empty);
            Write(Command.NRF_FLUSH_RX, Register.NRF_NOOP, _empty);
            // reflect RX_DR interrupt, TX_DS interrupt not reflected, power up, receive mode
            Write(Command.NRF_W_REGISTER, Register.NRF_CONFIG, 0b_0011_1011);
            chipEnablePort.State = false;

            SetAutoAck(false);
            SetChannel(channel);
            SetOutputPower(outputPower);
            SetDataRate(dataRate);
            SetRxPayload(_packetSize);
        }

        /// <summary>
        /// Initialize nRF24L01 Pipe
        /// </summary>
        private void InitializePipe()
        {
            Pipe0 = new Nrf24L01Pipe(this, 0);
            Pipe1 = new Nrf24L01Pipe(this, 1);
            Pipe2 = new Nrf24L01Pipe(this, 2);
            Pipe3 = new Nrf24L01Pipe(this, 3);
            Pipe4 = new Nrf24L01Pipe(this, 4);
            Pipe5 = new Nrf24L01Pipe(this, 5);
        }

        /// <summary>
        /// Set nRF24L01 Receive Packet Size (All Pipe)
        /// </summary>
        /// <param name="payload">Size, from 0 to 32</param>
        internal void SetRxPayload(byte payload)
        {
            // Details in the Datasheet P56
            if (payload > 32 || payload < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payload), $"{nameof(payload)} needs to be in the range of 0 to 32.");
            }

            chipEnablePort.State = false;

            Span<byte> writeData = stackalloc byte[1]
            {
                payload
            };

            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P0, writeData);
            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P1, writeData);
            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P2, writeData);
            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P3, writeData);
            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P4, writeData);
            Write(Command.NRF_W_REGISTER, Register.NRF_RX_PW_P5, writeData);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Set nRF24L01 Receive Packet Size
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <param name="payload">Size, from 0 to 32</param>
        internal void SetRxPayload(byte pipe, byte payload)
        {
            // Details in the Datasheet P56
            if (payload > 32 || payload < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payload), $"{nameof(payload)} needs to be in the range of 0 to 32.");
            }

            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            Span<byte> writeData = stackalloc byte[]
            {
                (byte)((byte)Command.NRF_W_REGISTER + (byte)Register.NRF_RX_PW_P0 + pipe),
                payload
            };

            chipEnablePort.State = false;

            Write(writeData);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Receive Packet Size
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <returns>Size</returns>
        internal byte ReadRxPayload(byte pipe)
        {
            // Details in the Datasheet P56
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, (Register)((byte)Register.NRF_RX_PW_P0 + pipe), 1);

            return readData[0];
        }

        /// <summary>
        /// Set nRF24L01 Auto Acknowledgment (All Pipe)
        /// </summary>
        /// <param name="isAutoAck">Is Enable</param>
        internal void SetAutoAck(bool isAutoAck)
        {
            // Details in the Datasheet P53
            chipEnablePort.State = false;

            if (isAutoAck)
            {
                // all pipe
                Write(Command.NRF_W_REGISTER, Register.NRF_EN_AA, 0b_0011_1111);
            }
            else
            {
                Write(Command.NRF_W_REGISTER, Register.NRF_EN_AA, 0b_0000_0000);
            }

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Set nRF24L01 Auto Acknowledgment
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <param name="isAutoAck">Is Enable</param>
        internal void SetAutoAck(byte pipe, bool isAutoAck)
        {
            // Details in the Datasheet P53
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_EN_AA, 1);

            byte setting;
            if (isAutoAck)
            {
                setting = (byte)(readData[0] | (1 << pipe));
            }
            else
            {
                setting = (byte)(readData[0] & ~(1 << pipe));
            }

            Write(Command.NRF_W_REGISTER, Register.NRF_EN_AA, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Auto Acknowledgment
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <returns>Is Enable</returns>
        internal bool ReadAutoAck(byte pipe)
        {
            // Details in the Datasheet P53
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_EN_AA, 1);

            byte ret = (byte)(readData[0] >> pipe & 0b_0000_0001);
            if (ret == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Set nRF24L01 Receive Pipe (All Pipe)
        /// </summary>
        /// <param name="isEnable">Is Enable Receive</param>
        internal void SetRxPipe(bool isEnable)
        {
            // Details in the Datasheet P54
            chipEnablePort.State = false;

            if (isEnable)
            {
                // all pipe
                Write(Command.NRF_W_REGISTER, Register.NRF_EN_RXADDR, 0b_0011_1111);
            }
            else
            {
                Write(Command.NRF_W_REGISTER, Register.NRF_EN_RXADDR, 0b_0000_0000);
            }

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Set nRF24L01 Receive Pipe is Enable
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <param name="isEnable">Is Enable the Pipe Receive</param>
        internal void SetRxPipe(byte pipe, bool isEnable)
        {
            // Details in the Datasheet P54
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_EN_RXADDR, 1);

            byte setting;
            if (isEnable)
            {
                setting = (byte)(readData[0] | (1 << pipe));
            }
            else
            {
                setting = (byte)(readData[0] & ~(1 << pipe));
            }

            Write(Command.NRF_W_REGISTER, Register.NRF_EN_RXADDR, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Receive Pipe is Enable
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <returns>Is Enable the Pipe Receive</returns>
        internal bool ReadRxPipe(byte pipe)
        {
            // Details in the Datasheet P54
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_EN_RXADDR, 1);

            byte ret = (byte)(readData[0] >> pipe & 0x01);
            if (ret == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Set nRF24L01 Power Mode
        /// </summary>
        /// <param name="mode">Power Mode</param>
        internal void SetPowerMode(PowerModeType mode)
        {
            // Details in the Datasheet P53
            // Register:0x00, bit[1]
            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_CONFIG, 1);

            byte setting;
            switch (mode)
            {
                case PowerModeType.UP:
                    setting = (byte)(readData[0] | (1 << 1));
                    break;
                case PowerModeType.DOWN:
                    setting = (byte)(readData[0] & ~(1 << 1));
                    break;
                default:
                    setting = (byte)(readData[0] | (1 << 1));
                    break;
            }

            Write(Command.NRF_W_REGISTER, Register.NRF_CONFIG, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Power Mode
        /// </summary>
        /// <returns>Power Mode</returns>
        internal PowerModeType ReadPowerMode()
        {
            // Details in the Datasheet P53
            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_CONFIG, 1);

            return (PowerModeType)((readData[0] >> 1) & 0b_0000_0001);
        }

        /// <summary>
        /// Set nRF24L01 Working Mode
        /// </summary>
        /// <param name="mode">Working Mode</param>
        internal void SetWorkingMode(WorkingModeType mode)
        {
            // Details in the Datasheet P53
            // Register:0x00, bit[0]
            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_CONFIG, 1);

            byte setting;
            switch (mode)
            {
                case WorkingModeType.Receive:
                    setting = (byte)(readData[0] | 1);
                    break;
                case WorkingModeType.Transmit:
                    setting = (byte)(readData[0] & ~1);
                    break;
                default:
                    setting = (byte)(readData[0] | 1);
                    break;
            }

            Write(Command.NRF_W_REGISTER, Register.NRF_CONFIG, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Working Mode
        /// </summary>
        /// <returns>Working Mode</returns>
        internal WorkingModeType ReadWorkwingMode()
        {
            // Details in the Datasheet P53
            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_CONFIG, 1);

            return (WorkingModeType)(readData[0] & 0b_0000_0001);
        }

        /// <summary>
        /// Set nRF24L01 Output Power
        /// </summary>
        /// <param name="power">Output Power</param>
        internal void SetOutputPower(OutputPowerType power)
        {
            // Details in the Datasheet P54
            // Register: 0x06, bit[2:1]
            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_RF_SETUP, 1);

            byte setting = (byte)(readData[0] & (~0b_0000_0110) | ((byte)power << 1));

            Write(Command.NRF_W_REGISTER, Register.NRF_RF_SETUP, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Output Power
        /// </summary>
        /// <returns>Output Power</returns>
        internal OutputPowerType ReadOutputPower()
        {
            // Details in the Datasheet P54
            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_RF_SETUP, 1);

            return (OutputPowerType)((readData[0] & 0b_0000_0110) >> 1);
        }

        /// <summary>
        /// Set nRF24L01 Send Rate
        /// </summary>
        /// <param name="rate">Send Rate</param>
        internal void SetDataRate(DataRateType rate)
        {
            // Details in the Datasheet P54
            // Register: 0x06, bit[3]
            chipEnablePort.State = false;

            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_RF_SETUP, 1);

            byte setting = (byte)(readData[0] & (~0b_0000_1000) | ((byte)rate << 1));

            Write(Command.NRF_W_REGISTER, Register.NRF_RF_SETUP, setting);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Send Rate
        /// </summary>
        /// <returns>Send Rate</returns>
        internal DataRateType ReadDataRate()
        {
            // Details in the Datasheet P54
            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_RF_SETUP, 1);

            return (DataRateType)((readData[0] & 0b_0000_1000) >> 3);
        }

        /// <summary>
        /// Set nRF24L01 Receive Address
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <param name="address">Address, if (pipe > 1) then (address.Length = 1), else if (pipe = 1 || pipe = 0) then (address.Length ≤ 5)</param>
        internal void SetRxAddress(byte pipe, ReadOnlySpan<byte> address)
        {
            // Details in the Datasheet P55
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            if (address.Length > 5 || address.Length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Array Length must less than 6.");
            }

            if (pipe > 1 && address.Length > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), "Array Length must equal 1 when pipe more than 1. Address equal pipe1's address the first 4 byte + one byte your custom.");
            }

            Span<byte> writeData = stackalloc byte[1 + address.Length];
            writeData[0] = (byte)((byte)Command.NRF_W_REGISTER + (byte)Register.NRF_RX_ADDR_P0 + pipe);
            for (int i = 0; i < address.Length; i++)
            {
                writeData[i + 1] = address[i];
            }

            chipEnablePort.State = false;

            Write(writeData);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Receive Address
        /// </summary>
        /// <param name="pipe">Pipe, form 0 to 5</param>
        /// <returns>Address</returns>
        internal byte[] ReadRxAddress(byte pipe)
        {
            // Details in the Datasheet P55
            if (pipe > 5 || pipe < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipe), $"{nameof(pipe)} needs to be in the range of 0 to 5.");
            }

            int length = 5;
            if (pipe > 1)
            {
                length = 1;
            }

            byte[] readData = WriteRead(Command.NRF_R_REGISTER, (Register)((byte)Register.NRF_RX_ADDR_P0 + pipe), length);

            return readData;
        }

        /// <summary>
        /// Set nRF24L01 Send Address
        /// </summary>
        /// <param name="address">Address, address.Length = 5</param>
        internal void SetTxAddress(ReadOnlySpan<byte> address)
        {
            // Details in the Datasheet P56
            if (address.Length > 5 || address.Length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(address), "Array Length must less than 6.");
            }

            chipEnablePort.State = false;

            Write(Command.NRF_W_REGISTER, Register.NRF_TX_ADDR, address);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read nRF24L01 Send Address
        /// </summary>
        /// <returns>Address</returns>
        internal byte[] ReadTxAddress()
        {
            // Details in the Datasheet P56
            return WriteRead(Command.NRF_R_REGISTER, Register.NRF_TX_ADDR, 5);
        }

        /// <summary>
        /// Set Working Channel
        /// </summary>
        /// <param name="channel">From 0 to 127</param>
        internal void SetChannel(byte channel)
        {
            // Details in the Datasheet P54
            // Register: 0x05, bit[6:0]
            if (channel < 0 || channel > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "Channel needs to be in the range of 0 to 127.");
            }

            chipEnablePort.State = false;

            Write(Command.NRF_W_REGISTER, Register.NRF_RF_CH, channel);

            chipEnablePort.State = true;
        }

        /// <summary>
        /// Read Working Channel
        /// </summary>
        /// <returns>Channel</returns>
        internal byte ReadChannel()
        {
            // Details in the Datasheet P54
            Span<byte> readData = WriteRead(Command.NRF_R_REGISTER, Register.NRF_RF_CH, 1);

            return readData[0];
        }
    
        internal void Write(ReadOnlySpan<byte> writeData)
        {
            _sensor.WriteBytes(writeData.ToArray());
        }

        internal void Write(Command command, Register register, byte writeByte)
        {
            Span<byte> writeBuf = stackalloc byte[2]
            {
                (byte)((byte)command + (byte)register),
                writeByte
            };

            _sensor.WriteBytes(writeBuf.ToArray());
//            Span<byte> readBuf = stackalloc byte[2];

//            _sensor.TransferFullDuplex(writeBuf, readBuf);
        }

        internal void Write(Command command, Register register, ReadOnlySpan<byte> writeData)
        {
            Span<byte> writeBuf = stackalloc byte[1 + writeData.Length];
          
            writeBuf[0] = (byte)((byte)command + (byte)register);
            writeData.CopyTo(writeBuf.Slice(1));

            _sensor.WriteBytes(writeBuf.ToArray());

         //   Span<byte> readBuf = stackalloc byte[1 + writeData.Length];

         //   _sensor.TransferFullDuplex(writeBuf, readBuf);
        }

        internal byte[] WriteRead(Command command, Register register, int dataLength)
        {
            Span<byte> writeBuf = stackalloc byte[1 + dataLength];
            writeBuf[0] = (byte)((byte)command + (byte)register);

            var ret = _sensor.WriteRead(writeBuf.ToArray(), (ushort)writeBuf.Length);

            return ret;
        }

        internal byte[] WriteRead(Command command, Register register, ReadOnlySpan<byte> writeData)
        {
            Span<byte> writeBuf = stackalloc byte[1 + writeData.Length];

            writeBuf[0] = (byte)((byte)command + (byte)register);
            writeData.CopyTo(writeBuf.Slice(1));

            var ret = _sensor.WriteRead(writeBuf.ToArray(), (ushort)writeBuf.Length);

            return ret;
        }

        #endregion Methods

        #region Classes

        /// <summary>
        /// nRF24L01 Data Received Event Args
        /// </summary>
        public class DataReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// nRF24L01 Received Data
            /// </summary>
            public byte[] Data { get; }

            /// <summary>
            /// Constructs DataReceivedEventArgs instance
            /// </summary>
            /// <param name="data">Data received</param>
            public DataReceivedEventArgs(byte[] data)
            {
                Data = data;
            }
        }

        /// <summary>
        /// nRF24L01 Receive Pipe
        /// </summary>
        public class Nrf24L01Pipe
        {
            private readonly byte _pipeID;
            private readonly Nrf24L01 _nrf;

            /// <summary>
            /// Creates a new instance of the Nrf24l01Pipe
            /// </summary>
            /// <param name="nrf">nRF24L01</param>
            /// <param name="pipeID">Pipe ID</param>
            public Nrf24L01Pipe(Nrf24L01 nrf, byte pipeID)
            {
                _nrf = nrf;
                _pipeID = pipeID;
            }

            /// <summary>
            /// Receive Pipe Address
            /// </summary>
            public byte[] Address { get => _nrf.ReadRxAddress(_pipeID); set => _nrf.SetRxAddress(_pipeID, value); }

            /// <summary>
            /// Auto Acknowledgment
            /// </summary>
            public bool AutoAck { get => _nrf.ReadAutoAck(_pipeID); set => _nrf.SetAutoAck(_pipeID, value); }

            /// <summary>
            /// Receive Pipe Payload
            /// </summary>
            public byte Payload { get => _nrf.ReadRxPayload(_pipeID); set => _nrf.SetRxPayload(_pipeID, value); }

            /// <summary>
            /// Enable Pipe
            /// </summary>
            public bool Enable { get => _nrf.ReadRxPipe(_pipeID); set => _nrf.SetRxPipe(_pipeID, value); }
        }

        #endregion Classes
    }
}