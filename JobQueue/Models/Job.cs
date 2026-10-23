using Microsoft.AspNetCore.SignalR.Protocol;

namespace JobQueue.Models
{
    public class Job
    {

        public int Id { get; set; }
        public required string Type { get; set; }

        public required string Payload { get; set; }

        public JobStatus Status { get; set; } = JobStatus.Pending;

        public int Attempts { get; set; }

        public string? Errors { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
