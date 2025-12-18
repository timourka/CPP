using Models.Interfaces;
using System;

namespace Models.Models
{
    public class Notification : IModel
    {
        public int Id { get; set; }

        public User? User { get; set; }

        public Answer? Answer { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }
    }
}
