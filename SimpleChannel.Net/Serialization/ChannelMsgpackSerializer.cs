using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MsgPack;

namespace SimpleChannel.Net.Serialization
{
    public class ChannelMsgpackSerializer : IChannelSerializer
    {
        private MsgPack.BoxingPacker _serializer;

        public ChannelMsgpackSerializer(Type[] types)
        {
            _serializer = new MsgPack.BoxingPacker();
        }

        public object Deserialize(byte[] data)
        {
            return _serializer.Unpack(data);
        }

        public byte[] Serialize(object item)
        {
            return _serializer.Pack(item);
        }
    }
}
