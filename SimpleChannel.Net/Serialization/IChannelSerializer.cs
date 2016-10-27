namespace SimpleChannel.Net.Serialization
{
    public interface IChannelSerializer
    {
        object Deserialize(byte[] data);
        byte[] Serialize(object item);
    }
}