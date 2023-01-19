using DeviceManager;
using System.ComponentModel;
using System.IO.Ports;
namespace BlazorDeviceControl.SystemServices
{
    public class DPSService : ISystemService
    {
        DPS5005Device? controlledDevice;
        CancellationTokenSource? cts;
        Task? deviceTask;

        public event Action? PropertiesUpdated;

        public DPSService()
        {

        }

        public bool Running { get; set; }

        public void Start()
        {
            this.Running = true;
            this.PropertiesUpdated?.Invoke();
            return;
            this.cts = new CancellationTokenSource();
            string? targetDev = Environment.GetEnvironmentVariable("BLAZOR_SERIAL_DPS");
            if(string.IsNullOrEmpty(targetDev) ) { 

            }
            else
            {
                SerialPort loadedPort = new SerialPort(targetDev);
                this.controlledDevice = new DPS5005Device(loadedPort);

                this.deviceTask = Task.Run(async () => {
                    var activeToken = this.cts.Token;
                    while (!activeToken.IsCancellationRequested)
                    {
                        this.controlledDevice.UpdateDeviceState();
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                
                });

                this.Running = true;
            this.PropertiesUpdated?.Invoke();
            }
        }

        public void Stop()
        {
            this.Running = false;
            this.PropertiesUpdated?.Invoke();
            return;

            if (this.Running)
            {
                cts?.Cancel();
                this.cts = null;
                this.deviceTask?.Wait();
                this.Running = false;
            this.PropertiesUpdated?.Invoke();
            }
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
