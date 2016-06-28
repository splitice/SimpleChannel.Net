using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleChannel.Net.ZMQ;

namespace SimpleChannel.Net.Tests
{
    [DataContract]
    class TestModel
    {
    }

    [TestFixture]
    public class ZeroMqTests
    {
        [TestCase]
        public void TestZmq()
        {
            ZeroMqQueueChannel<TestModel> qcOut = new ZeroMqQueueChannel<TestModel>("N", "tcp://127.0.0.1:5557", true);
            ZeroMqQueueChannel<TestModel> qcIn = new ZeroMqQueueChannel<TestModel>("N", "tcp://127.0.0.1:5557", false);

            TestModel tm = null;
            qcIn.Poll(out tm, 10);
            Assert.IsNull(tm);

            TestModel model;
            var task = Task.Run(() => model = qcIn.Take());

            qcOut.Put(new TestModel());
            
            Assert.IsTrue(task.Wait(1000));
        }
    }
}
