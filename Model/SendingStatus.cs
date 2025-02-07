using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationEngine.Model
{
    public class SendingStatus
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Notification))]
        public Guid NotificationId { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsSeen { get; set; } = false;

        // Navigation Properties
        public User User { get; set; }  // Uncommented the User navigation property
        public Notification Notification { get; set; }
    }
}