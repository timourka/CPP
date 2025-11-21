using Models.Interfaces;

namespace Repository
{
    public interface IRepository<T> where T : class, IModel
    {
        Task<T> GetById(int id);
        Task<List<T>> GetAll();
        Task Add(T entity);
        Task Update(T entity);
        Task Delete(T entity);
        Task Save();
    }
}
