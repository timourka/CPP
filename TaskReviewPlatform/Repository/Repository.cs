using Microsoft.EntityFrameworkCore;
using Models.Interfaces;
using Repository.Data;

namespace Repository
{
    public class Repository<T> : IRepository<T> where T : class, IModel
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _set;

        public Repository(AppDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public async System.Threading.Tasks.Task<T> GetById(int id) =>
            await _set.FirstOrDefaultAsync(x => x.Id == id);

        public async System.Threading.Tasks.Task<List<T>> GetAll() =>
            await _set.ToListAsync();

        public async System.Threading.Tasks.Task Add(T entity)
        {
            await _set.AddAsync(entity);
        }

        public System.Threading.Tasks.Task Update(T entity)
        {
            _set.Update(entity);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task Delete(T entity)
        {
            _set.Remove(entity);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public async System.Threading.Tasks.Task Save()
        {
            await _context.SaveChangesAsync();
        }
    }
}
