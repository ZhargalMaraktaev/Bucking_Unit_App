using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bucking_Unit_App.Interfaces;

namespace Bucking_Unit_App.Services
{
    // StatsService.cs
    public class StatsService
    {
        private readonly IStatsRepository _statsRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private int _lastKnownRepId;
        private int _lastKnownDowntimeId;

        public StatsService(IStatsRepository statsRepository, IEmployeeRepository employeeRepository)
        {
            _statsRepository = statsRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task UpdateStatsAsync(string personnelNumber, Action<string, string, string, string> updateUI)
        {
            var operatorId = await _employeeRepository.GetOperatorIdAsync(personnelNumber);
            if (operatorId == null) return;

            var (isDayShift, dayShiftDowntime, nightShiftDowntime) = await _statsRepository.GetDailyDowntimeAsync(operatorId.Value);
            var monthlyDowntime = await _statsRepository.GetMonthlyDowntimeAsync(operatorId.Value);
            var dailyOperationCount = await _statsRepository.GetDailyOperationCountAsync(operatorId.Value);
            var monthlyOperationCount = await _statsRepository.GetMonthlyOperationCountAsync(operatorId.Value);

            updateUI(
                dailyOperationCount.ToString(),
                (isDayShift ? (double)dayShiftDowntime : (double)nightShiftDowntime).ToString("F2"),
                monthlyOperationCount.ToString(),
                ((double)monthlyDowntime).ToString("F2")
            );
        }

        public async Task UpdateIdsAsync(string personnelNumber)
        {
            _lastKnownRepId = await _statsRepository.GetLastKnownRepIdAsync();
            _lastKnownDowntimeId = await _statsRepository.GetLastKnownDowntimeIdAsync();
            var operatorId = await _employeeRepository.GetOperatorIdAsync(personnelNumber);
            if (operatorId != null)
            {
                await _statsRepository.UpdateUnassignedOperatorIdsAsync(operatorId.Value, _lastKnownRepId);
                await _statsRepository.UpdateDowntimeOperatorIdAsync(operatorId.Value, _lastKnownDowntimeId);
                _lastKnownRepId = await _statsRepository.GetLastKnownRepIdAsync();
                _lastKnownDowntimeId = await _statsRepository.GetLastKnownDowntimeIdAsync();
            }
        }

        // Новый метод для обновления статистики всех операторов
        public async Task UpdateStatsForAllOperatorsAsync(
    Action<
        Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>,
        Dictionary<int, decimal>,
        Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>,
        Dictionary<int, int>> updateAllUI)
        {
            var dailyDowntime = await _statsRepository.GetDailyDowntimeByAllOperatorsAsync();
            var monthlyDowntime = await _statsRepository.GetMonthlyDowntimeByAllOperatorsAsync();
            var dailyOperationCount = await _statsRepository.GetDailyShiftOperationCountByAllOperatorsAsync();
            var monthlyOperationCount = await _statsRepository.GetMonthlyShiftOperationCountByAllOperatorsAsync();

            updateAllUI(dailyDowntime, monthlyDowntime, dailyOperationCount, monthlyOperationCount);
        }
    }
}