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
    public class DnsApiTest
    {
        [TestMethod]
        public void Api_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.Dns);
        }

        [TestMethod]
        [Ignore("takes forever")]
        public async Task Publish()
        {
            var ipfs = TestFixture.Ipfs;
            using (var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                var content = await ipfs.FileSystem.AddTextAsync("hello world");
                var key = await ipfs.Key.CreateAsync("name-publish-test", "rsa", 1024);
                try
                {
                    var result = await ipfs.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
                    Assert.IsNotNull(result);
                    StringAssert.EndsWith(result.NamePath, key.Id.ToString());
                    StringAssert.EndsWith(result.ContentPath, content.Id.Encode());
                }
                finally
                {
                    await ipfs.Key.RemoveAsync(key.Name);
                }
            }
        }

        [TestMethod]
        public async Task Resolve()
        {
            var ipfs = TestFixture.Ipfs;
            var path = await ipfs.Dns.ResolveAsync("ipfs.io");
            Assert.AreEqual("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", path);
        }
    }
}
