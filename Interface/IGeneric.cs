namespace TutorsWorldBackend.Interface
{
    public interface IRepository<T>
    {
        Task<long> SaveAsync(T entity);
    }

}