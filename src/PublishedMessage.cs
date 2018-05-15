namespace Ipfs.Api
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;

    /// <summary>A published message.</summary>
    /// <remarks>The <see cref="PubSubApi" /> is used to publish and subsribe to a message.</remarks>
    public class PublishedMessage : IPublishedMessage
    {
        /// <summary>Creates a new instance of <see cref="PublishedMessage" /> from the specified JSON string.</summary>
        /// <param name="json">The JSON representation of a published message.</param>
        public PublishedMessage(string json)
        {
            var obj = JObject.Parse(json);
            this.Sender = Convert.FromBase64String((string)obj["from"]).ToBase58();
            this.SequenceNumber = Convert.FromBase64String((string)obj["seqno"]);
            this.DataBytes = Convert.FromBase64String((string)obj["data"]);
            var topics = (JArray)(obj["topicIDs"]);
            this.Topics = topics.Cast<string>();
        }

        /// <inheritdoc />
        public byte[] DataBytes { get; private set; }

        /// <inheritdoc />
        public Stream DataStream => new MemoryStream(this.DataBytes, false);

        /// <summary>Contents as a string.</summary>
        /// <value>The contents interpreted as a UTF-8 string.</value>
        public string DataString => Encoding.UTF8.GetString(this.DataBytes);

        /// <summary>&gt; NOT SUPPORTED.</summary>
        /// <exception cref="NotSupportedException">A published message does not have a content id.</exception>
        public Cid Id => throw new NotSupportedException();

        /// <inheritdoc />
        public Peer Sender { get; private set; }

        /// <inheritdoc />
        public byte[] SequenceNumber { get; private set; }

        /// <inheritdoc />
        public long Size => this.DataBytes.Length;

        /// <inheritdoc />
        public IEnumerable<string> Topics { get; private set; }
    }
}
