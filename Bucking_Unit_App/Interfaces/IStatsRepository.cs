using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Interfaces
{
    public interface IStatsRepository
    {
        Task<(bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)> GetDailyDowntimeAsync(int operatorId);
        Task<decimal> GetMonthlyDowntimeAsync(int operatorId);
        Task<int> GetDailyOperationCountAsync(int operatorId);
        Task<int> GetMonthlyOperationCountAsync(int operatorId);
        Task<int> GetLastKnownRepIdAsync();
        Task<int> GetLastKnownDowntimeIdAsync();
        Task UpdateLastKnownRepIdAsync(int lastKnownRepId);
        Task UpdateLastKnownDowntimeIdAsync(int lastKnownDowntimeId);
        Task UpdateUnassignedOperatorIdsAsync(int operatorId, int lastKnownRepId);
        Task UpdateDowntimeOperatorIdAsync(int operatorId, int lastKnownDowntimeId);
        // Новый метод для обновления привязки/отвязки в OperatorIdExchange
        Task UpdateOperatorIdExchangeAsync(int sectorId, int? operatorId, bool isAuth);

        // Новый метод для обновления статистики авторизации в OperatorSysStat
        Task UpdateOperatorSysStatAsync(int operatorId, bool isAuth, DateTime? authFrom, DateTime? authTo, int sectorId);

        Task<Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>> GetDailyDowntimeByAllOperatorsAsync();
        Task<Dictionary<int, decimal>> GetMonthlyDowntimeByAllOperatorsAsync();
        Task<Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>> GetDailyShiftOperationCountByAllOperatorsAsync();
        Task<Dictionary<int, int>> GetMonthlyShiftOperationCountByAllOperatorsAsync();
    }
}