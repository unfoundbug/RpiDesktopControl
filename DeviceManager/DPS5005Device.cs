﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using System.ComponentModel;

namespace DeviceManager
{
    public class DPS5005Device : INotifyPropertyChanged
    {
        private SerialPort commsPort;
        private IModbusSerialMaster deviceMaster;
        private ushort[] latestState;


        public DPS5005Device(SerialPort commsPort)
        {
            commsPort.BaudRate = 9600;
            commsPort.StopBits = StopBits.One;
            commsPort.Parity = Parity.None;
            this.commsPort = commsPort;
            this.commsPort.Open();
            latestState = new ushort[(int)Device_State_Offset.Product_Firmware];
            var factory = new ModbusFactory();
            var newMaster = factory.CreateRtuMaster(commsPort);
            this.deviceMaster = newMaster;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateDeviceState()
        {
            this.latestState = deviceMaster.ReadHoldingRegisters(0, 0, 0x0c);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
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
        public bool Output_State { get; set; }
        public int Backlight_Brightness => this.latestState[(int)Device_State_Offset.CVCC_State];
        public int Product_Model => this.latestState[(int)Device_State_Offset.Product_Model];
        public int Product_Firmware => this.latestState[(int)Device_State_Offset.Product_Firmware];

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

    }
}