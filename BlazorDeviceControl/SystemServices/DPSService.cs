using DeviceManager;
using System.ComponentModel;
using System.IO.Ports;
using log4net;
using ServiceExtensions;
namespace BlazorDeviceControl.SystemServices
{
    public class DPSService
    {
        private static ILog log = LogManager.GetLogger(typeof(DPSService));

        DPS5005Device? controlledDevice;

        public event Action? PropertiesUpdated;

        public DPSService()
        {

        }

        public bool Running { get; set; }

        public void Start()
        {
            log.InfoDetail("Device start requested");
            string? targetDev = Environment.GetEnvironmentVariable("BLAZOR_SERIAL_DPS");
            if(string.IsNullOrEmpty(targetDev) ) {
                log.ErrorDetail("BLAZOR_SERIAL_DPS not defined");
            }
            else
            {
                log.InfoDetail("Device found: " + targetDev);
                SerialPort loadedPort = new SerialPort(targetDev);
                this.controlledDevice = new DPS5005Device(loadedPort);
                this.controlledDevice.PropertiesUpdated += this.ChildDevicePropertiesUpdated;
                log.InfoDetail("Device Intantiated");
            }
        }

        private void ChildDevicePropertiesUpdated()
        {
            this.PropertiesUpdated?.Invoke();
        }

        public DPS5005Device? Device
        {
            get => this.controlledDevice;
            set
            {

                this.controlledDevice = value;
                this.PropertiesUpdated?.Invoke();
            }
        }
    }
}
