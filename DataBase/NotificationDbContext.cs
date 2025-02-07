
using Microsoft.EntityFrameworkCore;
using NotificationEngine.Model;

namespace NotificationEngine.DataBase
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SendingStatus> SendingStatuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SendingStatus -> User
            modelBuilder.Entity<SendingStatus>()
                .HasOne(ss => ss.User)
                .WithMany(u => u.SendingStatuses)
                .HasForeignKey(ss => ss.UserId)
                .OnDelete(DeleteBehavior.Restrict);  

            // SendingStatus -> Notification
            modelBuilder.Entity<SendingStatus>()
                .HasOne(ss => ss.Notification)
                .WithMany(n => n.SendingStatuses)
                .HasForeignKey(ss => ss.NotificationId)
                .OnDelete(DeleteBehavior.Restrict);                  
        }
    }
}