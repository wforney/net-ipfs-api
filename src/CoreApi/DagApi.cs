namespace Ipfs.Api
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    internal class DagApi : IDagApi
    {
        private IpfsClient ipfs;

        internal DagApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<JObject> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        public Task<JToken> GetAsync(string path, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        public Task<T> GetAsync<T>(Cid id, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        public Task<Cid> PutAsync(JObject data, string contentType = "cbor", string multiHash = "sha2-256", bool pin = true, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        public Task<Cid> PutAsync(Stream data, string contentType = "cbor", string multiHash = "sha2-256", bool pin = true, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        public Task<Cid> PutAsync(object data, string contentType = "cbor", string multiHash = "sha2-256", bool pin = true, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();
    }
}
