// <copyright file="SwarmApi.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>
namespace Ipfs.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ipfs.CoreApi;
    using Newtonsoft.Json.Linq;

    /// <summary>Class SwarmApi.</summary>
    /// <seealso cref="Ipfs.CoreApi.ISwarmApi" />
    internal class SwarmApi : ISwarmApi
    {
        /// <summary>The ipfs</summary>
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="SwarmApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal SwarmApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>add address filter as an asynchronous operation.</summary>
        /// <param name="address">An allowed address. For example "/ip4/104.131.131.82" or "/ip4/192.168.0.0/ipcidr/16".</param>
        /// <param name="persist">If <b>true</b> the filter will persist across daemon reboots.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the address filter that was added.
        /// </returns>
        /// <seealso href="https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing" />
        public async Task<MultiAddress> AddAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
            var json = await this.ipfs.DoCommandAsync("swarm/filters/add", cancel, address.ToString());
            var addrs = (JArray)(JObject.Parse(json)["Strings"]);
            var a = addrs.FirstOrDefault();
            return (a == null) ? null : new MultiAddress((string)a);
        }

        /// <summary>addresses as an asynchronous operation.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a sequence of peer nodes.
        /// </returns>
        public async Task<IEnumerable<Peer>> AddressesAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("swarm/addrs", cancel);
            return ((JObject)JObject.Parse(json)["Addrs"])
                .Properties()
                .Select(p => new Peer
                {
                    Id = p.Name,
                    Addresses = ((JArray)p.Value).Where(v => !string.IsNullOrWhiteSpace((string)v)).Select(v => new MultiAddress((string)v))
                });
        }

        /// <summary>connect as an asynchronous operation.</summary>
        /// <param name="address">An ipfs <see cref="T:Ipfs.MultiAddress" />, such as <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>Task.</returns>
        public async Task ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            await this.ipfs.DoCommandAsync("swarm/connect", cancel, address.ToString());
        }

        /// <summary>disconnect as an asynchronous operation.</summary>
        /// <param name="address">An ipfs <see cref="T:Ipfs.MultiAddress" />, such as <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>Task.</returns>
        public async Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            await this.ipfs.DoCommandAsync("swarm/disconnect", cancel, address.ToString());
        }

        /// <summary>list address filters as an asynchronous operation.</summary>
        /// <param name="persist">If <b>true</b> only persisted filters are listed.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is a sequence of addresses filters.
        /// </returns>
        /// <seealso href="https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing" />
        public async Task<IEnumerable<MultiAddress>> ListAddressFiltersAsync(bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            JArray addrs;
            if (persist)
            {
                addrs = await this.ipfs.Config.GetAsync("Swarm.AddrFilters", cancel) as JArray;
            }
            else
            {
                var json = await this.ipfs.DoCommandAsync("swarm/filters", cancel);
                addrs = (JObject.Parse(json)["Strings"]) as JArray;
            }

            return (addrs == null) ? new MultiAddress[0] : addrs.Select(a => new MultiAddress((string)a));
        }

        /// <summary>peers as an asynchronous operation.</summary>
        /// <param name="cancel">
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is a sequence of
        ///     <see cref="T:Ipfs.Peer">Connected Peers</see>.
        /// </returns>
        /// <exception cref="FormatException">Unknown response from 'swarm/peers</exception>
        public async Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await this.ipfs.DoCommandAsync("swarm/peers", cancel, null, "verbose=true");
            var result = JObject.Parse(json);

            // Older servers return an array of strings
            var strings = (JArray)result["Strings"];
            if (strings != null)
            {
                return strings
                   .Select(s =>
                   {
                       var parts = ((string)s).Split(' ');
                       var address = new MultiAddress(parts[0]);
                       return new Peer
                       {
                           Id = address.Protocols.First(p => p.Name == nameof(ipfs)).Value,
                           ConnectedAddress = parts[0],
                           Latency = ParseLatency(parts[1])
                       };
                   });
            }

            // Current servers return JSON
            var peers = (JArray)result["Peers"];
            if (peers != null)
            {
                return peers.Select(p => new Peer
                {
                    Id = (string)p["Peer"],
                    ConnectedAddress = new MultiAddress((string)p["Addr"] + "/ipfs/" + (string)p["Peer"]),
                    Latency = ParseLatency((string)p["Latency"])
                });
            }

            // Hmmm. Another change we can handle
            throw new FormatException("Unknown response from 'swarm/peers");
        }

        /// <summary>remove address filter as an asynchronous operation.</summary>
        /// <param name="address">For example "/ip4/104.131.131.82" or "/ip4/192.168.0.0/ipcidr/16".</param>
        /// <param name="persist">If <b>true</b> the filter is also removed from the persistent store.</param>
        /// <param name="cancel"> 
        ///     Is used to stop the task. When cancelled, the
        ///     <see cref="T:System.Threading.Tasks.TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is the address filter that was removed.
        /// </returns>
        /// <seealso href="https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing" />
        public async Task<MultiAddress> RemoveAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
            var json = await this.ipfs.DoCommandAsync("swarm/filters/rm", cancel, address.ToString());
            var addrs = (JArray)(JObject.Parse(json)["Strings"]);
            var a = addrs.FirstOrDefault();
            return (a == null) ? null : new MultiAddress((string)a);
        }

        /// <summary>Parses the latency.</summary>
        /// <param name="latency">The latency.</param>
        /// <returns>TimeSpan.</returns>
        /// <exception cref="FormatException"></exception>
        private static TimeSpan ParseLatency(string latency)
        {
            if (latency == "n/a" || latency == "unknown")
            {
                return TimeSpan.Zero;
            }
            if (latency.EndsWith("ms", StringComparison.Ordinal))
            {
                var ms = double.Parse(latency.Substring(0, latency.Length - 2));
                return TimeSpan.FromMilliseconds(ms);
            }
            if (latency.EndsWith("s", StringComparison.Ordinal))
            {
                var sec = double.Parse(latency.Substring(0, latency.Length - 1));
                return TimeSpan.FromSeconds(sec);
            }

            throw new FormatException($"Invalid latency unit '{latency}'.");
        }
    }
}
