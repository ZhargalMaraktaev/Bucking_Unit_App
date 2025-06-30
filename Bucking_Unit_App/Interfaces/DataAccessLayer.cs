using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bucking_Unit_App.Models;

namespace Bucking_Unit_App.Interfaces
{
    // DataAccessLayer.cs
    public class DataAccessLayer : IEmployeeRepository, IStatsRepository
    {
        private readonly string _connectionString;

        public DataAccessLayer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Employee1CModel> GetEmployeeAsync(string cardNumber)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "SELECT idCard, TabNumber, FIO, Department, EmployName FROM Pilot.dbo.dic_SKUD WHERE idCard = @idCard";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idCard", cardNumber);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Employee1CModel
                {
                    CardNumber = reader["idCard"].ToString(),
                    PersonnelNumber = reader["TabNumber"].ToString(),
                    FullName = reader["FIO"].ToString(),
                    Department = reader["Department"].ToString(),
                    Position = reader["EmployName"].ToString()
                };
            }
            return null;
        }

        public async Task SaveEmployeeAsync(Employee1CModel employee)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            if (!await CheckCardIdAsync(conn, employee.PersonnelNumber))
            {
                string insertQuery = @"
                INSERT INTO Pilot.dbo.dic_SKUD (idCard, TabNumber, FIO, Department, EmployName)
                VALUES (@idCard, @TabNumber, @FIO, @Department, @Position)";
                using var cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@idCard", employee.CardNumber);
                cmd.Parameters.AddWithValue("@TabNumber", employee.PersonnelNumber);
                cmd.Parameters.AddWithValue("@FIO", employee.FullName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", employee.Department ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Position", employee.Position ?? (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int?> GetOperatorIdAsync(string personnelNumber)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "SELECT id FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TabNumber", personnelNumber);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : null;
        }

        public async Task<(bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)> GetDailyDowntimeAsync(int operatorId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetDailyDowntimeByOperator", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.GetBoolean(reader.GetOrdinal("IsDayShift")),
                    reader.IsDBNull(reader.GetOrdinal("DayShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DayShiftDowntimeMinutes")),
                    reader.IsDBNull(reader.GetOrdinal("NightShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NightShiftDowntimeMinutes"))
                );
            }
            return (false, 0, 0);
        }

        public async Task<(bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)> GetMonthlyDowntimeAsync(int operatorId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetMonthlyDowntimeByOperator", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.GetBoolean(reader.GetOrdinal("IsDayShift")),
                    reader.IsDBNull(reader.GetOrdinal("DayShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DayShiftDowntimeMinutes")),
                    reader.IsDBNull(reader.GetOrdinal("NightShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NightShiftDowntimeMinutes"))
                );
            }
            return (false, 0, 0);
        }

        public async Task<int> GetDailyOperationCountAsync(int operatorId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetDailyShiftOperationCount", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.IsDBNull(reader.GetOrdinal("ShiftOperationCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ShiftOperationCount"));
            }
            return 0;
        }

        public async Task<int> GetMonthlyOperationCountAsync(int operatorId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetMonthlyShiftOperationCount", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.IsDBNull(reader.GetOrdinal("ShiftOperationCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ShiftOperationCount"));
            }
            return 0;
        }

        public async Task<int> GetLastKnownRepIdAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "SELECT ISNULL(MAX(Id), 0) FROM Pilot.dbo.MuftN3_REP";
            using var cmd = new SqlCommand(query, conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> GetLastKnownDowntimeIdAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string query = "SELECT ISNULL(MAX(Id), 0) FROM Pilot.dbo.Downtime WHERE SectorId = 8";
            using var cmd = new SqlCommand(query, conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateLastKnownRepIdAsync(int lastKnownRepId)
        {
            // Здесь можно добавить логику, если нужно обновлять значение в базе (например, таблицу конфигурации)
        }

        public async Task UpdateLastKnownDowntimeIdAsync(int lastKnownDowntimeId)
        {
            // Здесь можно добавить логику, если нужно обновлять значение в базе
        }

        public async Task UpdateUnassignedOperatorIdsAsync(int operatorId, int lastKnownRepId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string updateQuery = @"
            UPDATE Pilot.dbo.MuftN3_REP
            SET OperatorId = @OperatorId
            WHERE OperatorId IS NULL AND Id >= @LastKnownId";
            using var cmd = new SqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            cmd.Parameters.AddWithValue("@LastKnownId", lastKnownRepId);
            int affected = await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateDowntimeOperatorIdAsync(int operatorId, int lastKnownDowntimeId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string updateQuery = @"
            UPDATE Pilot.dbo.Downtime
            SET OperatorId = @OperatorId
            WHERE OperatorId IS NULL AND Id >= @LastKnownId";
            using var cmd = new SqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            cmd.Parameters.AddWithValue("@LastKnownId", lastKnownDowntimeId);
            int affected = await cmd.ExecuteNonQueryAsync();
        }

        private async Task<bool> CheckCardIdAsync(SqlConnection conn, string tabNumber)
        {
            string query = "SELECT COUNT(*) FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TabNumber", tabNumber);
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }
    }
}
