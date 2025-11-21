using Models.Interfaces;

namespace Models.Models
{
    public class User : IModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Answer> Answers { get; set; } = new List<Answer>();
        public User(string name, string password) 
        {
            Name = name;
            Password = password;
        }
    }
}
