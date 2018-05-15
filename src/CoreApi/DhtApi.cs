namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    internal class DhtApi : IDhtApi
    {
        private static readonly ILog Log = LogManager.GetLogger<DhtApi>();

        private IpfsClient ipfs;

        internal DhtApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken)) => this.ipfs.IdAsync(id, cancel);

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var stream = await this.ipfs.PostDownloadAsync("dht/findprovs", cancel, id);
            return ProviderFromStream(stream);
        }

        private static IEnumerable<Peer> ProviderFromStream(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                while (!sr.EndOfStream)
                {
                    var json = sr.ReadLine();
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat("Provider {0}", json);
                    }

                    var r = JObject.Parse(json);
                    var id = (string)r["ID"];
                    if (id != string.Empty)
                    {
                        yield return new Peer { Id = new MultiHash(id) };
                    }
                    else
                    {
                        var responses = (JArray)r["Responses"];
                        if (responses != null)
                        {
                            foreach (var response in responses)
                            {
                                var rid = (string)response["ID"];
                                if (rid != string.Empty)
                                {
                                    yield return new Peer { Id = new MultiHash(rid) };
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
