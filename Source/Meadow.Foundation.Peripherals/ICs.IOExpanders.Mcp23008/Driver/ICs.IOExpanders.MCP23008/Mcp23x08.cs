using System;
using Meadow.Hardware;
using Meadow.Utilities;

namespace Meadow.Foundation.ICs.IOExpanders
{
    /// <summary>
    /// Provide an interface to connect to a MCP23008 port expander.
    /// 
    /// Note: this class is not yet implemented.
    /// </summary>
    public partial class Mcp23x08 : IIODevice
    {
        /// <summary>
        ///     Raised on Interrupt
        /// </summary>
        //public event EventHandler InterruptRaised = delegate { }; //ToDo - is this being used??

        private readonly II2cPeripheral _i2cPeripheral;

        public PinDefinitions Pins { get; } = new PinDefinitions();

        // state
        byte _iodir;
        byte _gpio;
        byte _olat;
        byte _gppu;

        /// <summary>
        ///     object for using lock() to do thread synch
        /// </summary>
        protected object _lock = new object();

        public DeviceCapabilities Capabilities => throw new NotImplementedException();

        protected Mcp23x08()
        { }

        /// <summary>
        /// Instantiates an Mcp23008 on the specified I2C bus using the appropriate
        /// peripheral address based on the pin settings. Use this method if you
        /// don't want to calculate the address.
        /// </summary>
        /// <param name="i2cBus"></param>
        /// <param name="pinA0"></param>
        /// <param name="pinA1"></param>
        /// <param name="pinA2"></param>
        public Mcp23x08(II2cBus i2cBus, bool pinA0, bool pinA1, bool pinA2)
            : this(i2cBus, McpAddressTable.GetAddressFromPins(pinA0, pinA1, pinA2))
        {
            // nothing goes here
        }

        /// <summary>
        /// Instantiates an Mcp23008 on the specified I2C bus, with the specified
        /// peripheral address.
        /// </summary>
        /// <param name="i2cBus"></param>
        /// <param name="address"></param>
        public Mcp23x08(II2cBus i2cBus, byte address = 0x20)
        {
            // tried this, based on a forum post, but seems to have no effect.
            //H.OutputPort SDA = new H.OutputPort(N.Pins.GPIO_PIN_A4, false);
            //H.OutputPort SCK = new H.OutputPort(N.Pins.GPIO_PIN_A5, false);
            //SDA.Dispose();
            //SCK.Dispose();

            // configure our i2c bus so we can talk to the chip
            _i2cPeripheral = new I2cPeripheral(i2cBus, address);

            Console.WriteLine("initialized.");

            Initialize();

            Console.WriteLine("Chip Reset.");

        } 

        /// <summary>
        /// Initializes the chip for use:
        ///  * Puts all IOs into an input state
        ///  * zeros out all setting and state registers
        /// </summary>
        protected void Initialize()
        {
            byte[] buffers = new byte[10];

            // IO Direction
            buffers[0] = 0xFF; //all input `11111111`

            // set all the other registers to zeros (we skip the last one, output latch)
            for (int i = 1; i < 10; i++ ) {
                buffers[i] = 0x00; //all zero'd out `00000000`
            }

            // the chip will automatically write all registers sequentially.
            _i2cPeripheral.WriteRegisters(RegisterAddresses.IODirectionRegister, buffers);

            // save our state
            _iodir = buffers[0];
            _gpio = 0x00;
            _olat = 0x00;
            _gppu = 0x00;

            // read in the initial state of the chip
            _iodir = _i2cPeripheral.ReadRegister(RegisterAddresses.IODirectionRegister);
            // tried some sleeping, but also has no effect on its reliability
            Console.WriteLine("IODIR: " + _iodir.ToString("X"));
            _gpio = _i2cPeripheral.ReadRegister(RegisterAddresses.GPIORegister);
            Console.WriteLine("GPIO: " + _gpio.ToString("X"));
            _olat = _i2cPeripheral.ReadRegister(RegisterAddresses.OutputLatchRegister);
            Console.WriteLine("OLAT: " + _olat.ToString("X"));

        }

        /// <summary>
        /// Creates a new DigitalOutputPort using the specified pin and initial state.
        /// </summary>
        /// <param name="pin">The pin number to create the port on.</param>
        /// <param name="initialState">Whether the pin is initially high or low.</param>
        /// <returns></returns>
        public IDigitalOutputPort CreateDigitalOutputPort(IPin pin, bool initialState = false)
        {
            if (IsValidPin(pin)) {
                // setup the port internally for output
                this.SetPortDirection(pin, PortDirectionType.Output);

                // create the convenience class
                return new DigitalOutputPort(this, pin, initialState);
            }

            throw new Exception("Pin is out of range");
        }

        //public IDigitalInputPort CreateDigitalInputPort(
        //    IPin pin,
        //    InterruptMode interruptMode = InterruptMode.None,
        //    ResistorMode resistorMode = ResistorMode.Disabled,
        //    int debounceDuration = 0,
        //    int glitchFilterCycleCount = 0)
        //{
        //    if (pin != null) {
        //        return device.CreateDigitalInputPort(pin, InterruptMode.None, ResistorMode.PullUp);
        //    }

        //    throw new Exception("Pin is out of range");
        //}

        /// <summary>
        /// Sets the direction of a particulare port.
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="direction"></param>
        public void SetPortDirection(IPin pin, PortDirectionType direction)
        {
            if (IsValidPin(pin)) {
                // if it's already configured, get out. (1 = input, 0 = output)
                if (direction == PortDirectionType.Input) {
                    if (BitHelpers.GetBitValue(_iodir, (byte)pin.Key)) return;
                    //if ((_iodir & (byte)(1 << pin)) != 0) return;
                } else {
                    if (!BitHelpers.GetBitValue(_iodir, (byte)pin.Key)) return;
                    //if ((_iodir & (byte)(1 << pin)) == 0) return;
                }

                // set the IODIR bit and write the setting
                _iodir = BitHelpers.SetBit(_iodir, (byte)pin.Key, (byte)direction);
                _i2cPeripheral.WriteRegister(RegisterAddresses.IODirectionRegister, _iodir);
            }
            else {
                throw new Exception("Pin is out of range");
            }
        }

        //public void ConfigureInputPort(byte pin, bool enablePullUp = false, bool enableInterrupt = true)
        //{
        //    if (IsValidPin(pin))
        //    {
        //        // set the port direction
        //        this.SetPortDirection(pin, PortDirectionType.Input);

        //        // refresh out pull up state
        //        // TODO: do away with this and trust internal state?
        //        _gppu = _i2cPeripheral.ReadRegister(_PullupResistorConfigurationRegister);

        //        _gppu = BitHelpers.SetBit(_gppu, pin, enablePullUp);

        //        _i2cPeripheral.WriteRegister(_PullupResistorConfigurationRegister, _gppu);

        //        if (enableInterrupt)
        //        {
        //            // interrupt on change (whether or not we want to raise an interrupt on the interrupt pin on change)
        //            byte gpinten = _i2cPeripheral.ReadRegister(_InterruptOnChangeRegister);
        //            gpinten = BitHelpers.SetBit(gpinten, pin, true);

        //            // interrupt control register; whether or not the change is based 
        //            // on default comparison value, or if a change from previous. We 
        //            // want to raise on change, so we set it to 0, always.
        //            byte interruptControl = _i2cPeripheral.ReadRegister(_InterruptControlRegister);
        //            interruptControl = BitHelpers.SetBit(interruptControl, pin, false);
        //        }
        //    }
        //    else
        //    {
        //        throw new Exception("Pin is out of range");
        //    }
        //}

        /// <summary>
        /// Sets a particular pin's value. If that pin is not 
        /// in output mode, this method will first set its 
        /// mode to output.
        /// </summary>
        /// <param name="pin">The pin to write to.</param>
        /// <param name="value">The value to write. True for high, false for low.</param>
        public void WriteToPort(IPin pin, bool value)
        {
            if (IsValidPin(pin)) {
                // if the pin isn't configured for output, configure it
                this.SetPortDirection(pin, PortDirectionType.Output);

                // update our output latch 
                _olat = BitHelpers.SetBit(_olat, (byte)pin.Key, value);

                // write to the output latch (actually does the output setting)
                _i2cPeripheral.WriteRegister(RegisterAddresses.OutputLatchRegister, _olat);
            }
            else {
                throw new Exception("Pin is out of range");
            }
        }

        //public bool ReadPort(byte pin)
        //{
        //    if (IsValidPin(pin))
        //    {
        //        // if the pin isn't set for input, configure it
        //        this.SetPortDirection((byte)pin, PortDirectionType.Input);

        //        // update our GPIO values
        //        _gpio = _i2cPeripheral.ReadRegister(_GPIORegister);

        //        // return the value on that port
        //        return BitHelpers.GetBitValue(_gpio, (byte)pin);
        //    }

        //    throw new Exception("Pin is out of range");
        //}

        /// <summary>
        /// Outputs a byte value across all of the pins by writing directly 
        /// to the output latch (OLAT) register.
        /// </summary>
        /// <param name="mask"></param>
        public void WriteToPorts(byte mask)
        {
            // set all IO to output
            if (_iodir != 0) {
                _iodir = 0;
                _i2cPeripheral.WriteRegister(RegisterAddresses.IODirectionRegister, _iodir);
            }
            // write the output
            _olat = mask;
            _i2cPeripheral.WriteRegister(RegisterAddresses.OutputLatchRegister, _olat);
        }

        protected bool IsValidPin(IPin pin)
        {
            var contains = this.Pins.AllPins.Contains(pin);
            return (this.Pins.AllPins.Contains(pin));
        }

        protected void ResetPin(IPin pin)
        {
            this.SetPortDirection(pin, PortDirectionType.Input);
        }

        public IBiDirectionalPort CreateBiDirectionalPort(IPin pin, bool initialState = false, bool glitchFilter = false, InterruptMode interruptMode = InterruptMode.None, ResistorMode resistorMode = ResistorMode.Disabled, PortDirectionType initialDirection = PortDirectionType.Input)
        {
            throw new NotImplementedException();
        }

        public IAnalogInputPort CreateAnalogInputPort(IPin pin, float voltageReference = 3.3F)
        {
            throw new NotImplementedException();
        }

        public IPwmPort CreatePwmPort(IPin pin, float frequency = 100, float dutyCycle = 0)
        {
            throw new NotImplementedException();
        }

        public ISerialPort CreateSerialPort(SerialPortName portName, int baudRate, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, int readBufferSize = 4096)
        {
            throw new NotImplementedException();
        }

        public ISpiBus CreateSpiBus(IPin[] pins, long speed)
        {
            throw new NotImplementedException();
        }

        public ISpiBus CreateSpiBus(IPin clock, IPin mosi, IPin miso, long speed)
        {
            throw new NotImplementedException();
        }

        public II2cBus CreateI2cBus(IPin[] pins, ushort speed = 100)
        {
            throw new NotImplementedException();
        }

        public II2cBus CreateI2cBus(IPin clock, IPin data, ushort speed = 100)
        {
            throw new NotImplementedException();
        }

        public IDigitalInputPort CreateDigitalInputPort(IPin pin, InterruptMode interruptMode = InterruptMode.None, ResistorMode resistorMode = ResistorMode.Disabled, int debounceDuration = 0, int glitchFilterCycleCount = 0)
        {
            throw new NotImplementedException();
        }

    }
}