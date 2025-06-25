using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.COM_Controller;
using System.Windows.Controls;

namespace Bucking_Unit_App
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Data Source=192.168.11.222,1433;Initial Catalog=Pilot;User ID=UserNotTrend;Password=NotTrend";
        private SqlConnection conn;
        private COMController comController;
        private Controller1C controller1C = new Controller1C();
        private Employee1CModel currentOperator;
        private CancellationTokenSource operatorUpdateCts;
        private int lastKnownRepId = 0;
        private int lastKnownDowntimeId = 0;

        public MainWindow()
        {
            InitializeComponent();
            conn = new SqlConnection(connectionString);
            InitializeCOMController();
            LoadInitialData();
        }

        private void InitializeCOMController()
        {
            var comParams = new COMControllerParamsModel("COM3", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            comController = new COMController(comParams);
            comController.StateChanged += ComController_StateChanged;
            comController.IsReading = true; // Запуск чтения
        }

        private async void ComController_StateChanged(object sender, COMEventArgs.ReadingDataEventArgs e)
        {
            Dispatcher.Invoke(() => MessageBox.Show($"State: {e.State}, CardId: {e.CardId ?? "null"}"));
            if (e.State == COMControllerParamsModel.COMStates.Detected && !string.IsNullOrEmpty(e.CardId))
            {
                string cardNumber = e.CardId;
                Dispatcher.Invoke(() => MessageBox.Show($"Processing CardId: {cardNumber}"));

                Employee1CModel employee = await GetEmployeeFromDatabase(cardNumber);
                if (employee == null)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Not found in DB, querying 1C for: {cardNumber}"));
                    employee = await controller1C.GetResp1CSKUD(cardNumber);
                    if (employee.ErrorCode == 0 && !string.IsNullOrEmpty(employee.PersonnelNumber))
                    {
                        await SaveEmployeeToDatabase(employee);
                    }
                    else
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Ошибка 1C: ErrorCode={employee.ErrorCode}, ErrorText={employee.ErrorText}"));
                        return;
                    }
                }

                if (employee != null && !string.IsNullOrEmpty(employee.PersonnelNumber))
                {
                    await UpdateLastKnownRepId();
                    await UpdateLastKnownDowntimeId();
                    StartOperatorUpdateLoop();

                    Dispatcher.Invoke(() =>
                    {
                        currentOperator = employee;
                        UpdateOperatorInfo();
                    });
                }
            }
            else if (e.State == COMControllerParamsModel.COMStates.Removed)
            {
                Dispatcher.Invoke(() =>
                {
                    currentOperator = null;
                    UpdateOperatorInfo();
                    StopOperatorUpdateLoop();
                });
            }
        }

        private void StartOperatorUpdateLoop()
        {
            if (operatorUpdateCts != null)
                operatorUpdateCts.Dispose();

            operatorUpdateCts = new CancellationTokenSource();
            var token = operatorUpdateCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await UpdateUnassignedOperatorIds();
                    await UpdateDowntime_OperatorId();
                    await UpdateStats();
                    await Task.Delay(5000, token); // Задержка 5 секунд
                }
            }, token);
        }

        private void StopOperatorUpdateLoop()
        {
            operatorUpdateCts?.Cancel();
            operatorUpdateCts?.Dispose();
            operatorUpdateCts = null;
        }

        private void LoadInitialData()
        {
            try
            {
                conn.Open();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void UpdateOperatorInfo()
        {
            if (currentOperator != null)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Updating: FIO={currentOperator.FullName}, TabNumber={currentOperator.PersonnelNumber}"));
                txtFIO.Text = currentOperator.FullName ?? "Не указано";
                txtIdCard.Text = currentOperator.CardNumber ?? "Не указано";
                txtTabNumber.Text = currentOperator.PersonnelNumber ?? "Не указано";
                txtDepartment.Text = currentOperator.Department ?? "Не указано";
                txtEmployName.Text = currentOperator.Position ?? "Не указано";
            }
            else
            {
                txtFIO.Text = "Не указано";
                txtIdCard.Text = "Не указано";
                txtTabNumber.Text = "Не указано";
                txtDepartment.Text = "Не указано";
                txtEmployName.Text = "Не указано";
            }
        }

        private async Task UpdateLastKnownRepId()
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT ISNULL(MAX(Id), 0) FROM Pilot.dbo.MuftN3_REP";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        lastKnownRepId = Convert.ToInt32(result);
                        Dispatcher.Invoke(() => MessageBox.Show($"📌 Сохранили lastKnownRepId = {lastKnownRepId}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка при получении последнего Id: " + ex.Message));
            }
        }

        private async Task UpdateLastKnownDowntimeId()
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT ISNULL(MAX(Id), 0) FROM Pilot.dbo.Downtime WHERE SectorId = 8;";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        lastKnownDowntimeId = Convert.ToInt32(result);
                        Dispatcher.Invoke(() => MessageBox.Show($"📌 Сохранили lastKnownDowntimeId = {lastKnownDowntimeId}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка при получении последнего Id: " + ex.Message));
            }
        }
        private async Task<Employee1CModel> GetEmployeeFromDatabase(string cardNumber)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT idCard, TabNumber, FIO, Department, EmployName FROM Pilot.dbo.dic_SKUD WHERE idCard = @idCard";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idCard", cardNumber);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка поиска в базе: " + ex.Message));
            }
            return null;
        }

        private async Task SaveEmployeeToDatabase(Employee1CModel employee)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string tabNumber = employee.PersonnelNumber;
                    if (!await CheckCardIdAsync(conn, tabNumber))
                    {
                        string insertQuery = @"
                            INSERT INTO Pilot.dbo.dic_SKUD (idCard, TabNumber, FIO, Department, EmployName)
                            VALUES (@idCard, @TabNumber, @FIO, @Department, @Position)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@idCard", employee.CardNumber);
                            cmd.Parameters.AddWithValue("@TabNumber", tabNumber);
                            cmd.Parameters.AddWithValue("@FIO", employee.FullName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Department", employee.Department ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Position", employee.Position ?? (object)DBNull.Value);
                            await cmd.ExecuteNonQueryAsync();
                            Dispatcher.Invoke(() => MessageBox.Show("Сохранен новый оператор: " + tabNumber));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка сохранения в базу: " + ex.Message));
            }
        }

        private async Task UpdateUnassignedOperatorIds()
        {
            if (currentOperator == null || string.IsNullOrEmpty(currentOperator.PersonnelNumber))
                return;

            if (lastKnownRepId <= 0)
            {
                Dispatcher.Invoke(() => MessageBox.Show("⚠️ lastKnownRepId не установлен, обновление отменено."));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string selectOperatorIdQuery = "SELECT id FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
                    int? operatorId = null;

                    using (var cmd = new SqlCommand(selectOperatorIdQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TabNumber", currentOperator.PersonnelNumber);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                            operatorId = Convert.ToInt32(result);
                        else
                            Dispatcher.Invoke(() => MessageBox.Show("⚠️ OperatorId не найден для TabNumber: " + currentOperator.PersonnelNumber));
                    }

                    string updateQuery = @"
                        UPDATE Pilot.dbo.MuftN3_REP
                        SET OperatorId = @OperatorId
                        WHERE OperatorId IS NULL AND Id >= @LastKnownId";

                    using (var updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@OperatorId", operatorId);
                        updateCmd.Parameters.AddWithValue("@LastKnownId", lastKnownRepId);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected > 0)
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show($"✅ Обновлено {affected} новых строк в MuftN3_REP"));
                            // Обновляем lastKnownRepId после успешного обновления
                            await UpdateLastKnownRepId();
                        }
                        else
                        {
                            Dispatcher.Invoke(() => MessageBox.Show("⚠️ Нет строк для обновления (affected = 0)"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка обновления новых строк: " + ex.Message));
            }
        }

        private async Task UpdateDowntime_OperatorId()
        {
            if (currentOperator == null || string.IsNullOrEmpty(currentOperator.PersonnelNumber))
                return;

            if (lastKnownDowntimeId <= 0)
            {
                Dispatcher.Invoke(() => MessageBox.Show("⚠️ lastKnownDowntimeId не установлен, обновление отменено."));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string selectOperatorIdQuery = "SELECT id FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
                    int? operatorId = null;

                    using (var cmd = new SqlCommand(selectOperatorIdQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TabNumber", currentOperator.PersonnelNumber);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                            operatorId = Convert.ToInt32(result);
                        else
                            Dispatcher.Invoke(() => MessageBox.Show("⚠️ OperatorId не найден для TabNumber: " + currentOperator.PersonnelNumber));
                    }

                    string updateQuery = @"
                        UPDATE Pilot.dbo.Downtime
                        SET OperatorId = @OperatorId
                        WHERE OperatorId IS NULL AND Id >= @LastKnownId";

                    using (var updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@OperatorId", operatorId);
                        updateCmd.Parameters.AddWithValue("@LastKnownId", lastKnownDowntimeId);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected > 0)
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show($"✅ Обновлено {affected} новых строк в Downtime"));
                            // Обновляем lastKnownRepId после успешного обновления
                            await UpdateLastKnownDowntimeId();
                        }
                        else
                        {
                            Dispatcher.Invoke(() => MessageBox.Show("⚠️ Нет строк для обновления (affected = 0)"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка обновления новых строк: " + ex.Message));
            }
        }

        private async Task<bool> CheckCardIdAsync(SqlConnection conn, string tabNumber)
        {
            string query = "SELECT COUNT(*) FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@TabNumber", tabNumber);
                return (int)await cmd.ExecuteScalarAsync() > 0;
            }
        }
        private async Task UpdateStats()
        {
            if (currentOperator == null || string.IsNullOrEmpty(currentOperator.PersonnelNumber))
            {
                Dispatcher.Invoke(() =>
                {
                    txtShiftItems.Text = "Не указано";
                    txtShiftDowntime.Text = "Не указано";
                    txtMonthItems.Text = "Не указано";
                    txtMonthDowntime.Text = "Не указано";
                });
                return;
            }

            try
            {
                conn.Open();

                string selectOperatorIdQuery = "SELECT id FROM Pilot.dbo.dic_SKUD WHERE TabNumber = @TabNumber";
                int? operatorId = null;

                using (var cmd = new SqlCommand(selectOperatorIdQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TabNumber", currentOperator.PersonnelNumber);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                        operatorId = Convert.ToInt32(result);
                    else
                        Dispatcher.Invoke(() => MessageBox.Show("⚠️ OperatorId не найден для TabNumber: " + currentOperator.PersonnelNumber));
                }
                // Вызываем процедуры
                using (SqlCommand cmdDailyDowntime = new SqlCommand("GetDailyDowntimeByOperator", conn))
                {
                    cmdDailyDowntime.CommandType = CommandType.StoredProcedure;
                    cmdDailyDowntime.Parameters.AddWithValue("@OperatorId", operatorId);
                    using (SqlDataReader reader = await cmdDailyDowntime.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool isDayShift = false;
                            decimal dayShiftDowntime = 0;
                            decimal nightShiftDowntime = 0;

                            int isDayShiftOrdinal = reader.GetOrdinal("IsDayShift");
                            int dayShiftDowntimeOrdinal = reader.GetOrdinal("DayShiftDowntimeMinutes");
                            int nightShiftDowntimeOrdinal = reader.GetOrdinal("NightShiftDowntimeMinutes");

                            if (!reader.IsDBNull(isDayShiftOrdinal))
                            {
                                isDayShift = reader.GetBoolean(isDayShiftOrdinal);
                            }
                            if (!reader.IsDBNull(dayShiftDowntimeOrdinal))
                            {
                                dayShiftDowntime = reader.GetDecimal(dayShiftDowntimeOrdinal);
                            }
                            if (!reader.IsDBNull(nightShiftDowntimeOrdinal))
                            {
                                nightShiftDowntime = reader.GetDecimal(nightShiftDowntimeOrdinal);
                            }

                            Dispatcher.Invoke(() =>
                            {
                                txtShiftDowntime.Text = (isDayShift ? (double)dayShiftDowntime : (double)nightShiftDowntime).ToString("F2");
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() => txtShiftDowntime.Text = "0.00");
                        }
                    }
                }

                using (SqlCommand cmdMonthlyDowntime = new SqlCommand("GetMonthlyDowntimeByOperator", conn))
                {
                    cmdMonthlyDowntime.CommandType = CommandType.StoredProcedure;
                    cmdMonthlyDowntime.Parameters.AddWithValue("@OperatorId", operatorId);
                    using (SqlDataReader reader = await cmdMonthlyDowntime.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool isDayShift = false;
                            decimal dayShiftDowntime = 0;
                            decimal nightShiftDowntime = 0;

                            int isDayShiftOrdinal = reader.GetOrdinal("IsDayShift");
                            int dayShiftDowntimeOrdinal = reader.GetOrdinal("DayShiftDowntimeMinutes");
                            int nightShiftDowntimeOrdinal = reader.GetOrdinal("NightShiftDowntimeMinutes");

                            if (!reader.IsDBNull(isDayShiftOrdinal))
                            {
                                isDayShift = reader.GetBoolean(isDayShiftOrdinal);
                            }
                            if (!reader.IsDBNull(dayShiftDowntimeOrdinal))
                            {
                                dayShiftDowntime = reader.GetDecimal(dayShiftDowntimeOrdinal);
                            }
                            if (!reader.IsDBNull(nightShiftDowntimeOrdinal))
                            {
                                nightShiftDowntime = reader.GetDecimal(nightShiftDowntimeOrdinal);
                            }

                            Dispatcher.Invoke(() =>
                            {
                                txtMonthDowntime.Text = (isDayShift ? (double)dayShiftDowntime : (double)nightShiftDowntime).ToString("F2");
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() => txtMonthDowntime.Text = "0.00");
                        }
                    }
                }

                using (SqlCommand cmdDailyOperation = new SqlCommand("GetDailyShiftOperationCount", conn))
                {
                    cmdDailyOperation.CommandType = CommandType.StoredProcedure;
                    cmdDailyOperation.Parameters.AddWithValue("@OperatorId", operatorId);
                    using (SqlDataReader reader = await cmdDailyOperation.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int shiftOperationCount = 0;
                            int shiftOperationCountOrdinal = reader.GetOrdinal("ShiftOperationCount");
                            if (!reader.IsDBNull(shiftOperationCountOrdinal))
                            {
                                shiftOperationCount = reader.GetInt32(shiftOperationCountOrdinal);
                            }
                            Dispatcher.Invoke(() =>
                            {
                                txtShiftItems.Text = shiftOperationCount.ToString();
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() => txtShiftItems.Text = "0");
                        }
                    }
                }

                using (SqlCommand cmdMonthlyOperation = new SqlCommand("GetMonthlyShiftOperationCount", conn))
                {
                    cmdMonthlyOperation.CommandType = CommandType.StoredProcedure;
                    cmdMonthlyOperation.Parameters.AddWithValue("@OperatorId", operatorId);
                    using (SqlDataReader reader = await cmdMonthlyOperation.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int shiftOperationCount = 0;
                            int shiftOperationCountOrdinal = reader.GetOrdinal("ShiftOperationCount");
                            if (!reader.IsDBNull(shiftOperationCountOrdinal))
                            {
                                shiftOperationCount = reader.GetInt32(shiftOperationCountOrdinal);
                            }
                            Dispatcher.Invoke(() =>
                            {
                                txtMonthItems.Text = shiftOperationCount.ToString();
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() => txtMonthItems.Text = "0");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка обновления статистики: " + ex.Message));
            }
            finally
            {
                conn.Close();
            }
        }
        private void cbOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция пока не реализована. Требуется интеграция графика.");
        }
    }
}