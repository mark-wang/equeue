using System;

namespace EQueue.Protocols
{
    [Serializable]
    public class Message
    {
        public string Topic { get; private set; }
        public int Code { get; private set; }
        public byte[] Body { get; private set; }
        public MessageStatus Status { get; private set; }

        public Message(string topic, int code, byte[] body, MessageStatus status)
        {
            Topic = topic;
            Code = code;
            Body = body;
            Status = status;
        }
    }
}
