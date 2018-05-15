namespace Ipfs.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    internal class KeyApi : IKeyApi
    {
        /// <summary>The ipfs</summary>
        private readonly IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="KeyApi" /> class.</summary>
        /// <param name="ipfs">The interplanetary file system.</param>
        internal KeyApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>create as an asynchronous operation.</summary>
        /// <param name="name">   The local name of the key.</param>
        /// <param name="keyType">The type of key to create; "rsa" or "ed25519".</param>
        /// <param name="size">   The size, in bits, of the key.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the key that was created.
        /// </returns>
        public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
            => await this.ipfs.DoCommandAsync<KeyInfo>("key/gen", cancel,
                name,
                $"type={keyType}",
                $"size={size}");

        /// <summary>Export a key to a PEM encoded password protected PKCS #8 container.</summary>
        /// <param name="name">    The local name of the key.</param>
        /// <param name="password">The PEM's password.</param>
        /// <param name="cancel">  
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the password protected PEM string.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        /// <summary>Import a key from a PEM encoded password protected PKCS #8 container.</summary>
        /// <param name="name">    The local name of the key.</param>
        /// <param name="pem">     The PEM encoded PKCS #8 container.</param>
        /// <param name="password">The <paramref name="pem" />'s password.</param>
        /// <param name="cancel">  
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the newly imported key.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        /// <summary>list as an asynchronous operation.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is a sequence of IPFS keys.
        /// </returns>
        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("key/list", cancel, null, "l=true");
            var keys = (JArray)(JObject.Parse(json)["Keys"]);
            return keys
                .Select(k => new KeyInfo
                {
                    Id = (string)k["Id"],
                    Name = (string)k["Name"]
                });
        }

        /// <summary>remove as an asynchronous operation.</summary>
        /// <param name="name">  The local name of the key.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the key that was deleted.
        /// </returns>
        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("key/rm", cancel, name);
            var keys = (JArray)(JObject.Parse(json)["Keys"]);
            return keys
                .Select(k => new KeyInfo
                {
                    Id = (string)k["Id"],
                    Name = (string)k["Name"]
                })
                .First();
        }

        /// <summary>Rename the specified key.</summary>
        /// <param name="oldName">The local name of the key.</param>
        /// <param name="newName">The new local name of the key.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is a sequence of IPFS keys that were renamed.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken)) => throw new NotImplementedException();

        /// <summary>Information about a local key.</summary>
        public class KeyInfo : IKey
        {
            /// <inheritdoc />
            public MultiHash Id { get; set; }

            /// <inheritdoc />
            public string Name { get; set; }

            /// <inheritdoc />
            public override string ToString() => this.Name;
        }
    }
}
