namespace CAT240Parser
{
    public class Cat240DataBlock
    {
        public Cat240DataHeader Header { get; set; }
        public Cat240DataItems Items { get; set; }

        public Cat240DataBlock(byte[] buffer, long size)
        {
            Header = new Cat240DataHeader(buffer);
            Items = new Cat240DataItems(Header, buffer, size);
        }
    }
}
