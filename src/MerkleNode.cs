// <copyright file="MerkleNode.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>

namespace Ipfs.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     The IPFS <see href="https://github.com/ipfs/specs/tree/master/merkledag">MerkleDag</see> is the datastructure
    ///     at the heart of IPFS. It is an acyclic directed graph whose edges are hashes.
    /// </summary>
    /// <seealso cref="IMerkleNode{IMerkleLink}" />
    /// <seealso cref="IEquatable{MerkleNode}" />
    /// <remarks>Initially an <b>MerkleNode</b> is just constructed with its Cid.</remarks>
    public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
    {
        /// <summary>The prefix</summary>
        private const string Prefix = "/ipfs/";

        /// <summary>The merkle node lock</summary>
        private readonly object MerkleNodeLock = new object();

        /// <summary>The block size</summary>
        private long blockSize;

        /// <summary>The has block stats</summary>
        private bool HasBlockStats;

        /// <summary>The ipfs client</summary>
        private IpfsClient ipfsClient;

        /// <summary>The links</summary>
        private IEnumerable<IMerkleLink> links;

        /// <summary>The name</summary>
        private string name;

        /// <summary>
        ///     Creates a new instance of the <see cref="MerkleNode" /> with the specified <see cref="Cid" /> and
        ///     optional <see cref="Name" />.
        /// </summary>
        /// <param name="id">  The <see cref="Cid" /> of the node.</param>
        /// <param name="name">A name for the node.</param>
        /// <exception cref="ArgumentNullException">id</exception>
        public MerkleNode(Cid id, string name = null)
        {
            this.Id = id ?? throw new ArgumentNullException(nameof(id));
            this.Name = name;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="MerkleNode" /> with the specified <see cref="Id">cid</see> and
        ///     optional <see cref="Name" />.
        /// </summary>
        /// <param name="path">The string representation of a <see cref="Cid" /> of the node or "/ipfs/cid".</param>
        /// <param name="name">A name for the node.</param>
        /// <exception cref="ArgumentNullException">path</exception>
        public MerkleNode(string path, string name = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.StartsWith(Prefix, StringComparison.Ordinal))
            {
                path = path.Substring(Prefix.Length);
            }

            this.Id = Cid.Decode(path);
            this.Name = name;
        }

        /// <summary>Creates a new instance of the <see cref="MerkleNode" /> from the <see cref="IMerkleLink" />.</summary>
        /// <param name="link">The link to a node.</param>
        public MerkleNode(IMerkleLink link)
        {
            this.Id = link.Id;
            this.Name = link.Name;
            this.blockSize = link.Size;
            this.HasBlockStats = true;
        }

        /// <summary>Size of the raw, encoded node.</summary>
        /// <value>The size of the block.</value>
        public long BlockSize => this.blockSize = ReadBlockStatsAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        /// <remarks>It is never <b>null</b>.</remarks>
        public byte[] DataBytes => this.IpfsClient.Block.GetAsync(this.Id).GetAwaiter().GetResult().DataBytes;

        /// <inheritdoc />
        public Stream DataStream => this.IpfsClient.Block.GetAsync(this.Id).GetAwaiter().GetResult().DataStream;

        /// <inheritdoc />
        public Cid Id { get; }

        /// <inheritdoc />
        /// <remarks>
        ///     It is never <b>null</b>.
        ///     <para>
        ///         The links are sorted ascending by <see cref="P:Ipfs.IMerkleLink.Name" />. A <b>null</b> name is
        ///         compared as "".
        ///     </para>
        /// </remarks>
        public IEnumerable<IMerkleLink> Links => this.links ?? (this.links = this.IpfsClient.Object.LinksAsync(this.Id).GetAwaiter().GetResult());

        /// <summary>The name for the node. If unknown it is "" (not null).</summary>
        /// <value>The name.</value>
        public string Name
        {
            get => this.name;
            set => this.name = value ?? string.Empty;
        }

        /// <summary>The size (in bytes) of the data.</summary>
        /// <value>Number of bytes.</value>
        /// <inheritdoc />
        /// <seealso cref="BlockSize" />
        public long Size => this.BlockSize;

        /// <summary>Gets or sets the ipfs client.</summary>
        /// <value>The ipfs client.</value>
        internal IpfsClient IpfsClient
        {
            get
            {
                if (this.ipfsClient == null)
                {
                    lock (this.MerkleNodeLock)
                    {
                        this.ipfsClient = new IpfsClient();
                    }
                }

                return this.ipfsClient;
            }

            set => this.ipfsClient = value;
        }

        /// <summary>TODO</summary>
        /// <param name="hash">The hash.</param>
        /// <returns>The result of the conversion.</returns>
        static public implicit operator MerkleNode(string hash) => new MerkleNode(hash);

        /// <summary>TODO</summary>
        /// <param name="first"> a.</param>
        /// <param name="second">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MerkleNode first, MerkleNode second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return false;
            }

            if (object.ReferenceEquals(first, null))
            {
                return true;
            }

            if (object.ReferenceEquals(second, null))
            {
                return true;
            }

            return !first.Equals(second);
        }

        /// <summary>Implements the == operator.</summary>
        /// <param name="first"> The first node.</param>
        /// <param name="second">The second node.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MerkleNode first, MerkleNode second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return true;
            }

            if (object.ReferenceEquals(first, null))
            {
                return false;
            }

            if (object.ReferenceEquals(second, null))
            {
                return false;
            }

            return first.Equals(second);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as MerkleNode;
            return that != null && this.Id == that.Id;
        }

        /// <inheritdoc />
        public bool Equals(MerkleNode other) => other != null && this.Id == other.Id;

        /// <inheritdoc />
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <inheritdoc />
        public IMerkleLink ToLink(string name = null) => new DagLink(name ?? this.Name, this.Id, this.BlockSize);

        /// <inheritdoc />
        public override string ToString() => $"/ipfs/{this.Id}";

        /// <summary>Get block statistics about the node, <c>ipfs block stat <i>key</i></c></summary>
        /// <remarks>The object stats include the block stats.</remarks>
        private async Task<long> ReadBlockStatsAsync()
        {
            if (this.HasBlockStats)
            {
                return this.blockSize;
            }

            var stats = await this.IpfsClient.Block.StatAsync(this.Id);
            this.blockSize = stats.Size;

            this.HasBlockStats = true;

            return this.blockSize;
        }
    }
}
