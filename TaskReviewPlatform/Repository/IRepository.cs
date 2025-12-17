using Models.Interfaces;

namespace Repository
{
    public interface IRepository<T> where T : class, IModel
    {
        System.Threading.Tasks.Task<T> GetById(int id);
        System.Threading.Tasks.Task<List<T>> GetAll();
        System.Threading.Tasks.Task Add(T entity);
        System.Threading.Tasks.Task Update(T entity);
        System.Threading.Tasks.Task Delete(T entity);
        System.Threading.Tasks.Task Save();
    }
}
