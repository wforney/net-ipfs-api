namespace Ipfs.Api
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Ipfs.CoreApi;
    using Newtonsoft.Json;

    /// <summary>A client that allows access to the InterPlanetary File System (IPFS).</summary>
    /// <remarks>The API is based on the <see href="https://ipfs.io/docs/commands/">IPFS commands</see>.</remarks>
    /// <seealso href="https://ipfs.io/docs/api/">IPFS API</seealso>
    /// <seealso href="https://ipfs.io/docs/commands/">IPFS commands</seealso>
    /// <remarks><b>IpfsClient</b> is thread safe, only one instance is required by the application.</remarks>
    public partial class IpfsClient : ICoreApi
    {
        /// <summary>The default URL to the IPFS HTTP API server.</summary>
        /// <value>The default is "http://localhost:5001".</value>
        /// <remarks>The environment variable "IpfsHttpApi" overrides this value.</remarks>
        public static Uri DefaultApiUri = new Uri(
            Environment.GetEnvironmentVariable("IpfsHttpApi")
            ?? "http://localhost:5001");

        private static readonly HttpClient Client;
        private static readonly HttpClientHandler ClientHandler;
        private static readonly ILog Log = LogManager.GetLogger(typeof(IpfsClient));
        private static readonly object Safe = new object();

        /// <summary>Initializes static members of the <see cref="IpfsClient" /> class.</summary>
        static IpfsClient()
        {
            ClientHandler = new HttpClientHandler();
            if (ClientHandler.SupportsAutomaticDecompression)
            {
                ClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var version = typeof(IpfsClient).GetTypeInfo().Assembly.GetName().Version;
            UserAgent = $"net-ipfs/{version.Major}.{version.Minor}";

            if (Client == null)
            {
                lock (Safe)
                {
                    if (Client == null)
                    {
                        Client = new HttpClient(ClientHandler);
                        Client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    }
                }
            }
        }

        /// <summary>Creates a new instance of the <see cref="IpfsClient" /> class and sets the default values.</summary>
        /// <remarks>
        ///     All methods of IpfsClient are thread safe. Typically, only one instance is required for an application.
        /// </remarks>
        public IpfsClient()
        {
            this.ApiUri = DefaultApiUri;
            this.TrustedPeers = new TrustedPeerCollection(this);

            this.Bootstrap = new BootstrapApi(this);
            this.Bitswap = new BitswapApi(this);
            this.Block = new BlockApi(this);
            this.Config = new ConfigApi(this);
            this.Pin = new PinApi(this);
            this.Dht = new DhtApi(this);
            this.Swarm = new SwarmApi(this);
            this.Dag = new DagApi(this);
            this.Object = new ObjectApi(this);
            this.FileSystem = new FileSystemApi(this);
            this.PubSub = new PubSubApi(this);
            this.Key = new KeyApi(this);
            this.Generic = this;
            this.Name = new NameApi(this);
            this.Dns = new DnsApi(this);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IpfsClient" /> class and specifies the <see cref="ApiUri">API
        ///     host's URL</see>. default values
        /// </summary>
        /// <param name="host">The URL of the API host. For example "http://localhost:5001" or "http://ipv4.fiddler:5001".</param>
        public IpfsClient(string host)
            : this()
        {
            this.ApiUri = new Uri(host);
        }

        /// <summary>The value of HTTP User-Agent header sent to the API server.</summary>
        /// <value>
        ///     The default value is "net-ipfs/M.N", where M is the major and N is minor version numbers of the assembly.
        /// </value>
        public static string UserAgent { get; set; }

        /// <summary>The URL to the IPFS API server. The default is "http://localhost:5001".</summary>
        public Uri ApiUri { get; set; }

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; }

        /// <inheritdoc />
        public IBlockApi Block { get; }

        /// <inheritdoc />
        public IBootstrapApi Bootstrap { get; }

        /// <inheritdoc />
        public IConfigApi Config { get; }

        /// <inheritdoc />
        public IDagApi Dag { get; }

        /// <inheritdoc />
        public IDhtApi Dht { get; }

        /// <inheritdoc />
        public IDnsApi Dns { get; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; }

        /// <inheritdoc />
        public IGenericApi Generic { get; }

        /// <inheritdoc />
        public IKeyApi Key { get; }

        /// <inheritdoc />
        public INameApi Name { get; }

        /// <inheritdoc />
        public IObjectApi Object { get; }

        /// <inheritdoc />
        public IPinApi Pin { get; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; }

        /// <summary>The list of peers that are initially trusted by IPFS.</summary>
        /// <remarks>This is equilivent to <c>ipfs bootstrap list</c>.</remarks>
        public TrustedPeerCollection TrustedPeers { get; }

        /// <summary>Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a string.</summary>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="arg">    The optional argument to the command.</param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>A string representation of the command's result.</returns>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<string> DoCommandAsync(string command, CancellationToken cancel, string arg = null, params string[] options)
        {
            var url = BuildCommand(command, arg, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("POST " + url);
            }

            using (var response = await Client.PostAsync(url, null, cancel))
            {
                await ThrowOnErrorAsync(response);
                var body = await response.Content.ReadAsStringAsync();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("RSP " + body);
                }

                return body;
            }
        }

        /// <summary>
        ///     Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a specific <see cref="Type" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type" /> of object to return.</typeparam>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="arg">    The optional argument to the command.</param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>A <typeparamref name="T" />.</returns>
        /// <remarks>The command's response is converted to <typeparamref name="T" /> using <c>JsonConvert</c>.</remarks>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<T> DoCommandAsync<T>(string command, CancellationToken cancel, string arg = null, params string[] options)
        {
            var json = await DoCommandAsync(command, cancel, arg, options);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a <see cref="Stream" />.</summary>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="arg">    The optional argument to the command.</param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>A <see cref="Stream" /> containing the command's result.</returns>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<Stream> DownloadAsync(string command, CancellationToken cancel, string arg = null, params string[] options)
        {
            var url = BuildCommand(command, arg, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GET " + url);
            }

            var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
            await ThrowOnErrorAsync(response);
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        ///     Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a a byte array.
        /// </summary>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="arg">    The optional argument to the command.</param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>A byte array containing the command's result.</returns>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<byte[]> DownloadBytesAsync(string command, CancellationToken cancel, string arg = null, params string[] options)
        {
            var url = BuildCommand(command, arg, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GET " + url);
            }

            var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
            await ThrowOnErrorAsync(response);
            return await response.Content.ReadAsByteArrayAsync();
        }

        /// <summary>Post an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a stream.</summary>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="arg">    The optional argument to the command.</param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>A <see cref="Stream" /> containing the command's result.</returns>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<Stream> PostDownloadAsync(string command, CancellationToken cancel, string arg = null, params string[] options)
        {
            var url = BuildCommand(command, arg, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("POST " + url);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);
            await ThrowOnErrorAsync(response);
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        ///     Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> that requires uploading of a "file".
        /// </summary>
        /// <param name="command">The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0add">"add"</see>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <param name="data">   A <see cref="Stream" /> containing the data to upload.</param>
        /// <param name="name">   
        ///     The name associated with the <paramref name="data" />, can be <b>null</b>. Typically a filename, such as "hello.txt".
        /// </param>
        /// <param name="options">The optional flags to the command.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is the HTTP response as a string.
        /// </returns>
        /// <exception cref="HttpRequestException">When the IPFS server indicates an error.</exception>
        public async Task<string> UploadAsync(string command, CancellationToken cancel, Stream data, string name, params string[] options)
        {
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(data);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            if (string.IsNullOrEmpty(name))
            {
                content.Add(streamContent, "file");
            }
            else
            {
                content.Add(streamContent, "file", name);
            }

            var url = BuildCommand(command, null, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("POST " + url);
            }

            using (var response = await Client.PostAsync(url, content, cancel))
            {
                await ThrowOnErrorAsync(response);
                var json = await response.Content.ReadAsStringAsync();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("RSP " + json);
                }

                return json;
            }
        }

        /// <summary>upload as an asynchronous operation.</summary>
        /// <param name="command">The command.</param>
        /// <param name="cancel"> The cancel.</param>
        /// <param name="data">   The data.</param>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> UploadAsync(string command, CancellationToken cancel, byte[] data, params string[] options)
        {
            var content = new MultipartFormDataContent();
            var streamContent = new ByteArrayContent(data);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file");

            var url = BuildCommand(command, null, options);
            if (Log.IsDebugEnabled)
            {
                Log.Debug("POST " + url);
            }

            using (var response = await Client.PostAsync(url, content, cancel))
            {
                await ThrowOnErrorAsync(response);
                var json = await response.Content.ReadAsStringAsync();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("RSP " + json);
                }

                return json;
            }
        }

        /// <summary>Throws an <see cref="HttpRequestException" /> if the response does not indicate success.</summary>
        /// <param name="response">The response message.</param>
        /// <returns>Returns <b>true</b> if successful.</returns>
        /// <exception cref="HttpRequestException">There has been an HTTP request exception.</exception>
        /// <remarks>The API server returns an JSON error in the form <c>{ "Message": "...", "Code": ... }</c>.</remarks>
        private async static Task<bool> ThrowOnErrorAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var error = $"Invalid IPFS command: {response.RequestMessage.RequestUri}";
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"ERR {error}");
                }

                throw new HttpRequestException(error);
            }

            var body = await response.Content.ReadAsStringAsync();
            if (Log.IsDebugEnabled)
            {
                Log.Debug($"ERR {body}");
            }

            var message = body;
            try
            {
                message = (string)JsonConvert.DeserializeObject<dynamic>(body).Message;
            }
            catch (Exception e)
            {
                Log.Warn(e.Message);
            }

            throw new HttpRequestException(message);
        }

        /// <summary>Builds the command.</summary>
        /// <param name="command">The command.</param>
        /// <param name="arg">    The argument.</param>
        /// <param name="options">The options.</param>
        /// <returns>Uri.</returns>
        private Uri BuildCommand(string command, string arg = null, params string[] options)
        {
            var q = new StringBuilder();
            if (arg != null)
            {
                q.Append("&arg=");
                q.Append(WebUtility.UrlEncode(arg));
            }

            foreach (var option in options)
            {
                q.Append('&');
                var index = option.IndexOf('=');
                if (index < 0)
                {
                    q.Append(option);
                }
                else
                {
                    q.Append(option, 0, index)
                        .Append('=')
                        .Append(WebUtility.UrlEncode(option.Substring(index + 1)));
                }
            }

            var url = $"/api/v0/{command}";
            if (q.Length > 0)
            {
                q[0] = '?';
                q.Insert(0, url);
                url = q.ToString();
            }

            return new Uri(this.ApiUri, url);
        }
    }
}
