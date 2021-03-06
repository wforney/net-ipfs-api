using Ipfs.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Api
{
    [TestClass]
    public class PubSubApiTest
    {
        private volatile int messageCount = 0;

        private volatile int messageCount1 = 0;

        [TestMethod]
        public void Api_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.PubSub);
        }

        [TestMethod]
        [Ignore("go-ipfs doesn't allow multiple subscribe to the same topic")]
        public async Task Multiple_Subscribe_Mutiple_Messages()
        {
            this.messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                Action<IPublishedMessage> processMessage = (msg) =>
{
    Interlocked.Increment(ref this.messageCount);
};
                try
                {
                    await ipfs.PubSub.Subscribe(topic, processMessage, cs.Token);
                    await ipfs.PubSub.Subscribe(topic, processMessage, cs.Token);
                    foreach (var msg in messages)
                    {
                        await ipfs.PubSub.Publish(topic, msg);
                    }

                    await Task.Delay(1000);
                    Assert.AreEqual(messages.Length * 2, this.messageCount);
                }
                finally
                {
                    cs.Cancel();
                }
            }
        }

        [TestMethod]
        public async Task Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                try
                {
                    await ipfs.PubSub.Subscribe(topic, msg => { }, cs.Token);
                    var peers = ipfs.PubSub.PeersAsync().Result.ToArray();
                    Assert.IsTrue(peers.Length > 0);
                }
                finally
                {
                    cs.Cancel();
                }
            }
        }

        [TestMethod]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-unknown" + Guid.NewGuid().ToString();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.AreEqual(0, peers.Length);
        }

        [TestMethod]
        public async Task Subscribe()
        {
            this.messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                try
                {
                    await ipfs.PubSub.Subscribe(topic, msg =>
                    {
                        Interlocked.Increment(ref this.messageCount);
                    }, cs.Token);
                    await ipfs.PubSub.Publish(topic, "hello world!");

                    await Task.Delay(1000);
                    Assert.AreEqual(1, this.messageCount);
                }
                finally
                {
                    cs.Cancel();
                }
            }
        }

        [TestMethod]
        public async Task Subscribe_Mutiple_Messages()
        {
            this.messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                try
                {
                    await ipfs.PubSub.Subscribe(topic, msg =>
                    {
                        Interlocked.Increment(ref this.messageCount);
                    }, cs.Token);
                    foreach (var msg in messages)
                    {
                        await ipfs.PubSub.Publish(topic, msg);
                    }

                    await Task.Delay(1000);
                    Assert.AreEqual(messages.Length, this.messageCount);
                }
                finally
                {
                    cs.Cancel();
                }
            }
        }

        [TestMethod]
        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                try
                {
                    await ipfs.PubSub.Subscribe(topic, msg => { }, cs.Token);
                    var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                    Assert.IsTrue(topics.Length > 0);
                    CollectionAssert.Contains(topics, topic);
                }
                finally
                {
                    cs.Cancel();
                }
            }
        }

        [TestMethod]
        public async Task Unsubscribe()
        {
            this.messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-api-test-" + Guid.NewGuid().ToString();
            using (var cs = new CancellationTokenSource())
            {
                await ipfs.PubSub.Subscribe(topic, msg =>
{
    Interlocked.Increment(ref this.messageCount1);
}, cs.Token);
                await ipfs.PubSub.Publish(topic, "hello world!");
                await Task.Delay(1000);
                Assert.AreEqual(1, this.messageCount1);

                cs.Cancel();
                await ipfs.PubSub.Publish(topic, "hello world!!!");
                await Task.Delay(1000);
                Assert.AreEqual(1, this.messageCount1);
            }
        }
    }
}
