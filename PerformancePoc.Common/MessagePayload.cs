using System;

namespace PerformancePoc.Common
{
    public class MessagePayload
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] Data { get; set; } // To simulate payload size

        public static MessagePayload Create(int sizeInBytes)
        {
            return new MessagePayload
            {
                Id = Guid.NewGuid(),
                Content = "Benchmark Payload",
                CreatedAt = DateTime.UtcNow,
                Data = new byte[sizeInBytes]
            };
        }
    }
}
