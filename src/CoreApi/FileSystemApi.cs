namespace Ipfs.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Ipfs.CoreApi;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class FileSystemApi : IFileSystemApi
    {
        private static readonly ILog Log = LogManager.GetLogger<FileSystemApi>();

        private readonly IpfsClient IpFs;
        private readonly AsyncLazy<DagNode> EmptyFolder;

        /// <summary>Initializes a new instance of the <see cref="FileSystemApi" /> class.</summary>
        /// <param name="ipfs">The interplanetary file system.</param>
        internal FileSystemApi(IpfsClient ipfs)
        {
            this.IpFs = ipfs;
            this.EmptyFolder = new AsyncLazy<DagNode>(() => ipfs.Object.NewDirectoryAsync());
        }

        /// <summary>add file as an asynchronous operation.</summary>
        /// <param name="path">   The name of the local file.</param>
        /// <param name="options">The options when adding data to the IPFS file system.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is the file's node.</returns>
        public async Task<IFileSystemNode> AddFileAsync(string path, AddFileOptions options = null, CancellationToken cancel = default(CancellationToken))
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await AddAsync(stream, Path.GetFileName(path), options, cancel);
            }
        }

        /// <summary>Add some text to the interplanetary file system.</summary>
        /// <param name="text">   The string to add to IPFS. It is UTF-8 encoded.</param>
        /// <param name="options">The options when adding data to the IPFS file system.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is the text's node.</returns>
        public Task<IFileSystemNode> AddTextAsync(string text, AddFileOptions options = null, CancellationToken cancel = default(CancellationToken))
            => AddAsync(new MemoryStream(Encoding.UTF8.GetBytes(text), false), "", options, cancel);

        /// <summary>add as an asynchronous operation.</summary>
        /// <param name="stream"> The stream of data to add to IPFS.</param>
        /// <param name="name">   A name for the <paramref name="stream" />.</param>
        /// <param name="options">The options when adding data to the IPFS file system.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is the data's node.</returns>
        public async Task<IFileSystemNode> AddAsync(Stream stream, string name = "", AddFileOptions options = null, CancellationToken cancel = default(CancellationToken))
        {
            if (options == null)
            {
                options = new AddFileOptions();
            }

            var opts = new List<string>();
            if (!options.Pin)
            {
                opts.Add("pin=false");
            }

            if (options.Wrap)
            {
                opts.Add("wrap-with-directory=true");
            }

            if (options.RawLeaves)
            {
                opts.Add("raw-leaves=true");
            }

            if (options.OnlyHash)
            {
                opts.Add("only-hash=true");
            }

            if (options.Trickle)
            {
                opts.Add("trickle=true");
            }

            if (options.Hash != "sha2-256")
            {
                opts.Add($"hash=${options.Hash}");
            }

            opts.Add($"chunker=size-{options.ChunkSize}");

            var json = await this.IpFs.UploadAsync("add", cancel, stream, name, opts.ToArray());

            // The result is a stream of LDJSON objects. See https://github.com/ipfs/go-ipfs/issues/4852
            FileSystemNode fsn = null;
            using (var sr = new StringReader(json))
            using (var jr = new JsonTextReader(sr) { SupportMultipleContent = true })
            {
                while (jr.Read())
                {
                    var r = await JObject.LoadAsync(jr, cancel);
                    fsn = new FileSystemNode
                    {
                        Id = (string)r["Hash"],
                        Size = long.Parse((string)r["Size"]),
                        IsDirectory = false,
                        Name = name,
                        IpfsClient = IpFs
                    };
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"added {fsn.Id} {fsn.Name}");
                    }
                }
            }

            fsn.IsDirectory = options.Wrap;
            return fsn;
        }

        /// <summary>add directory as an asynchronous operation.</summary>
        /// <param name="path">     The path to directory.</param>
        /// <param name="recursive"><b>true</b> to add sub-folders.</param>
        /// <param name="options">  The options when adding data to the IPFS file system.</param>
        /// <param name="cancel">   
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is the directory's node.</returns>
        public async Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, AddFileOptions options = null, CancellationToken cancel = default(CancellationToken))
        {
            if (options == null)
            {
                options = new AddFileOptions();
            }

            options.Wrap = false;

            // Add the files and sub-directories.
            path = Path.GetFullPath(path);
            var files = Directory
                .EnumerateFiles(path)
                .Select(p => AddFileAsync(p, options, cancel));
            if (recursive)
            {
                var folders = Directory
                    .EnumerateDirectories(path)
                    .Select(dir => AddDirectoryAsync(dir, recursive, options, cancel));
                files = files.Union(folders);
            }

            // go-ipfs v0.4.14 sometimes fails when sending lots of 'add file' requests. It's happy with adding one file
            // at a time.
#if true
            var links = new List<IFileSystemLink>();
            foreach (var file in files)
            {
                var node = await file;
                links.Add(node.ToLink());
            }
#else
            var nodes = await Task.WhenAll(files);
            var links = nodes.Select(node => node.ToLink());
#endif
            // Create the directory with links to the created files and sub-directories
            var folder = this.EmptyFolder.GetAwaiter().GetResult().AddLinks(links);
            var directory = await this.IpFs.Object.PutAsync(folder, cancel);

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("added {0} {1}", directory.Id, Path.GetFileName(path)));
            }

            return new FileSystemNode
            {
                Id = directory.Id,
                Name = Path.GetFileName(path),
                Links = links,
                IsDirectory = true,
                Size = directory.Size,
                IpfsClient = IpFs
            };
        }

        /// <summary>Reads the content of an existing IPFS file as text.</summary>
        /// <param name="path">  
        ///     A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about" or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>The contents of the <paramref name="path" /> as a <see cref="string" />.</returns>
        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            using (var data = await ReadFileAsync(path, cancel))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync();
            }
        }

        /// <summary>Opens an existing IPFS file for reading.</summary>
        /// <param name="path">  
        ///     A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about" or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A <see cref="Stream" /> to the file contents.</returns>
        public Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default(CancellationToken)) => this.IpFs.DownloadAsync("cat", cancel, path);

        /// <summary>Reads the file asynchronous.</summary>
        /// <param name="path">  The path.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count"> The length.</param>
        /// <param name="cancel">The cancel.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        public Task<Stream> ReadFileAsync(string path, long offset, long count = 0, CancellationToken cancel = default(CancellationToken)) =>
            // TODO: length is not yet supported by daemons
            this.IpFs.DownloadAsync("cat", cancel, path, $"offset={offset}");

        /// <summary>Get information about the file or directory.</summary>
        /// <param name="path">  
        ///     A path to an existing file or directory, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///     or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns></returns>
        public async Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.IpFs.DoCommandAsync("file/ls", cancel, path);
            var r = JObject.Parse(json);
            var hash = (string)r["Arguments"][path];
            var obj = (JObject)r["Objects"][hash];
            var node = new FileSystemNode
            {
                Id = (string)obj["Hash"],
                Size = (long)obj["Size"],
                IsDirectory = (string)obj["Type"] == "Directory",
                Links = new FileSystemLink[0]
            };
            if (obj["Links"] is JArray links)
            {
                node.Links = links
                    .Select(l => new FileSystemLink
                    {
                        Name = (string)l["Name"],
                        Id = (string)l["Hash"],
                        Size = (long)l["Size"],
                        IsDirectory = (string)l["Type"] == "Directory",
                    })
                    .ToArray();
            }

            return node;
        }
    }
}
