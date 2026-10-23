using Microsoft.EntityFrameworkCore;
using JobQueue.Models;


public class JobQueueDbContext : DbContext
{
    public JobQueueDbContext(DbContextOptions<JobQueueDbContext> options) : base(options)
    {
    }
    public DbSet<Jobs> Jobs { get; set; }
}
