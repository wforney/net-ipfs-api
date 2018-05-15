namespace Ipfs.Api
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    internal class NameApi : INameApi
    {
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="NameApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal NameApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>publish as an asynchronous operation.</summary>
        /// <param name="path">    The CID or path to the content to publish.</param>
        /// <param name="resolve"> Resolve <paramref name="path" /> before publishing. Defaults to <b>true</b>.</param>
        /// <param name="key">     The local key name used to sign the content. Defaults to "self".</param>
        /// <param name="lifetime">Duration that the record will be valid for. Defaults to 24 hours.</param>
        /// <param name="cancel">  
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is the
        ///     <see cref="T:Ipfs.NamedContent" /> of the published content.
        /// </returns>
        public async Task<NamedContent> PublishAsync(string path, bool resolve = true, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("name/publish", cancel,
                path,
                "lifetime=24h",
                $"resolve={resolve.ToString().ToLowerInvariant()}",
                $"key={key}");
            // TODO: lifetime
            var info = JObject.Parse(json);
            return new NamedContent
            {
                NamePath = (string)info["Name"],
                ContentPath = (string)info["Value"]
            };
        }

        /// <summary>Publish an IPFS name.</summary>
        /// <param name="id">      The <see cref="T:Ipfs.Cid" /> of the content to publish.</param>
        /// <param name="key">     The local key name used to sign the content. Defaults to "self".</param>
        /// <param name="lifetime">Duration that the record will be valid for. Defaults to 24 hours.</param>
        /// <param name="cancel">  
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is the
        ///     <see cref="T:Ipfs.NamedContent" /> of the published content.
        /// </returns>
        public Task<NamedContent> PublishAsync(Cid id, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default(CancellationToken))
            => PublishAsync("/ipfs/" + id.Encode(), resolve: false, key: key, lifetime: lifetime, cancel: cancel);

        /// <summary>Resolve an IPNS name.</summary>
        /// <param name="name">     An IPNS address, such as: /ipns/ipfs.io or a CID.</param>
        /// <param name="recursive">Resolve until the result is not an IPNS name. Defaults to <b>false</b>.</param>
        /// <param name="nocache">  Do not use cached entries. Defaults to <b>false</b>.</param>
        /// <param name="cancel">   
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is the resolved path as a
        ///     <see cref="T:System.String" />, such as <c>/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao</c>.
        /// </returns>
        public async Task<string> ResolveAsync(string name, bool recursive = false, bool nocache = false, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync(
                "name/resolve",
                cancel,
                name,
                $"recursive={recursive.ToString().ToLowerInvariant()}",
                $"nocache={nocache.ToString().ToLowerInvariant()}");

            return (string)(JObject.Parse(json)["Path"]);
        }
    }
}
