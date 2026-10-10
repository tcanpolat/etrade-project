using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ETICARET.DataAccess.Abstract
{
    // Repository pattern arayüzü
    // Ortak CRUD operasyonlarını tanımlamak için kullanılacak
    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        T GetOne(Expression<Func<T,bool>> filter = null);
        List<T> GetAll(Expression<Func<T,bool>> filter = null);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
