using LagoVista.Client.Core.ViewModels;
using LagoVista.Core.Commanding;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.ViewModels
{
    public class MainViewModel : AppViewModelBase
    {

        public MainViewModel(ISerialTelemetryLink telemeteryLink)
        {
            TelemetryLink = telemeteryLink;
            TelemetryLink.MessageParsed += _telemeteryLink_MessageParsed;        
            OpenSerialPortCommand = new RelayCommand(HandleConnectClick, CanPressConnect);
            Title = "Kevin";
        }

        private void _telemeteryLink_MessageParsed(object sender, MAVLinkMessage e)
        {
         
        }

        public bool CanPressConnect()
        {
            return !TelemetryLink.IsConected;
        }

        public async override Task InitAsync()
        {
            Ports = await DeviceManager.GetSerialPortsAsync();
            await base.InitAsync();
        }

        public void HandleConnectClick()
        {
            if (TelemetryLink.IsConected)
            {
                CloseSerialPort();
            }
            else
            {
                OpenSerialPort();                
            }
        }

        public async void OpenSerialPort()
        {
            SelectedPort.BaudRate = 57600;
            await TelemetryLink.OpenAsync(SelectedPort);
            ConnectMessage = "Disconnect";
            OpenSerialPortCommand.RaiseCanExecuteChanged();
        }

        public async void CloseSerialPort()
        {
            await TelemetryLink.CloseAsync();
            ConnectMessage = "Connect";
            OpenSerialPortCommand.RaiseCanExecuteChanged();
        }       

        private SerialPortInfo _serialPortInfo;
        public SerialPortInfo SelectedPort
        {
            get { return _serialPortInfo; }
            set
            {
                Set(ref _serialPortInfo, value);
                OpenSerialPortCommand.RaiseCanExecuteChanged();
            }
        }

        private String _connectMessage = "Connect";
        public String ConnectMessage
        {
            get { return _connectMessage; }
            set { Set(ref _connectMessage, value); }
        }

        IEnumerable<SerialPortInfo> _ports;
        public IEnumerable<SerialPortInfo> Ports
        {
            get { return _ports; }
            set { Set(ref _ports, value); }
        }


        public String Title
        { get; set; }

        public RelayCommand OpenSerialPortCommand { get; }

        public ISerialTelemetryLink TelemetryLink { get; }
    }
}
