using System.Net;
using System.Net.Sockets;

namespace CAT240Parser
{
    public class SimpleUdpClient : UdpClient
    {
        private readonly int _clientId;
        private readonly string _udpDestIp;
        private readonly int _port;

        private bool _isRunning;

        private readonly IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        // 定义事件
        public event UdpConnectedEventHandler OnUdpConnected;
        public event Cat240ReceivedEventHandler OnCat240Received;
        public event UdpDisconnectedEventHandler OnUdpDisconnected;
        public event UdpErrorEventHandler OnUdpError;

        public SimpleUdpClient(int clientId, int port) : base(port)
        {
            _clientId = clientId;
            _udpDestIp = "127.0.0.1";
            _port = port;
        }

        public SimpleUdpClient(int clientId, string udpDestIp, int port) : base(port)
        {
            _clientId = clientId;
            _udpDestIp = udpDestIp;
            _port = port;
        }

        public void Listen()
        {
            int coreCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(coreCount, coreCount);

            OnUdpConnected?.Invoke(this, _clientId, _udpDestIp, _port);

            _isRunning = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(async (obj) =>
            {
                while (_isRunning)
                {
                    try
                    {
                        UdpReceiveResult result = await ReceiveAsync();

                        Cat240DataBlock dataBlock = new Cat240DataBlock(result.Buffer, result.Buffer.Length);

                        OnCat240Received?.Invoke(this, _clientId, dataBlock);

                    }
                    catch (Exception e)
                    {
                        OnUdpError?.Invoke(this, _clientId, _udpDestIp, _port);
                    }
                }
            }));
        }

        public void Disconnect()
        {
            _isRunning = false;
            OnUdpDisconnected?.Invoke(this, _clientId, _udpDestIp, _port);
            Close();
        }

        public void DisconnectAndStop()
        {
            Disconnect();
            while (Active)
                Thread.Yield();
        }
    }
}
