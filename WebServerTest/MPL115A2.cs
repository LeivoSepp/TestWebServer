using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace WebServerTest
{
    class MPL115A2
    {
        // Address Constant
        public const int MPL115A2_ADDRESS = 0x60;
        private const int MPL115A2_REGISTER_STARTCONVERSION = 0x12;
        private const int MPL115A2_REGISTER_PRESSURE_MSB = 0x00;
        private const int MPL115A2_REGISTER_TEMP_MSB = 0x02;
        private const int MPL115A2_REGISTER_A0_COEFF_MSB = 0x04;
        private const int MPL115A2_REGISTER_B1_COEFF_MSB = 0x06;
        private const int MPL115A2_REGISTER_B2_COEFF_MSB = 0x08;
        private const int MPL115A2_REGISTER_C12_COEFF_MSB = 0x0A;

        private double _mpl115a2_a0;
        private double _mpl115a2_b1;
        private double _mpl115a2_b2;
        private double _mpl115a2_c12;

        // I2C Device
        private I2cDevice I2C;
        private int I2C_ADDRESS { get; set; } = MPL115A2_ADDRESS;
        public MPL115A2(int i2cAddress = MPL115A2_ADDRESS)
        {
            I2C_ADDRESS = i2cAddress;
        }
        private void readCoefficients()
        {
            int a0coeff;
            int b1coeff;
            int b2coeff;
            int c12coeff;

            a0coeff = I2CRead16(MPL115A2_REGISTER_A0_COEFF_MSB);
            b1coeff = I2CRead16(MPL115A2_REGISTER_B1_COEFF_MSB);
            b2coeff = I2CRead16(MPL115A2_REGISTER_B2_COEFF_MSB);
            c12coeff = I2CRead16(MPL115A2_REGISTER_C12_COEFF_MSB) >> 2;

            _mpl115a2_a0 = (double)a0coeff / 8;
            _mpl115a2_b1 = (double)b1coeff / 8192;
            _mpl115a2_b2 = (double)b2coeff / 16384;
            _mpl115a2_c12 = (double)c12coeff;
            _mpl115a2_c12 /= (double)4194304.0;
        }
        public static bool IsInitialised { get; private set; } = false;
        private void Initialise()
        {
            if (!IsInitialised)
            {
                EnsureInitializedAsync().Wait();
            }
        }
        private async Task EnsureInitializedAsync()
        {
            if (IsInitialised) { return; }
            try
            {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                settings.SharingMode = I2cSharingMode.Shared;
                string aqs = I2cDevice.GetDeviceSelector("I2C1");         /* Find the selector string for the I2C bus controller */
                var dis = await DeviceInformation.FindAllAsync(aqs);      /* Find the I2C bus controller device with our selector string           */
                I2C = await I2cDevice.FromIdAsync(dis[0].Id, settings);   /* Create an I2cDevice with our selected bus controller and I2C settings */

                readCoefficients();
                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }
        private double getRawTemperature()
        {
            Initialise();
            write8(MPL115A2_REGISTER_STARTCONVERSION, 0x00);
            Task.Delay(3).Wait();
            //read temperature RAW data from device
            return (uint)I2CRead16(MPL115A2_REGISTER_TEMP_MSB) >> 6;
        }
        public double getTemperature()
        {
            double rawTemp = getRawTemperature();
            //return (rawTemp - 498.0) / -5.35 + 25.0;       // C
            //return rawTemp * -0.307 + 234.1 //F
            return rawTemp * -0.1706 + 112.27; //C
        }
        public double getPressure()
        {
            uint pressure;
            double rawTemp = getRawTemperature();
            double pressureComp;
            //read pressure data from device
            pressure = (uint)I2CRead16(MPL115A2_REGISTER_PRESSURE_MSB) >> 6;
            // adding factory correction
            pressureComp = _mpl115a2_a0 + (_mpl115a2_b1 + _mpl115a2_c12 * rawTemp) * pressure + _mpl115a2_b2 * rawTemp;
            //calculate pressure and return pressure data
            return ((65.0 / 1023.0) * pressure) + 50.0;        // kPa
        }
        private void write8(byte addr, byte cmd)
        {
            byte[] Command = new byte[] { (byte)(addr), cmd };
            I2C.Write(Command);
        }
        private ushort I2CRead16(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)(addr) };
            byte[] data = new byte[2];

            I2C.WriteRead(aaddr, data);

            return (ushort)((data[0] << 8) | (data[1]));
        }
    }
}
