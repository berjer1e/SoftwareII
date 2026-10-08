using CompanyDataTransferObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO=CompanyDataTransferObject;

namespace CompanyDataAccessLayer
{
    public interface IBaseDAL<T> where T : DTO.BaseDTO
    {
        List<T> GetAll();
        T? GetById(Guid id);    //T? - nullable, бо може не знайти по id 
        bool Add(T item);       // після успіху item.Id = згенерований ID
        bool Update(T item);
        bool Delete(Guid id);
    }

  
}
   
