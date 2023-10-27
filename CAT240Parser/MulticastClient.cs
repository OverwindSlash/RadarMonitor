using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    public delegate void UdpConnectedEventHandler(object sender, int clientId, string ip, int port);
    public delegate void Cat240ReceivedEventHandler(object sender, int clientId, Cat240DataBlock data);
    public delegate void UdpDisconnectedEventHandler(object sender, int clientId, string ip, int port);
    public delegate void UdpErrorEventHandler(object sender, int clientId, string ip, int port);

    public class MulticastClient : UdpClient
    {
        public string Multicast;

        private int _clientId;
        private bool _stop;
        private ConcurrentDictionary<uint, int> _dataBlockIds = new();

        private int _dataBlockCount;

        // 定义事件
        public event UdpConnectedEventHandler OnUdpConnected;
        public event Cat240ReceivedEventHandler OnCat240Received;
        public event UdpDisconnectedEventHandler OnUdpDisconnected;
        public event UdpErrorEventHandler OnUdpError;

        public MulticastClient(int clientId, string address, int port) 
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
            Disconnect();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Trace.WriteLine($"Multicast UDP client connected a new session with Id {Id}");

            OnUdpConnected?.Invoke(this, _clientId, Address, Port);

            // Join UDP multicast group
            JoinMulticastGroup(Multicast);

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
            ThreadPool.QueueUserWorkItem(((obj) =>
            {
                Cat240DataBlock dataBlock = new Cat240DataBlock(buffer, size);

                if (_dataBlockCount++ % 500 == 0)
                {
                    _dataBlockIds.Clear();
                }

                // TODO: 后续看是否需要这样的性能优化
                // if (_dataBlockCount % 2 == 0)
                // {
                //     return;
                // }
                
                if (_dataBlockIds.ContainsKey(dataBlock.Items.MessageIndex))
                {
                    return;
                }

                _dataBlockIds.TryAdd(dataBlock.Items.MessageIndex, 1);
                OnCat240Received?.Invoke(this, _clientId, dataBlock);
            }));

            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            OnUdpError?.Invoke(this, _clientId, Address, Port);
            Trace.WriteLine($"Multicast UDP client caught an error with code {error}");
        }
    }
}
