﻿using LagoVista.Client.Core.ViewModels;
using LagoVista.Core.Commanding;
using LagoVista.Core.Models;
using LagoVista.Drone;
using LagoVista.Drone.Models;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.ViewModels
{
    public class MainViewModel : AppViewModelBase
    {

        IDrone _apmDrone;
        IDroneAdapter _droneAdapter;
        IMissionPlanner _planner;

        public MainViewModel(ISerialTelemetryLink telemeteryLink, IDroneAdapter droneAdapter, IMissionPlanner planner)
        {
            TelemetryLink = telemeteryLink;
            TelemetryLink.MessageParsed += _telemeteryLink_MessageParsed;        
            OpenSerialPortCommand = new RelayCommand(HandleConnectClick, CanPressConnect);
            GetWaypointsCommand = new RelayCommand(GetWaypoints, CanDoConnectedStuff);

            Title = "Kevin";

            _apmDrone = new LagoVista.Drone.Models.Drone();
            _droneAdapter = droneAdapter;
            _planner = planner;
        }

        private void _telemeteryLink_MessageParsed(object sender, MAVLinkMessage msg)
        {
            _droneAdapter.UpdateDroneAsync(_apmDrone, msg);
        }

        public bool CanPressConnect()
        {
            return _serialPortInfo != null;
        }

        public bool CanDoConnectedStuff()
        {
            return TelemetryLink.IsConected;
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

        public async void GetWaypoints()
        {
            await _planner.GetWayPoints(_apmDrone, TelemetryLink);
        }

        public async void OpenSerialPort()
        {
            SelectedPort.BaudRate = 57600;
            await TelemetryLink.OpenAsync(SelectedPort);
            ConnectMessage = "Disconnect";
            OpenSerialPortCommand.RaiseCanExecuteChanged();
            GetWaypointsCommand.RaiseCanExecuteChanged();
        }

        public async void CloseSerialPort()
        {
            await TelemetryLink.CloseAsync();
            ConnectMessage = "Connect";
            OpenSerialPortCommand.RaiseCanExecuteChanged();
            GetWaypointsCommand.RaiseCanExecuteChanged();
        }       

        private SerialPortInfo _serialPortInfo;
        public SerialPortInfo SelectedPort
        {
            get { return _serialPortInfo; }
            set
            {
                Set(ref _serialPortInfo, value);
                OpenSerialPortCommand.RaiseCanExecuteChanged();
                GetWaypointsCommand.RaiseCanExecuteChanged();
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
        public RelayCommand GetWaypointsCommand { get; }

        public ISerialTelemetryLink TelemetryLink { get; }
    }
}
