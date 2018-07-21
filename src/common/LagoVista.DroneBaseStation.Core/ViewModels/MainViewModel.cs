using LagoVista.Client.Core.ViewModels;
using LagoVista.Core.Commanding;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
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
        ISerialPort _serialPort;
        StreamReader _reader;
        StreamWriter _writer;

        bool _running;

        public MainViewModel(IDeviceManager deviceManager)
        {
            OpenSerialPortCommand = new RelayCommand(HandleConnectClick, CanPressConnect);
            Title = "Kevin";
        }

        public bool CanPressConnect()
        {
            return SelectedPort != null;
        }

        public void HandleConnectClick()
        {
            if(_serialPort == null)
            {
                OpenSerialPort();
            }
            else
            {
                CloseSerialPort();
            }
        }

        public async void OpenSerialPort()
        {
            SelectedPort.BaudRate = 115200;

            _serialPort = DeviceManager.CreateSerialPort(SelectedPort);
            OpenSerialPortCommand.RaiseCanExecuteChanged();
            await _serialPort.OpenAsync();
            ConnectMessage = "Disconnect";

            _reader = new StreamReader(_serialPort.InputStream);
            _writer = new StreamWriter(_serialPort.InputStream);

            StartListening();
        }

        public async void CloseSerialPort()
        {
            await _serialPort.CloseAsync();
            _serialPort.Dispose();
            _serialPort = null;
            ConnectMessage = "Connect";
        }
       

        enum MavLinkParserStates
        {
            ExpectingStx,
            ExpectingLen,
            ExpectingIncompatFlags,
            ExpectingCompatFlag,
            ExpectingSeq,
            ExpectingSysId,
            ExpectingComponentId,
            ExpectingMsgId07,
            ExpectingMsgId815,
            ExpectingMsgId1623,
            ExpectingTargetSysId,
            ExpectingTargetComponentId,
            ReadingPayload,
            ExpectingCheckSum1,
            ExpectingCheckSum2,
            ExpectingSignature,


        }



        MavLinkParserStates _mavLinkParserState = MavLinkParserStates.ExpectingStx;

        MAVLinkMessage _currentMessage = null;

        private async void StartListening()
        {
            _running = true;
            var buffer = new char[256];
            //TODO: Move to a service class
            while(_running)
            {
                var readCount = await _reader.ReadAsync(buffer, 0, buffer.Length);
                for (var idx = 0; idx < readCount; ++idx)
                {
                    var value = (byte)buffer[idx];
                    Debug.WriteLine($"{idx:000}. 0x{value:x2}");                   

                    switch(_mavLinkParserState)
                    {
                        case MavLinkParserStates.ExpectingStx:
                            if(value == MAVLINK_STX)
                            {
                                _currentMessage = new MAVLinkMessage();
                                _mavLinkParserState = MavLinkParserStates.ExpectingLen;
                            }
                            break;
                        case MavLinkParserStates.ExpectingLen:
                            _currentMessage.payloadlength = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingIncompatFlags;
                            break;
                        case MavLinkParserStates.ExpectingIncompatFlags:
                            _currentMessage.incompat_flags = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingCompatFlag;
                            break;
                        case MavLinkParserStates.ExpectingCompatFlag:
                            _currentMessage.compat_flags = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingSeq;
                            break;
                        case MavLinkParserStates.ExpectingSeq:
                            _currentMessage.seq = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingSysId;
                            break;
                        case MavLinkParserStates.ExpectingSysId:
                            _currentMessage.sysid = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingComponentId;
                            break;
                        case MavLinkParserStates.ExpectingComponentId:
                            _currentMessage.compid = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingMsgId07;
                            _currentMessage.msgid = 0x00;
                            break;
                        case MavLinkParserStates.ExpectingMsgId07:
                            _currentMessage.msgid = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingMsgId815;
                            break;
                        case MavLinkParserStates.ExpectingMsgId815:
                            _mavLinkParserState = MavLinkParserStates.ExpectingMsgId1623;
                            _currentMessage.msgid |= (UInt32) (value << 8);
                            break;
                        case MavLinkParserStates.ExpectingMsgId1623:
                            _mavLinkParserState = MavLinkParserStates.ExpectingTargetSysId;
                            _currentMessage.msgid |= (UInt32)(value << 16);
                            break;
                        case MavLinkParserStates.ExpectingTargetSysId:
                            _currentMessage.targetsysid = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingTargetComponentId;
                            break;
                        case MavLinkParserStates.ExpectingTargetComponentId:
                            _currentMessage.targetcomponentid = value;
                            _currentMessage.payload_index = 0;
                            _mavLinkParserState = MavLinkParserStates.ReadingPayload;
                            break;
                        case MavLinkParserStates.ReadingPayload:
                            if (_currentMessage.payload_index < _currentMessage.Length)
                            {
                                _currentMessage.payload[_currentMessage.payload_index++] = value;
                            }

                            if (_currentMessage.payload_index == _currentMessage.Length)
                            { 
                                _mavLinkParserState = MavLinkParserStates.ExpectingCheckSum1;
                            }

                            break;
                        case MavLinkParserStates.ExpectingCheckSum1:
                            _currentMessage.crc16 = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingSignature;
                            break;
                        case MavLinkParserStates.ExpectingCheckSum2:
                            _currentMessage.crc16 |= (UInt16)(8 <<  value);
                            _mavLinkParserState = MavLinkParserStates.ExpectingSignature;
                            break;
                        case MavLinkParserStates.ExpectingSignature:
                            _currentMessage.signature = value;
                            _mavLinkParserState = MavLinkParserStates.ExpectingStx;
                            break;
                    }
                }

                /*
                ushort crc = MavlinkCRC.crc_calculate(buffer, buffer.Length - 2);

                // calc extra bit of crc for mavlink 1.0+
                if (message.header == MAVLINK_STX || message.header == MAVLINK_STX_MAVLINK1)
                {
                    crc = MavlinkCRC.crc_accumulate(MAVLINK_MESSAGE_INFOS.GetMessageInfo(message.msgid).crc, crc);
                }

                // check crc
                if ((message.crc16 >> 8) != (crc >> 8) ||
                          (message.crc16 & 0xff) != (crc & 0xff))
                {
                    badCRC++;
                    // crc fail
                    return null;
                }
                */
            }
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

        public async override Task InitAsync()
        {
            Ports = await DeviceManager.GetSerialPortsAsync();
            await base.InitAsync();
        }

        public String Title
        { get; set; }

        public RelayCommand OpenSerialPortCommand { get; }
    }
}
