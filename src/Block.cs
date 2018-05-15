namespace Ipfs.Api
{
    using System.IO;

    /// <inheritdoc />
    public class Block : IDataBlock
    {
        private long? size;

        /// <inheritdoc />
        public byte[] DataBytes { get; set; }

        /// <inheritdoc />
        public Stream DataStream => new MemoryStream(this.DataBytes, false);

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size
        {
            get => this.size ?? this.DataBytes.Length;
            set => this.size = value;
        }
    }
}
