using Models.Interfaces;
using System;

namespace Models.Models
{
    public class ReviewComment : IModel
    {
        public int Id { get; set; }

        public Answer? Answer { get; set; }

        public User? Reviewer { get; set; }

        public string? FileName { get; set; }

        public int? LineNumber { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
