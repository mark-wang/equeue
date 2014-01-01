﻿using System;
using System.Net.Sockets;
using System.Text;
using EQueue.Common.Logging;

namespace EQueue.Common.Socketing
{
    public class SocketService
    {
        private ILogger _logger;

        public SocketService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(GetType().Name);
        }
        public void SendMessage(Socket targetSocket, byte[] message, Action<byte[]> messageSentCallback)
        {
            if (message.Length > 0)
            {
                targetSocket.BeginSend(
                    message,
                    0,
                    message.Length,
                    0,
                    new AsyncCallback(SendCallback),
                    new SendContext(targetSocket, message, messageSentCallback));
            }
        }
        public void ReceiveMessage(Socket sourceSocket, Action<byte[]> messageReceivedCallback)
        {
            ReceiveInternal(new ReceiveState(sourceSocket, messageReceivedCallback), 4);
        }

        private void ReceiveInternal(ReceiveState receiveState, int size)
        {
            receiveState.SourceSocket.BeginReceive(receiveState.Buffer, 0, size, 0, ReceiveCallback, receiveState);
        }
        private void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                var sendContext = (SendContext)asyncResult.AsyncState;
                sendContext.TargetSocket.EndSend(asyncResult);
                sendContext.MessageSentCallback(sendContext.Message);
            }
            catch (SocketException socketException)
            {
                _logger.ErrorFormat("Socket send exception, ErrorCode:{0}", socketException.SocketErrorCode);
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("Unknown socket send exception:{0}", ex);
            }
        }
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            var receiveState = (ReceiveState)asyncResult.AsyncState;
            var sourceSocket = receiveState.SourceSocket;
            var receivedData = receiveState.Data;
            var bytesRead = 0;

            try
            {
                bytesRead = sourceSocket.EndReceive(asyncResult);
            }
            catch (SocketException socketException)
            {
                _logger.ErrorFormat("Socket receive exception, ErrorCode:{0}", socketException.SocketErrorCode);
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("Unknown socket receive exception:{0}", ex);
            }

            if (bytesRead > 0)
            {
                if (receiveState.MessageSize == null)
                {
                    receiveState.MessageSize = SocketUtils.ParseMessageLength(receiveState.Buffer);
                    var size = receiveState.MessageSize <= ReceiveState.BufferSize ? receiveState.MessageSize.Value : ReceiveState.BufferSize;
                    ReceiveInternal(receiveState, size);
                }
                else
                {
                    for (var index = 0; index < bytesRead; index++)
                    {
                        receivedData.Add(receiveState.Buffer[index]);
                    }
                    if (receivedData.Count < receiveState.MessageSize.Value)
                    {
                        var remainSize = receiveState.MessageSize.Value - receivedData.Count;
                        var size = remainSize <= ReceiveState.BufferSize ? remainSize : ReceiveState.BufferSize;
                        ReceiveInternal(receiveState, size);
                    }
                    else
                    {
                        receiveState.MessageReceivedCallback(receivedData.ToArray());
                        receiveState.MessageSize = null;
                        receivedData.Clear();
                        ReceiveInternal(receiveState, 4);
                    }
                }
            }
        }
    }
}