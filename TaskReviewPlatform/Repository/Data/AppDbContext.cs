using Microsoft.EntityFrameworkCore;
using Models.Models;

namespace Repository.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Models.Models.Task> Tasks { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<AnswerFile> AnswerFiles { get; set; }
        public DbSet<ReviewComment> ReviewComments { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Course <-> User (Avtors)
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Avtors)
                .WithMany()
                .UsingEntity(j => j.ToTable("CourseAvtors"));

            // Course <-> User (Participants)
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Participants)
                .WithMany()
                .UsingEntity(j => j.ToTable("CourseParticipants"));
        }
    }
}
