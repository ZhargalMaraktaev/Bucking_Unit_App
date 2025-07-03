using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;

namespace Bucking_Unit_App.Services
{
    public class GraphService
    {
        private readonly string _connectionString;
        private readonly int? _pipeCounter; // Для фильтрации по номеру трубы

        public GraphService(string connectionString, int? pipeCounter = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _pipeCounter = pipeCounter; // Присваиваем int? напрямую
        }

        public (ObservableCollection<ObservablePoint> TorquePoints, Axis[] XAxes, Axis[] YAxes) GetGraphData(DateTime startTime, DateTime endTime)
        {
            try
            {
                var synchronizedData = FetchSynchronizedData(startTime, endTime);

                if (!synchronizedData.Any())
                {
                    throw new InvalidOperationException($"Нет данных для построения графика. Период: {startTime} - {endTime}, PipeCounter: {_pipeCounter?.ToString() ?? "All"}");
                }

                var torquePoints = new ObservableCollection<ObservablePoint>(synchronizedData.Select(d => new ObservablePoint(d.DateTime.ToOADate(), d.Torque)));

                var xAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value =>
                        {
                            var dateTime = new DateTime((long)(value * TimeSpan.TicksPerDay));
                            var dataPoint = synchronizedData.FirstOrDefault(d => Math.Abs(d.DateTime.ToOADate() - value) < 0.00001); // Примерное соответствие
                            return dataPoint.Turns.ToString("F2");
                        },
                        Name = "Количество оборотов", // Подпись для оси X
                        LabelsRotation = 45,
                        MinLimit = synchronizedData.Min(d => d.DateTime.ToOADate()),
                        MaxLimit = synchronizedData.Max(d => d.DateTime.ToOADate())
                    }
                };

                var yAxes = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("F2"),
                        Name = "Крутящий момент", // Подпись для оси Y
                        MinLimit = 0,
                        MaxLimit = synchronizedData.Max(d => d.Torque) * 1.1
                    }
                };

                return (torquePoints, xAxes, yAxes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating graph data: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public (DateTime StartTime, DateTime EndTime) GetTimeRange()
        {
            using var conn = new SqlConnection(_connectionString.Replace("Runtime", "Pilot"));
            conn.Open();
            var cmd = new SqlCommand(
                @"SELECT StartDateTime AS StartTime, EndDateTime AS EndTime FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter = @PipeCounter",
                conn);
            cmd.Parameters.AddWithValue("@PipeCounter", _pipeCounter.HasValue ? (object)_pipeCounter.Value : DBNull.Value);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var startTime = reader.IsDBNull(0) ? DateTime.MinValue : reader.GetDateTime(0);
                var endTime = reader.IsDBNull(1) ? DateTime.MaxValue : reader.GetDateTime(1);
                return (startTime, endTime);
            }
            return (DateTime.Today, DateTime.Now); // Значение по умолчанию, если данных нет
        }

        public List<(DateTime DateTime, double Turns, double Torque)> FetchSynchronizedData(DateTime startTime, DateTime endTime)
        {
            var data = new List<(DateTime DateTime, double Turns, double Torque)>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var adjustedStartTime = startTime;
            var adjustedEndTime = endTime;
            // Обрезаем миллисекунды у adjustedStartTime и adjustedEndTime
            adjustedStartTime = new DateTime(adjustedStartTime.Year, adjustedStartTime.Month, adjustedStartTime.Day, adjustedStartTime.Hour, adjustedStartTime.Minute, adjustedStartTime.Second, DateTimeKind.Utc);
            adjustedEndTime = new DateTime(adjustedEndTime.Year, adjustedEndTime.Month, adjustedEndTime.Day, adjustedEndTime.Hour, adjustedEndTime.Minute, adjustedEndTime.Second, DateTimeKind.Utc);
            MessageBox.Show($"Adjusted Time Range - Start: {adjustedStartTime:yyyy-MM-ddTHH:mm:ss}, End: {adjustedEndTime:yyyy-MM-ddTHH:mm:ss}");

            // Получаем все данные за интервал
            var cmd = new SqlCommand(
                @"
        USE Runtime

        DECLARE @StartDate DateTime
        DECLARE @EndDate DateTime
        SET @StartDate = @pStartTime
        SET @EndDate = @pEndTime
        SELECT DateTime, TagName, Value
        FROM History
        WHERE History.TagName IN ('NOT_MN3_ACT_TURNS', 'NOT_MN3_ACT_TORQUE')
        AND wwRetrievalMode = 'Cyclic'
        AND wwCycleCount = 100
        AND wwQualityRule = 'Extended'
        AND wwVersion = 'Latest'
        AND DateTime >= @StartDate
        AND DateTime <= @EndDate
        ORDER BY DateTime",
                conn);
            cmd.Parameters.AddWithValue("@pStartTime", adjustedStartTime.ToString("yyyy-MM-ddTHH:mm:ss")); // Без миллисекунд
            cmd.Parameters.AddWithValue("@pEndTime", adjustedEndTime.ToString("yyyy-MM-ddTHH:mm:ss"));     // Без миллисекунд

            var tagValues = new Dictionary<DateTime, Dictionary<string, double>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dt = reader.GetDateTime(0);
                // Обрезаем миллисекунды, оставляя только целые секунды
                var roundedDt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);
                var tagName = reader.GetString(1);
                var value = Convert.ToDouble(reader["Value"]);
                if (!tagValues.ContainsKey(roundedDt))
                    tagValues[roundedDt] = new Dictionary<string, double>();
                tagValues[roundedDt][tagName] = value;
                Console.WriteLine($"Fetched: {roundedDt:yyyy-MM-ddTHH:mm:ss}, Tag: {tagName}, Value: {value}");
            }

            // Генерируем временную ось на основе startTime и endTime
            var currentTime = adjustedStartTime;
            var timeStep = TimeSpan.FromSeconds(1); // Шаг в 1 секунду
            while (currentTime <= adjustedEndTime)
            {
                Console.WriteLine($"Checking time: {currentTime:yyyy-MM-ddTHH:mm:ss}, HasData: {tagValues.ContainsKey(currentTime)}");
                if (tagValues.ContainsKey(currentTime))
                {
                    var tags = tagValues[currentTime];
                    data.Add((
                        currentTime,
                        tags.ContainsKey("NOT_MN3_ACT_TURNS") ? tags["NOT_MN3_ACT_TURNS"] : 0.0,
                        tags.ContainsKey("NOT_MN3_ACT_TORQUE") ? tags["NOT_MN3_ACT_TORQUE"] : 0.0
                    ));
                }
                else
                {
                    data.Add((currentTime, 0.0, 0.0)); // Заполнение нулями, если данных нет
                }
                // Обрезаем миллисекунды у следующего currentTime
                currentTime = currentTime.AddSeconds(1);
                currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second, DateTimeKind.Utc);
            }

            MessageBox.Show($"Total records generated: {data.Count}");
            if (data.Count == 0)
            {
                Console.WriteLine($"Available Tags: {GetAvailableTags()}");
            }
            return data;
        }

        private string GetAvailableTags()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT DISTINCT TagName FROM [Runtime].[dbo].[History]", conn);
            using var reader = cmd.ExecuteReader();
            var tags = new List<string>();
            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }
            return string.Join(", ", tags);
        }
    }
}