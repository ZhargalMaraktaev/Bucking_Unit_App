using Bucking_Unit_App.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Interfaces
{
    // DataAccessLayer.cs
    public class DataAccessLayer : IEmployeeRepository, IStatsRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DataAccessLayer>? _logger; // Логгер необязательный

        public DataAccessLayer(string connectionString)
        {
            _connectionString = connectionString;
        }
        public DataAccessLayer(string connectionString, ILogger<DataAccessLayer> logger) : this(connectionString)
        {
            _logger = logger;
        }

        public async Task<Employee1CModel> GetEmployeeAsync(string cardNumber)
        {
            try
            {
                Employee1CModel employee = null;
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string query = "SELECT idCard, TabNumber, FIO, Department, EmployName, TORoleId FROM Pilot.dbo.dic_SKUD WHERE idCard = @idCard";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idCard", cardNumber);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    employee = new Employee1CModel
                    {
                        CardNumber = reader["idCard"].ToString(),
                        PersonnelNumber = reader["TabNumber"].ToString(),
                        FullName = reader["FIO"].ToString(),
                        Department = reader["Department"].ToString(),
                        Position = reader["EmployName"].ToString(),
                        TORoleId = reader["TORoleId"] != DBNull.Value ? Convert.ToInt32(reader["TORoleId"]) : null
                    };
                }
                // Закрываем reader перед выполнением нового запроса
                reader.Close();
                if (employee != null && !employee.TORoleId.HasValue)
                {
                    employee.TORoleId = DetermineRoleFromEmployName(employee.Position);
                    if (employee.TORoleId.HasValue)
                    {
                        await UpdateTORoleIdAsync(conn, cardNumber, employee.TORoleId.Value);
                    }
                }

                if (employee != null)
                {
                    return employee;
                }
                _logger.LogInformation("Employee not found for cardNumber: {CardNumber}", cardNumber);
                return new Employee1CModel
                {
                    CardNumber = cardNumber,
                    ErrorCode = (int)Employee1CModel.ErrorCodes.EmployeeNotFound,
                    ErrorText = "Employee not found in dic_SKUD."
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in GetEmployeeAsync for cardNumber: {CardNumber}", cardNumber);
                return new Employee1CModel
                {
                    CardNumber = cardNumber,
                    ErrorCode = (int)Employee1CModel.ErrorCodes.SpecificError,
                    ErrorText = $"Database error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetEmployeeAsync for cardNumber: {CardNumber}", cardNumber);
                return new Employee1CModel
                {
                    CardNumber = cardNumber,
                    ErrorCode = (int)Employee1CModel.ErrorCodes.UnknownError,
                    ErrorText = $"Unexpected error: {ex.Message}"
                };
            }
        }
        private async Task UpdateTORoleIdAsync(SqlConnection conn, string cardNumber, int toRoleId)
        {
            try
            {
                using var cmd = new SqlCommand("UPDATE Pilot.dbo.dic_SKUD SET TORoleId = @TORoleId WHERE idCard = @IdCard", conn);
                cmd.Parameters.Add("@TORoleId", SqlDbType.Int).Value = toRoleId;
                cmd.Parameters.Add("@IdCard", SqlDbType.NVarChar).Value = cardNumber;
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Updated TORoleId={TORoleId} for cardNumber: {CardNumber}", toRoleId, cardNumber);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error in UpdateTORoleIdAsync for cardNumber: {CardNumber}", cardNumber);
                throw;
            }
        }

        private int? DetermineRoleFromEmployName(string employName)
        {
            if (string.IsNullOrEmpty(employName))
            {
                _logger.LogWarning("EmployName is null or empty, cannot determine TORoleId");
                return null;
            }

            employName = employName.ToLower();
            if (employName.Contains("слесарь"))
            {
                _logger.LogInformation("Assigned TORoleId=2 for EmployName: {EmployName}", employName);
                return 2;
            }
            else if (employName.Contains("оператор"))
            {
                _logger.LogInformation("Assigned TORoleId=1 for EmployName: {EmployName}", employName);
                return 1;
            }
            else if (employName.Contains("инженер") || employName.Contains("мастер") ||
                     employName.Contains("начальник") || employName.Contains("заместитель"))
            {
                _logger.LogInformation("Assigned TORoleId=4 for EmployName: {EmployName}", employName);
                return 4;
            }

            _logger.LogInformation("No matching role found for EmployName: {EmployName}", employName);
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

        public async Task<decimal> GetMonthlyDowntimeAsync(int operatorId)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                Debug.WriteLine($"Opening connection at {DateTime.Now}");
                await conn.OpenAsync();
                using var cmd = new SqlCommand("GetMonthlyDowntimeByOperator", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                cmd.Parameters.AddWithValue("@OperatorId", operatorId);
                Debug.WriteLine($"Executing command at {DateTime.Now}");
                using var reader = await cmd.ExecuteReaderAsync();
                Debug.WriteLine($"Starting to read data at {DateTime.Now}");
                if (await reader.ReadAsync())
                {
                    decimal totalDowntime = reader.IsDBNull(reader.GetOrdinal("TotalDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDowntimeMinutes"));
                    Debug.WriteLine($"OperatorId={operatorId}, TotalDowntimeMinutes={totalDowntime}");
                    return totalDowntime;
                }
                Debug.WriteLine($"No data found for OperatorId={operatorId}");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMonthlyDowntimeAsync: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                    Debug.WriteLine("Connection closed");
                }
            }
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
                return reader.IsDBNull(reader.GetOrdinal("TotalOperations")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalOperations"));
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
            WHERE OperatorId IS NULL AND Id >= @LastKnownId and EndDateTime is not NULL";
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
            WHERE OperatorId IS NULL 
            AND Id >= @LastKnownId 
            AND SectorId = 8"; // Добавлен фильтр по SectorId
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

        // Новый метод для GetDailyDowntimeByAllOperators
        public async Task<Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>> GetDailyDowntimeByAllOperatorsAsync()
        {
            var result = new Dictionary<int, (bool IsDayShift, decimal DayShiftDowntime, decimal NightShiftDowntime)>();
            using var conn = new SqlConnection(_connectionString);
            try
            {
                Debug.WriteLine($"Opening connection at {DateTime.Now}");
                await conn.OpenAsync();
                using var cmd = new SqlCommand("GetDailyDowntimeByAllOperators", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                Debug.WriteLine($"Executing command at {DateTime.Now}");
                using var reader = await cmd.ExecuteReaderAsync();
                Debug.WriteLine($"Starting to read data at {DateTime.Now}");
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    try
                    {
                        int operatorId = reader.IsDBNull(reader.GetOrdinal("OperatorId")) ? -1 : reader.GetInt32(reader.GetOrdinal("OperatorId"));
                        bool isDayShift = reader.GetBoolean(reader.GetOrdinal("IsDayShift"));
                        decimal dayShiftDowntime = reader.IsDBNull(reader.GetOrdinal("DayShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DayShiftDowntimeMinutes"));
                        decimal nightShiftDowntime = reader.IsDBNull(reader.GetOrdinal("NightShiftDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NightShiftDowntimeMinutes"));
                        result[operatorId] = (isDayShift, dayShiftDowntime, nightShiftDowntime);
                        Debug.WriteLine($"Row {++rowCount}: OperatorId={operatorId}, IsDayShift={isDayShift}, DayShiftDowntime={dayShiftDowntime}, NightShiftDowntime={nightShiftDowntime}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing row {rowCount + 1}: {ex.Message}");
                        throw;
                    }
                }
                Debug.WriteLine($"Finished reading {rowCount} rows at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDailyDowntimeByAllOperatorsAsync: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                    Debug.WriteLine("Connection closed");
                }
            }
            return result;
        }

        // Новый метод для GetMonthlyDowntimeByAllOperators
        public async Task<Dictionary<int, decimal>> GetMonthlyDowntimeByAllOperatorsAsync()
        {
            var result = new Dictionary<int, decimal>();
            using var conn = new SqlConnection(_connectionString);
            try
            {
                Debug.WriteLine($"Opening connection at {DateTime.Now}");
                await conn.OpenAsync();
                using var cmd = new SqlCommand("GetMonthlyDowntimeByAllOperators", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                Debug.WriteLine($"Executing command at {DateTime.Now}");
                using var reader = await cmd.ExecuteReaderAsync();
                Debug.WriteLine($"Starting to read data at {DateTime.Now}");
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    try
                    {
                        int operatorId = reader.IsDBNull(reader.GetOrdinal("OperatorId")) ? -1 : reader.GetInt32(reader.GetOrdinal("OperatorId"));
                        decimal totalDowntime = reader.IsDBNull(reader.GetOrdinal("TotalDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDowntimeMinutes"));
                        result[operatorId] = totalDowntime;
                        Debug.WriteLine($"Row {++rowCount}: OperatorId={operatorId}, TotalDowntimeMinutes={totalDowntime}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing row {rowCount + 1}: {ex.Message}");
                        throw;
                    }
                }
                Debug.WriteLine($"Finished reading {rowCount} rows at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMonthlyDowntimeByAllOperatorsAsync: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                    Debug.WriteLine("Connection closed");
                }
            }
            return result;
        }

        // Новый метод для GetDailyShiftOperationCountByAllOperators
        public async Task<Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>> GetDailyShiftOperationCountByAllOperatorsAsync()
        {
            var result = new Dictionary<int, (bool IsDayShift, int ShiftOperationCount)>();
            using var conn = new SqlConnection(_connectionString);
            try
            {
                Debug.WriteLine($"Opening connection at {DateTime.Now}");
                await conn.OpenAsync();
                using var cmd = new SqlCommand("GetDailyShiftOperationCountByAllOperators", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                Debug.WriteLine($"Executing command at {DateTime.Now}");
                using var reader = await cmd.ExecuteReaderAsync();
                Debug.WriteLine($"Starting to read data at {DateTime.Now}");
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    try
                    {
                        int operatorId = reader.IsDBNull(reader.GetOrdinal("OperatorId")) ? -1 : reader.GetInt32(reader.GetOrdinal("OperatorId"));
                        bool isDayShift = reader.GetBoolean(reader.GetOrdinal("IsDayShift"));
                        int shiftOperationCount = reader.IsDBNull(reader.GetOrdinal("ShiftOperationCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ShiftOperationCount"));
                        result[operatorId] = (isDayShift, shiftOperationCount);
                        Debug.WriteLine($"Row {++rowCount}: OperatorId={operatorId}, IsDayShift={isDayShift}, ShiftOperationCount={shiftOperationCount}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing row {rowCount + 1}: {ex.Message}");
                        throw;
                    }
                }
                Debug.WriteLine($"Finished reading {rowCount} rows at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDailyShiftOperationCountByAllOperatorsAsync: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                    Debug.WriteLine("Connection closed");
                }
            }
            return result;
        }

        // Новый метод для GetMonthlyShiftOperationCountByAllOperators
        public async Task<Dictionary<int, int>> GetMonthlyShiftOperationCountByAllOperatorsAsync()
        {
            var result = new Dictionary<int, int>();
            using var conn = new SqlConnection(_connectionString);
            try
            {
                Debug.WriteLine($"Opening connection at {DateTime.Now}");
                await conn.OpenAsync();
                using var cmd = new SqlCommand("GetMonthlyShiftOperationCountByAllOperators", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;
                Debug.WriteLine($"Executing command at {DateTime.Now}");
                using var reader = await cmd.ExecuteReaderAsync();
                Debug.WriteLine($"Starting to read data at {DateTime.Now}");
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    try
                    {
                        int operatorId = reader.IsDBNull(reader.GetOrdinal("OperatorId")) ? -1 : reader.GetInt32(reader.GetOrdinal("OperatorId"));
                        int totalOperationCount = reader.IsDBNull(reader.GetOrdinal("TotalOperationCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalOperationCount"));
                        result[operatorId] = totalOperationCount;
                        Debug.WriteLine($"Row {++rowCount}: OperatorId={operatorId}, TotalOperationCount={totalOperationCount}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing row {rowCount + 1}: {ex.Message}");
                        throw;
                    }
                }
                Debug.WriteLine($"Finished reading {rowCount} rows at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMonthlyShiftOperationCountByAllOperatorsAsync: {ex.Message}");
                throw;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                    Debug.WriteLine("Connection closed");
                }
            }
            return result;
        }
        public async Task UpdateOperatorIdExchangeAsync(int sectorId, int? operatorId, bool isAuth)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                string query;
                if (await IsSectorExistsInExchangeAsync(conn, transaction, sectorId))
                {
                    // Обновление существующей записи
                    query = @"
                        UPDATE Pilot.dbo.OperatorIdExchange
                        SET OperatorId = @OperatorId
                        WHERE SectorId = @SectorId";
                }
                else
                {
                    // Вставка новой записи
                    query = @"
                        INSERT INTO Pilot.dbo.OperatorIdExchange (SectorId, OperatorId)
                        VALUES (@SectorId, @OperatorId)";
                }

                using var cmd = new SqlCommand(query, conn, transaction);
                cmd.Parameters.AddWithValue("@SectorId", sectorId);
                cmd.Parameters.AddWithValue("@OperatorId", (object)operatorId ?? DBNull.Value);  // Обработка null для отвязки
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                Debug.WriteLine($"DataAccessLayer: OperatorIdExchange обновлено для SectorId={sectorId}, OperatorId={operatorId}, IsAuth={isAuth}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine($"DataAccessLayer: Ошибка в UpdateOperatorIdExchangeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateOperatorSysStatAsync(int operatorId, bool isAuth, DateTime? authFrom, DateTime? authTo, int sectorId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Проверка существования OperatorId
                string checkOperatorQuery = "SELECT COUNT(*) FROM Pilot.dbo.dic_SKUD WHERE id = @OperatorId";
                using var checkCmd = new SqlCommand(checkOperatorQuery, conn, transaction);
                checkCmd.Parameters.AddWithValue("@OperatorId", operatorId);
                if ((int)await checkCmd.ExecuteScalarAsync() == 0)
                {
                    Debug.WriteLine($"DataAccessLayer: OperatorId={operatorId} не найден в dic_SKUD.");
                    throw new InvalidOperationException("Оператор не найден в dic_SKUD.");
                }

                // Получаем StatId из dic_SKUD
                int? statId = await GetStatIdAsync(conn, transaction, operatorId);

                string query;
                if (statId.HasValue)
                {
                    // Обновление существующей записи
                    if (isAuth)
                    {
                        // При авторизации обновляем LastAuthFrom, LastAuthTo=null, LastSectorId
                        query = @"
                            UPDATE Pilot.dbo.OperatorSysStat
                            SET LastAuthFrom = @LastAuthFrom,
                                LastAuthTo = NULL,
                                LastSectorId = @LastSectorId
                            WHERE Id = @StatId";
                    }
                    else
                    {
                        // При деавторизации обновляем только LastAuthTo и LastSectorId
                        query = @"
                            UPDATE Pilot.dbo.OperatorSysStat
                            SET LastAuthTo = @LastAuthTo,
                                LastSectorId = @LastSectorId
                            WHERE Id = @StatId";
                    }
                }
                else
                {
                    // Вставка новой записи
                    query = @"
                        INSERT INTO Pilot.dbo.OperatorSysStat (RegistrationDate, LastAuthFrom, LastAuthTo, LastSectorId)
                        VALUES (@RegistrationDate, @LastAuthFrom, @LastAuthTo, @LastSectorId);
                        SELECT SCOPE_IDENTITY();";  // Получаем новый Id
                }

                using var cmd = new SqlCommand(query, conn, transaction);
                if (statId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@StatId", statId.Value);
                    if (isAuth)
                    {
                        cmd.Parameters.AddWithValue("@LastAuthFrom", (object)authFrom ?? DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@LastAuthTo", (object)authTo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastSectorId", sectorId);
                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Для вставки: Устанавливаем RegistrationDate
                    DateTime registrationDate = isAuth ? authFrom ?? DateTime.Now : authTo ?? DateTime.Now;
                    cmd.Parameters.AddWithValue("@RegistrationDate", registrationDate);
                    cmd.Parameters.AddWithValue("@LastAuthFrom", isAuth ? (object)authFrom ?? DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastAuthTo", (object)authTo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastSectorId", sectorId);

                    var newStatId = await cmd.ExecuteScalarAsync();  // Получаем новый Id
                    if (newStatId != null)
                    {
                        // Обновляем StatId в dic_SKUD
                        string updateDicQuery = @"
                            UPDATE Pilot.dbo.dic_SKUD
                            SET StatId = @NewStatId
                            WHERE id = @OperatorId";
                        using var updateCmd = new SqlCommand(updateDicQuery, conn, transaction);
                        updateCmd.Parameters.AddWithValue("@NewStatId", Convert.ToInt32(newStatId));
                        updateCmd.Parameters.AddWithValue("@OperatorId", operatorId);
                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            Debug.WriteLine($"DataAccessLayer: Не удалось обновить StatId в dic_SKUD для OperatorId={operatorId}.");
                            throw new InvalidOperationException("Не удалось обновить StatId в dic_SKUD: запись не найдена.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("DataAccessLayer: Не удалось получить новый StatId из OperatorSysStat.");
                        throw new InvalidOperationException("Не удалось создать запись в OperatorSysStat.");
                    }
                }

                // Обновление RoleId в dic_SKUD при авторизации
                if (isAuth)
                {
                    int roleId = (operatorId == 109 || operatorId == 92 || operatorId == 11 || operatorId == 16 || operatorId == 18) ? 2 : 1;
                    string updateRoleQuery = @"
                        UPDATE Pilot.dbo.dic_SKUD
                        SET RoleId = @RoleId
                        WHERE id = @OperatorId";
                    using var roleCmd = new SqlCommand(updateRoleQuery, conn, transaction);
                    roleCmd.Parameters.AddWithValue("@RoleId", roleId);
                    roleCmd.Parameters.AddWithValue("@OperatorId", operatorId);
                    int roleRowsAffected = await roleCmd.ExecuteNonQueryAsync();

                    if (roleRowsAffected == 0)
                    {
                        Debug.WriteLine($"DataAccessLayer: Не удалось обновить RoleId в dic_SKUD для OperatorId={operatorId}.");
                        throw new InvalidOperationException("Не удалось обновить RoleId в dic_SKUD: запись не найдена.");
                    }
                    Debug.WriteLine($"DataAccessLayer: RoleId={roleId} установлен для OperatorId={operatorId}.");
                }

                await transaction.CommitAsync();
                Debug.WriteLine($"DataAccessLayer: OperatorSysStat обновлено для OperatorId={operatorId}, IsAuth={isAuth}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine($"DataAccessLayer: Ошибка в UpdateOperatorSysStatAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        // Вспомогательный метод: Проверка существования сектора в OperatorIdExchange
        private async Task<bool> IsSectorExistsInExchangeAsync(SqlConnection conn, SqlTransaction transaction, int sectorId)
        {
            string query = "SELECT COUNT(*) FROM Pilot.dbo.OperatorIdExchange WHERE SectorId = @SectorId";
            using var cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@SectorId", sectorId);
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        // Вспомогательный метод: Получение StatId из dic_SKUD
        private async Task<int?> GetStatIdAsync(SqlConnection conn, SqlTransaction transaction, int operatorId)
        {
            string query = "SELECT StatId FROM Pilot.dbo.dic_SKUD WHERE id = @OperatorId";
            using var cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@OperatorId", operatorId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : null;
        }
        public async Task<Dictionary<int, decimal>> GetMonthlyDowntimeByShiftAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetMonthlyDowntimeByAllOperatorsByShift", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();

            var dict = new Dictionary<int, decimal>();
            while (await reader.ReadAsync())
            {
                int shift = reader.GetInt32(reader.GetOrdinal("Shift")); // Smena (1=А, 2=Б, 3=В, 4=Г)
                decimal downtime = reader.IsDBNull(reader.GetOrdinal("TotalDowntimeMinutes")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDowntimeMinutes"));

                if (dict.ContainsKey(shift))
                    dict[shift] += downtime;
                else
                    dict[shift] = downtime;
            }
            return dict;
        }
        // Новый метод: Месячное количество операций по сменам (агрегация по Smena)
        public async Task<Dictionary<int, int>> GetMonthlyOperationCountByShiftAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("GetPipeCounterBySmenaForProductionMonth", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            using var reader = await cmd.ExecuteReaderAsync();

            var dict = new Dictionary<int, int>();
            while (await reader.ReadAsync())
            {
                int shift = reader.GetInt32(reader.GetOrdinal("Smena")); // Smena (1=А, 2=Б, 3=В, 4=Г)
                int count = reader.IsDBNull(reader.GetOrdinal("TotalOperationCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalOperationCount"));

                if (dict.ContainsKey(shift))
                    dict[shift] += count;
                else
                    dict[shift] = count;
            }
            return dict;
        }

        // (Если нужно план — добавьте метод с hardcoded значениями на основе изображения)
        public async Task<Dictionary<int, double>> GetMonthlyPlanByShiftAsync(DateTime month)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync();
                DateTime now = DateTime.Now;
                // Начало производственного месяца: 08:00 первого числа
                DateTime monthStart = new DateTime(now.Year, now.Month, 1);
                // Начало производственного месяца: 08:00 первого числа
                DateTime periodStart = monthStart.AddHours(8);
                // Конец периода: текущий момент
                DateTime periodEnd = now;

                string query = @"
WITH ShiftIntervals AS (
    SELECT 
        Id,
        [DateTime],
        Smena,
        [Plan],
        CAST(DATEADD(HOUR, -8, [DateTime]) AS DATE) AS ProductionDay,
        LEAD([DateTime]) OVER (PARTITION BY Smena, CAST(DATEADD(HOUR, -8, [DateTime]) AS DATE) ORDER BY [DateTime]) AS NextDateTime,
        CASE 
            WHEN DATEPART(HOUR, [DateTime]) >= 8 AND DATEPART(HOUR, [DateTime]) < 20 
            THEN CAST(CAST([DateTime] AS DATE) AS DATETIME) + CAST('08:00:00' AS DATETIME)
            WHEN DATEPART(HOUR, [DateTime]) >= 20 
            THEN CAST(CAST([DateTime] AS DATE) AS DATETIME) + CAST('20:00:00' AS DATETIME)
            WHEN DATEPART(HOUR, [DateTime]) < 8 
            THEN DATEADD(DAY, -1, CAST(CAST([DateTime] AS DATE) AS DATETIME)) + CAST('20:00:00' AS DATETIME)
        END AS ShiftStart,
        CASE 
            WHEN DATEPART(HOUR, [DateTime]) >= 8 AND DATEPART(HOUR, [DateTime]) < 20 
            THEN CAST(CAST([DateTime] AS DATE) AS DATETIME) + CAST('20:00:00' AS DATETIME)
            WHEN DATEPART(HOUR, [DateTime]) >= 20 
            THEN DATEADD(DAY, 1, CAST(CAST([DateTime] AS DATE) AS DATETIME)) + CAST('08:00:00' AS DATETIME)
            WHEN DATEPART(HOUR, [DateTime]) < 8 
            THEN CAST(CAST([DateTime] AS DATE) AS DATETIME) + CAST('08:00:00' AS DATETIME)
        END AS ShiftEnd
    FROM Pilot.dbo.MuftN3_REP
    WHERE 
        [DateTime] >= @PeriodStart
        AND [DateTime] <= @PeriodEnd
        AND Smena IN (1, 2, 3, 4)
        AND [DateTime] IS NOT NULL

),
PlanDurations AS (
    SELECT 
        ProductionDay,
        Smena,
        [Plan],
        [DateTime],
        DATEDIFF(SECOND, 
            [DateTime], 
            COALESCE(NextDateTime, ShiftEnd)
        ) / 3600.0 AS DurationHours
    FROM ShiftIntervals
    WHERE 
        [DateTime] >= ShiftStart
        AND [DateTime] < ShiftEnd
),
AdjustedTotals AS (
    SELECT 
        ProductionDay,
        Smena,
        [Plan],
        [DateTime],
        DurationHours,
        SUM(DurationHours) OVER (PARTITION BY ProductionDay, Smena ORDER BY [DateTime] ROWS UNBOUNDED PRECEDING) AS RunningTotal
    FROM PlanDurations
),
ShiftPlans AS (
    SELECT 
        ProductionDay,
        Smena,
        FLOOR(SUM([Plan] * CASE 
            WHEN (RunningTotal - DurationHours) >= 10.5 THEN 0
            WHEN RunningTotal <= 10.5 THEN DurationHours
            ELSE 10.5 - (RunningTotal - DurationHours)
        END)) AS ShiftPlan
    FROM AdjustedTotals
    GROUP BY ProductionDay, Smena
)
SELECT 
    Smena,
    CAST(SUM(ShiftPlan) AS FLOAT) AS CumulativePlan
FROM ShiftPlans
GROUP BY Smena
ORDER BY Smena;";

                using var cmd = new SqlCommand(query, conn);
                cmd.CommandTimeout = 30;
                cmd.Parameters.AddWithValue("@PeriodStart", periodStart);
                cmd.Parameters.AddWithValue("@PeriodEnd", periodEnd);

                var result = new Dictionary<int, double>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int smena = reader.GetInt32(0);
                    double cumulativePlan = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
                    result[smena] = cumulativePlan;
                    //Debug.WriteLine($"GetMonthlyPlanByShiftAsync: Smena={smena}, CumulativePlan={cumulativePlan:F0}");
                }
                await reader.CloseAsync();

                // Убедимся, что все смены присутствуют
                for (int smena = 1; smena <= 4; smena++)
                {
                    if (!result.ContainsKey(smena))
                    {
                        result[smena] = 0.0;
                    }
                }

                return result;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"GetMonthlyPlanByShiftAsync: Задача отменена: {ex.Message}");
                return new Dictionary<int, double> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetMonthlyPlanByShiftAsync: Ошибка: {ex.Message}");
                return new Dictionary<int, double> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } };
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }
        public async Task<int> GetSmenaNumber(DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return 0;

            using var conn = new SqlConnection(_connectionString); // Предполагается, что _connectionString определена
            try
            {
                await conn.OpenAsync();

                // Определяем границы текущей смены (08:00–20:00 или 20:00–08:00)
                var mskTime = dt.Kind == DateTimeKind.Utc ? dt.AddHours(3) : dt;
                int hour = mskTime.Hour;
                DateTime shiftStart, shiftEnd;

                if (hour >= 8 && hour < 20)
                {
                    // Смена 1: 08:00–20:00 текущего дня
                    shiftStart = mskTime.Date.AddHours(8);
                    shiftEnd = mskTime.Date.AddHours(20);
                }
                else
                {
                    // Смена 3: 20:00 (предыдущего дня) – 08:00 (текущего дня) или 20:00 (текущего дня) – 08:00 (следующего дня)
                    if (hour >= 20)
                    {
                        shiftStart = mskTime.Date.AddHours(20);
                        shiftEnd = mskTime.Date.AddDays(1).AddHours(8);
                    }
                    else
                    {
                        shiftStart = mskTime.Date.AddDays(-1).AddHours(20);
                        shiftEnd = mskTime.Date.AddHours(8);
                    }
                }

                string query = @"
            SELECT TOP 1 Smena
            FROM Pilot.dbo.MuftN3_REP
            WHERE 
                [DateTime] >= @ShiftStart
                AND [DateTime] < @ShiftEnd
                AND Smena IS NOT NULL
            ORDER BY [DateTime] DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ShiftStart", shiftStart);
                cmd.Parameters.AddWithValue("@ShiftEnd", shiftEnd);

                var result = await cmd.ExecuteScalarAsync();
                int smena = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;

                Debug.WriteLine($"GetSmenaNumber: DateTime={dt:yyyy-MM-dd HH:mm:ss}, ShiftStart={shiftStart}, ShiftEnd={shiftEnd}, Smena={smena}");
                return smena;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetSmenaNumber: Ошибка: {ex.Message}");
                return 0;
            }
        }

        private (DateTime shiftStart, DateTime shiftEnd) GetShiftBoundaries(DateTime productionDay)
        {
            var currentTime = DateTime.Now;
            int hour = currentTime.Hour;

            // Определяем границы смены
            DateTime dayStart = productionDay.Date.AddHours(8);
            DateTime dayEnd = productionDay.Date.AddHours(20);
            DateTime nightStart = productionDay.Date.AddHours(20);
            DateTime nightEnd = productionDay.Date.AddDays(1).AddHours(8);

            // Проверяем, находится ли текущее время в дневной смене (08:00–20:00)
            if (hour >= 8 && hour < 20)
            {
                // Дневная смена
                return (dayStart, dayEnd);
            }
            else
            {
                // Ночная смена
                // Если время до 08:00, корректируем границы для ночной смены предыдущего дня
                if (hour < 8)
                {
                    nightStart = productionDay.Date.AddDays(-1).AddHours(20);
                    nightEnd = productionDay.Date.AddHours(8);
                }
                return (nightStart, nightEnd);
            }
        }

        private async Task<double> GetShiftPlanAsync(int? operatorId, int smena, DateTime productionDay)
        {
            double shiftPlan;
            using var conn = new SqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync();
                var (shiftStart, shiftEnd) = GetShiftBoundaries(productionDay);
                // Полная смена: 12 часов (до 20:00)
                Debug.WriteLine($"GetShiftPlanAsync: Smena={smena}, ProductionDay={productionDay:yyyy-MM-dd}, ShiftStart={shiftStart:yyyy-MM-dd HH:mm}, ShiftEnd={shiftEnd:yyyy-MM-dd HH:mm}, OperatorId={operatorId}");

                // Проверяем, изменялся ли Plan
                string checkPlanQuery = @"
SELECT 
    MIN([Plan]) AS MinPlan,
    MAX([Plan]) AS MaxPlan
FROM Pilot.dbo.MuftN3_REP
WHERE 
    [DateTime] IS NOT NULL
    AND Smena = @Smena
    AND [DateTime] >= @ShiftStart
    AND [DateTime] < @ShiftEnd
    AND [Plan] IS NOT NULL";
                if (operatorId.HasValue)
                {
                    checkPlanQuery += " AND OperatorId = @OperatorId";
                }

                using var checkCmd = new SqlCommand(checkPlanQuery, conn);
                checkCmd.Parameters.AddWithValue("@Smena", smena);
                checkCmd.Parameters.AddWithValue("@ShiftStart", shiftStart);
                checkCmd.Parameters.AddWithValue("@ShiftEnd", shiftEnd);
                if (operatorId.HasValue)
                {
                    checkCmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);
                }

                double? minPlan = null, maxPlan = null;
                using (var reader = await checkCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync() && !reader.IsDBNull(0) && !reader.IsDBNull(1))
                    {
                        minPlan = Convert.ToDouble(reader.GetInt32(0)); // Приводим INT к Double
                        maxPlan = Convert.ToDouble(reader.GetInt32(1)); // Приводим INT к Double
                    }
                }

                if (!minPlan.HasValue || !maxPlan.HasValue)
                {
                    if (operatorId.HasValue)
                    {
                        string authQuery = @"
                        SELECT COUNT(*)
                        FROM Pilot.dbo.OperatorIdExchange
                        WHERE SectorId = 8
                        AND OperatorId = @OperatorId";

                        using var authCmd = new SqlCommand(authQuery, conn);
                        authCmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);

                        int authCount = (int)await authCmd.ExecuteScalarAsync();
                        if (authCount > 0)
                        {
                            // Получаем последний Plan из MuftN3_REP для SectorId=8
                            string lastPlanQuery = @"
                        SELECT TOP 1 [Plan]
                        FROM Pilot.dbo.MuftN3_REP
                        WHERE 
                            [Plan] IS NOT NULL
                        ORDER BY [DateTime] DESC";

                            using var lastPlanCmd = new SqlCommand(lastPlanQuery, conn);
                            object lastPlanResult = await lastPlanCmd.ExecuteScalarAsync();
                            if (lastPlanResult != null && lastPlanResult != DBNull.Value)
                            {
                                double lastPlan = Convert.ToDouble(lastPlanResult);
                                shiftPlan = Math.Floor(lastPlan * 10.5);
                                Debug.WriteLine($"GetShiftPlanAsync: No records in MuftN3_REP for OperatorId={operatorId}, but operator authorized. Using last Plan={lastPlan}, ShiftPlan={shiftPlan}");
                                return shiftPlan;
                            }
                            Debug.WriteLine($"GetShiftPlanAsync: No records in MuftN3_REP for OperatorId={operatorId}, and no Plan found for SectorId=8, returning 0");
                            return 0.0;
                        }
                    }
                    Debug.WriteLine("GetShiftPlanAsync: No records in MuftN3_REP and no authorization in OperatorIdExchange, returning 0");
                    return 0.0;
                }

                // Если Plan не изменялся, берем последнюю запись и умножаем на 10.5
                if (minPlan == maxPlan)
                {
                    string singlePlanQuery = @"
                    SELECT TOP 1 
                        CAST(FLOOR([Plan] * 10.5) AS FLOAT) AS ShiftPlan
                    FROM Pilot.dbo.MuftN3_REP
                    WHERE 
                        [DateTime] IS NOT NULL
                        AND Smena = @Smena
                        AND [DateTime] >= @ShiftStart
                        AND [DateTime] < @ShiftEnd
                        AND [Plan] IS NOT NULL";
                    if (operatorId.HasValue)
                    {
                        singlePlanQuery += " AND OperatorId = @OperatorId";
                    }
                    singlePlanQuery += @"
                    ORDER BY [DateTime] DESC";

                    using var singleCmd = new SqlCommand(singlePlanQuery, conn);
                    singleCmd.Parameters.AddWithValue("@Smena", smena);
                    singleCmd.Parameters.AddWithValue("@ShiftStart", shiftStart);
                    singleCmd.Parameters.AddWithValue("@ShiftEnd", shiftEnd);
                    if (operatorId.HasValue)
                    {
                        singleCmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);
                    }

                    double ShiftPlan = await singleCmd.ExecuteScalarAsync() as double? ?? 0.0;
                    Debug.WriteLine($"GetShiftPlanAsync: Plan не изменялся, ShiftPlan={ShiftPlan:F0}");
                    return ShiftPlan;
                }

                // Если Plan изменялся, учитываем DurationHours с начала смены
                string multiPlanQuery = @"
                WITH ShiftIntervals AS (
                    SELECT 
                        [Plan],
                        [DateTime],
                        LEAD([DateTime]) OVER (ORDER BY [DateTime]) AS NextDateTime
                    FROM Pilot.dbo.MuftN3_REP
                    WHERE 
                        [DateTime] IS NOT NULL
                        AND Smena = @Smena
                        AND [DateTime] >= @ShiftStart
                        AND [DateTime] < @ShiftEnd
                        AND [Plan] IS NOT NULL";
                if (operatorId.HasValue)
                {
                    multiPlanQuery += " AND OperatorId = @OperatorId";
                }
                multiPlanQuery += @"
                    ),
                    PlanDurations AS (
                        SELECT 
                            [Plan],
                            [DateTime],
                            DATEDIFF(SECOND, 
                                [DateTime], 
                                COALESCE(NextDateTime, @ShiftEnd)
                            ) / 3600.0 AS DurationHours
                        FROM ShiftIntervals
                        WHERE 
                            [DateTime] >= @ShiftStart
                            AND [DateTime] < @ShiftEnd
                        UNION ALL
                        -- Добавляем начальный интервал от shiftStart до первой записи
                        SELECT 
                            @FirstPlan AS [Plan],
                            @ShiftStart AS [DateTime],
                            DATEDIFF(SECOND, @ShiftStart, (SELECT MIN([DateTime]) FROM ShiftIntervals)) / 3600.0 AS DurationHours
                        FROM ShiftIntervals
                        WHERE EXISTS (SELECT 1 FROM ShiftIntervals WHERE [DateTime] > @ShiftStart)
                    )
                    SELECT 
                        CAST(FLOOR(SUM([Plan] * DurationHours) * 10.5 / 12.0) AS FLOAT) AS ShiftPlan
                    FROM PlanDurations;";

                // Получаем первый Plan для начального интервала
                string firstPlanQuery = @"
                SELECT TOP 1 [Plan]
                FROM Pilot.dbo.MuftN3_REP
                WHERE 
                [DateTime] IS NOT NULL
                AND Smena = @Smena
                AND [DateTime] >= @ShiftStart
                AND [DateTime] < @ShiftEnd
                AND [Plan] IS NOT NULL";
                if (operatorId.HasValue)
                {
                    firstPlanQuery += " AND OperatorId = @OperatorId";
                }
                firstPlanQuery += @"
                ORDER BY [DateTime] ASC";

                using var firstPlanCmd = new SqlCommand(firstPlanQuery, conn);
                firstPlanCmd.Parameters.AddWithValue("@Smena", smena);
                firstPlanCmd.Parameters.AddWithValue("@ShiftStart", shiftStart);
                firstPlanCmd.Parameters.AddWithValue("@ShiftEnd", shiftEnd);
                if (operatorId.HasValue)
                {
                    firstPlanCmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);
                }

                double? firstPlan = null;
                object firstPlanResult = await firstPlanCmd.ExecuteScalarAsync();
                if (firstPlanResult != null && firstPlanResult != DBNull.Value)
                {
                    firstPlan = Convert.ToDouble(firstPlanResult); // Приводим INT к Double
                }

                if (!firstPlan.HasValue)
                {
                    Debug.WriteLine("GetShiftPlanAsync: Нет первой записи для начального интервала, возвращаем 0");
                    return 0.0;
                }

                using var multiCmd = new SqlCommand(multiPlanQuery, conn);
                multiCmd.CommandTimeout = 30;
                multiCmd.Parameters.AddWithValue("@Smena", smena);
                multiCmd.Parameters.AddWithValue("@ShiftStart", shiftStart);
                multiCmd.Parameters.AddWithValue("@ShiftEnd", shiftEnd);
                multiCmd.Parameters.AddWithValue("@FirstPlan", firstPlan.Value);
                if (operatorId.HasValue)
                {
                    multiCmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);
                }

                shiftPlan = await multiCmd.ExecuteScalarAsync() as double? ?? 0.0;
                Debug.WriteLine($"GetShiftPlanAsync: Plan изменялся, ShiftPlan={shiftPlan:F0}");

                return shiftPlan;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"GetShiftPlanAsync: Задача отменена: {ex.Message}");
                return 0.0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetShiftPlanAsync: Ошибка: {ex.Message}");
                return 0.0;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }

        public async Task<(int monthPlan, double shiftPlan)> CalculatePlansAsync(int? operatorId = null)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync();
                DateTime now = DateTime.Now;
                DateTime monthStart = new DateTime(now.Year, now.Month, 1);
                DateTime periodStart = monthStart.AddHours(8); // Начало производственного месяца: 08:00 первого числа
                DateTime periodEnd = now; // Конец периода: текущий момент
                DateTime currentProductionDay = now.Hour < 8 ? now.Date.AddDays(-1) : now.Date;

                // Текущая смена
                int smena = await GetSmenaNumber(now);
                var (shiftStart, _) = GetShiftBoundaries(currentProductionDay);
                double shiftPlan = await GetShiftPlanAsync(operatorId, smena, currentProductionDay);

                string query = @"
DECLARE @TempShiftPlans TABLE (
    ProductionDay DATE,
    Smena INT,
    ShiftPlan INT
);

WITH ShiftIntervals AS (
    SELECT 
        Id,
        [DateTime],
        Smena,
        [Plan],
        CAST(DATEADD(HOUR, -8, [DateTime]) AS DATE) AS ProductionDay,
        ROW_NUMBER() OVER (PARTITION BY Smena, CAST(DATEADD(HOUR, -8, [DateTime]) AS DATE) ORDER BY [DateTime] DESC) AS RowNum
    FROM Pilot.dbo.MuftN3_REP
    WHERE 
        [DateTime] >= @PeriodStart
        AND [DateTime] <= @PeriodEnd
        AND Smena IN (1, 2, 3, 4)
        AND [DateTime] IS NOT NULL
        AND [Plan] IS NOT NULL";

                if (operatorId.HasValue)
                {
                    query += " AND OperatorId = @OperatorId";
                }

                query += @"
),
ShiftPlans AS (
    SELECT 
        ProductionDay,
        Smena,
        CAST(FLOOR([Plan] * 10.5) AS INT) AS ShiftPlan
    FROM ShiftIntervals
    WHERE RowNum = 1
)
INSERT INTO @TempShiftPlans (ProductionDay, Smena, ShiftPlan)
SELECT ProductionDay, Smena, ShiftPlan
FROM ShiftPlans;

SELECT ProductionDay, Smena, ShiftPlan
FROM @TempShiftPlans
ORDER BY Smena, ProductionDay;

SELECT 
    SUM(ShiftPlan) AS MonthPlan
FROM @TempShiftPlans;";

                using var cmd = new SqlCommand(query, conn);
                cmd.CommandTimeout = 60;
                cmd.Parameters.AddWithValue("@PeriodStart", periodStart);
                cmd.Parameters.AddWithValue("@PeriodEnd", periodEnd);
                if (operatorId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@OperatorId", operatorId.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                // Чтение промежуточных результатов
                while (await reader.ReadAsync())
                {
                    DateTime productionDay = reader.GetDateTime(0);
                    int Smena = reader.GetInt32(1);
                    int shiftPlanValue = reader.GetInt32(2); // Используем GetInt32 для ShiftPlan
                    Debug.WriteLine($"CalculatePlansAsync: ProductionDay={productionDay:yyyy-MM-dd}, Smena={smena}, ShiftPlan={shiftPlanValue}");
                }

                // Перейти к следующему набору результатов (MonthPlan)
                await reader.NextResultAsync();
                int monthPlan = await reader.ReadAsync() && !reader.IsDBNull(0) ? reader.GetInt32(0) : 0; // Используем GetInt32 для MonthPlan

                Debug.WriteLine($"CalculatePlansAsync: OperatorId={operatorId}, MonthPlan={monthPlan}, ShiftPlan={shiftPlan}");

                return (monthPlan, shiftPlan);
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"CalculatePlansAsync: Задача отменена: {ex.Message}");
                return (0, 0.0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CalculatePlansAsync: Ошибка: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return (0, 0.0);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }
    }
}