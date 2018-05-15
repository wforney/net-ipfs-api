using Ipfs.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Api
{

    [TestClass]
    public class BlockApiTest
    {
        private IpfsClient ipfs = TestFixture.Ipfs;
        private string id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
        private byte[] blob = Encoding.UTF8.GetBytes("blorb");

        [TestMethod]
        public void Put_Bytes()
        {
            var cid = this.ipfs.Block.PutAsync(this.blob).Result;
            Assert.AreEqual(this.id, (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Bytes_ContentType()
        {
            var cid = this.ipfs.Block.PutAsync(this.blob, contentType: "raw").Result;
            Assert.AreEqual("zb2rhYDhWhxyHN6HFAKGvHnLogYfnk9KvzBUZvCg7sYhS22N8", (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Bytes_Hash()
        {
            var cid = this.ipfs.Block.PutAsync(this.blob, "raw", "sha2-512").Result;
            Assert.AreEqual("zB7NCfbtX9WqFowgroqE19J841VESUhLc1enF7faMSMhTPMR4M3kWq7rS2AfCvdHeZ3RdfoSM45q7svoMQmw2NDD37z9F", (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Bytes_Pinned()
        {
            var data1 = new byte[] { 23, 24, 127 };
            var cid1 = this.ipfs.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
            var pins = this.ipfs.Pin.ListAsync().Result;
            Assert.IsTrue(pins.Any(pin => pin == cid1));

            var data2 = new byte[] { 123, 124, 27 };
            var cid2 = this.ipfs.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
            pins = this.ipfs.Pin.ListAsync().Result;
            Assert.IsFalse(pins.Any(pin => pin == cid2));
        }

        [TestMethod]
        public void Put_Stream()
        {
            var cid = this.ipfs.Block.PutAsync(new MemoryStream(this.blob)).Result;
            Assert.AreEqual(this.id, (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Stream_ContentType()
        {
            var cid = this.ipfs.Block.PutAsync(new MemoryStream(this.blob), contentType: "raw").Result;
            Assert.AreEqual("zb2rhYDhWhxyHN6HFAKGvHnLogYfnk9KvzBUZvCg7sYhS22N8", (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Stream_Hash()
        {
            var cid = this.ipfs.Block.PutAsync(new MemoryStream(this.blob), "raw", "sha2-512").Result;
            Assert.AreEqual("zB7NCfbtX9WqFowgroqE19J841VESUhLc1enF7faMSMhTPMR4M3kWq7rS2AfCvdHeZ3RdfoSM45q7svoMQmw2NDD37z9F", (string)cid);

            var data = this.ipfs.Block.GetAsync(cid).Result;
            Assert.AreEqual(this.blob.Length, data.Size);
            CollectionAssert.AreEqual(this.blob, data.DataBytes);
        }

        [TestMethod]
        public void Put_Stream_Pinned()
        {
            var data1 = new MemoryStream(new byte[] { 23, 24, 127 });
            var cid1 = this.ipfs.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
            var pins = this.ipfs.Pin.ListAsync().Result;
            Assert.IsTrue(pins.Any(pin => pin == cid1));

            var data2 = new MemoryStream(new byte[] { 123, 124, 27 });
            var cid2 = this.ipfs.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
            pins = this.ipfs.Pin.ListAsync().Result;
            Assert.IsFalse(pins.Any(pin => pin == cid2));
        }

        [TestMethod]
        public void Get()
        {
            var _ = this.ipfs.Block.PutAsync(this.blob).Result;
            var block = this.ipfs.Block.GetAsync(this.id).Result;
            Assert.AreEqual(this.id, (string)block.Id);
            CollectionAssert.AreEqual(this.blob, block.DataBytes);
            var blob1 = new byte[this.blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            CollectionAssert.AreEqual(this.blob, blob1);
        }

        [TestMethod]
        public void Stat()
        {
            var _ = this.ipfs.Block.PutAsync(this.blob).Result;
            var info = this.ipfs.Block.StatAsync(this.id).Result;
            Assert.AreEqual(this.id, (string)info.Id);
            Assert.AreEqual(5, info.Size);
        }

        [TestMethod]
        public async Task Remove()
        {
            var _ = this.ipfs.Block.PutAsync(this.blob).Result;
            var cid = await this.ipfs.Block.RemoveAsync(this.id);
            Assert.AreEqual(this.id, (string)cid);
        }

        [TestMethod]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() => { var _ = this.ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result; });
        }

        [TestMethod]
        public async Task Remove_Unknown_OK()
        {
            var cid = await this.ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
            Assert.AreEqual(null, cid);
        }

    }
}
