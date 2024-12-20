using System;
using System.Collections.Generic;

using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.SignalR.Messages;

namespace Best.SignalR
{
    /// <summary>
    /// Interface for message encoding-decoding protocols used in a SignalR communication.
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// Name of the protocol. This name must be known by the server.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Type of the encoded message, it can be <see cref="TransferModes.Binary">binary</see> or <see cref="TransferModes.Text">textual</see>.
        /// </summary>
        TransferModes Type { get; }

        /// <summary>
        /// An optional <see cref="IEncoder"/> implementation if the implementation requires one. With its help, the protocol implementor is able to support different encoders for the same protocol (like a json protocol with pluggable LitJson or JSON .NET encoders).
        /// </summary>
        IEncoder Encoder { get; }

        /// <summary>
        /// The parent <see cref="HubConnection"/> instance that the implementation can use to access type informations.
        /// </summary>
        HubConnection Connection { get; set; }

        /// <summary>
        /// Parses binary message representations into a list of messages.
        /// </summary>
        void ParseMessages(BufferSegment segment, ref List<Message> messages);

        /// <summary>
        /// Encodes a message into its binary representation.
        /// </summary>
        BufferSegment EncodeMessage(Message message);

        /// <summary>
        /// Converts argument values to their respective types.
        /// </summary>
        object[] GetRealArguments(Type[] argTypes, object[] arguments);

        /// <summary>
        /// Converts a value to a given type.
        /// </summary>
        object ConvertTo(Type toType, object obj);
    }

    /// <summary>
    /// Implementation of the JSON protocol for SignalR communication.
    /// </summary>
    public sealed class JsonProtocol : IProtocol
    {
        public const char Separator = (char)0x1E;

        public string Name { get { return "json"; } }

        public TransferModes Type { get { return TransferModes.Binary; } }

        public IEncoder Encoder { get; private set; }

        public HubConnection Connection { get; set; }

        public JsonProtocol(IEncoder encoder)
        {
            if (encoder == null)
                throw new ArgumentNullException("encoder");

            this.Encoder = encoder;
        }

        public void ParseMessages(BufferSegment segment, ref List<Message> messages) {
            if (segment.Data == null || segment.Count == 0)
                return;

            int from = segment.Offset;
            int separatorIdx = Array.IndexOf<byte>(segment.Data, (byte)JsonProtocol.Separator, from);
            if (separatorIdx == -1)
                throw new Exception("Missing separator in data! Segment: " + segment.ToString());

            while (separatorIdx != -1)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose("JsonProtocol", "ParseMessages - " + System.Text.Encoding.UTF8.GetString(segment.Data, from, separatorIdx - from));
                var message = this.Encoder.DecodeAs<Message>(new BufferSegment(segment.Data, from, separatorIdx - from));

                messages.Add(message);

                from = separatorIdx + 1;
                separatorIdx = Array.IndexOf<byte>(segment.Data, (byte)JsonProtocol.Separator, from);
            }
        }
        
        public BufferSegment EncodeMessage(Message message)
        {
            BufferSegment result = BufferSegment.Empty;

            // While message contains all informations already, the spec states that no additional field are allowed in messages
            //  So we are creating 'specialized' messages here to send to the server.
            switch (message.type)
            {
                case MessageTypes.StreamItem:
                    result = this.Encoder.Encode<StreamItemMessage>(new StreamItemMessage()
                    {
                        type = message.type,
                        invocationId = message.invocationId,
                        item = message.item
                    });
                    break;

                case MessageTypes.Completion:
                    if (!string.IsNullOrEmpty(message.error))
                    {
                        result = this.Encoder.Encode<CompletionWithError>(new CompletionWithError()
                        {
                            type = MessageTypes.Completion,
                            invocationId = message.invocationId,
                            error = message.error
                        });
                    }
                    else if (message.result != null)
                    {
                        result = this.Encoder.Encode<CompletionWithResult>(new CompletionWithResult()
                        {
                            type = MessageTypes.Completion,
                            invocationId = message.invocationId,
                            result = message.result
                        });
                    }
                    else
                        result = this.Encoder.Encode<Completion>(new Completion()
                        {
                            type = MessageTypes.Completion,
                            invocationId = message.invocationId
                        });
                    break;

                case MessageTypes.Invocation:
                case MessageTypes.StreamInvocation:
                    if (message.streamIds != null)
                    {
                        result = this.Encoder.Encode<UploadInvocationMessage>(new UploadInvocationMessage()
                        {
                            type = message.type,
                            invocationId = message.invocationId,
                            nonblocking = message.nonblocking,
                            target = message.target,
                            arguments = message.arguments,
                            streamIds = message.streamIds
                        });
                    }
                    else
                    {
                        result = this.Encoder.Encode<InvocationMessage>(new InvocationMessage()
                        {
                            type = message.type,
                            invocationId = message.invocationId,
                            nonblocking = message.nonblocking,
                            target = message.target,
                            arguments = message.arguments
                        });
                    }
                    break;

                case MessageTypes.CancelInvocation:
                    result = this.Encoder.Encode<CancelInvocationMessage>(new CancelInvocationMessage()
                    {
                        invocationId = message.invocationId
                    });
                    break;

                case MessageTypes.Ping:
                    //result = this.Encoder.Encode<PingMessage>(new PingMessage());
                    // fast path to encode a well-known json string
                    result = EncodeKnown("{\"type\":6}");
                    break;

                case MessageTypes.Close:
                    if (!string.IsNullOrEmpty(message.error))
                    {
                        result = this.Encoder.Encode<CloseWithErrorMessage>(new CloseWithErrorMessage() { error = message.error });
                    }
                    else
                    {
                        //result = this.Encoder.Encode<CloseMessage>(new CloseMessage());
                        // fast path to encode a well-known json string
                        result = EncodeKnown("{\"type\":7}");
                    }
                    break;

                case MessageTypes.Ack:
                    result = EncodeKnown($"{{\"type\":8,sequenceId:{message.sequenceId}}}");
                    break;
                case MessageTypes.Sequence:
                    result = EncodeKnown($"{{\"type\":9,sequenceId:{message.sequenceId}}}");
                    break;
            }

            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose("JsonProtocol", "EncodeMessage - json: " + System.Text.Encoding.UTF8.GetString(result.Data, 0, result.Count - 1));

            return result;
        }

        private BufferSegment EncodeKnown(string json)
        {
            int len = System.Text.Encoding.UTF8.GetByteCount(json);
            byte[] buffer = BufferPool.Get(len + 1, true);
            System.Text.Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
            buffer[len] = (byte)JsonProtocol.Separator;
            return new BufferSegment(buffer, 0, len + 1);
        }

        public object[] GetRealArguments(Type[] argTypes, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return null;

            if (argTypes.Length > arguments.Length)
                throw new Exception(string.Format("argType.Length({0}) < arguments.length({1})", argTypes.Length, arguments.Length));

            object[] realArgs = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; ++i)
                realArgs[i] = ConvertTo(argTypes[i], arguments[i]);

            return realArgs;
        }

        public object ConvertTo(Type toType, object obj)
        {
            if (obj == null)
                return null;

            if (toType.IsEnum)
                return Enum.Parse(toType, obj.ToString(), true);

            if (toType.IsPrimitive)
                return Convert.ChangeType(obj, toType);

            if (toType == typeof(string))
                return obj.ToString();

            if (toType == typeof(DateTime))
                return Convert.ChangeType(obj, typeof(DateTime));

            if (toType.IsGenericType && toType.Name == "Nullable`1")
                return Convert.ChangeType(obj, toType.GetGenericArguments()[0]);

            return this.Encoder.ConvertTo(toType, obj);
        }

        /// <summary>
        /// Returns the given string parameter's bytes with the added separator(0x1E).
        /// </summary>
        public static BufferSegment WithSeparator(string str)
        {
            int len = System.Text.Encoding.UTF8.GetByteCount(str);

            byte[] buffer = BufferPool.Get(len + 1, true);

            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);

            buffer[len] = 0x1e;

            return new BufferSegment(buffer, 0, len + 1);
        }
    }
}
