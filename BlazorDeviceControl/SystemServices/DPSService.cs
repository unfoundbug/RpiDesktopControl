﻿using DeviceManager;
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
        CancellationTokenSource? cts;
        Task? deviceTask;

        public event Action? PropertiesUpdated;

        public DPSService()
        {

        }

        public bool Running { get; set; }

        public void Start()
        {
            log.InfoDetail("Device start requested");

            this.cts = new CancellationTokenSource();
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

                this.deviceTask = Task.Run(async () => {
                    var activeToken = this.cts.Token;
                    log.InfoDetail("Device open");
                    this.controlledDevice.Open();
                    log.InfoDetail("Device start polling");
                    while (!activeToken.IsCancellationRequested)
                    {
                        this.controlledDevice.UpdateDeviceState();
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                    log.InfoDetail("Device done polling");
                });

                this.Running = true;
                this.PropertiesUpdated?.Invoke();
            }
        }

        public void Stop()
        {

            log.InfoDetail("Device stop requested");
            if (this.Running)
            {
                log.InfoDetail("Device stop starting");
                cts?.Cancel();
                this.cts = null;
                this.deviceTask?.Wait();
                this.controlledDevice.Close();
                this.controlledDevice.PropertiesUpdated -= this.ChildDevicePropertiesUpdated;
                this.controlledDevice = null;
                this.Running = false;
                this.PropertiesUpdated?.Invoke();
            }
            log.InfoDetail("Device stop done");
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
