// <copyright file="ObjectApi.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>

namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>Class ObjectApi.</summary>
    /// <seealso cref="Ipfs.CoreApi.IObjectApi" />
    internal class ObjectApi : IObjectApi
    {
        /// <summary>The log</summary>
        private static ILog Log = LogManager.GetLogger<ObjectApi>();

        /// <summary>The ipfs</summary>
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="ObjectApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal ObjectApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>Get the data of a MerkleDAG node.</summary>
        /// <param name="id">    The <see cref="T:Ipfs.Cid" /> of the node.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a stream of data.</returns>
        /// <remarks>The caller must dispose the returned <see cref="T:System.IO.Stream" />.</remarks>
        public Task<Stream> DataAsync(Cid id, CancellationToken cancel = default(CancellationToken)) => this.ipfs.DownloadAsync("object/data", cancel, id);

        /// <summary>get as an asynchronous operation.</summary>
        /// <param name="id">    The <see cref="T:Ipfs.Cid" /> to the node.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a <see cref="T:Ipfs.DagNode" />.</returns>
        public async Task<DagNode> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("object/get", cancel, id);
            return GetDagFromJson(json);
        }

        /// <summary>links as an asynchronous operation.</summary>
        /// <param name="id">    The <see cref="T:Ipfs.Cid" /> id of the node.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a sequence of links.</returns>
        public async Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("object/links", cancel, id);
            return GetDagFromJson(json).Links;
        }

        /// <summary>new as an asynchronous operation.</summary>
        /// <param name="template"><b>null</b> or "unixfs-dir".</param>
        /// <param name="cancel">  
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a <see cref="T:Ipfs.DagNode" /> to
        ///     the new directory.
        /// </returns>
        /// <remarks>Caveat: So far, only UnixFS object layouts are supported.</remarks>
        public async Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("object/new", cancel, template);
            var hash = (string)(JObject.Parse(json)["Hash"]);
            return await GetAsync(hash);
        }

        /// <summary>Creates a new file directory in IPFS.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a <see cref="T:Ipfs.DagNode" /> to
        ///     the new directory.
        /// </returns>
        /// <remarks>Equivalent to <c>NewAsync("unixfs-dir")</c>.</remarks>
        public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default(CancellationToken)) => NewAsync("unixfs-dir", cancel);

        /// <summary>Store a MerkleDAG node.</summary>
        /// <param name="data">  The opaque data, can be <b>null</b>.</param>
        /// <param name="links"> The links to other nodes.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a <see cref="T:Ipfs.DagNode" />.</returns>
        public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null, CancellationToken cancel = default(CancellationToken)) => PutAsync(new DagNode(data, links), cancel);

        /// <summary>put as an asynchronous operation.</summary>
        /// <param name="node">  A merkle dag</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a <see cref="T:Ipfs.DagNode" />.</returns>
        public async Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.UploadAsync("object/put", cancel, node.ToArray(), "inputenc=protobuf");
            return node;
        }

        /// <summary>Get the statistics of a MerkleDAG node.</summary>
        /// <param name="id">    The <see cref="Cid" /> of the node.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>Task&lt;DagInfo&gt;.</returns>
        public Task<DagInfo> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken)) => this.ipfs.DoCommandAsync<DagInfo>("object/stat", cancel, id);

        /// <summary>Gets the dag from json.</summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>DagNode.</returns>
        private static DagNode GetDagFromJson(string json)
        {
            var result = JObject.Parse(json);
            byte[] data = null;
            var stringData = (string)result["Data"];
            if (stringData != null)
            {
                data = Encoding.UTF8.GetBytes(stringData);
            }

            var links = ((JArray)result["Links"])
                .Select(link => new DagLink(
                    (string)link["Name"],
                    (string)link["Hash"],
                    (long)link["Size"]));
            return new DagNode(data, links);
        }

        /// <summary>Class DagInfo.</summary>
        public class DagInfo
        {
            /// <summary>Gets or sets the size of the block.</summary>
            /// <value>The size of the block.</value>
            public long BlockSize { get; set; }

            /// <summary>Gets or sets the size of the cumulative.</summary>
            /// <value>The size of the cumulative.</value>
            public long CumulativeSize { get; set; }

            /// <summary>Gets or sets the size of the data.</summary>
            /// <value>The size of the data.</value>
            public long DataSize { get; set; }

            /// <summary>Gets or sets the hash.</summary>
            /// <value>The hash.</value>
            public string Hash { get; set; }

            /// <summary>Gets or sets the size of the links.</summary>
            /// <value>The size of the links.</value>
            public long LinksSize { get; set; }

            /// <summary>Gets or sets the number links.</summary>
            /// <value>The number links.</value>
            public int NumLinks { get; set; }
        }

        // TOOD: patch sub API
    }
}
