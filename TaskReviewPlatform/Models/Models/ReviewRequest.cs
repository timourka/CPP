using Models.Interfaces;
using System;

namespace Models.Models
{
    public class ReviewRequest : IModel
    {
        public int Id { get; set; }
        public Answer? Answer { get; set; }
        public User? Reviewer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Completed { get; set; }
    }
}
