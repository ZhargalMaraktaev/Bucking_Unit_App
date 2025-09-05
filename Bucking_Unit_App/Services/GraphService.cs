using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SkiaSharp;

namespace Bucking_Unit_App.Services
{
    public class GraphService
    {
        private readonly string _connectionString;
        private readonly int? _pipeCounter;

        public GraphService(string connectionString, int? pipeCounter = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _pipeCounter = pipeCounter;
        }

        public (ObservableCollection<ObservablePoint> TorquePoints, Axis[] XAxes, Axis[] YAxes, string ErrorMessage) GetGraphData(DateTime startTime, DateTime endTime, bool isActiveProcess)
        {
            try
            {
                if (!isActiveProcess && endTime < startTime)
                {
                    string errormessage = $"Ошибка: EndDateTime ({endTime:yyyy-MM-ddTHH:mm:ss}) раньше StartDateTime ({startTime:yyyy-MM-ddTHH:mm:ss}) для PipeCounter: {_pipeCounter?.ToString() ?? "All"}";
                    Debug.WriteLine($"GraphService.GetGraphData: {errormessage}");
                    return (new ObservableCollection<ObservablePoint>(), new Axis[0], new Axis[0], errormessage);
                }

                var synchronizedData = FetchSynchronizedData(startTime, endTime, isActiveProcess);
                string errorMessage = null;

                if (!synchronizedData.Any())
                {
                    errorMessage = $"Нет данных для построения графика. Период: {startTime:yyyy-MM-ddTHH:mm:ss} - {(isActiveProcess ? DateTime.Now : endTime):yyyy-MM-ddTHH:mm:ss}, PipeCounter: {_pipeCounter?.ToString() ?? "All"}";
                    Debug.WriteLine($"GraphService.GetGraphData: {errorMessage}");
                    return (new ObservableCollection<ObservablePoint>(), new Axis[] { new Axis { Name = "Количество оборотов", LabelsRotation = 45, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } }, new Axis[] { new Axis { Name = "Крутящий момент", Labeler = value => value.ToString("F2"), MinLimit = 0, MaxLimit = 25000, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 } } }, errorMessage);
                }

                // Фильтрация по NOT_MN3_SCREW_ON = true только для активного процесса
                // Фильтрация по NOT_MN3_SCREW_ON = true только для активного процесса
                var filteredData = isActiveProcess
                ? synchronizedData.Where(d => d.ScrewOn).ToList()
                : synchronizedData.ToList(); // Включаем все данные для неактивного процесса в 4-секундном окне

                if (!filteredData.Any())
                {
                    errorMessage = $"Нет данных для отображения. Период: {startTime:yyyy-MM-ddTHH:mm:ss} - {(isActiveProcess ? DateTime.Now : endTime):yyyy-MM-ddTHH:mm:ss}, PipeCounter: {_pipeCounter?.ToString() ?? "All"}";
                    Debug.WriteLine($"GraphService.GetGraphData: {errorMessage}");
                    return (new ObservableCollection<ObservablePoint>(),
                            new Axis[] { new Axis { Name = "Количество оборотов", LabelsRotation = 45, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } },
                            new Axis[] { new Axis { Name = "Крутящий момент", Labeler = value => value.ToString("F2"), MinLimit = 0, MaxLimit = 25000, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 } } },
                            errorMessage);
                }

                var torquePoints = new ObservableCollection<ObservablePoint>(
                    filteredData.Select((d, index) => new ObservablePoint(index, d.Torque)));
                var xAxisLabels = filteredData.Select(d => d.Turns.ToString("F2")).ToArray();

                Debug.WriteLine($"GraphService.GetGraphData: torquePoints.Count={torquePoints.Count}, xAxisLabels.Count={xAxisLabels.Length}");

                var xAxes = new Axis[]
                {
            new Axis
            {
                Name = "Количество оборотов",
                Labels = xAxisLabels,
                LabelsRotation = 45,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12
            }
                };

                var yAxes = new Axis[]
                {
            new Axis
            {
                Labeler = value => value.ToString("F2"),
                Name = "Крутящий момент",
                MinLimit = 0,
                MaxLimit = filteredData.Any(d => d.Torque > 0) ? filteredData.Max(d => d.Torque) * 1.1 : 25000,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
                };

                Debug.WriteLine($"GraphService.GetGraphData: Найдено {torquePoints.Count} точек для графика: PipeCounter={_pipeCounter}, StartTime={startTime:yyyy-MM-ddTHH:mm:ss}, EndTime={(isActiveProcess ? DateTime.Now : endTime):yyyy-MM-ddTHH:mm:ss}, IsActive={isActiveProcess}");
                return (torquePoints, xAxes, yAxes, null);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Ошибка генерации данных графика: {ex.Message}";
                Debug.WriteLine($"GraphService.GetGraphData: {errorMessage}\nStackTrace: {ex.StackTrace}");
                return (new ObservableCollection<ObservablePoint>(), new Axis[] { new Axis { Name = "Количество оборотов", LabelsRotation = 45, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } }, new Axis[] { new Axis { Name = "Крутящий момент", Labeler = value => value.ToString("F2"), MinLimit = 0, MaxLimit = 25000, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 } } }, errorMessage);
            }
        }

        public (DateTime? StartTime, DateTime? EndTime) GetTimeRange()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString.Replace("Runtime", "Pilot"));
                conn.Open();
                var cmd = new SqlCommand(
                    @"SELECT StartDateTime AS StartTime, EndDateTime AS EndTime 
                      FROM Pilot.dbo.MuftN3_REP 
                      WHERE PipeCounter = @PipeCounter",
                    conn);
                cmd.Parameters.AddWithValue("@PipeCounter", _pipeCounter.HasValue ? (object)_pipeCounter.Value : DBNull.Value);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    DateTime? startTime = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                    DateTime? endTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                    Debug.WriteLine($"GraphService.GetTimeRange: PipeCounter={_pipeCounter}, StartTime={startTime?.ToString("yyyy-MM-ddTHH:mm:ss.fff") ?? "null"}, EndTime={endTime?.ToString("yyyy-MM-ddTHH:mm:ss.fff") ?? "null"}");
                    return (startTime, endTime);
                }
                Debug.WriteLine($"GraphService.GetTimeRange: Данные для PipeCounter={_pipeCounter} не найдены.");
                return (null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GraphService.GetTimeRange: Ошибка получения временного диапазона: {ex.Message}");
                return (null, null);
            }
        }

        public List<(DateTime DateTime, double Turns, double Torque, bool ScrewOn)> FetchSynchronizedData(DateTime startTime, DateTime endTime, bool isActiveProcess)
        {
            var data = new List<(DateTime DateTime, double Turns, double Torque, bool ScrewOn)>();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var adjustedEndTime = isActiveProcess ? DateTime.Now.AddSeconds(5) : endTime; // Буфер для активного процесса
                var adjustedStartTime = startTime.AddSeconds(-5); // Буфер для учета задержек

                if (adjustedEndTime < adjustedStartTime)
                {
                    adjustedEndTime = adjustedStartTime.AddSeconds(2);
                    Debug.WriteLine($"GraphService.FetchSynchronizedData: adjustedEndTime ({adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}) меньше startTime ({adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}), скорректировано на {adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}");
                }

                Debug.WriteLine($"GraphService.FetchSynchronizedData: Запрос данных для PipeCounter={_pipeCounter}, StartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, EndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}, IsActive={isActiveProcess}");

                var cmd = new SqlCommand(
                    @"
                SELECT DateTime, TagName, Value
                FROM Runtime.dbo.History
                WHERE TagName IN ('NOT_MN3_ACT_TURNS', 'NOT_MN3_ACT_TORQUE', 'NOT_MN3_SCREW_ON')
                AND wwRetrievalMode = 'Cyclic'
                AND wwCycleCount = 100
                AND wwQualityRule = 'Extended'
                AND wwVersion = 'Latest'
                AND DateTime >= @pStartTime
                AND DateTime <= @pEndTime
                AND Value is not NULL
                ORDER BY DateTime ASC",
                    conn);

                cmd.Parameters.AddWithValue("@pStartTime", adjustedStartTime);
                cmd.Parameters.AddWithValue("@pEndTime", adjustedEndTime);

                var tagValues = new Dictionary<DateTime, Dictionary<string, double?>>();
                var screwOnValues = new Dictionary<DateTime, bool>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dt = reader.GetDateTime(0);
                    var tagName = reader.GetString(1);
                    double? value = reader.IsDBNull(2) ? null : reader.GetDouble(2);

                    if (!tagValues.ContainsKey(dt))
                        tagValues[dt] = new Dictionary<string, double?>();

                    if (tagName == "NOT_MN3_SCREW_ON")
                    {
                        screwOnValues[dt] = value.HasValue && value.Value == 1;
                    }
                    else
                    {
                        tagValues[dt][tagName] = value;
                    }

                  
                }

                foreach (var dt in tagValues.Keys.OrderBy(dt => dt))
                {
                    var tags = tagValues[dt];
                    double turns = tags.ContainsKey("NOT_MN3_ACT_TURNS") && tags["NOT_MN3_ACT_TURNS"].HasValue ? tags["NOT_MN3_ACT_TURNS"].Value : 0.0;
                    double torque = tags.ContainsKey("NOT_MN3_ACT_TORQUE") && tags["NOT_MN3_ACT_TORQUE"].HasValue ? tags["NOT_MN3_ACT_TORQUE"].Value : 0.0;
                    bool screwOn = screwOnValues.ContainsKey(dt) ? screwOnValues[dt] : false;
                    data.Add((dt, turns, torque, screwOn));
                    //Debug.WriteLine($"GraphService.Adding point: DateTime={dt:yyyy-MM-ddTHH:mm:ss.fff}, Turns={turns:F2}, Torque={torque:F2}, ScrewOn={screwOn}");
                }

                Debug.WriteLine($"GraphService.FetchSynchronizedData: Сформировано {data.Count} синхронизированных записей для PipeCounter={_pipeCounter}, StartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, EndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}");

                if (data.Count == 0)
                {
                    Debug.WriteLine($"GraphService.FetchSynchronizedData: Доступные теги: {GetAvailableTags()}");
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GraphService.FetchSynchronizedData: Ошибка получения данных: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return data;
            }
        }

        private string GetAvailableTags()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var cmd = new SqlCommand(
                    @"
            SELECT DISTINCT TagName 
            FROM Runtime.dbo.History 
            WHERE TagName IN ('NOT_MN3_ACT_TURNS', 'NOT_MN3_ACT_TORQUE')
            AND DateTime >= @StartTime 
            AND DateTime <= @EndTime",
                    conn);
                cmd.Parameters.AddWithValue("@StartTime", DateTime.Now.AddHours(-24));
                cmd.Parameters.AddWithValue("@EndTime", DateTime.Now);

                var tags = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tags.Add(reader.GetString(0));
                }
                Debug.WriteLine($"GraphService.GetAvailableTags: Найдено тегов: {tags.Count}");
                return string.Join(", ", tags);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GraphService.GetAvailableTags: Ошибка получения тегов: {ex.Message}");
                return $"Ошибка получения тегов: {ex.Message}";
            }
        }
    }
}