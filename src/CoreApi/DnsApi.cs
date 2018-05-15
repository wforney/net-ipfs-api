namespace Ipfs.Api
{
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    internal class DnsApi : IDnsApi
    {
        private IpfsClient ipfs;

        internal DnsApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync(
                "dns",
                cancel,
                name,
                $"recursive={recursive.ToString().ToLowerInvariant()}");

            return (string)(JObject.Parse(json)["Path"]);
        }
    }
}
