﻿using Citadel.Core.Windows.Util;
using Citadel.IPC.Messages;
using Filter.Platform.Common.IPC;

namespace Te.Citadel.Platform
{
    public class WindowsPipeClient : IPipeClient
    {
        private NamedPipeWrapper.NamedPipeClient<BaseMessage> client;

        public WindowsPipeClient(string channel, bool autoReconnect = false)
        {
            client = new NamedPipeWrapper.NamedPipeClient<BaseMessage>(channel);
            client.AutoReconnect = autoReconnect;

            client.Connected += (conn) => Connected?.Invoke();
            client.Disconnected += (conn) => Disconnected?.Invoke();
            client.ServerMessage += (conn, msg) => ServerMessage?.Invoke(msg);
            client.Error += (ex) => Error?.Invoke(ex);
        }

        public bool AutoReconnect
        {
            get
            {
                return client.AutoReconnect;
            }

            set
            {
                client.AutoReconnect = value;
            }
        }

        public event ClientConnectionHandler Connected;
        public event ClientConnectionHandler Disconnected;
        public event ServerMessageHandler ServerMessage;
        public event PipeExceptionHandler Error;

        public void PushMessage(BaseMessage msg) => client.PushMessage(msg);
        public void Start() => client.Start();
        public void Stop() => client.Stop();
        public void WaitForConnection() => client.WaitForConnection();
    }
}
