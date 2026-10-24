using Microsoft.EntityFrameworkCore;
using JobQueue.Models;

namespace JobQueue.Data
{
    public class JobQueueDbContext : DbContext
    {
        public JobQueueDbContext(
            DbContextOptions<JobQueueDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job>()
                .Property(j => j.RowVersion)
                .IsConcurrencyToken();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            UpdateRowVersions();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateRowVersions();
            return base.SaveChanges();
        }

        private void UpdateRowVersions()
        {
            foreach (var entry in ChangeTracker
                .Entries<Job>()
                .Where(e => e.State == EntityState.Modified))
            {
                entry.Entity.RowVersion = Guid.NewGuid();
            }
        }
    }
}