using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.COM_Controller;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.Models;
using Bucking_Unit_App.Interfaces;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;

namespace Bucking_Unit_App.Views
{
    public partial class MainWindow : Window
    {
        private readonly string _connectionString = "Data Source=192.168.11.222,1433;Initial Catalog=Pilot;User ID=UserNotTrend;Password=NotTrend";
        private readonly SqlConnection _conn;
        private readonly COMController _comController;
        private readonly OperatorService _operatorService;
        private readonly StatsService _statsService;
        private CancellationTokenSource _operatorUpdateCts;
        private int? _selectedPipeCounter = null;
        private int _selectedYear = 2025; // Значение по умолчанию

        public MainWindow()
        {
            InitializeComponent();
            _conn = new SqlConnection(_connectionString);
            var dataAccess = new DataAccessLayer(_connectionString);
            _operatorService = new OperatorService(dataAccess, new Controller1C());
            _statsService = new StatsService(dataAccess, dataAccess);
            _comController = new COMController(new COMControllerParamsModel("COM3", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One));
            _comController.StateChanged += ComController_StateChanged;
            _comController.IsReading = true;
            _operatorService.OnOperatorChanged += OperatorService_OnOperatorChanged;
            cbYear.SelectedIndex = 2; // Устанавливаем 2025 по умолчанию
        }

        private async void txtPipeCounter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            string filter = textBox.Text.Trim();
            if (string.IsNullOrEmpty(filter) || !int.TryParse(new string(filter.Where(char.IsDigit).ToArray()), out _))
            {
                Dispatcher.Invoke(() => lbPipeCounterSuggestions.ItemsSource = null); // Сбрасываем ItemsSource
                Dispatcher.Invoke(() => lbPipeCounterSuggestions.Visibility = Visibility.Collapsed);
                return;
            }

            Dispatcher.Invoke(() => progressBar.Visibility = Visibility.Visible);
            Dispatcher.Invoke(() => lbPipeCounterSuggestions.ItemsSource = null); // Сбрасываем перед новой загрузкой

            await Task.Run(async () =>
            {
                var filtered = new Dictionary<int, string>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand(
                        $"SELECT TOP 1000 PipeCounter FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter LIKE '%{filter}%' AND PipeCounter IS NOT NULL ORDER BY PipeCounter", conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int pipeCounter = reader.GetInt32(0);
                        filtered[pipeCounter] = $"Pipe {pipeCounter}";
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    lbPipeCounterSuggestions.ItemsSource = filtered.Select(kvp => new KeyValuePair<int, string>(kvp.Key, kvp.Value));
                    lbPipeCounterSuggestions.Visibility = filtered.Any() ? Visibility.Visible : Visibility.Collapsed;
                    progressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void lbPipeCounterSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbPipeCounterSuggestions.SelectedItem is KeyValuePair<int, string> selected)
            {
                _selectedPipeCounter = selected.Key;
                txtPipeCounter.Text = selected.Value;
                lbPipeCounterSuggestions.ItemsSource = null; // Сбрасываем ItemsSource после выбора
                lbPipeCounterSuggestions.Visibility = Visibility.Collapsed;
            }
        }

        private void cbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbYear.SelectedItem is ComboBoxItem item)
            {
                _selectedYear = int.Parse(item.Content.ToString());
                Console.WriteLine($"Selected year: {_selectedYear}");
            }
        }

        private async void ComController_StateChanged(object sender, COMEventArgs.ReadingDataEventArgs e)
        {
            if (e.State == COMControllerParamsModel.COMStates.Detected && !string.IsNullOrEmpty(e.CardId))
            {
                await _operatorService.InitializeOperatorAsync(e.CardId);
                if (_operatorService.CurrentOperator != null)
                {
                    await _statsService.UpdateIdsAsync(_operatorService.CurrentOperator.PersonnelNumber);
                    StartOperatorUpdateLoop();
                }
            }
            else if (e.State == COMControllerParamsModel.COMStates.Removed)
            {
                _operatorService.CurrentOperator = null;
                StopOperatorUpdateLoop();
                Dispatcher.Invoke(() =>
                {
                    txtShiftItems.Text = "Не указано";
                    txtShiftDowntime.Text = "Не указано";
                    txtMonthItems.Text = "Не указано";
                    txtMonthDowntime.Text = "Не указано";
                });
            }
        }

        private void OperatorService_OnOperatorChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var operatorInfo = _operatorService.CurrentOperator;
                txtIdCard.Text = operatorInfo?.CardNumber ?? "Не указано";
                txtTabNumber.Text = operatorInfo?.PersonnelNumber ?? "Не указано";
                txtFIO.Text = operatorInfo?.FullName ?? "Не указано";
                txtDepartment.Text = operatorInfo?.Department ?? "Не указано";
                txtEmployName.Text = operatorInfo?.Position ?? "Не указано";
            });
        }

        private void StartOperatorUpdateLoop()
        {
            _operatorUpdateCts?.Dispose();
            _operatorUpdateCts = new CancellationTokenSource();
            var token = _operatorUpdateCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"Обновление в {DateTime.Now}");
                    await _statsService.UpdateIdsAsync(_operatorService.CurrentOperator.PersonnelNumber);
                    await _statsService.UpdateStatsAsync(_operatorService.CurrentOperator.PersonnelNumber, (shiftItems, shiftDowntime, monthItems, monthDowntime) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtShiftItems.Text = shiftItems;
                            txtShiftDowntime.Text = shiftDowntime;
                            txtMonthItems.Text = monthItems;
                            txtMonthDowntime.Text = monthDowntime;
                        });
                    });
                    await Task.Delay(5000, token);
                }
            }, token);
        }

        private void StopOperatorUpdateLoop()
        {
            _operatorUpdateCts?.Cancel();
            _operatorUpdateCts?.Dispose();
            _operatorUpdateCts = null;
        }

        private async void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedPipeCounter == null)
                {
                    MessageBox.Show("Пожалуйста, выберите PipeCounter.");
                    return;
                }

                string runtimeConnectionString = "Data Source=192.168.11.222,1433;Initial Catalog=Runtime;User ID=UserNotTrend;Password=NotTrend";
                MessageBox.Show($"Attempting to create GraphService with PipeCounter: {_selectedPipeCounter}, Year: {_selectedYear}");

                var graphService = new GraphService(runtimeConnectionString, _selectedPipeCounter);
                MessageBox.Show("GraphService created successfully.");

                var (startTime, endTime) = graphService.GetTimeRange();
                if (startTime.Year > _selectedYear || endTime.Year < _selectedYear)
                {
                    startTime = new DateTime(_selectedYear, 1, 1);
                    endTime = new DateTime(_selectedYear, 12, 31, 23, 59, 59);
                }

                var (torquePoints, xAxes, yAxes) = graphService.GetGraphData(startTime, endTime);

                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var chart = new CartesianChart
                        {
                            Series = new ISeries[]
                            {
                        new LineSeries<ObservablePoint>
                        {
                            Values = torquePoints,
                            Name = "Torque",
                            XToolTipLabelFormatter = (chartPoint) =>
                            {
                                double? torqueValueNullable = chartPoint.Model.Y;
                                double torqueValue = torqueValueNullable ?? 0.0;
                                return torqueValue.ToString("F2");
                            },
                            Stroke = new SolidColorPaint(SKColors.Red),
                            Fill = null
                        }
                            },
                            XAxes = xAxes,
                            YAxes = yAxes,
                            LegendPosition = LegendPosition.Top,
                            Width = 450,
                            Height = 400
                        };
                        canvasGraph.Children.Clear();
                        canvasGraph.Children.Add(chart);
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении графика: {ex.Message}");
            }
        }

        private void btnSaveGraph_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("График сохранен (функция не реализована).");
        }

        private void btnResetGraph_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                canvasGraph.Children.Clear();
                _selectedPipeCounter = null;
                txtPipeCounter.Text = string.Empty;
            });
        }
    }
}