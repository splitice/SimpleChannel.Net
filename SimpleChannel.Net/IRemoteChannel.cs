using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleChannel.Net.Serialization;

namespace SimpleChannel.Net
{
    interface IRemoteChannel<T>: IChannel<T>
    {
        IChannelSerializer Serializer { get; set; }
    }
}
