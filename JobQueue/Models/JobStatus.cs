namespace JobQueue.Models
{
    public enum JobStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        DeadLettered
    }
}