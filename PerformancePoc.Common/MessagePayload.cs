using System;

namespace PerformancePoc.Common
{
    public class MessagePayload
    {
        public required Guid Id { get; set; }
        public required string Content { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required byte[] Data { get; set; } // To simulate payload size

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
