using System.ComponentModel.DataAnnotations;

namespace NotificationEngine.Model
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserName { get; set; }

        public DateTime Created { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<SendingStatus> SendingStatuses { get; set; }
    }
}