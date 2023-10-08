using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    public delegate void Cat240ReceivedEventHandler(object sender, Cat240DataBlock data);

    public class MulticastClient : UdpClient
    {
        public string Multicast;

        // 定义事件
        public event Cat240ReceivedEventHandler OnCat240Received;


        public MulticastClient(string address, int port) : base(address, port) {
            int coreCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(coreCount, coreCount);
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
            Console.WriteLine($"Multicast UDP client connected a new session with Id {Id}");

            // Join UDP multicast group
            JoinMulticastGroup(Multicast);

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Multicast UDP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                Connect();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            {
                double lastAzimuth = 0.0;

                Cat240DataBlock dataBlock = new Cat240DataBlock(buffer, size);

                if (dataBlock.Items.StartAzimuth != lastAzimuth)
                {
                    OnCat240Received?.Invoke(this, dataBlock);
                    lastAzimuth = dataBlock.Items.StartAzimuth;
                }


            }));

            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            //Console.WriteLine($"Multicast UDP client caught an error with code {error}");
        }

        private bool _stop;
    }
}
