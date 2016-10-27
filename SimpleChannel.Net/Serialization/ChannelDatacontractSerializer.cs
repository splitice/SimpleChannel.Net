using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SimpleChannel.Net.Serialization
{
    public class ChannelDatacontractSerializer : IChannelSerializer
    {
        private DataContractSerializer _serializer;

        public ChannelDatacontractSerializer(Type[] types)
        {
            _serializer = new DataContractSerializer(typeof(object), types);
        }

        public object Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
               return _serializer.ReadObject(stream);
            }
        }

        public byte[] Serialize(object item)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                _serializer.WriteObject(stream, item);
                return stream.ToArray();
            }
        }
    }
}
