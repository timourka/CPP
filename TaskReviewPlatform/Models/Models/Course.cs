using Models.Interfaces;

namespace Models.Models
{
    public class Course : IModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<User> Avtors { get; set; } = new List<User>();
        public List<User> Participants { get; set; } = new List<User>();
        public List<Task> Tasks { get; set; } = new List<Task>();
    }
}
