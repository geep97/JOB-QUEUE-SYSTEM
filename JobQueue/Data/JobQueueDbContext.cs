using Microsoft.EntityFrameworkCore;
using JobQueue.Models;


namespace JobQueue.Data
{

    public class JobQueueDbContext : DbContext
    {
        public JobQueueDbContext(DbContextOptions<JobQueueDbContext> options) : base(options)
        {
        }
        public DbSet<Job> Jobs { get; set; }
    }

}
