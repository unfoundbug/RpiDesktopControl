using DeviceManager;
using System.ComponentModel;
using System.IO.Ports;
namespace BlazorDeviceControl.SystemServices
{
    public class DPSService : ISystemService, INotifyPropertyChanged
    {
        DPS5005Device? controlledDevice;
        CancellationTokenSource? cts;
        Task? deviceTask;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DPSService()
        {

        }

        public bool Running { get; set; }

        public void Start()
        {
            this.Running = true;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Running)));
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
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Running)));
            }
        }

        public void Stop()
        {
            this.Running = false;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Running)));
            return;

            if (this.Running)
            {
                cts?.Cancel();
                this.cts = null;
                this.deviceTask?.Wait();
                this.Running = false;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Running)));
            }
        }

        public DPS5005Device? Device
        {
            get => this.controlledDevice;
            set
            {

                this.controlledDevice = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Device)));
            }
        }
    }
}
