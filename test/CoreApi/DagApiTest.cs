using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Api
{
    [TestClass]
    public class DagApiTest
    {
        [TestMethod]
        public void GetAsync()
        {
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<NotImplementedException>(() => ipfs.Dag.GetAsync("cid"));
        }

        [TestMethod]
        public void PutAsync()
        {
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<NotImplementedException>(() => ipfs.Dag.PutAsync((object)null));
        }
    }
}
