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
    public class BootstapApiTest
    {
        private IpfsClient ipfs = TestFixture.Ipfs;
        private MultiAddress somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        [TestMethod]
        public async Task Add_Remove()
        {
            var addr = await this.ipfs.Bootstrap.AddAsync(this.somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(this.somewhere, addr);
            var addrs = await this.ipfs.Bootstrap.ListAsync();
            Assert.IsTrue(addrs.Any(a => a == this.somewhere));

            addr = await this.ipfs.Bootstrap.RemoveAsync(this.somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(this.somewhere, addr);
            addrs = await this.ipfs.Bootstrap.ListAsync();
            Assert.IsFalse(addrs.Any(a => a == this.somewhere));
        }

        [TestMethod]
        public async Task List()
        {
            var addrs = await this.ipfs.Bootstrap.ListAsync();
            Assert.IsNotNull(addrs);
            Assert.AreNotEqual(0, addrs.Count());
        }

        [TestMethod]
        public async Task Remove_All()
        {
            var original = await this.ipfs.Bootstrap.ListAsync();
            await this.ipfs.Bootstrap.RemoveAllAsync();
            var addrs = await this.ipfs.Bootstrap.ListAsync();
            Assert.AreEqual(0, addrs.Count());
            foreach (var addr in original)
            {
                await this.ipfs.Bootstrap.AddAsync(addr);
            }
        }

        [TestMethod]
        public async Task Add_Defaults()
        {
            var original = await this.ipfs.Bootstrap.ListAsync();
            await this.ipfs.Bootstrap.RemoveAllAsync();
            try
            {
                await this.ipfs.Bootstrap.AddDefaultsAsync();
                var addrs = await this.ipfs.Bootstrap.ListAsync();
                Assert.AreNotEqual(0, addrs.Count());
            }
            finally
            {
                await this.ipfs.Bootstrap.RemoveAllAsync();
                foreach (var addr in original)
                {
                    await this.ipfs.Bootstrap.AddAsync(addr);
                }
            }
        }
    }
}
