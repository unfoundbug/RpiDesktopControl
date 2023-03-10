using log4net;
using NModbus;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceExtensions;
using Stateless;
using System.Configuration;

namespace DeviceManager
{
    public class LoadDevice
    {
        private ILog log = LogManager.GetLogger(typeof(LoadDevice));

        private StateMachine<States, Transition> stateMachine;

        private SerialPort commsPort;
        
        private bool outputEnabled;
        private float targetCurrent;
        private float lvp;
        private float ovp;
        private float ocp;
        private float opp;
        private float oah;
        private TimeSpan ohp;

        private float loadVoltage = 0;
        private float loadCurrent = 0;
        private float capacityDrained = 0;
        private TimeSpan activeTimer = TimeSpan.MinValue;

        public LoadDevice(SerialPort commsPort)
        {
            commsPort.BaudRate = 9600;
            commsPort.StopBits = StopBits.One;
            commsPort.Parity = Parity.None;
            commsPort.ReadTimeout = 2000;
            this.commsPort = commsPort;

            this.stateMachine = new StateMachine<States, Transition>(States.Startup);
            this.stateMachine.Configure(States.Startup)
                .Permit(Transition.Start, States.Idle);

            this.stateMachine.Configure(States.Idle)
                .Permit(Transition.Connected, States.Connected)
                .OnEntry(async () =>
                {
                    log.InfoDetail("Starting Load handling");
                    while(!this.commsPort.IsOpen) 
                    {
                        try
                        {
                            this.commsPort.Open();
                            log.InfoDetail("Load Port open");
                            this.stateMachine.Fire(Transition.Connected);
                            break;
                        } catch (Exception ex) 
                        { 
                            this.log.Error("Unable to open serial port", ex);
                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                });

            this.stateMachine.Configure(States.Connected)
                .Permit(Transition.Disconnect, States.Idle)
                .OnEntry(async () =>
                {
                    // Read initial state
                    log.InfoDetail("Disable Output drive");
                    this.ActionDisable();

                    log.InfoDetail("Try Stop");
                    this.ActionStop();

                    log.InfoDetail("Read Params");
                    this.ActionRead();

                    log.InfoDetail("Start Monitoring");
                    this.ActionStart();

                    log.InfoDetail("Device running");
                    while (true)
                    {
                        try
                        {
                            var dataLine = this.commsPort.ReadLine();
                            var vals = dataLine.Split(',');
                            if (vals.Length == 4)
                            {
                                this.loadVoltage = float.Parse(vals[0].TrimEnd('V'));
                                this.loadCurrent = float.Parse(vals[1].TrimEnd('A'));
                                this.capacityDrained = float.Parse(vals[2].TrimEnd(new char[] { 'A', 'h' }));
                                var tsComp = vals[3].Split(':');
                                this.activeTimer = new TimeSpan(int.Parse(tsComp[0]), int.Parse(tsComp[1]), 0);
                                this.PropertiesUpdated?.Invoke();
                            }
                            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            this.stateMachine.Fire(Transition.Disconnect);
                            break;
                        }
                    }
                });
            this.stateMachine.Fire(Transition.Start);
            log.InfoDetail("Device State machine fired");
        }

        public event Action PropertiesUpdated;

        public float LoadVoltage => this.loadVoltage;

        public float LoadCurrent => this.loadCurrent;

        public float CapacityDrained => this.capacityDrained;

        public TimeSpan ActiveTimer => this.activeTimer;


        public bool IsEnabled
        {
            get => this.outputEnabled;
            set
            {
                if (value)
                {
                    this.ActionEnable();
                }
                else
                {
                    this.ActionDisable();
                }
            }
        }

        public float TargetCurrent
        {
            get => this.targetCurrent;
            set => this.SetCurrent(value);
        }

        public float LVP
        {
            get => (float)this.lvp;
            set => this.SetLVP(value);
        }

        public float OVP
        {
            get => this.ovp;
            set => this.SetOVP(value);
        }

        public float OCP
        {
            get => this.ocp;
            set => this.SetOCP(value);
        }

        public float OPP
        {
            get => this.opp;
            set => this.SetOPP(value);
        }

        public float OAH
        {
            get => this.oah;
            set => this.SetOAH(value);
        }
        public TimeSpan OHP
        {
            get => this.ohp;
            set => this.SetOHP(value);
        }

        private void ActionRead()
        {
            this.commsPort.WriteLine("read");
            var sourceData = this.commsPort.ReadLine();
            var entries = sourceData.Split(',');
            foreach ( var entry in entries ) {
                var comp = entry.Split(':');
                if(comp.Length == 3)
                {
                    // must be OHP
                    this.ohp = new TimeSpan(0, int.Parse(comp[1]), int.Parse(comp[2]));
                }
                else
                {
                    switch (comp[0])
                    {
                        case "OVP":
                            this.ovp = float.Parse(comp[1]);
                            break;
                        case "OCP":
                            this.ocp = float.Parse(comp[1]);
                            break;
                        case "OPP":
                            this.opp = float.Parse(comp[1]);
                            break;
                        case "LVP":
                            this.lvp = float.Parse(comp[1]);
                            break;
                        case "OAH":
                            this.oah = float.Parse(comp[1]);
                            break;
                    }
                }
            }
        }

        private void ActionEnable()
        {
            this.DoCmd("on");
            this.outputEnabled = true;
            this.PropertiesUpdated?.Invoke();
        }

        private void ActionDisable()
        {
            this.DoCmd("off");
            this.outputEnabled = false;
            this.PropertiesUpdated?.Invoke();
        }

        private void SetCurrent(float newValue)
        {
            float newResult = float.Min(newValue, 0);
            newResult = float.Max(newValue, 3.0f);
            this.DoCmd($"{newResult.ToString("0.00")}A");
        }

        private void SetLVP(float voltage)
        {
            float newResult = float.Min(voltage, 0);
            newResult = float.Max(voltage, 15.0f);
            if (newResult != this.lvp)
            {
                this.lvp = newResult;
                this.PropertiesUpdated?.Invoke();
                this.DoCmd($"LVP:{newResult.ToString("00.0")}");
            }
        }

        private void SetOVP(float voltage)
        {
            float newResult = float.Min(voltage, 0);
            newResult = float.Max(voltage, 15.0f);
            if (newResult != this.ovp)
            {
                this.ovp = newResult;
                this.PropertiesUpdated?.Invoke();
                this.DoCmd($"OVP:{newResult.ToString("00.0")}");
            }
        }

        private void SetOCP(float voltage)
        {
            float newResult = float.Min(voltage, 0);
            newResult = float.Max(voltage, 9.99f);
            if (newResult != this.ocp)
            {
                this.ocp = newResult;
                this.PropertiesUpdated?.Invoke();
                this.DoCmd($"OCP:{newResult.ToString("0.00")}");
            }
        }

        private void SetOPP(float voltage)
        {
            float newResult = float.Min(voltage, 0);
            newResult = float.Max(voltage, 15.0f);
            if (newResult != this.opp)
            {
                this.opp = newResult;
                this.PropertiesUpdated?.Invoke();
                this.DoCmd($"OPP:{newResult.ToString("00.00")}");
            }
        }

        private void SetOAH(float voltage)
        {
            float newResult = float.Min(voltage, 0);
            newResult = float.Max(voltage, 15.0f);
            if (newResult != this.oah)
            {
                this.oah = newResult;
                this.PropertiesUpdated?.Invoke();
                this.DoCmd($"OAH:{newResult.ToString("0.000")}");
            }
        }

        private void SetOHP(TimeSpan timeout)
        {
            this.ohp = timeout;
            this.PropertiesUpdated?.Invoke();
            this.DoCmd($"OHP:{timeout.Hours}:{timeout.Minutes}");
        }

        private void ActionStart()
        {
            this.DoCmd("start");
        }

        private void ActionStop()
        {
            this.DoCmd("stop");
        }

        private void DoCmd(string cmd)
        {
            string response = string.Empty;
            this.commsPort.WriteLine(cmd);
            try
            {
                response = this.commsPort.ReadLine();
            }
            catch { }

            this.log.InfoDetail($"Sent: {cmd}");
            this.log.InfoDetail($"Recv: {response}");
        }

        private bool Open()
        {
            try
            {
                this.log.InfoDetail("Device about to open");
                this.commsPort.Open();
                this.log.InfoDetail("Device open");

                return true;
            }
            catch (Exception commsFault)
            {
                this.log.Error("Unable to open serial port", commsFault);
                return false;
            }
        }

        private void Close()
        {
            this.log.InfoDetail("Device about to close");
            this.commsPort.Close();
            this.log.InfoDetail("Device close");
        }

        private void UpdateDeviceState()
        {
            throw new NotImplementedException();
        }

        private enum States
        {
            Startup,

            Idle,

            Connected,
        }

        private enum Transition
        {
            Start,

            Connected,

            Disconnect
        }
    }
}
