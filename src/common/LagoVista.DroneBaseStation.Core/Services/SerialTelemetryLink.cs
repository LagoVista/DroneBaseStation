using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.DroneBaseStation.Core.Interfaces;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static MAVLink;
using LagoVista.Core.Validation;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LagoVista.Drone;

namespace LagoVista.DroneBaseStation.Core.Services
{
    public sealed class WaitOnRequest<TModel>
    {
        public WaitOnRequest(uint msgId)
        {
            CompletionSource = new TaskCompletionSource<TModel>();
            MsgId = msgId;
            Details = new List<string>();
        }

        public List<String> Details { get; private set; }

        public uint MsgId { get; private set; }

        public DateTime Enqueued { get; private set; }

        public TaskCompletionSource<TModel> CompletionSource { get; private set; }
    }

    public class SerialTelemetryLink : ModelBase, ISerialTelemetryLink
    {
        public event EventHandler<MAVLink.MAVLinkMessage> MessageParsed;

        protected ConcurrentDictionary<uint, WaitOnRequest<object>> Sessions { get; } = new ConcurrentDictionary<uint, WaitOnRequest<object>>();

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
                Complete(msg);
                _dispatcherServices.Invoke(() =>
                {
                    MessageParsed(sndr, msg);
                    lock (this)
                    {
                        MessagesReceived++;
                        if (!Messages.Where(emsg => emsg.MessageInfo.MsgId == msg.MessageInfo.MsgId).Any())
                        {
                            Messages.Insert(0, msg);
                            if (Messages.Count == 100)
                            {
                                Messages.Remove(Messages.Last());
                            }
                        }
                    }
                });
            };
        }

        protected void Complete(MAVLink.MAVLinkMessage msg)
        {
            if (Sessions.TryGetValue(msg.msgid, out var tcs))
            {
                tcs.CompletionSource.SetResult(msg);
            }
        }

        public Task<InvokeResult<MAVLinkMessage>> WaitForMessageAsync(MAVLINK_MSG_ID messageId, TimeSpan timeout)
        {
            try
            {
                var wor = new WaitOnRequest<object>((uint)messageId);
                Sessions[(uint)messageId] = wor;
                wor.CompletionSource.Task.Wait(timeout);
 
                if (!wor.CompletionSource.Task.IsCompleted)
                {
                    return Task.FromResult(InvokeResult<MAVLinkMessage>.FromError("Timeout waiting for response."));
                }
                else if (wor.CompletionSource.Task.Result == null)
                {
                    return Task.FromResult(InvokeResult<MAVLinkMessage>.FromError("Null Response From Completion Routine."));
                }
                else
                {
                    var result = wor.CompletionSource.Task.Result;
                    if (result is MAVLinkMessage typedResult)
                    {
                        return Task.FromResult(InvokeResult<MAVLinkMessage>.Create(typedResult));
                    }
                    else
                    {
                        return Task.FromResult(InvokeResult<MAVLinkMessage>.FromError($"Type Mismatch - Expected: {typeof(MAVLinkMessage).Name} - Actual: {result.GetType().Name}."));
                    }
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(InvokeResult<MAVLinkMessage>.FromException("AsyncCoupler_WaitOnAsync", ex));
            }
            finally
            {
                Sessions.TryRemove((uint)messageId, out WaitOnRequest<Object> obj);
            }
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

            IsConected = false;
        }

        public async Task OpenAsync(SerialPortInfo portInfo)
        {
            _serialPort = _deviceManager.CreateSerialPort(portInfo);

            await _serialPort.OpenAsync();

            StartListening();

            IsConected = true;
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

        public async Task<InvokeResult> SendMessage(IDrone drone, MAVLINK_MSG_ID messageId, object req)
        {
            var buffer = MavlinkUtil.GeneratePacket(drone, MAVLINK_MSG_ID.MISSION_REQUEST_LIST, req);
            await _serialPort.WriteAsync(buffer);
            return InvokeResult.Success;
        }
    }
}
