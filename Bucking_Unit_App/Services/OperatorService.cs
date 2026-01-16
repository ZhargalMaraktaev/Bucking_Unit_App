using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.Interfaces;
using Bucking_Unit_App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            try
            {
                Employee1CModel current = null;
                if (isAuth)
                {
                    await InitializeOperatorAsync(cardNumber);
                    current = CurrentOperator;
                    if (current == null) return;
                }
                else
                {
                    current = CurrentOperator; // Используем текущего для deauth
                    if (current == null) return;
                }

                int? operatorId = await _employeeRepository.GetOperatorIdAsync(current.PersonnelNumber);
                if (!operatorId.HasValue) return;

                // Обновление с null для deauth
                await _statsRepository.UpdateOperatorIdExchangeAsync(8, isAuth ? operatorId : null, isAuth);

                DateTime? authFrom = isAuth ? authTime : null;
                DateTime? authTo = isAuth ? null : authTime;
                await _statsRepository.UpdateOperatorSysStatAsync(operatorId.Value, isAuth, authFrom, authTo, 8);

                if (!isAuth)
                {
                    CurrentOperator = null; // Сброс после всех обновлений, вызовет OnOperatorChanged
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OperatorService: Ошибка в AuthenticateOperatorAsync: {ex.Message}");
                // Можно добавить логирование или обработку
            }
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

        public async Task<int?> GetOperatorIdAsync(string personnelNumber)
        {
            return await _employeeRepository.GetOperatorIdAsync(personnelNumber);
        }
        public async Task InitializeOperatorAsync(string cardNumber)
        {
            try
            {
                Debug.WriteLine("InitializeOperatorAsync called for cardNumber: {CardNumber}", cardNumber);
                var employeeFrom1C = await _controller1C.GetResp1CSKUD(cardNumber);

                if (employeeFrom1C.ErrorCode != 0 || string.IsNullOrEmpty(employeeFrom1C.PersonnelNumber))
                {
                    Debug.WriteLine("Failed to fetch valid employee from 1C for cardNumber: {CardNumber}. ErrorCode: {ErrorCode}, ErrorText: {ErrorText}",
                        cardNumber, employeeFrom1C.ErrorCode, employeeFrom1C.ErrorText);
                    CurrentOperator = null;
                    return;
                }

                var syncedEmployee = await _employeeRepository.SyncEmployeeAsync(employeeFrom1C);

                if (syncedEmployee.ErrorCode != (int)Employee1CModel.ErrorCodes.EmployeeFound)
                {
                    Debug.WriteLine("Failed to sync employee for cardNumber: {CardNumber}. ErrorCode: {ErrorCode}, ErrorText: {ErrorText}",
                        cardNumber, syncedEmployee.ErrorCode, syncedEmployee.ErrorText);
                    CurrentOperator = null;
                    return;
                }

                CurrentOperator = syncedEmployee;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in InitializeOperatorAsync for cardNumber: {CardNumber}", cardNumber);
                CurrentOperator = null;
            }
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
