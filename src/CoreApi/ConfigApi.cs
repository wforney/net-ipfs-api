using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Api
{

    internal class ConfigApi : IConfigApi
    {
        private IpfsClient ipfs;

        internal ConfigApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<JObject> GetAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("config/show", cancel);
            return JObject.Parse(json);
        }

        public async Task<JToken> GetAsync(string key, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("config", cancel, key);
            var r = JObject.Parse(json);
            return r["Value"];
        }

        public async Task SetAsync(string key, string value, CancellationToken cancel = default(CancellationToken))
        {
            var _ = await this.ipfs.DoCommandAsync("config", cancel, key, "arg=" + value);
            return;
        }

        public async Task SetAsync(string key, JToken value, CancellationToken cancel = default(CancellationToken))
        {
            var _ = await this.ipfs.DoCommandAsync("config", cancel,
                key,
                "arg=" + value.ToString(Formatting.None),
                "json=true");
            return;
        }

        public Task ReplaceAsync(JObject config)
        {
            throw new NotImplementedException();
        }
    }

}
