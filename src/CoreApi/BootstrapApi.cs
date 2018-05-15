// <copyright file="BootstrapApi.cs" company="Richard Schneider">
//     © 2015-2018 Richard Schneider
// </copyright>

namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Class BootstrapApi.
    /// </summary>
    /// <seealso cref="Ipfs.CoreApi.IBootstrapApi" />
    ///
    internal class BootstrapApi : IBootstrapApi
    {
        /// <summary>
        ///     The ipfs
        /// </summary>
        private IpfsClient ipfs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BootstrapApi" /> class.
        /// </summary>
        /// <param name="ipfs">
        ///     The ipfs.
        /// </param>
        internal BootstrapApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///     add as an asynchronous operation.
        /// </summary>
        /// <param name="address">
        ///     The address must end with the ipfs protocol and the public ID of the peer. For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the address that was added or
        ///     <b>null</b> if the address is already in the bootstrap list.
        /// </returns>
        public async Task<MultiAddress> AddAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("bootstrap/add", cancel, address.ToString());
            var addrs = (JArray)(JObject.Parse(json)["Peers"]);
            var a = addrs.FirstOrDefault();
            if (a == null)
            {
                return null;
            }

            return new MultiAddress((string)a);
        }

        /// <summary>
        ///     add defaults as an asynchronous operation.
        /// </summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the sequence of addresses that
        ///     were added.
        /// </returns>
        public async Task<IEnumerable<MultiAddress>> AddDefaultsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("bootstrap/add/default", cancel);
            var addrs = (JArray)(JObject.Parse(json)["Peers"]);
            return addrs
                .Select(a => new MultiAddress((string)a));
        }

        /// <summary>
        ///     list as an asynchronous operation.
        /// </summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is a sequence of addresses.
        /// </returns>
        public async Task<IEnumerable<MultiAddress>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("bootstrap/list", cancel);
            var addrs = (JArray)(JObject.Parse(json)["Peers"]);
            return addrs
                .Select(a => new MultiAddress((string)a));
        }

        /// <summary>
        ///     Remove all the peers.
        /// </summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        public Task RemoveAllAsync(CancellationToken cancel = default(CancellationToken))
        {
            return this.ipfs.DoCommandAsync("bootstrap/rm/all", cancel);
        }

        /// <summary>
        ///     remove as an asynchronous operation.
        /// </summary>
        /// <param name="address">
        ///     The address must end with the ipfs protocol and the public ID of the peer. For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the address that was removed or
        ///     <b>null</b> if the <paramref name="address" /> is not in the bootstrap list.
        /// </returns>
        public async Task<MultiAddress> RemoveAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("bootstrap/rm", cancel, address.ToString());
            var addrs = (JArray)(JObject.Parse(json)["Peers"]);
            var a = addrs.FirstOrDefault();
            if (a == null)
            {
                return null;
            }

            return new MultiAddress((string)a);
        }
    }
}
