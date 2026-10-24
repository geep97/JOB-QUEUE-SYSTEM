
using JobQueue.Data;
using JobQueue.Models;
using JobQueue.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JobQueue.Tests;

public class JobsServiceTests
{
    private static (JobQueueDbContext context, JobsService service) CreateService()
    {
        var options = new DbContextOptionsBuilder<JobQueueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JobQueueDbContext(options);

        var configuration = new ConfigurationBuilder().Build();
        var publisher = new RabbitMqPublisher(configuration);

        var service = new JobsService(context, publisher);

        return (context, service);
    }

    [Fact]
    public async Task UpdateJobStatus_WhenJobExists_UpdatesStatus()
    {
        // Arrange
        var (context, service) = CreateService();

        var job = new Job
        {
            Type = "SendEmail",
            Payload = "{}",
            Status = JobStatus.InProgress,
            ClaimedAt = DateTime.UtcNow
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateJobStatusWithBackoffAsync(
            job.Id,
            JobStatus.Completed,
            null,
            job.Attempts);

        // Assert
        var updatedJob = await context.Jobs.FindAsync(job.Id);

        Assert.True(result);
        Assert.Equal(JobStatus.Completed, updatedJob!.Status);
        Assert.Null(updatedJob.ClaimedAt);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task UpdateJobStatus_WhenJobDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var (context, service) = CreateService();

        // Act
        var result = await service.UpdateJobStatusWithBackoffAsync(
            999,
            JobStatus.Completed,
            null,
            0);

        // Assert
        Assert.False(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task UpdateJobStatus_WhenResetToPending_AppliesFirstRetryDelay()
    {
        // Arrange
        var (context, service) = CreateService();

        var job = new Job
        {
            Type = "SendEmail",
            Payload = "{}",
            Status = JobStatus.InProgress,
            Attempts = 1,
            ClaimedAt = DateTime.UtcNow
        };

        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await service.UpdateJobStatusWithBackoffAsync(
            job.Id,
            JobStatus.Pending,
            "Temporary failure",
            1);

        // Assert
        var updatedJob = await context.Jobs.FindAsync(job.Id);

        Assert.True(result);
        Assert.Equal(JobStatus.Pending, updatedJob!.Status);
        Assert.Equal("Temporary failure", updatedJob.Errors);
        Assert.Null(updatedJob.ClaimedAt);
        Assert.True(updatedJob.ProcessAfter >= beforeUpdate.AddSeconds(10));
        Assert.True(updatedJob.ProcessAfter <= DateTime.UtcNow.AddSeconds(11));

        await context.DisposeAsync();
    }
}
