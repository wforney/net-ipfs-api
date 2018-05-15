// <copyright file="BlockApi.cs" company="Richard Schneider">
//     © 2015-2018 Richard Schneider
// </copyright>

namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Class BlockApi.
    /// </summary>
    /// <seealso cref="Ipfs.CoreApi.IBlockApi" />
    ///
    internal class BlockApi : IBlockApi
    {
        /// <summary>
        ///     The ipfs
        /// </summary>
        private IpfsClient ipfs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BlockApi" /> class.
        /// </summary>
        /// <param name="ipfs">
        ///     The ipfs.
        /// </param>
        internal BlockApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///     get as an asynchronous operation.
        /// </summary>
        /// <param name="id">
        ///     The <see cref="T:Ipfs.Cid" /> of the block.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task's value contains the block's id and data.
        /// </returns>
        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken)) // TODO CID support
        {
            var data = await this.ipfs.DownloadBytesAsync("block/get", cancel, id);
            return new Block
            {
                DataBytes = data,
                Id = id
            };
        }

        /// <summary>
        ///     put as an asynchronous operation.
        /// </summary>
        /// <param name="data">
        ///     The byte array to send to the IPFS network.
        /// </param>
        /// <param name="contentType">
        ///     The content type or format of the <paramref name="data" />; such as "raw" or "dag-db". See
        ///     <see cref="T:Ipfs.MultiCodec" /> for more details.
        /// </param>
        /// <param name="multiHash">
        ///     The <see cref="T:Ipfs.MultiHash" /> algorithm name used to produce the <see cref="T:Ipfs.Cid" />.
        /// </param>
        /// <param name="pin">
        ///     If <b>true</b> the block is pinned to local storage and will not be garbage collected. The default is <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous put operation. The task's value is the block's <see cref="T:Ipfs.Cid" />.
        /// </returns>
        public async Task<Cid> PutAsync(
            byte[] data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default(CancellationToken))
        {
            var options = new List<string>();
            if (multiHash != MultiHash.DefaultAlgorithmName || contentType != Cid.DefaultContentType)
            {
                options.Add($"mhtype={multiHash}");
                options.Add($"format={contentType}");
            }
            var json = await this.ipfs.UploadAsync("block/put", cancel, data, options.ToArray());
            var info = JObject.Parse(json);
            Cid cid = (string)info["Key"];

            if (pin)
            {
                await this.ipfs.Pin.AddAsync(cid, recursive: false, cancel: cancel);
            }

            return cid;
        }

        /// <summary>
        ///     put as an asynchronous operation.
        /// </summary>
        /// <param name="data">
        ///     The <see cref="T:System.IO.Stream" /> of data to send to the IPFS network.
        /// </param>
        /// <param name="contentType">
        ///     The content type or format of the <paramref name="data" />; such as "raw" or "dag-db". See
        ///     <see cref="T:Ipfs.MultiCodec" /> for more details.
        /// </param>
        /// <param name="multiHash">
        ///     The <see cref="T:Ipfs.MultiHash" /> algorithm name used to produce the <see cref="T:Ipfs.Cid" />.
        /// </param>
        /// <param name="pin">
        ///     If <b>true</b> the block is pinned to local storage and will not be garbage collected. The default is <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous put operation. The task's value is the block's <see cref="T:Ipfs.Cid" />.
        /// </returns>
        public async Task<Cid> PutAsync(
            Stream data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default(CancellationToken))
        {
            var options = new List<string>();
            if (multiHash != MultiHash.DefaultAlgorithmName || contentType != Cid.DefaultContentType)
            {
                options.Add($"mhtype={multiHash}");
                options.Add($"format={contentType}");
            }
            var json = await this.ipfs.UploadAsync("block/put", cancel, data, null, options.ToArray());
            var info = JObject.Parse(json);
            Cid cid = (string)info["Key"];

            if (pin)
            {
                await this.ipfs.Pin.AddAsync(cid, recursive: false, cancel: cancel);
            }

            return cid;
        }

        /// <summary>
        ///     remove as an asynchronous operation.
        /// </summary>
        /// <param name="id">
        ///     The <see cref="T:Ipfs.Cid" /> of the block.
        /// </param>
        /// <param name="ignoreNonexistent">
        ///     If <b>true</b> do not raise exception when <paramref name="id" /> does not exist. Default value is <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     The awaited Task will return the deleted <paramref name="id" /> or <b>null</b> if the
        ///     <paramref name="id" /> does not exist and <paramref name="ignoreNonexistent" /> is <b>true</b>.
        /// </returns>
        /// <exception cref="HttpRequestException">
        /// </exception>
        /// <remarks>
        ///     This removes the block from the local cache and does not affect other peers.
        /// </remarks>
        public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken)) // TODO CID support
        {
            var json = await this.ipfs.DoCommandAsync("block/rm", cancel, id, "force=" + ignoreNonexistent.ToString().ToLowerInvariant());
            if (json.Length == 0)
            {
                return null;
            }

            var result = JObject.Parse(json);
            var error = (string)result["Error"];
            if (error != null)
            {
                throw new HttpRequestException(error);
            }

            return (Cid)(string)result["Hash"];
        }

        /// <summary>
        ///     stat as an asynchronous operation.
        /// </summary>
        /// <param name="id">
        ///     The <see cref="T:Ipfs.Cid" /> of the block.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value contains the block's id and size or <b>null</b>.
        /// </returns>
        /// <remarks>
        ///     Only the local repository is consulted for the block. If <paramref name="id" /> does not exist, then
        ///     <b>null</b> is retuned.
        /// </remarks>
        public async Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("block/stat", cancel, id);
            var info = JObject.Parse(json);
            return new Block
            {
                Size = (long)info["Size"],
                Id = (string)info["Key"]
            };
        }
    }
}
