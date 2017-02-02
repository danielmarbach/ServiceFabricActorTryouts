using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace OutboxActorService.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IOutboxActorService : IActor
    {
        Task<OutboxMessage> GetMessage();

        Task Commit(OutboxMessage msg);
    }

    [DataContract]
    public class OutboxMessage
    {
        public OutboxMessage()
        {
            Empty = true;
        }

        public OutboxMessage(TransportOperation[] operations)
        {
            TransportOperations = operations;
            Empty = false;
        }

        [DataMember]
        public bool Empty { get; private set; }

        /// <summary>
        /// The list of operations performed during the processing of the incoming message.
        /// </summary>
        [DataMember]
        public TransportOperation[] TransportOperations { get; private set; }
    }

    [DataContract]
    public class TransportOperation
    {
        /// <summary>
        /// Creates a new instance of a <see cref="TransportOperation" />.
        /// </summary>
        /// <param name="messageId">The identifier of the outgoing message.</param>
        /// <param name="options">The sending options.</param>
        /// <param name="body">The message body.</param>
        /// <param name="headers">The message headers.</param>
        /// .
        public TransportOperation(string messageId, Dictionary<string, string> options, byte[] body, Dictionary<string, string> headers)
        {
            MessageId = messageId;
            Options = options;
            Body = body;
            Headers = headers;
        }

        /// <summary>
        /// Gets the identifier of the outgoing message.
        /// </summary>
        [DataMember]
        public string MessageId { get; private set; }

        /// <summary>
        /// Sending options.
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        /// Gets a byte array to the body content of the outgoing message.
        /// </summary>
        [DataMember]
        public byte[] Body { get; private set; }

        /// <summary>
        /// Gets outgoing message headers.
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Headers { get; private set; }
    }
}
