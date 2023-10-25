using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    public delegate void Cat240ReceivedEventHandler(object sender, Cat240DataBlock data, int radarId);

    public class MulticastClient : UdpClient
    {
        public string Multicast;

        // 定义事件
        public event Cat240ReceivedEventHandler OnCat240Received;
        private int _radarId;

        public MulticastClient(string address, int port, int radarId) : base(address, port)
        {
            int coreCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(coreCount, coreCount);
            _radarId = radarId;
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
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
            {
                Trace.WriteLine($"Reconnecting...");
                Connect();
            }
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {

            //byte[] data=new byte[8192]; 
            //buffer.CopyTo(data,0);
            
            ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            {
                //double lastAzimuth = 0.0;

                Cat240DataBlock dataBlock = new Cat240DataBlock(buffer, size);
                //Trace.TraceInformation($"{Id}: {dataBlock.Items.StartAzimuth}");


                //if (dataBlock.Items.StartAzimuth != lastAzimuth)
                //{
                    OnCat240Received?.Invoke(this, dataBlock, _radarId);
                //    lastAzimuth = dataBlock.Items.StartAzimuth;
                //}
            }));

            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Trace.WriteLine($"Multicast UDP client caught an error with code {error}");
        }

        private bool _stop;
    }
}
