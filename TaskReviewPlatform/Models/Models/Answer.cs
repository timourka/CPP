using Models.Interfaces;

namespace Models.Models
{
    public class Answer : IModel
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public User? Student { get; set; }
        public Task? Task { get; set; }

        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        public int Grade { get; set; } = -1;
        public string Status { get; set; } = "Черновик";
        public bool ReviewRequested { get; set; }
        public bool AllowResubmit { get; set; }
    }
}
