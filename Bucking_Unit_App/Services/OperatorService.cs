using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.Interfaces;
using Bucking_Unit_App.Models;

namespace Bucking_Unit_App.Services
{
    // OperatorService.cs
    public class OperatorService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly Controller1C _controller1C;
        private Employee1CModel _currentOperator;

        public OperatorService(IEmployeeRepository employeeRepository, Controller1C controller1C)
        {
            _employeeRepository = employeeRepository;
            _controller1C = controller1C;
        }

        public Employee1CModel CurrentOperator
        {
            get => _currentOperator;
            set
            {
                _currentOperator = value;
                OnOperatorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnOperatorChanged;

        public async Task InitializeOperatorAsync(string cardNumber)
        {
            CurrentOperator = await _employeeRepository.GetEmployeeAsync(cardNumber) ?? await FetchAndSaveFrom1C(cardNumber);
        }

        private async Task<Employee1CModel> FetchAndSaveFrom1C(string cardNumber)
        {
            var employee = await _controller1C.GetResp1CSKUD(cardNumber);
            if (employee.ErrorCode == 0 && !string.IsNullOrEmpty(employee.PersonnelNumber))
            {
                await _employeeRepository.SaveEmployeeAsync(employee);
                return employee;
            }
            return null;
        }
    }
}
