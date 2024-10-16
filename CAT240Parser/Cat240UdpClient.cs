﻿using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    public delegate void UdpConnectedEventHandler(object sender, int clientId, string ip, int port);
    public delegate void Cat240ReceivedEventHandler(object sender, int clientId, Cat240DataBlock data);
    public delegate void UdpDisconnectedEventHandler(object sender, int clientId, string ip, int port);
    public delegate void UdpErrorEventHandler(object sender, int clientId, string ip, int port);

    public class Cat240UdpClient : UdpClient
    {
        public string DestinationIp;

        private int _clientId;
        private bool _stop;

        private bool _isMultiCast;

        // 定义事件
        public event UdpConnectedEventHandler OnUdpConnected;
        public event Cat240ReceivedEventHandler OnCat240Received;
        public event UdpDisconnectedEventHandler OnUdpDisconnected;
        public event UdpErrorEventHandler OnUdpError;

        public Cat240UdpClient(int clientId, string address, int port) 
            : base(address, port) 
        {
            int coreCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(coreCount, coreCount);

            _clientId = clientId;
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            if (_isMultiCast)
            {
                LeaveMulticastGroup(DestinationIp);
            }
            
            Disconnect();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Trace.WriteLine($"Multicast UDP client connected a new session with Id {Id}");

            OnUdpConnected?.Invoke(this, _clientId, Address, Port);

            var ips = DestinationIp.Split('.');
            var ipPart1 = int.Parse(ips[0]);

            if (ipPart1 >= 224 & ipPart1 <= 239)
            {
                _isMultiCast = true;
                // Join UDP multicast group
                JoinMulticastGroup(DestinationIp);
            }

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnDisconnected()
        {
            Trace.WriteLine($"Multicast UDP client disconnected a session with Id {Id}");

            OnUdpDisconnected?.Invoke(this, _clientId, Address, Port);

            // Wait for a while...
            Thread.Sleep(200);

            // Try to connect again
            if (!_stop)
            {
                Trace.WriteLine($"Reconnecting...");
                Connect();
            }
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {

            ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            {
                //double lastAzimuth = 0.0;

                Cat240DataBlock dataBlock = new Cat240DataBlock(buffer, size);
                //Trace.TraceInformation($"{Id}: {dataBlock.Items.StartAzimuth}");


                //if (dataBlock.Items.StartAzimuth != lastAzimuth)
                //{
                //Trace.TraceInformation($"{_radarId}");

                OnCat240Received?.Invoke(this, _clientId, dataBlock);
                //    lastAzimuth = dataBlock.Items.StartAzimuth;
                //}
                ReceiveAsync();
            }));

        }

        protected override void OnError(SocketError error)
        {
            OnUdpError?.Invoke(this, _clientId, Address, Port);
            Trace.WriteLine($"Multicast UDP client caught an error with code {error}");
        }
    }
}
