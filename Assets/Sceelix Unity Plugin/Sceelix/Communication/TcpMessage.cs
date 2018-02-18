using System;

namespace Assets.Sceelix.Communication
{
    /// <summary>
    /// Simple message class to structure a message data and its subject.
    /// </summary>
    public class TcpMessage
    {
        public string Subject { get; set; }
        public Object Data { get; set; }

        public TcpMessage(string subject, object data)
        {
            Subject = subject;
            Data = data;
        }
    }
}
