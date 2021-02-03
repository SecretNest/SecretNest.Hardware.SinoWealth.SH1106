using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Text;
using System.Threading;

namespace SecretNest.Hardware.SinoWealth
{
    public class SH1106 : IDisposable
    {
        private bool _disposedValue;

        private GpioController _controller;
        private SpiDevice _spiDevice;
        private readonly int _gpioReset, _gpioDc, _gpioCs;
        private const int OledResetSleepTime = 100;
        private const byte PageCommandOffset = 0xb0;
        private const byte LowColumnAddress = 0x02;
        private const byte HighColumnAddress = 0x10;

        public SH1106(GpioController controller, int gpioReset, int gpioDc, int gpioCs, int clockFrequency)
        {
            _controller = controller;
            _gpioReset = gpioReset;
            _gpioDc = gpioDc;
            _gpioCs = gpioCs;

            controller.OpenPin(_gpioReset, PinMode.Output);
            controller.OpenPin(_gpioDc, PinMode.Output);
            controller.OpenPin(_gpioCs, PinMode.Output);
            InitializeHat();

            SpiConnectionSettings spiConnectionSettings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = clockFrequency,
                Mode = SpiMode.Mode0 //0B00
            };

            _spiDevice = SpiDevice.Create(spiConnectionSettings);

            ResetScreen();
            InitializeScreen();
        }

        public void ResetScreen()
        {
            _controller.Write(_gpioReset, PinValue.High);
            Thread.Sleep(OledResetSleepTime);
            _controller.Write(_gpioReset, PinValue.Low);
            Thread.Sleep(OledResetSleepTime);
            _controller.Write(_gpioReset, PinValue.High);
            Thread.Sleep(OledResetSleepTime);
        }

        public void InitializeScreen()
        {
            SendSpiCommands(
                0xae, //turn off oled panel
                0x02, //set low column address
                0x10, //set high column address
                0x40, //set start line address: set mapping RAM display start line (0x00~0x3F)
                0x81, //set contrast control register
                0xa0, //set SEG/Column Mapping
                0xc0, //set COM/Row scan direction
                0xa6, //set normal display
                0xa8, //set multiplex ratio(1 to 64)
                0x3f, //1/64 duty
                0xd3, //set display offset: shift mapping RAM counter (0x00~0x3F)
                0x00, //not offset
                0xd5, //set display clock divide ratio/oscillator frequency
                0x80, //set divide ratio: set clock as 100 frames/sec
                0xd9, //set pre-charge period
                0xf1, //set pre-charge as 15 clocks & discharge as 1 clock
                0xda, //set com pins hardware configuration
                0x12,
                0xdb, //set vcomh
                0x40, //set VCOM deselect level
                0x20, //set page addressing mode (0x00/0x01/0x02)
                0x02,
                0xa4, //disable entire display on (0xa4/0xa5)
                0xa6 //disable inverse display on (0xa6/a7) 
                );
            Thread.Sleep(OledResetSleepTime);
            SendSpiCommand(0xaf); //turn on oled panel
        }

        private void InitializeHat()
        {
            _controller.Write(_gpioCs, PinValue.Low);
            _controller.Write(_gpioDc, PinValue.Low);
        }

        private void CloseHat()
        {
            _controller.Write(_gpioReset, PinValue.Low);
            _controller.Write(_gpioDc, PinValue.Low);
        }

        public void SendSpiCommand(byte command)
        {
            SetGpio25Low();
            _spiDevice.WriteByte(command);
        }

        public void SendSpiCommands(params byte[] command)
        {
            SetGpio25Low();
            _spiDevice.Write(command);
        }

        public void SendSpiCommandData(params byte[] data)
        {
            SetGpio25High();
            SendSpiData(data);
        }

        public void SendSpiCommand(byte command, params byte[] data)
        {
            SendSpiCommand(command);
            SendSpiCommandData(data);
        }

        public void SendSpiData(params byte[] data)
        {
            _spiDevice.Write(data);
        }

        public void SendSpiData(ReadOnlySpan<byte> data)
        {
            _spiDevice.Write(data);
        }

        public void SendSpiDataByte(byte data)
        {
            _spiDevice.WriteByte(data);
        }

        public void SetGpio25High()
        {
            _controller.Write(_gpioDc, PinValue.High);
        }

        public void SetGpio25Low()
        {
            _controller.Write(_gpioDc, PinValue.Low);
        }

        public void ShowPage(int page, ReadOnlySpan<byte> buffer)
        {
            SendSpiCommand((byte) (PageCommandOffset + page));
            SendSpiCommand(LowColumnAddress);
            SendSpiCommand(HighColumnAddress);
            SetGpio25High();
            SendSpiData(buffer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _spiDevice.Dispose();

                    CloseHat();

                    _controller.ClosePin(_gpioReset); 
                    _controller.ClosePin(_gpioDc); 
                    _controller.ClosePin(_gpioCs);
                }

                _spiDevice = null;
                _controller = null;

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
