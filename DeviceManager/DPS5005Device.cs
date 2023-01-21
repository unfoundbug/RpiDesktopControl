using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using System.ComponentModel;
using log4net;
using ServiceExtensions;
using Stateless;

namespace DeviceManager
{
    public class DPS5005Device
    {
        private ILog log = LogManager.GetLogger(nameof(DPS5005Device));

        private StateMachine<States, Transition> stateMachine;
        private SerialPort commsPort;
        private IModbusSerialMaster? deviceMaster;
        private ushort[] latestState;


        public DPS5005Device(SerialPort commsPort)
        {
            commsPort.BaudRate = 19200;
            commsPort.StopBits = StopBits.One;
            commsPort.Parity = Parity.None;
            commsPort.ReadTimeout = 2000;
            this.commsPort = commsPort;
            latestState = new ushort[(int)Device_State_Offset.Product_Firmware];

            this.stateMachine = new StateMachine<States, Transition>(States.Startup);

            this.stateMachine.Configure(States.Startup)
                .Permit(Transition.Start, States.Idle);

            this.stateMachine.Configure(States.Idle)
                .Permit(Transition.Connected, States.Connected)
                .OnEntry(async () =>
                {
                    log.InfoDetail("Starting DPS handling");
                    while (!this.commsPort.IsOpen)
                    {
                        try
                        {
                            this.log.InfoDetail("Device about to open");
                            this.commsPort.Open();
                            this.log.InfoDetail("Device open");
                            var factory = new ModbusFactory();
                            var newMaster = factory.CreateRtuMaster(commsPort);
                            this.deviceMaster = newMaster;
                            this.log.InfoDetail("Master Established");
                            this.stateMachine.Fire(Transition.Connected);
                            break;
                        }
                        catch (Exception commsFault)
                        {
                            this.log.Error("Unable to open serial port", commsFault);
                            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                        }
                    }
                });

            this.stateMachine.Configure(States.Connected)
                .Permit(Transition.Disconnect, States.Idle)
                .OnEntry(async () => {
                    this.log.InfoDetail("DPS Connected");
                    while (true)
                    {
                        try
                        {
                            this.UpdateDeviceState();

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

        public event Action? PropertiesUpdated;

        public void UpdateDeviceState()
        {
            try
            {
                var newState = deviceMaster.ReadHoldingRegisters(1, 0, 0x0c);
                if (newState != this.latestState)
                {
                    this.latestState = newState;
                    this.PropertiesUpdated?.Invoke();
                }
            } 
            catch(InvalidOperationException io)
            {
                log.Info("Port closed while read pending: " + io.Message);
            }
            catch(Exception ex)
            {
                this.log.ErrorDetail("Error handled reading DPS device: " + ex.Message, ex);
            }
        }

        private void Close()
        {
            this.deviceMaster = null;
            this.log.InfoDetail("Device about to close");
            this.commsPort.Close();
            this.log.InfoDetail("Device close");
        }


        public float VoltageSetting { get; set; }

        public float CurrentSetting { get; set; }

        public float Output_Voltage => this.latestState[(int)Device_State_Offset.Output_Voltage] / 100.0f;

        public float Output_Current => this.latestState[(int)Device_State_Offset.Output_Current] / 1000.0f;
        public float Output_Power => this.latestState[(int)Device_State_Offset.Output_Power] / 100.0f;
        public float Input_Voltage => this.latestState[(int)Device_State_Offset.Input_Voltage] / 100.0f;
        public bool Key_Lock => this.latestState[(int)Device_State_Offset.Key_Lock] == 0;
        public Protection_State Protection_Status => (Protection_State)this.latestState[(int)Device_State_Offset.Output_Voltage];
        public bool IsConstantCurrent => this.latestState[(int)Device_State_Offset.CVCC_State] == 0;
        public bool IsConstantVoltage => this.latestState[(int)Device_State_Offset.CVCC_State] == 1;

        public bool Output_State => this.latestState[(int)Device_State_Offset.Output_State] == 1;
        public bool Enable_Output { get; set; }
        public int Backlight_Brightness => this.latestState[(int)Device_State_Offset.CVCC_State];
        public int Product_Model => this.latestState[(int)Device_State_Offset.Product_Model];
        public int Product_Firmware => this.latestState[(int)Device_State_Offset.Product_Firmware];

        public bool IsOpen => this.commsPort.IsOpen;


        public enum Protection_State
        {
            Good,

            Over_Voltage,

            Over_Current,

            Over_Power
        }

        private enum Device_State_Offset
        {
            Voltage_Setting = 0,

            Current_Setting = 1,

            Output_Voltage = 2,

            Output_Current = 3,

            Output_Power = 4,

            Input_Voltage = 5,

            Key_Lock = 6,

            Protection_Status = 7,

            CVCC_State = 8,

            Output_State = 9,

            Backlight_Brightness = 10,

            Product_Model = 11,

            Product_Firmware = 12,
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
