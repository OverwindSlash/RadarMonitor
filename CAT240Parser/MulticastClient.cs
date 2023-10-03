using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UdpClient = NetCoreServer.UdpClient;

namespace CAT240Parser
{
    class MulticastClient : UdpClient
    {
        public string Multicast;

        public MulticastClient(string address, int port) : base(address, port) { }

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
            Cat240DataBlock dataBlock = new Cat240DataBlock(buffer, size);

            Trace.WriteLine($"SAzi:{dataBlock.Items.StartAzimuthInDegree:F4}, SRng:{dataBlock.Items.StartRange}, CellDur:{dataBlock.Items.CellDuration}, " +
                              $"ValidB:{dataBlock.Items.ValidBytesInDataBlock}, ValidC:{dataBlock.Items.ValidCellsInDataBlock}");

            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Multicast UDP client caught an error with code {error}");
        }

        private bool _stop;
    }
}
