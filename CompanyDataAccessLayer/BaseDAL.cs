using CompanyDataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO = CompanyDataTransferObject;

namespace CompanyDataAccessLayer
{
    public abstract class BaseDAL<TEntity, TDTO> : IBaseDAL<TDTO>
        where TEntity : class
        where TDTO : DTO.BaseDTO
    {
        private readonly string _connectionString; // Connection string for the database
        protected BaseDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected SoftwrDbContext CreateContext() =>
            new SoftwrDbContext(new DbContextOptionsBuilder<SoftwrDbContext>()
                .UseSqlServer(_connectionString).Options); // Create a new instance of SoftwrDbContext with the provided connection string

        // Abstract methods to convert between entity and DTO
        protected abstract TDTO ToDto(TEntity entity);
        protected abstract TEntity ToEntity(TDTO dto);


        public bool Add(TDTO item)
        {   
            try
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                using var db = CreateContext();
                var entity = ToEntity(item);
                db.Add(entity);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error adding item: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
                return false;
            }
           
        }

        public bool Delete(Guid id)
        {
            try 
            {
                using var db = CreateContext();
                var entity = db.Set<TEntity>().Find(id);
                if (entity == null)
                {
                    return false; // Item not found
                }
                db.Remove(entity);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error deleting item: {ex.Message}");
                return false;
            }
        }

        public List<TDTO> GetAll()
        {
            try
            {
                using var db = CreateContext();
                var entities = db.Set<TEntity>().ToList();
                return entities.Select(ToDto).ToList();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error retrieving items: {ex.Message}");
                return new List<TDTO>();
            }
        }

        public TDTO? GetById(Guid id)
        {
            try { 
                using var db = CreateContext();
                var entity = db.Set<TEntity>().Find(id);
                return entity != null ? ToDto(entity) : null;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error retrieving item by ID: {ex.Message}");
                return null;
            }  
            }

        public bool Update(TDTO item)
        {
            try { 
                using var db = CreateContext();
                var entity = ToEntity(item);
                db.Update(entity);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error updating item: {ex.Message}");
                return false;
            }
        }
    }
}
