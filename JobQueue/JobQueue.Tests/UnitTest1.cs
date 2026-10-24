using JobQueue.Models;

namespace JobQueue.Tests;

public class JobTests
{
    [Fact]
    public void NewJob_ShouldHaveCorrectDefaultValues()
    {
        var job = new Job
        {
            Type = "SendEmail",
            Payload = "{\"to\":\"test@example.com\"}"
        };

        Assert.Equal(0, job.Id);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal(0, job.Attempts);
        Assert.Null(job.Errors);
        Assert.Null(job.ClaimedAt);
        Assert.NotEqual(Guid.Empty, job.RowVersion);

        Assert.True(job.CreatedAt <= DateTime.UtcNow);
        Assert.True(job.ProcessAfter <= DateTime.UtcNow);
    }
}