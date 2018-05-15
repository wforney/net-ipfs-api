namespace Ipfs.Api
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A list of trusted peers.</summary>
    /// <remarks>
    ///     This is the list of peers that are initially trusted by IPFS. Its equivalent to the
    ///     <see href="https://ipfs.io/ipfs/QmTkzDwWqPbnAh5YiV5VwcTLnGdwSNsNTn2aDxdXBFca7D/example#/ipfs/QmThrNbvLj7afQZhxH72m5Nn1qiVn3eMKWFYV49Zp2mv9B/bootstrap/readme.md">ipfs
    ///     bootstrap</see> command.
    /// </remarks>
    /// <returns>
    ///     A series of <see cref="MultiAddress" />. Each address ends with an IPNS node id, for example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ".
    /// </returns>
    public class TrustedPeerCollection : ICollection<MultiAddress>
    {
        /// <summary>The interplanetary file system client.</summary>
        private readonly IpfsClient IpFs;

        /// <summary>The peers</summary>
        private MultiAddress[] Peers;

        /// <summary>Initializes a new instance of the <see cref="TrustedPeerCollection" /> class.</summary>
        /// <param name="ipfs">The interplanetary file system.</param>
        internal TrustedPeerCollection(IpfsClient ipfs)
        {
            this.IpFs = ipfs;
        }

        /// <inheritdoc />
        public int Count => this.CountAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        void ICollection<MultiAddress>.Add(MultiAddress item) => this.AddAsync(item).GetAwaiter().GetResult();

        /// <summary>Adds the specified peer.</summary>
        /// <param name="peer">The peer to add.</param>
        /// <returns>A Task.</returns>
        /// <exception cref="ArgumentNullException">peer</exception>
        public async Task AddAsync(MultiAddress peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException(nameof(peer));
            }

            await this.IpFs.DoCommandAsync("bootstrap/add", default(CancellationToken), peer.ToString());
            this.Peers = null;
        }

        /// <summary>Add the default bootstrap nodes to the trusted peers.</summary>
        /// <remarks>Equivalent to <c>ipfs bootstrap add --default</c>.</remarks>
        public async Task AddDefaultNodesAsync()
        {
            await this.IpFs.DoCommandAsync("bootstrap/add", default(CancellationToken), null, "default=true");
            this.Peers = null;
        }

        /// <inheritdoc />
        void ICollection<MultiAddress>.Clear() => this.ClearAsync().GetAwaiter().GetResult();

        /// <summary>Remove all the trusted peers.</summary>
        /// <remarks>Equivalent to <c>ipfs bootstrap rm --all</c>.</remarks>
        public async Task ClearAsync()
        {
            await this.IpFs.DoCommandAsync("bootstrap/rm", default(CancellationToken), null, "all=true");
            this.Peers = null;
        }

        /// <inheritdoc />
        bool ICollection<MultiAddress>.Contains(MultiAddress item) => this.ContainsAsync(item).GetAwaiter().GetResult();

        /// <summary>contains as an asynchronous operation.</summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> ContainsAsync(MultiAddress item)
        {
            await FetchAsync();
            return this.Peers.Contains(item);
        }

        /// <inheritdoc />
        void ICollection<MultiAddress>.CopyTo(MultiAddress[] array, int arrayIndex) => this.CopyToAsync(array, arrayIndex).GetAwaiter().GetResult();

        /// <summary>copy to as an asynchronous operation.</summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The starting index.</param>
        /// <returns>Task.</returns>
        public async Task CopyToAsync(MultiAddress[] array, int index)
        {
            await FetchAsync();
            this.Peers.CopyTo(array, index);
        }

        /// <summary>count as an asynchronous operation.</summary>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        public async Task<int> CountAsync()
        {
            if (this.Peers == null)
            {
                await FetchAsync();
            }

            return this.Peers.Length;
        }

        /// <inheritdoc />
        IEnumerator<MultiAddress> IEnumerable<MultiAddress>.GetEnumerator() => this.GetEnumeratorAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumeratorAsync().GetAwaiter().GetResult();

        /// <summary>Gets the enumerator.</summary>
        /// <returns>Task&lt;IEnumerator&lt;MultiAddress&gt;&gt;.</returns>
        public async Task<IEnumerator<MultiAddress>> GetEnumeratorAsync()
        {
            await FetchAsync();
            return ((IEnumerable<MultiAddress>)this.Peers).GetEnumerator();
        }

        /// <inheritdoc />
        bool ICollection<MultiAddress>.Remove(MultiAddress item) => this.RemoveAsync(item).GetAwaiter().GetResult();

        /// <summary>Remove the trusted peer.</summary>
        /// <param name="peer">The peer to remove.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">peer cannot be null.</exception>
        /// <remarks>Equivalent to <c>ipfs bootstrap rm <i>peer</i></c>.</remarks>
        public async Task<bool> RemoveAsync(MultiAddress peer)
        {
            if (peer == null)
            {
                throw new ArgumentNullException(nameof(peer));
            }

            await this.IpFs.DoCommandAsync("bootstrap/rm", default(CancellationToken), peer.ToString());
            this.Peers = null; // clear local cache
            return true;
        }

        /// <summary>fetch as an asynchronous operation.</summary>
        /// <returns>Task&lt;MultiAddress[]&gt;.</returns>
        private async Task<MultiAddress[]> FetchAsync()
        {
            var response = await this.IpFs.DoCommandAsync<BootstrapListResponse>("bootstrap/list", default(CancellationToken));
            this.Peers = response.Peers;
            return response.Peers;
        }

        /// <summary>Class BootstrapListResponse.</summary>
        private class BootstrapListResponse
        {
            /// <summary>Gets or sets the peers.</summary>
            /// <value>The peers.</value>
            public MultiAddress[] Peers { get; set; }
        }
    }
}
