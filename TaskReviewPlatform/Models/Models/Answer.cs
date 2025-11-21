using Models.Interfaces;

namespace Models.Models
{
    public class Answer : IModel
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public User? Student { get; set; }
        public Task? Task { get; set; }

        public int Grade { get; set; }
    }
}
