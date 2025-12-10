using Models.Interfaces;

namespace Models.Models
{
    public class AnswerFile : IModel
    {
        public int Id { get; set; }

        public string RelativePath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public Answer? Answer { get; set; }
    }
}
