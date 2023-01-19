using DeviceManager;
using System.ComponentModel;
using System.IO.Ports;
using log4net;
using ServiceExtensions;
namespace BlazorDeviceControl.SystemServices
{
    public class FZ35Service
    {
        private static ILog log = LogManager.GetLogger(typeof(DPSService));

        LoadDevice? controlledDevice;
        CancellationTokenSource? cts;
        Task? deviceTask;

        public event Action? PropertiesUpdated;

        public FZ35Service()
        {

        }

        public bool Running { get; set; }

        public void Start()
        {
            log.InfoDetail("Device start requested");

            this.cts = new CancellationTokenSource();
            string? targetDev = Environment.GetEnvironmentVariable("BLAZOR_SERIAL_LOAD");
            if(string.IsNullOrEmpty(targetDev) ) {
                log.ErrorDetail("BLAZOR_SERIAL_LOAD not defined");
            }
            else
            {
                log.InfoDetail("Device found: " + targetDev);
                SerialPort loadedPort = new SerialPort(targetDev);
                this.controlledDevice = new LoadDevice(loadedPort);
                this.controlledDevice.PropertiesUpdated += this.ChildDevicePropertiesUpdated;
                log.InfoDetail("Device Intantiated");

                this.deviceTask = Task.Run(async () => {
                    var activeToken = this.cts.Token;
                    log.InfoDetail("Device start polling");
                    while (!activeToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                    log.InfoDetail("Device done polling");
                });

                this.Running = true;
                this.PropertiesUpdated?.Invoke();
            }
        }

        private void ChildDevicePropertiesUpdated()
        {
            this.PropertiesUpdated?.Invoke();
        }

        public LoadDevice? Device
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
