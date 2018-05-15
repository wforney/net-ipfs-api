namespace Ipfs.Api
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <inheritdoc />
    public class FileSystemNode : IFileSystemNode
    {
        private readonly object Lock = new object();
        private IpfsClient ipfsClient;
        private bool? isDirectory;
        private IEnumerable<IFileSystemLink> links;
        private long? size;

        /// <inheritdoc />
        public byte[] DataBytes
        {
            get
            {
                using (var stream = this.DataStream)
                using (var data = new MemoryStream())
                {
                    stream.CopyTo(data);
                    return data.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public Stream DataStream => this.IpfsClient.FileSystem.ReadFileAsync(this.Id).GetAwaiter().GetResult();

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <summary>Determines if the link is a directory (folder).</summary>
        /// <value><b>true</b> if the link is a directory; Otherwise <b>false</b>, the link is some type of a file.</value>
        public bool IsDirectory
        {
            get
            {
                if (!this.isDirectory.HasValue)
                {
                    GetInfoAsync().GetAwaiter().GetResult();
                }

                return this.isDirectory.Value;
            }
            set => this.isDirectory = value;
        }

        /// <inheritdoc />
        public IEnumerable<IFileSystemLink> Links
        {
            get
            {
                if (this.links == null)
                {
                    GetInfoAsync().GetAwaiter().GetResult();
                }

                return this.links;
            }
            set => this.links = value;
        }

        /// <summary>The file name of the IPFS node.</summary>
        public string Name { get; set; }

        /// <summary>Size of the file contents.</summary>
        /// <value>This is the size of the file not the raw encoded contents of the block.</value>
        public long Size
        {
            get
            {
                if (!this.size.HasValue)
                {
                    GetInfoAsync().GetAwaiter().GetResult();
                }

                return this.size.Value;
            }
            set => this.size = value;
        }

        /// <summary>Gets or sets the ipfs client.</summary>
        /// <value>The ipfs client.</value>
        internal IpfsClient IpfsClient
        {
            get
            {
                if (this.ipfsClient == null)
                {
                    lock (this.Lock)
                    {
                        this.ipfsClient = new IpfsClient();
                    }
                }

                return this.ipfsClient;
            }
            set => this.ipfsClient = value;
        }

        /// <inheritdoc />
        public IFileSystemLink ToLink(string name = "")
        {
            return new FileSystemLink
            {
                Name = string.IsNullOrWhiteSpace(name) ? this.Name : name,
                Id = Id,
                Size = Size,
                IsDirectory = IsDirectory
            };
        }

        /// <summary>Gets the information.</summary>
        /// <returns>Task.</returns>
        private async Task GetInfoAsync()
        {
            var node = await this.IpfsClient.FileSystem.ListFileAsync(this.Id);
            this.IsDirectory = node.IsDirectory;
            this.Links = node.Links;
            this.Size = node.Size;
        }
    }
}
