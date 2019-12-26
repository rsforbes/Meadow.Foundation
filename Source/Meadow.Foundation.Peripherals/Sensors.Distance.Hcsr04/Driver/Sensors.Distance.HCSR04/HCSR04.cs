using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Distance;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Meadow.Foundation.Sensors.Distance
{
    /// <summary>
    /// HCSR04 Distance Sensor
    /// </summary>
    public class Hcsr04 : IRangeFinder
    {
        #region Properties

        /// <summary>
        /// Returns current distance detected in cm.
        /// </summary>
        public float CurrentDistance { get; private set; } = -1;

        /// <summary>
        /// Minimum valid distance in cm (CurrentDistance returns -1 if below).
        /// </summary>
        public float MinimumDistance => 2;

        /// <summary>
        /// Maximum valid distance in cm (CurrentDistance returns -1 if above).
        /// </summary>
        public float MaximumDistance => 400;

        /// <summary>
        /// Raised when an received a rebound trigger signal
        /// </summary>
        public event EventHandler<DistanceEventArgs> DistanceDetected = delegate { };

        #endregion

        #region Member variables / fields

        /// <summary>
        /// Trigger Pin.
        /// </summary>
        protected IDigitalOutputPort triggerPort;

        /// <summary>
        /// Echo Pin.
        /// </summary>
        protected IDigitalInputPort echoPort;

        protected long tickStart;

        private bool _directRegisterAccess = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor is private to prevent it being called.
        /// </summary>
        private Hcsr04() 
        {
            // *HACK HACK HACK* this is STM32F7 Meadow-only
            // how fast do we run
            var sw = new Stopwatch();
            var et = Environment.TickCount;
            sw.Start();
            TestMethod();
            sw.Stop();
            et = Environment.TickCount - et;
            Console.WriteLine($"Method call took {sw.ElapsedMilliseconds}ms ({et} ticks)");
            if(sw.ElapsedMilliseconds > 1)
            {
                Console.WriteLine($"using direct register access");
                _directRegisterAccess = true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int TestMethod()
        {
            return 0;
        }

        /// <summary>
        /// Create a new HCSR04 object with an IO Device
        /// </summary>
        /// <param name="triggerPin"></param>
        /// <param name="echoPin"></param>
        public Hcsr04(IIODevice device, IPin triggerPin, IPin echoPin) :
            this (device.CreateDigitalOutputPort(triggerPin, false), 
                  device.CreateDigitalInputPort(echoPin, InterruptMode.EdgeBoth)) { }

        /// <summary>
        /// Create a new HCSR04 object 
        /// </summary>
        /// <param name="triggerPort"></param>
        /// <param name="echoPort"></param>
        public Hcsr04(IDigitalOutputPort triggerPort, IDigitalInputPort echoPort)
            : this()
        {
            this.triggerPort = triggerPort;

            this.echoPort = echoPort;
            this.echoPort.Changed += OnEchoPortChanged;
        }

        #endregion

        /// <summary>
        /// Sends a trigger signal
        /// </summary>
        public void MeasureDistance()
        {
            Console.WriteLine($"Measuring distance...");

            CurrentDistance = -1;

            if (_directRegisterAccess)
            {
                CurrentDistance = MeasureDistanceUsingRegisterAccess();
            }
            else
            {
                CurrentDistance = MeasureDistanceUsingObjectModel();
            }
        }

        private float MeasureDistanceUsingObjectModel()
        {
            // Raise trigger port to high for 10+ micro-seconds
            triggerPort.State = true;
            Thread.Sleep(1); //smallest amount of time we can wait

            // Start Clock
            tickStart = DateTime.Now.Ticks;
            // Trigger device to measure distance via sonic pulse
            triggerPort.State = false;

            throw new NotImplementedException();
        }

        const uint GPIOB_IDR = 0x40020410;
        const uint GPIOB_ODR = 0x40020414;
        const uint GPIOB_BSSR = 0x40020418;

        private unsafe float MeasureDistanceUsingRegisterAccess()
        {
            Console.WriteLine($"Measuring distance RA...");

            // D03 is F7 PB8
            // D04 is F7 PB9
            uint highMask8 = 1 << 8;
            uint highMask9 = 1 << 9;
            uint lowMask9 = ~highMask9;
            int start = 0;
            int end = 0;

            // Raise trigger port to high for 10+ micro-seconds
            *(uint*)GPIOB_BSSR = (1 << 8);
            Thread.Sleep(1); //smallest amount of time we can wait

            var timeoutA = Environment.TickCount + 2000;
            var timeoutB = Environment.TickCount + 4000;

            // Trigger device by setting the output low
            *(uint*)GPIOB_BSSR = (1 << 24);

            // wait for the input to go high - that's the start of the clock
            while ((*(uint*)GPIOB_IDR & highMask8) == 0)
            {
                start = Environment.TickCount;
                if(start > timeoutA)
                {
                    Console.WriteLine("Timeout waiting for high");
                    return -1f;
                }
            }

            // wait for the input to go low, that's the end
            while ((*(uint*)GPIOB_ODR & highMask9) != 0)
            {
                end = Environment.TickCount;
                if (start > timeoutB)
                {
                    Console.WriteLine("Timeout waiting for low");
                    return -1f;
                }
            }
            var et = end - start;
            Console.WriteLine($"Took {et} ticks");

            // value will be:
            //  ET (in seconds) * 34300 (cm/s for sound) / 2 (round trip of wave)

            return 0.0f;
        }

        private void OnEchoPortChanged(object sender, DigitalInputPortEventArgs e)
        {
            if (e.Value == true)
          // if(_echoPort.State == true)
            {
          ///      Console.WriteLine("true");
                tickStart = DateTime.Now.Ticks;
                return;
            }

        //    Console.WriteLine("false");

            // Calculate Difference
            float elapsed = DateTime.Now.Ticks - tickStart;

            // Return elapsed ticks
            // x10 for ticks to micro sec
            // divide by 58 for cm (assume speed of sound is 340m/s)
            CurrentDistance = elapsed / 580f;

        //    if (CurrentDistance < MinimumDistance || CurrentDistance > MaximumDistance)
        //       CurrentDistance = -1;

            DistanceDetected?.Invoke(this, new DistanceEventArgs(CurrentDistance));
        }
    }
}