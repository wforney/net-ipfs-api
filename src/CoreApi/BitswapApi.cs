// <copyright file="BitswapApi.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>

namespace Ipfs.Api
{
    using Ipfs.CoreApi;

    /// <summary>Class BitswapApi.</summary>
    /// <seealso cref="Ipfs.CoreApi.IBitswapApi" />
    internal class BitswapApi : IBitswapApi
    {
        /// <summary>The ipfs</summary>
        private IpfsClient ipfs;

        /// <summary>Initializes a new instance of the <see cref="BitswapApi" /> class.</summary>
        /// <param name="ipfs">The ipfs.</param>
        internal BitswapApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }
    }
}
