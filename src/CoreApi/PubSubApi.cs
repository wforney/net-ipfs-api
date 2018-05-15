// <copyright file="PubSubApi.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>
namespace Ipfs.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>Class PubSubApi.</summary>
    /// <seealso cref="Ipfs.CoreApi.IPubSubApi" />
    internal class PubSubApi : IPubSubApi
    {
        /// <summary>The log</summary>
        private static ILog Log = LogManager.GetLogger<PubSubApi>();

        /// <summary>The ipfs</summary>
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="PubSubApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal PubSubApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>subscribed topics as an asynchronous operation.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a sequence of
        ///     <see cref="T:System.String" /> for each topic.
        /// </returns>
        public async Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("pubsub/ls", cancel);
            var result = JObject.Parse(json);
            return (result["Strings"] is JArray strings) ? strings.Cast<string>() : new string[0];
        }

        /// <summary>peers as an asynchronous operation.</summary>
        /// <param name="topic"> When specified, only peers pubsubing on the topic are returned.</param>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task's value is a sequence of <see cref="T:Ipfs.Peer" />.</returns>
        public async Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("pubsub/peers", cancel, topic);
            var result = JObject.Parse(json);
            var strings = result["Strings"] as JArray;
            return strings == null ? new Peer[0] : strings.Select(s => new Peer { Id = (string)s });
        }

        /// <summary>Publish a message to a given topic.</summary>
        /// <param name="topic">  The topic name.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Publish(string topic, string message, CancellationToken cancel = default(CancellationToken))
        {
            var _ = await this.ipfs.DoCommandAsync("pubsub/pub", cancel, topic, "arg=" + message);
            return;
        }

        /// <summary>Subscribe to messages on a given topic.</summary>
        /// <param name="topic">            The topic name.</param>
        /// <param name="handler">          
        ///     The action to perform when a <see cref="T:Ipfs.IPublishedMessage" /> is received.
        /// </param>
        /// <param name="cancellationToken">
        ///     Is used to stop the topic listener. When cancelled, the
        ///     <see cref="T:System.OperationCanceledException" /> is <b>NOT</b> raised.
        /// </param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>The <paramref name="handler" /> is invoked on the topic listener thread.</remarks>
        public async Task Subscribe(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
        {
            var messageStream = await this.ipfs.PostDownloadAsync("pubsub/sub", cancellationToken, topic);
            var sr = new StreamReader(messageStream);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ProcessMessages(topic, handler, sr, cancellationToken));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return;
        }

        /// <summary>Processes the messages.</summary>
        /// <param name="topic">  The topic.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="sr">     The sr.</param>
        /// <param name="ct">     The ct.</param>
        private static void ProcessMessages(string topic, Action<PublishedMessage> handler, StreamReader sr, CancellationToken ct)
        {
            Log.DebugFormat("Start listening for '{0}' messages", topic);

            // .Net needs a ReadLine(CancellationToken) As a work-around, we register a function to close the stream
            ct.Register(() => sr.Dispose());
            try
            {
                while (!sr.EndOfStream && !ct.IsCancellationRequested)
                {
                    var json = sr.ReadLine();
                    if (json == null)
                    {
                        break;
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat("PubSub message {0}", json);
                    }

                    // go-ipfs 0.4.13 and earlier always send empty JSON as the first response.
                    if (json == "{}")
                    {
                        continue;
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        handler?.Invoke(new PublishedMessage(json));
                    }
                }
            }
            catch (Exception e)
            {
                // Do not report errors when cancelled.
                if (!ct.IsCancellationRequested)
                {
                    Log.Error(e);
                }
            }
            finally
            {
                sr.Dispose();
            }

            Log.DebugFormat("Stop listening for '{0}' messages", topic);
        }
    }
}
