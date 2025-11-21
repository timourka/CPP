using Models.Interfaces;

namespace Models.Models
{
    public class Task : IModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Course Course { get; set; }

        public Task(string name, string description, Course course)
        {
            Name = name;
            Description = description;
            Course = course;
        }
    }
}
