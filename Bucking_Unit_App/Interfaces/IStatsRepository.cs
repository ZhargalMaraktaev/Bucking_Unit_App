using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Interfaces
{
    // IStatsRepository.cs
    public interface IStatsRepository
    {
        Task<(bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)> GetDailyDowntimeAsync(int operatorId);
        Task<(bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)> GetMonthlyDowntimeAsync(int operatorId);
        Task<int> GetDailyOperationCountAsync(int operatorId);
        Task<int> GetMonthlyOperationCountAsync(int operatorId);
        Task<int> GetLastKnownRepIdAsync();
        Task<int> GetLastKnownDowntimeIdAsync();
        Task UpdateLastKnownRepIdAsync(int lastKnownRepId);
        Task UpdateLastKnownDowntimeIdAsync(int lastKnownDowntimeId);
        Task UpdateUnassignedOperatorIdsAsync(int operatorId, int lastKnownRepId);
        Task UpdateDowntimeOperatorIdAsync(int operatorId, int lastKnownDowntimeId);

        Task<Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>> GetDailyDowntimeByAllOperatorsAsync();
        Task<Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>> GetMonthlyDowntimeByAllOperatorsAsync();
        Task<Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>> GetDailyShiftOperationCountByAllOperatorsAsync();
        Task<Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>> GetMonthlyShiftOperationCountByAllOperatorsAsync();
    }
}