using JobQueue.Models;

namespace JobQueue.DTOs
{
    public class UpdateJobStatusDto
    {

        public JobStatus Status { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
