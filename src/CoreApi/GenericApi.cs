namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    public partial class IpfsClient : IGenericApi
    {
        /// <inheritdoc />
        public Task<Peer> IdAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken)) => DoCommandAsync<Peer>("id", cancel, peer?.ToString());

        /// <summary>resolve as an asynchronous operation.</summary>
        /// <param name="name">     The name to resolve.</param>
        /// <param name="recursive">Resolve until the result is an IPFS name. Defaults to <b>false</b>.</param>
        /// <param name="cancel">   
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is the resolved path as a <see cref="T:System.String" />.
        /// </returns>
        /// <remarks>The <paramref name="name" /> can be <see cref="T:Ipfs.Cid" /> + [path], "/ipfs/..." or "/ipns/...".</remarks>
        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            var json = await DoCommandAsync("resolve", cancel,
                name,
                $"recursive={recursive.ToString().ToLowerInvariant()}");
            return (string)(JObject.Parse(json)["Path"]);
        }

        /// <inheritdoc />
        public async Task ShutdownAsync() => await DoCommandAsync("shutdown", default(CancellationToken));

        /// <inheritdoc />
        public Task<Dictionary<string, string>> VersionAsync(CancellationToken cancel = default(CancellationToken)) => DoCommandAsync<Dictionary<string, string>>("version", cancel);
    }
}
