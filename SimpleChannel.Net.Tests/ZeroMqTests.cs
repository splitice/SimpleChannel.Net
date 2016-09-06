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
        private void SetupConnecitons<T>(out ZeroMqQueueChannel<T> qcOut, out ZeroMqQueueChannel<T> qcIn, int port) where T : class
        {
            qcOut = new ZeroMqQueueChannel<T>("N", "tcp://127.0.0.1:"+port, true);
            qcIn = new ZeroMqQueueChannel<T>("N", "tcp://127.0.0.1:" + port, false);
            
            //Put into listening mode
            T tm;
            qcIn.Poll(out tm, 10);
        }

        [TestCase]
        public void TestZmqEmpty()
        {
            ZeroMqQueueChannel<TestModel> qcOut, qcIn;
            SetupConnecitons(out qcOut, out qcIn, 1111);

            TestModel tm = null;
            qcIn.Poll(out tm, 10);
            Assert.IsNull(tm);

            qcOut.Dispose();
            qcIn.Dispose();
        }

        [TestCase]
        public void TestZmqPutTake()
        {
            ZeroMqQueueChannel<TestModel> qcOut, qcIn;
            SetupConnecitons(out qcOut, out qcIn, 1112);

            TestModel model;
            var task = Task.Run(() => model = qcIn.Take());

            qcOut.Put(new TestModel());

            Assert.IsTrue(task.Wait(1000));

            qcOut.Dispose();
            qcIn.Dispose();
        }


        [TestCase]
        public void TestZmqPutPoll()
        {
            ZeroMqQueueChannel<TestModel> qcOut, qcIn;
            SetupConnecitons(out qcOut, out qcIn, 1113);

            TestModel model;

            qcOut.Put(new TestModel());

            Assert.IsTrue(qcIn.Poll(out model, 1000));
            Assert.IsNotNull(model);

            qcOut.Dispose();
            qcIn.Dispose();
        }
    }
}
