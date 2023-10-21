using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    public delegate void Cat240ReceivedEventHandler(object sender, int clientId, Cat240DataBlock data);

    public class MulticastClient : UdpClient
    {
        public string Multicast;

        private int _clientIndex;
        private bool _stop;
        private ConcurrentDictionary<uint, int> _dataBlockIds = new();

        private int _dataBlockCount;

        // 定义事件
        public event Cat240ReceivedEventHandler OnCat240Received;

        public MulticastClient(int clientIndex, string address, int port) 
            : base(address, port) 
        {
            int coreCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(coreCount, coreCount);

            _clientIndex = clientIndex;
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

            // Join UDP multicast group
            JoinMulticastGroup(Multicast);

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnDisconnected()
        {
            Trace.WriteLine($"Multicast UDP client disconnected a session with Id {Id}");

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

                if (_dataBlockIds.ContainsKey(dataBlock.Items.MessageIndex))
                {
                    return;
                }

                _dataBlockIds.TryAdd(dataBlock.Items.MessageIndex, 1);
                OnCat240Received?.Invoke(this, _clientIndex, dataBlock);
            }));

            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Trace.WriteLine($"Multicast UDP client caught an error with code {error}");
        }
    }
}
