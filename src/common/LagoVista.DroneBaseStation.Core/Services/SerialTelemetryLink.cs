using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static MAVLink;

namespace LagoVista.DroneBaseStation.Core.Services
{
    public class SerialTelemetryLink : ModelBase, ISerialTelemetryLink
    {
        public event EventHandler<MAVLink.MAVLinkMessage> MessageParsed;

        IMavLinkMessageParser _messageParser;
        IDeviceManager _deviceManager;
        IDispatcherServices _dispatcherServices;
        public SerialTelemetryLink(IDeviceManager deviceManager, IMavLinkMessageParser messageParser, IDispatcherServices dispatcherServices)
        {
            _deviceManager = deviceManager;
            _messageParser = messageParser;
            _dispatcherServices = dispatcherServices;

            Messages = new ObservableCollection<MAVLinkMessage>();

            _messageParser.MessageParsed += (sndr, msg) =>
            {
                _dispatcherServices.Invoke(() =>
                {
                    MessageParsed(sndr, msg);
                    lock (this)
                    {
                        MessagesReceived++;
                        Messages.Insert(0, msg);
                        if (Messages.Count == 100)
                        {
                            Messages.Remove(Messages.Last());
                        }
                    }
                });
            };
        }

        ISerialPort _serialPort;
        bool _running = false;

        private int _messagesReceived = 0;
        public int MessagesReceived
        {
            get { return _messagesReceived; }
            private set { Set(ref _messagesReceived, value); }
        }

        private int _errors;
        public int Errors
        {
            get { return _errors; }
            private set { Set(ref _errors, value); }
        }

        private long _bytesReceived = 0;
        public long BytesReceived
        {
            get { return _bytesReceived; }
            private set { Set(ref _bytesReceived, value); }
        }

        private bool _isConnected = false;
        public bool IsConected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        public TimeSpan Timeout
        {
            get; set;
        }

        public ObservableCollection<MAVLinkMessage> Messages { get; }

        public async Task CloseAsync()
        {
            await _serialPort.CloseAsync();
            _serialPort.Dispose();
            _serialPort = null;
        }

        public async Task OpenAsync(SerialPortInfo portInfo)
        {
            _serialPort = _deviceManager.CreateSerialPort(portInfo);

            await _serialPort.OpenAsync();

            StartListening();
        }

        private async void StartListening()
        {
            _running = true;
            var buffer = new byte[4096];
            //https://mavlink.io/en/guide/serialization.html
            //https://mavlink.io/en/protocol/overview.html
            //TODO: Move to a service class
            while (_running)
            {
                var readCount = await _serialPort.ReadAsync(buffer, 0, buffer.Length);
                _dispatcherServices.Invoke(() =>
                {
                    BytesReceived += readCount;
                });

                if (readCount > 0)
                {
                    _messageParser.HandleBuffer(buffer, readCount);
                }
            }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }
}
