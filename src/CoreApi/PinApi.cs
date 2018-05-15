// <copyright file="PinApi.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>
namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>Class PinApi.</summary>
    /// <seealso cref="Ipfs.CoreApi.IPinApi" />
    internal class PinApi : IPinApi
    {
        /// <summary>The ipfs</summary>
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="PinApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal PinApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>add as an asynchronous operation.</summary>
        /// <param name="path">     
        ///     A CID or path to an existing object, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about" or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="recursive">
        ///     <b>true</b> to recursively pin links of the object; otherwise, <b>false</b> to only pin the specified
        ///     object. Default is <b>true</b>.
        /// </param>
        /// <param name="cancel">   
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a sequence of
        ///     <see cref="T:Ipfs.Cid" /> that were pinned.
        /// </returns>
        public async Task<IEnumerable<Cid>> AddAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var opts = "recursive=" + recursive.ToString().ToLowerInvariant();
            var json = await this.ipfs.DoCommandAsync("pin/add", cancel, path, opts);
            return ((JArray)JObject.Parse(json)["Pins"]).Cast<string>().Cast<Cid>();
        }

        /// <summary>list as an asynchronous operation.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a sequence of <see cref="T:Ipfs.Cid" />.</returns>
        public async Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("pin/ls", cancel);
            var keys = (JObject)(JObject.Parse(json)["Keys"]);
            return keys
                .Properties()
                .Select(p => p.Name)
                .Cast<Cid>();
        }

        /// <summary>remove as an asynchronous operation.</summary>
        /// <param name="id">       The CID of the object.</param>
        /// <param name="recursive">
        ///     <b>true</b> to recursively unpin links of object; otherwise, <b>false</b> to only unpin the specified
        ///     object. Default is <b>true</b>.
        /// </param>
        /// <param name="cancel">   
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a sequence of
        ///     <see cref="T:Ipfs.Cid" /> that were unpinned.
        /// </returns>
        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var opts = $"recursive={recursive.ToString().ToLowerInvariant()}";
            var json = await this.ipfs.DoCommandAsync("pin/rm", cancel, id, opts);
            return ((JArray)JObject.Parse(json)["Pins"]).Cast<string>().Cast<Cid>();
        }
    }
}
