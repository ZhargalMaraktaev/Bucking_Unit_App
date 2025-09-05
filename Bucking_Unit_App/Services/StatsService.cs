using Bucking_Unit_App.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task UpdateStatsAsync(string personnelNumber, Action<string, string, string, string, string, string> updateUI)
        {
            var operatorId = await _employeeRepository.GetOperatorIdAsync(personnelNumber);
            if (operatorId == null) return;

            var (isDayShift, dayShiftDowntime, nightShiftDowntime) = await _statsRepository.GetDailyDowntimeAsync(operatorId.Value);
            var monthlyDowntime = await _statsRepository.GetMonthlyDowntimeAsync(operatorId.Value);
            var dailyOperationCount = await _statsRepository.GetDailyOperationCountAsync(operatorId.Value);
            var monthlyOperationCount = await _statsRepository.GetMonthlyOperationCountAsync(operatorId.Value);
            var (monthPlan, shiftPlan) = await _statsRepository.CalculatePlansAsync(operatorId.Value);

            updateUI(
                dailyOperationCount.ToString(),
                (isDayShift ? (double)dayShiftDowntime : (double)nightShiftDowntime).ToString("F2"),
                monthlyOperationCount.ToString(),
                ((double)monthlyDowntime).ToString("F2"),
                monthPlan.ToString(),
                shiftPlan.ToString()
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
    Action<Dictionary<int, decimal>, Dictionary<int, int>, Dictionary<int, double>> updateAllUI)
        {
            var monthlyDowntimeByShift = await _statsRepository.GetMonthlyDowntimeByShiftAsync();
            var monthlyOperationCountByShift = await _statsRepository.GetMonthlyOperationCountByShiftAsync();
            var monthlyPlanByShift = await _statsRepository.GetMonthlyPlanByShiftAsync(DateTime.Now);

            Debug.WriteLine("UpdateStatsForAllOperatorsAsync: Планы по сменам:");
            foreach (var kvp in monthlyPlanByShift)
            {
                Debug.WriteLine($"Smena={kvp.Key}, Plan={kvp.Value:F0}");
            }

            updateAllUI(monthlyDowntimeByShift, monthlyOperationCountByShift, monthlyPlanByShift);
        }
    }
}