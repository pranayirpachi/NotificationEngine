using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationEngine.DataBase;
using NotificationEngine.Dto;
using NotificationEngine.Model;

namespace NotificationEngine.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationController> _logger;
        private readonly NotificationDbContext _context;

        public NotificationController(IConfiguration configuration, ILogger<NotificationController> logger, NotificationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }
        [HttpGet("update-status")]
        public async Task<IActionResult> UpdateSendingStatus(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .ToListAsync();

            if (!notifications.Any())
            {
                return NotFound(new { message = "No notifications found for the given UserId." });
            }

            var sendingStatuses = await _context.SendingStatuses
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!sendingStatuses.Any())
            {
                return NotFound(new { message = "No sending statuses found for the given UserId." });
            }


            bool allSeen = sendingStatuses.All(s => s.IsSeen);
            if (allSeen)
            {
                return Ok(new { message = "All notifications are already seen." });
            }

            // Update only unseen notifications
            foreach (var status in sendingStatuses.Where(s => !s.IsSeen))
            {
                status.IsSeen = true;
            }

            _context.SendingStatuses.UpdateRange(sendingStatuses);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sending statuses updated successfully." });
        }

        /*

        [HttpGet("check-status")]
        public async Task<IActionResult> CheckStatus(Guid userId)
        {
            var sendingStatuses = await _context.SendingStatuses
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!sendingStatuses.Any())
            {
                return NotFound(new { message = "No sending statuses found." });
            }

            bool allSeen = sendingStatuses.All(s => s.IsSeen);

            return Ok(new { isSafe = allSeen });
        }
        */




        // here are code

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationDto notificationDto)
        {
            if (notificationDto == null)
            {
                return BadRequest("Notification data is required.");
            }

            // 1. Validate the incoming DTO (Data Transfer Object)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Find the related User (important for ForeignKey relationship)
            var user = await _context.Users.FindAsync(notificationDto.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // 3. Create the Notification entity
            var notification = new Notification
            {
                Id = Guid.NewGuid(),  
                UserId = user.Id, 
                QuotationName = notificationDto.QuotationName,
                CreatedDate = DateTime.UtcNow, 
                ExpiryDate = notificationDto.ExpiryDate,
                IsDeleted = false // Set initial values
            };

            // 4. Add and save changes to the database
             _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();


            // 5. Create initial SendingStatus entries (one for each user, or as required)
            var sendingStatus = new SendingStatus
            {
                Id = Guid.NewGuid(),
                UserId = user.Id, // Associate the sending status with the user
                NotificationId = notification.Id, 
                CreatedDate = DateTime.UtcNow,
                IsSeen = false
            };
            _context.SendingStatuses.Add(sendingStatus);
            await _context.SaveChangesAsync();



            // 6. Return the created notification (good practice)
            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification); 
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            return Ok(notification);
        }

        // Notification Count as per UserId

        [HttpGet("unseen-count/{userId}")]
        public async Task<IActionResult> GetUnseenNotificationCount(Guid userId)
        {
            var unseenCount = await _context.SendingStatuses
                .Where(ss => ss.UserId == userId && !ss.IsSeen)
                .CountAsync();

            return Ok(new { UserId = userId, UnseenCount = unseenCount });
        }



        // List of notification as per UserID (unseen)
        [HttpGet("unseen-notifications/{userId}")]
        public async Task<IActionResult> GetUnseenNotifications(Guid userId)
        {
            // 1. Retrieve the data *first* (without the username)
            var unseenNotifications = await _context.SendingStatuses
                .Include(ss => ss.Notification)
                .Where(ss => ss.UserId == userId && !ss.IsSeen)
                .ToListAsync();

           
            var results = new List<object>();  
            foreach (var ss in unseenNotifications)
            {
                var userName = await GetUserNameAsync(ss.UserId); 
                results.Add(new
                {
                    NotificationId = ss.Notification.Id,
                    QuotationName = ss.Notification.QuotationName,
                    CreatedDate = ss.Notification.CreatedDate,
                    ExpiryDate = ss.Notification.ExpiryDate,
                    SendingStatusId = ss.Id,
                    Notification = new
                    {
                        Id = ss.Notification.Id,
                        QuotationName = ss.Notification.QuotationName,
                        CreatedDate = ss.Notification.CreatedDate,
                        ExpiryDate = ss.Notification.ExpiryDate
                    },
                    UserName = userName
                });
            }

            return Ok(results);
        }

        private async Task<string> GetUserNameAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.UserName;
        }

        // Both seen and unseen 
        [HttpGet("unseen-seen-notification/{userId}")]
        public async Task<IActionResult> GetBoth(Guid userId)
        {
            var notifications = await _context.SendingStatuses
         .Include(ss => ss.Notification)
         .Where(ss => ss.UserId == userId) // No filter for IsSeen
         .Select(ss => new 
         {
             NotificationId = ss.Notification.Id,
             QuotationName = ss.Notification.QuotationName,
             CreatedDate = ss.Notification.CreatedDate,
             ExpiryDate = ss.Notification.ExpiryDate,
             SendingStatusId = ss.Id,
             IsSeen = ss.IsSeen, // Include IsSeen status
             Notification = new 
             {
                 Id = ss.Notification.Id,
                 QuotationName = ss.Notification.QuotationName,
                 CreatedDate = ss.Notification.CreatedDate,
                 ExpiryDate = ss.Notification.ExpiryDate
             }
         })
         .ToListAsync();

            return Ok(notifications);
        }

        // Get Single Notifications as per UserId (View Purpose)
        // Get all seen notifications for a user
        [HttpGet("Notification-View-Purpose/{userId}")]
        public async Task<IActionResult> GetNotificationsByUser(Guid userId)
        {
            var username = await GetUserNameAsync(userId);

            var notifications = await _context.SendingStatuses
                .Include(ss => ss.Notification)
                .Where(ss => ss.UserId == userId && ss.IsSeen) // Only get seen notifications
                .OrderByDescending(ss => ss.Notification.CreatedDate)
                .ToListAsync();

            if (!notifications.Any())
            {
                return NotFound(new { message = "No seen notifications found for this user." });
            }
           
            var result = notifications.Select(notification => new
            {
                Username = username,

                NotificationId = notification.Notification.Id,
                QuotationName = notification.Notification.QuotationName,
                CreatedDate = notification.Notification.CreatedDate,
                ExpiryDate = notification.Notification.ExpiryDate,
                SendingStatusId = notification.Id,
            }).ToList();

            return Ok(result);
        }

        /*

        //2. Update seen Status of Notifications as per UserID
        [HttpPut("mark-as-seen/{userId}")]
        public async Task<IActionResult> MarkAsSeen(Guid userId)
        {
            var sendingStatuses = await _context.SendingStatuses
                .Where(s => s.UserId == userId && !s.IsSeen) 
                .ToListAsync();

            if (!sendingStatuses.Any())
            {
                return Ok(new { message = "No unseen notifications found for this user." });
            }

            foreach (var status in sendingStatuses)
            {
                status.IsSeen = true;
            }

            _context.SendingStatuses.UpdateRange(sendingStatuses);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notifications marked as seen successfully." });
        }
        */




        [HttpPut("mark-as-seen/{userId}")]
        public async Task<IActionResult> MarkAsSeen(Guid userId)
        {
            var nextUnseenStatus = await _context.SendingStatuses
                .Where(s => s.UserId == userId && !s.IsSeen) // Find first unseen notification
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (nextUnseenStatus == null)
            {
                return Ok(new { message = "All notifications have already been seen." });
            }

            nextUnseenStatus.IsSeen = true;
            _context.SendingStatuses.Update(nextUnseenStatus);
            await _context.SaveChangesAsync();

            return Ok(new { message = "One notification marked as seen successfully." });
        }





    }
}
