using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bucking_Unit_App.Models;

namespace Bucking_Unit_App.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee1CModel> GetEmployeeAsync(string cardNumber);
        Task SaveEmployeeAsync(Employee1CModel employee);
        Task<int?> GetOperatorIdAsync(string personnelNumber);
    }
}
