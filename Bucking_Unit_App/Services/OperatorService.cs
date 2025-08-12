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
        private readonly IStatsRepository _statsRepository;  // Добавьте зависимость в конструктор

        public OperatorService(IEmployeeRepository employeeRepository, Controller1C controller1C, IStatsRepository statsRepository)
        {
            _employeeRepository = employeeRepository;
            _controller1C = controller1C;
            _statsRepository = statsRepository;
        }

        public async Task AuthenticateOperatorAsync(string cardNumber, bool isAuth, DateTime? authTime)
        {
            await InitializeOperatorAsync(cardNumber);  // Инициализация оператора
            if (CurrentOperator == null) return;

            int? operatorId = await _employeeRepository.GetOperatorIdAsync(CurrentOperator.PersonnelNumber);
            if (!operatorId.HasValue) return;

            // Привязка/отвязка
            await _statsRepository.UpdateOperatorIdExchangeAsync(8, isAuth ? operatorId : null, isAuth);

            // Статистика авторизации
            DateTime? authFrom = isAuth ? authTime : null;
            DateTime? authTo = isAuth ? null : authTime;
            await _statsRepository.UpdateOperatorSysStatAsync(operatorId.Value, isAuth, authFrom, authTo, 8);

            // Вызов события изменения оператора
            OnOperatorChanged?.Invoke(this, EventArgs.Empty);
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
