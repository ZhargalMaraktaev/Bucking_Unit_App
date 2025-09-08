using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.COM_Controller;
using Bucking_Unit_App.Interfaces;
using Bucking_Unit_App.Models;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.SiemensPLC.Models;
using Bucking_Unit_App.Utilities;
using FontAwesome.WPF;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Sharp7;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Bucking_Unit_App.SiemensPLC.Models.SiemensPLCModels.PLCReadWriteModel;

namespace Bucking_Unit_App.Views
{
    public partial class MainWindow : Window
    {
        private readonly string _connectionString = "Data Source=192.168.0.230,1433;Initial Catalog=Pilot;User ID=UserNotTrend;Password=NotTrend";
        private readonly SqlConnection _conn;
        private readonly COMController _comController;
        private readonly OperatorService _operatorService;
        private readonly StatsService _statsService;
        private CancellationTokenSource _operatorUpdateCts;
        private CancellationTokenSource _allStatsUpdateCts;
        private int? _selectedPipeCounter = null;
        private int _selectedYear = DateTime.Now.Year;
        private int? _currentPipeCounter = null;
        private readonly string _runtimeConnectionString = "Data Source=192.168.0.230,1433;Initial Catalog=Runtime;User ID=UserNotTrend;Password=NotTrend";
        private CancellationTokenSource _currentPipeUpdateCts;
        private S7Client s7Client;
        private CancellationTokenSource _cts;
        private PLCDataWindow _plcDataWindow;
        private ReadFromPLC _plcReader;
        private Task _plcUpdateTask;
        private Task _operatorUpdateTask;
        private Task _allStatsUpdateTask;
        private Task _currentPipeUpdateTask;
        private CancellationTokenSource _connectionCheckCts;
        private CancellationTokenSource _torqueUpdateCts;
        private CancellationTokenSource _graphUpdateCts;
        private ObservableCollection<ObservablePoint> _torquePoints;
        private CartesianChart _currentChart;
        private int? _currentGraphPipeCounter;
        private bool _isTextChangeProgrammatic;
        private DispatcherTimer _comStatusTimer;
        private bool? _lastScrewOnStatus;
        private DateTime? _lastScrewOnTrueTime;
        private bool _isInScrewOnWindow;
        private bool _wasCycleStoppedRecently = false;
        private List<string> _xAxisLabels; // Новая коллекция для хранения меток оси X
        private const int SectorId = 8;  // Ваш SectorId
        private readonly IStatsRepository _statsRepository;  // Добавьте зависимость в конструктор

        public MainWindow()
        {
            InitializeComponent();
            _conn = new SqlConnection(_connectionString);
            var dataAccess = new DataAccessLayer(_connectionString);
            _statsRepository = dataAccess;
            _operatorService = new OperatorService(dataAccess, new Controller1C(), _statsRepository);
            _statsService = new StatsService(dataAccess, dataAccess);
            _comController = new COMController(new COMControllerParamsModel("COM3", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One));
            _comController.StateChanged += ComController_StateChanged;
            _comController.IsReading = true;
            _operatorService.OnOperatorChanged += OperatorService_OnOperatorChanged;
            txtPipeCounter.Text = string.Empty;
            Loaded += Window_Loaded;
            SizeChanged += Window_SizeChanged;
            LoadYearsFromDatabase();
            StartCurrentPipeUpdateLoop();
            StartAllStatsUpdateLoop();
            StartPLCUpdateLoop();
            StartTorqueDataUpdateLoop();
            txtPipeCounter.LostFocus += txtPipeCounter_LostFocus;
        }

        private bool TryConnectToPLC()
        {
            System.Diagnostics.Debug.WriteLine("MainWindow: Попытка подключения к PLC...");
            int maxRetries = 3;
            int retryDelayMs = 1000;
            for (int i = 0; i < maxRetries; i++)
            {
                int result = s7Client.ConnectTo("192.168.0.200", 0, 1);
                if (result == 0)
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow: Успешное подключение к PLC.");
                    return true;
                }
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка подключения к PLC, попытка {i + 1}/{maxRetries}, код: {s7Client.ErrorText(result)}");
                Thread.Sleep(retryDelayMs);
            }
            System.Diagnostics.Debug.WriteLine("MainWindow: Не удалось подключиться к PLC после всех попыток.");
            return false;
        }

        private async void StartConnectionStatusCheck()
        {
            _connectionCheckCts = new CancellationTokenSource();
            try
            {
                while (!_connectionCheckCts.Token.IsCancellationRequested)
                {
                    if (!s7Client.Connected)
                    {
                        System.Diagnostics.Debug.WriteLine("MainWindow: Попытка переподключения к ПЛК...");
                        TryConnectToPLC();
                    }
                    else
                    {
                        // Активная проверка соединения
                        int result = s7Client.DBRead(23, 0, 1, new byte[1]); // Проверка чтения 1 байта из DB23
                        if (result != 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"MainWindow: Потеря соединения с ПЛК, код ошибки: {s7Client.ErrorText(result)}");
                            s7Client.Disconnect(); // Принудительно обновляем статус
                        }
                    }

                    if (_plcDataWindow != null && _plcDataWindow.IsLoaded)
                    {
                        _plcDataWindow.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var lblConnectionStatus = _plcDataWindow.FindName("lblConnectionStatus") as System.Windows.Controls.Label;
                                if (lblConnectionStatus != null)
                                {
                                    lblConnectionStatus.Content = s7Client.Connected ? "Подключено к ПЛК" : "Ошибка подключения к ПЛК";
                                    lblConnectionStatus.Foreground = s7Client.Connected ? Brushes.Green : Brushes.Red;
                                    System.Diagnostics.Debug.WriteLine($"MainWindow: Обновлен статус в PLCDataWindow: {s7Client.Connected}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("MainWindow: lblConnectionStatus не найден в PLCDataWindow.");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при обновлении статуса в PLCDataWindow: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MainWindow: PLCDataWindow не открыт или не загружен.");
                    }

                    await Task.Delay(1000, _connectionCheckCts.Token); // Уменьшаем интервал для более быстрого реагирования
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Проверка состояния подключения остановлена.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка в проверке состояния подключения: {ex.Message}");
            }
        }

        private async Task StartGraphUpdateLoop(CancellationToken token)
        {
            Debug.WriteLine($"MainWindow: Цикл обновления графика начат для PipeCounter={_selectedPipeCounter}, вызван из {new StackTrace().GetFrame(1).GetMethod().Name}");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Debug.WriteLine("MainWindow: Выполняется итерация цикла обновления графика.");
                    await UpdateGraphAsync();
                    await Task.Delay(250, token);
                }
                Debug.WriteLine("MainWindow: Цикл обновления графика остановлен по токену отмены.");
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("MainWindow: Цикл обновления графика остановлен (TaskCanceledException).");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка в цикле обновления графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"MainWindow: Ошибка в цикле обновления графика: {ex.Message}\nStackTrace: {ex.StackTrace}");
                });
            }
            finally
            {
                Debug.WriteLine($"MainWindow: Цикл обновления графика завершен окончательно для PipeCounter={_selectedPipeCounter}.");
            }
        }
        private bool CheckScrewOnStatus()
        {
            try
            {
                using (var connection = new SqlConnection(_runtimeConnectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 10 Value FROM Runtime.dbo.History WHERE TagName = 'NOT_MN3_SCREW_ON' AND Value IS NOT NULL ORDER BY DateTime DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        bool isScrewOn = result != null && !Convert.IsDBNull(result) && Convert.ToBoolean(result);
                        Debug.WriteLine($"MainWindow: CheckScrewOnStatus: NOT_MN3_SCREW_ON = {isScrewOn}");
                        return isScrewOn;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Ошибка при проверке NOT_MN3_SCREW_ON: {ex.Message}");
                return false; // Если ошибка, считаем процесс неактивным
            }
        }

        private bool _isUpdatingGraph = false;

        private async Task UpdateGraphAsync()
        {
            if (_isUpdatingGraph)
            {
                Debug.WriteLine("MainWindow: UpdateGraphAsync уже выполняется, вызов пропущен.");
                return;
            }

            _isUpdatingGraph = true;
            DateTime adjustedStartTime = DateTime.Now.AddSeconds(-5);
            DateTime adjustedEndTime = DateTime.Now;
            Debug.WriteLine("MainWindow: UpdateGraphAsync начат");
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"MainWindow: canvasGraph Size - Width: {canvasGraph.ActualWidth}, Height: {canvasGraph.ActualHeight}, Visibility: {canvasGraph.Visibility}, ZIndex={Canvas.GetZIndex(canvasGraph)}");
                });

                if (!_selectedPipeCounter.HasValue)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lblGraphStatus.Content = "Пожалуйста, выберите или укажите текущую трубу.";
                        canvasGraph.Children.Clear();
                        _torquePoints = null;
                        _currentChart = null;
                        _currentGraphPipeCounter = null;
                        _xAxisLabels = null; // Сбрасываем метки
                        Debug.WriteLine("MainWindow: UpdateGraphAsync завершен: _selectedPipeCounter не установлен");
                    });
                    return;
                }

                bool isScrewOn = CheckScrewOnStatus();
                Debug.WriteLine($"MainWindow: UpdateGraphAsync: NOT_MN3_SCREW_ON = {isScrewOn}, _lastScrewOnStatus={_lastScrewOnStatus}, _isInScrewOnWindow={_isInScrewOnWindow}, _graphUpdateCts={(_graphUpdateCts == null ? "null" : "active")}");

                bool isCurrentPipeActive = _selectedPipeCounter == _currentPipeCounter;
                Debug.WriteLine($"MainWindow: UpdateGraphAsync: isCurrentPipeActive={isCurrentPipeActive}, _selectedPipeCounter={_selectedPipeCounter}, _currentPipeCounter={_currentPipeCounter}");

                var graphService = new GraphService(_runtimeConnectionString, _selectedPipeCounter);
                var (startTime, endTime) = graphService.GetTimeRange();
                Debug.WriteLine($"MainWindow: GetTimeRange вернул StartTime={startTime?.ToString("yyyy-MM-ddTHH:mm:ss.fff") ?? "null"}, EndTime={endTime?.ToString("yyyy-MM-ddTHH:mm:ss.fff") ?? "null"}");

                bool isActiveProcess = isScrewOn && isCurrentPipeActive;

                if (isScrewOn && isActiveProcess)
                {
                    _lastScrewOnTrueTime = DateTime.Now;
                    _isInScrewOnWindow = false;
                    _wasCycleStoppedRecently = false;
                    Debug.WriteLine("MainWindow: NOT_MN3_SCREW_ON = true, окно и флаг остановки сброшены, обновление продолжается.");
                }
                else if (_lastScrewOnStatus == true && !_isInScrewOnWindow && !isScrewOn)
                {
                    _isInScrewOnWindow = true;
                    if (!_lastScrewOnTrueTime.HasValue)
                    {
                        _lastScrewOnTrueTime = DateTime.Now;
                    }
                    Debug.WriteLine("MainWindow: NOT_MN3_SCREW_ON переключилось на false, начато 8-секундное окно.");
                }

                if (_isInScrewOnWindow && _lastScrewOnTrueTime.HasValue)
                {
                    if (DateTime.Now - _lastScrewOnTrueTime.Value > TimeSpan.FromSeconds(8))
                    {
                        _isInScrewOnWindow = false;
                        Dispatcher.Invoke(() =>
                        {
                            lblGraphStatus.Content = "График остановлен (8-секундное окно завершено)";
                            _torquePoints = null;
                            _currentChart = null;
                            _currentGraphPipeCounter = null;
                            //_xAxisLabels = null; // Сбрасываем метки
                        });
                        if (_graphUpdateCts != null)
                        {
                            _graphUpdateCts.Cancel();
                            _graphUpdateCts.Dispose();
                            _graphUpdateCts = null;
                            _wasCycleStoppedRecently = true;
                            Debug.WriteLine("MainWindow: Цикл обновления графика остановлен после завершения 8-секундного окна.");
                        }
                        _lastScrewOnStatus = isScrewOn;
                        return;
                    }
                    else
                    {
                        adjustedStartTime = _lastScrewOnTrueTime.Value.AddSeconds(-9);
                        adjustedEndTime = DateTime.Now;
                        Debug.WriteLine($"MainWindow: В 8-секундном окне: StartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, EndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}");
                    }
                }
                else
                {
                    _lastScrewOnStatus = isScrewOn;
                }

                if (!startTime.HasValue)
                {
                    try
                    {
                        using (var conn = new SqlConnection(_runtimeConnectionString))
                        {
                            await conn.OpenAsync();
                            var cmd = new SqlCommand(
                                "SELECT DateTime FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter = @PipeCounter",
                                conn);
                            cmd.Parameters.AddWithValue("@PipeCounter", _selectedPipeCounter);
                            var dateTime = await cmd.ExecuteScalarAsync() as DateTime?;
                            if (dateTime.HasValue)
                            {
                                adjustedStartTime = dateTime.Value;
                                Debug.WriteLine($"MainWindow: StartDateTime отсутствует, использован DateTime из MuftN3_REP: adjustedStartTime={adjustedStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
                            }
                            else
                            {
                                adjustedStartTime = DateTime.Now.AddHours(-2);
                                Debug.WriteLine($"MainWindow: DateTime отсутствует в MuftN3_REP, использован запасной диапазон: adjustedStartTime={adjustedStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        adjustedStartTime = DateTime.Now.AddHours(-2);
                        Debug.WriteLine($"MainWindow: Ошибка при получении DateTime из MuftN3_REP: {ex.Message}, использован запасной диапазон: adjustedStartTime={adjustedStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")}");
                    }
                    adjustedEndTime = DateTime.Now;
                    Debug.WriteLine($"MainWindow: StartDateTime отсутствует, установлены временные значения: adjustedStartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, adjustedEndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}");
                }
                else
                {
                    adjustedStartTime = startTime.Value;
                    adjustedEndTime = isCurrentPipeActive ? DateTime.Now : endTime.Value;
                    Debug.WriteLine($"MainWindow: adjustedStartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, adjustedEndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}, isActiveProcess={isActiveProcess}");
                }

                if (adjustedStartTime.Year > _selectedYear || (endTime.HasValue && endTime.Value.Year < _selectedYear))
                {
                    adjustedStartTime = new DateTime(_selectedYear, 1, 1);
                    adjustedEndTime = new DateTime(_selectedYear, 12, 31, 23, 59, 59);
                    Debug.WriteLine($"MainWindow: Временной диапазон скорректирован: adjustedStartTime={adjustedStartTime:yyyy-MM-ddTHH:mm:ss.fff}, adjustedEndTime={adjustedEndTime:yyyy-MM-ddTHH:mm:ss.fff}");
                }

                double maxTorqueLimit = 25000;
                Dispatcher.Invoke(() =>
                {
                    if (lblOptimalTorque.Content != null && lblOptimalTorque.Content.ToString() != "N/A" && double.TryParse(lblOptimalTorque.Content.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        maxTorqueLimit = parsedValue * 1.1 + 5000;
                        Debug.WriteLine($"MainWindow: Используется MaxTorque из lblMaxTorque: {maxTorqueLimit}");
                    }
                    else
                    {
                        Debug.WriteLine($"MainWindow: Не удалось получить MaxTorque из lblMaxTorque, используется значение по умолчанию: {maxTorqueLimit}");
                    }
                });

                var (torquePoints, xAxes, yAxes, errorMessage) = !startTime.HasValue
                    ? (new ObservableCollection<ObservablePoint>(),
                       new Axis[] { new Axis { Name = "Количество оборотов", LabelsRotation = 45, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12 } },
                       new Axis[] { new Axis { Name = "Крутящий момент", Labeler = value => value.ToString("F2"), MinLimit = 0, MaxLimit = maxTorqueLimit, LabelsPaint = new SolidColorPaint(SKColors.Black), TextSize = 12, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 } } },
                       "Ожидание данных: StartDateTime отсутствует")
                    : graphService.GetGraphData(adjustedStartTime, adjustedEndTime, isActiveProcess);

                Debug.WriteLine($"MainWindow: GetGraphData вернул {torquePoints.Count} точек, errorMessage={(string.IsNullOrEmpty(errorMessage) ? "null" : errorMessage)}");

                // Получаем xAxisLabels из filteredData внутри GetGraphData
                var filteredData = graphService.FetchSynchronizedData(adjustedStartTime, adjustedEndTime, isActiveProcess);
                filteredData = isActiveProcess ? filteredData.Where(d => d.ScrewOn).ToList() : filteredData.ToList();
                var newLabels = filteredData.Select(d => d.Turns.ToString("F2")).ToList();

                Dispatcher.Invoke(() =>
                {
                    lblGraphStatus.Content = !string.IsNullOrEmpty(errorMessage) ? errorMessage : (isActiveProcess ? "График обновляется (барабан крутится)" : "График статичен (барабан остановлен)");

                    var fixedYAxis = new Axis
                    {
                        Name = "Крутящий момент",
                        MinLimit = 0,
                        MaxLimit = maxTorqueLimit,
                        Labeler = value => value.ToString("F2"),
                        LabelsPaint = new SolidColorPaint(SKColors.Black),
                        TextSize = 12,
                        SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
                    };

                    if (_currentGraphPipeCounter != _selectedPipeCounter || _currentChart == null)
                    {
                        _torquePoints = new ObservableCollection<ObservablePoint>();
                        _xAxisLabels = new List<string>(); // Инициализируем коллекцию меток
                        _currentChart = new CartesianChart
                        {
                            Series = new ISeries[]
                            {
                                new StepLineSeries<ObservablePoint>
                                {
                                    Values = _torquePoints,
                                    Name = "Крутящий момент",
                                    XToolTipLabelFormatter = (chartPoint) =>
                                    {
                                        if (!chartPoint.Model.X.HasValue || _xAxisLabels == null || chartPoint.Model.X >= _xAxisLabels.Count || chartPoint.Model.X < 0)
                                        {
                                            Debug.WriteLine($"MainWindow: XToolTipLabelFormatter: Некорректный индекс X={chartPoint.Model.X}, LabelsCount={_xAxisLabels?.Count ?? 0}");
                                            return "N/A";
                                        }
                                        return $"Обороты: {_xAxisLabels[(int)chartPoint.Model.X]}";
                                    },
                                    YToolTipLabelFormatter = (chartPoint) => chartPoint.Model.Y.HasValue ? chartPoint.Model.Y.Value.ToString("F2") : "N/A",
                                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                                    Fill = null,
                                    GeometrySize = 0.5,
                                    GeometryFill = new SolidColorPaint(SKColors.Black),
                                    GeometryStroke = new SolidColorPaint(SKColors.Black)
                                }
                            },
                            XAxes = xAxes,
                            YAxes = new[] { fixedYAxis },
                            LegendPosition = LegendPosition.Top
                        };

                        _currentChart.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth") { Source = canvasGraph });
                        _currentChart.SetBinding(FrameworkElement.HeightProperty, new System.Windows.Data.Binding("ActualHeight") { Source = canvasGraph });

                        canvasGraph.Children.Clear();
                        canvasGraph.Children.Add(_currentChart);
                        _currentGraphPipeCounter = _selectedPipeCounter;
                        Debug.WriteLine($"MainWindow: Новый график создан для PipeCounter={_selectedPipeCounter}");
                    }

                    if (torquePoints.Any() || _currentGraphPipeCounter != _selectedPipeCounter)
                    {
                        _torquePoints.Clear();
                        _xAxisLabels.Clear(); // Очищаем метки перед добавлением новых
                        int index = 0;
                        foreach (var point in torquePoints)
                        {
                            _torquePoints.Add(point);
                            if (index < newLabels.Count)
                            {
                                _xAxisLabels.Add(newLabels[index]); // Добавляем метку для каждой точки
                                //Debug.WriteLine($"MainWindow: Добавлена точка: X={point.X:F2}, Y={point.Y:F2}, Label={newLabels[index]}");
                            }
                            else
                            {
                                _xAxisLabels.Add("0.00"); // Запасное значение, если меток меньше
                                //Debug.WriteLine($"MainWindow: Добавлена точка: X={point.X:F2}, Y={point.Y:F2}, Label=0.00 (запасное)");
                            }
                            index++;
                        }

                        // Обновляем xAxes с новыми метками
                        if (_currentChart.XAxes != null && xAxes != null && xAxes.Any())
                        {
                            _currentChart.XAxes = new Axis[]
                            {
                                new Axis
                                {
                                    Name = "Количество оборотов",
                                    Labels = _xAxisLabels.ToArray(),
                                    LabelsRotation = 45,
                                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                                    TextSize = 12
                                }
                            };
                        }
                    }

                    _currentChart.YAxes = new[] { fixedYAxis };
                    Debug.WriteLine($"MainWindow: Обновлены точки графика для PipeCounter={_selectedPipeCounter}, всего точек: {_torquePoints.Count}, xAxes[0].Labels.Count={_xAxisLabels.Count}");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"MainWindow: Ошибка в UpdateGraphAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    MessageBox.Show($"Ошибка при обновлении графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblGraphStatus.Content = $"Ошибка отрисовки: {ex.Message}";
                    canvasGraph.Children.Clear();
                    _torquePoints = null;
                    _currentChart = null;
                    _currentGraphPipeCounter = null;
                    _xAxisLabels = null; // Сбрасываем метки при ошибке
                });
            }
            finally
            {
                _isUpdatingGraph = false;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentChart != null)
            {
                _currentChart.Width = canvasGraph.ActualWidth;
                _currentChart.Height = canvasGraph.ActualHeight;
                Debug.WriteLine($"MainWindow: Окно изменило размер, обновляем размеры графика: Width={_currentChart.Width}, Height={_currentChart.Height}");
            }
        }

        private async void StartPLCUpdateLoop()
        {
            s7Client = new S7Client();
            _plcReader = new Services.ReadFromPLC(s7Client);
            var writer = new Services.WriteToPLC(s7Client);

            if (!TryConnectToPLC())
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Ошибка подключения к PLC. Периодическое чтение не запущено.");
                    System.Diagnostics.Debug.WriteLine("MainWindow: Периодическое чтение не запущено из-за ошибки подключения.");
                    //StartConnectionStatusCheck();
                });
                return;
            }

            var parameters = new[]
            {
                    "TorqueUpperLimitHMI", "IdleTorqueHMI", "StopTorqueHMI",
                    "TorqueLowerLimitHMI", "QuantityHMI", "StartingTorqueHMI",
                    "FeedDelayTimeHMI", "ReturnDelayTimeHMI", "RPMUpperLimitHMI"
            };

            foreach (var param in parameters)
            {
                _plcReader.AddAddress(param, CreateAddressModel(param, s7Client));
                writer.AddAddress(param, CreateAddressModel(param, s7Client));
            }

            _cts = new CancellationTokenSource();
            try
            {
                _plcUpdateTask = _plcReader.StartPeriodicReadAsync(TimeSpan.FromSeconds(1), (key, result) =>
                {
                }, _cts.Token);

                StartConnectionStatusCheck();
                await _plcUpdateTask;
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Чтение остановлено.");
                    System.Diagnostics.Debug.WriteLine("MainWindow: Чтение PLC остановлено.");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка в цикле чтения PLC: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка в цикле чтения PLC: {ex.Message}");
                });
            }
            finally
            {
                _plcReader.Disconnect();
                writer.Disconnect();
                System.Diagnostics.Debug.WriteLine("MainWindow: PLCReader и PLCWriter отключены.");
            }
        }

        private async void StartTorqueDataUpdateLoop()
        {
            _torqueUpdateCts = new CancellationTokenSource();
            var tagNames = new Dictionary<string, string>
            {
                { "lblMaxTorque", "NOT_MN3_TorqueUpperLimitHMI" },
                { "lblOptimalTorque", "NOT_MN3_IdleTorqueHMI" },
                { "lblStopTorque", "NOT_MN3_StopTorqueHMI" },
                { "lblMinTorque", "NOT_MN3_TorqueLowerLimitHMI" },
                { "lblActualTorque", "NOT_MN3_ActualTorqueHMI" },
                { "lblInternalTorque", "NOT_MN3_InternalTorqueHMI" }
            };

            while (!_torqueUpdateCts.Token.IsCancellationRequested)
            {
                try
                {
                    using (var connection = new SqlConnection(_runtimeConnectionString))
                    {
                        await connection.OpenAsync();
                        foreach (var tag in tagNames)
                        {
                            string query = "SELECT TOP 1 CASE WHEN ABS(Value) < 0.00001 THEN 0 ELSE ROUND(Value, 6) END AS Value FROM Runtime.dbo.History WHERE TagName = @TagName ORDER BY DateTime DESC";
                            using (var command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@TagName", tag.Value);
                                var result = await command.ExecuteScalarAsync();
                                Dispatcher.Invoke(() =>
                                {
                                    var label = FindName(tag.Key) as Label;
                                    if (label != null)
                                    {
                                        label.Content = result != null ? Convert.ToSingle(result).ToString(CultureInfo.InvariantCulture) : "N/A";
                                    }
                                });
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Debug.WriteLine($"MainWindow: Ошибка подключения к БД: {ex.Message}");
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var tag in tagNames)
                        {
                            var label = FindName(tag.Key) as Label;
                            if (label != null)
                            {
                                label.Content = "N/A";
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MainWindow: Ошибка при чтении из БД: {ex.Message}");
                }
                await Task.Delay(200);
            }
            Debug.WriteLine("MainWindow: Цикл обновления данных крутящего момента остановлен.");
        }

        private object CreateAddressModel(string param, S7Client s7Client)
        {
            switch (param)
            {
                case "TorqueUpperLimitHMI": return new SiemensPLCModels.DBAddressModel.TorqueUpperLimitHMI(s7Client);
                case "IdleTorqueHMI": return new SiemensPLCModels.DBAddressModel.IdleTorqueHMI(s7Client);
                case "StopTorqueHMI": return new SiemensPLCModels.DBAddressModel.StopTorqueHMI(s7Client);
                case "TorqueLowerLimitHMI": return new SiemensPLCModels.DBAddressModel.TorqueLowerLimitHMI(s7Client);
                case "QuantityHMI": return new SiemensPLCModels.DBAddressModel.QuantityHMI(s7Client);
                case "StartingTorqueHMI": return new SiemensPLCModels.DBAddressModel.StartingTorqueHMI(s7Client);
                case "CuringRotationCount": return new SiemensPLCModels.DBAddressModel.CuringRotationCount(s7Client);
                case "StatusValue": return new SiemensPLCModels.DBAddressModel.StatusValue(s7Client);
                case "StatusTime": return new SiemensPLCModels.DBAddressModel.StatusTime(s7Client);
                case "FeedDelayTimeHMI": return new SiemensPLCModels.DBAddressModel.FeedDelayTimeHMI(s7Client);
                case "ReturnDelayTimeHMI": return new SiemensPLCModels.DBAddressModel.ReturnDelayTimeHMI(s7Client);
                case "RPMUpperLimitHMI": return new SiemensPLCModels.DBAddressModel.RPMUpperLimitHMI(s7Client);
                default: throw new ArgumentException($"Неизвестный параметр: {param}");
            }
        }

        private void btnShowPLCData_Click(object sender, RoutedEventArgs e)
        {
            if (_plcDataWindow == null || !_plcDataWindow.IsLoaded)
            {
                _plcDataWindow = new PLCDataWindow(_plcReader, s7Client);
                _plcDataWindow.Closed += (s, args) => _plcDataWindow = null;
                _plcDataWindow.Show();
                System.Diagnostics.Debug.WriteLine("MainWindow: PLCDataWindow создан и открыт.");
            }
            else
            {
                _plcDataWindow.Activate();
                System.Diagnostics.Debug.WriteLine("MainWindow: PLCDataWindow активирован.");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var images = new[] { imgShowGraph, imgSaveGraph, imgResetGraph, imgResumeUpdate, imgOpenFolder };
            foreach (var image in images)
            {
                if (image?.Source == null)
                {
                    MessageBox.Show($"Изображение для кнопки {image?.Name} не загружено!");
                }
            }
        }

        private async void LoadYearsFromDatabase()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT DISTINCT Year FROM MuftN3_REP WHERE Year IS NOT NULL ORDER BY Year";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    System.Data.DataTable dt = new System.Data.DataTable();
                    adapter.Fill(dt);
                    Dispatcher.Invoke(() =>
                    {
                        cbYear.ItemsSource = dt.DefaultView;
                        if (dt.Rows.Count > 0)
                        {
                            cbYear.SelectedIndex = 0;
                            _selectedYear = Convert.ToInt32(dt.Rows[0]["Year"]);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Ошибка загрузки годов из базы данных: {ex.Message}"));
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка загрузки годов: {ex.Message}");
            }
        }

        //private void txtPipeCounter_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    try
        //    {
        //        Process.Start(new ProcessStartInfo
        //        {
        //            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "osk.exe"),
        //            UseShellExecute = true
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка запуска экранной клавиатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void txtPipeCounter_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("osk"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии osk.exe: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось закрыть экранную клавиатуру: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка закрытия клавиатуры: {ex.Message}");
            }
        }

        private async void cbYear_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbYear.SelectedItem != null)
            {
                System.Data.DataRowView selectedRow = cbYear.SelectedItem as System.Data.DataRowView;
                if (selectedRow != null)
                {
                    _selectedYear = Convert.ToInt32(selectedRow["Year"]);
                    Console.WriteLine($"Выбранный год: {_selectedYear}");
                    if (_selectedPipeCounter.HasValue)
                        btnShowGraph_Click(null, null);
                }
            }
        }

        private async void ComController_StateChanged(object sender, COMEventArgs.ReadingDataEventArgs e)
        {
            if (e.State == COMControllerParamsModel.COMStates.Detected && !string.IsNullOrEmpty(e.CardId))
            {
                await _operatorService.InitializeOperatorAsync(e.CardId);
                await _operatorService.AuthenticateOperatorAsync(_operatorService.CurrentOperator.CardNumber, true, DateTime.Now);
                Debug.WriteLine("MainWindow: Оператор авторизован и привязан к SectorId=8");
                if (_operatorService.CurrentOperator != null)
                {
                    await _statsService.UpdateIdsAsync(_operatorService.CurrentOperator.PersonnelNumber);
                    StartOperatorUpdateLoop();
                }
            }
            else if (e.State == COMControllerParamsModel.COMStates.Removed)
            {
                if (_operatorService.CurrentOperator != null)
                {
                    await _operatorService.AuthenticateOperatorAsync(_operatorService.CurrentOperator.CardNumber, false, DateTime.Now);
                    _operatorService.CurrentOperator = null;
                }
                else
                {
                    Debug.WriteLine("MainWindow: Попытка деавторизации при отсутствии текущего оператора.");
                }
                StopOperatorUpdateLoop();
                Debug.WriteLine("MainWindow: Оператор деавторизован и отвязан от SectorId=8");
                Dispatcher.Invoke(() =>
                {
                    //lblStatus.Visibility = Visibility.Collapsed;
                    //_currentPipeCounter = null;
                    operatorDataPanel.Visibility = Visibility.Collapsed;
                    statsDataPanel.Visibility = Visibility.Collapsed;
                    txtInsertCardPrompt.Visibility = Visibility.Visible;
                    txtNoStatsPrompt.Visibility = Visibility.Visible;
                    lblIdCard.Content = string.Empty;
                    lblTabNumber.Content = string.Empty;
                    lblFIO.Content = string.Empty;
                    lblDepartment.Text = string.Empty;
                    lblEmployName.Content = string.Empty;
                    lblShiftItems.Content = string.Empty;
                    lblShiftDowntime.Content = string.Empty;
                    lblMonthItems.Content = string.Empty;
                    lblMonthDowntime.Content = string.Empty;
                    lblShiftPlanItems.Content = string.Empty;
                    lblMonthPlanItems.Content = string.Empty;
                    //txtCurrentPipe.Content = string.Empty;
                });
            }
            else if (e.State == COMControllerParamsModel.COMStates.None && e.ErrorCode == (int)COMControllerParamsModel.ErrorCodes.ReadingError)
            {
                Dispatcher.Invoke(() =>
                {
                    lblCOMConnectionStatus.Content = e.ErrorText ?? "Считыватель отключен.";
                    lblCOMConnectionStatus.Foreground = Brushes.Red;
                    lblCOMConnectionStatus.Visibility = Visibility.Visible;
                    if (_comStatusTimer != null)
                    {
                        _comStatusTimer.Stop();
                        Debug.WriteLine("MainWindow: Таймер остановлен при отключении считывателя.");
                    }
                    else
                    {
                        Debug.WriteLine("MainWindow: Попытка остановки таймера, но _comStatusTimer равен null.");
                    }
                });
            }
            else if (e.State == COMControllerParamsModel.COMStates.ReaderConnecting && e.ErrorCode == (int)COMControllerParamsModel.ErrorCodes.ReaderConnecting)
            {
                Dispatcher.Invoke(() =>
                {
                    lblCOMConnectionStatus.Content = "Подключение к считывателю восстановлено.";
                    lblCOMConnectionStatus.Foreground = Brushes.Green;
                    lblCOMConnectionStatus.Visibility = Visibility.Visible;
                    if (_comStatusTimer != null)
                    {
                        _comStatusTimer.Stop();
                        _comStatusTimer.Start();
                        Debug.WriteLine("MainWindow: Таймер запущен для скрытия сообщения об успешном подключении.");
                    }
                    else
                    {
                        Debug.WriteLine("MainWindow: Попытка запуска таймера, но _comStatusTimer равен null.");
                        Task.Delay(5000).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                lblCOMConnectionStatus.Visibility = Visibility.Collapsed;
                                Debug.WriteLine("MainWindow: Метка скрыта через Task.Delay из-за null таймера.");
                            });
                        });
                    }
                });
            }
        }

        private void OperatorService_OnOperatorChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var operatorInfo = _operatorService.CurrentOperator;
                if (operatorInfo == null)
                {
                    lblIdCard.Content = string.Empty;
                    lblTabNumber.Content = string.Empty;
                    lblFIO.Content = string.Empty;
                    lblDepartment.Text = string.Empty;
                    lblEmployName.Content = string.Empty;
                    lblShiftItems.Content = string.Empty;
                    lblShiftDowntime.Content = string.Empty;
                    lblMonthItems.Content = string.Empty;
                    lblMonthDowntime.Content = string.Empty;
                    lblMonthPlanItems.Content = string.Empty;
                    lblShiftPlanItems.Content = string.Empty;
                    operatorDataPanel.Visibility = Visibility.Collapsed;
                    statsDataPanel.Visibility = Visibility.Collapsed;
                    txtInsertCardPrompt.Visibility = Visibility.Visible;
                    txtNoStatsPrompt.Visibility = Visibility.Visible;
                    //txtCurrentPipe.Content = string.Empty;
                }
                else
                {
                    lblIdCard.Content = operatorInfo?.CardNumber ?? string.Empty;
                    lblTabNumber.Content = operatorInfo?.PersonnelNumber ?? string.Empty;
                    lblFIO.Content = operatorInfo?.FullName ?? string.Empty;
                    lblDepartment.Text = operatorInfo?.Department ?? string.Empty;
                    lblEmployName.Content = operatorInfo?.Position ?? string.Empty;
                    txtInsertCardPrompt.Visibility = Visibility.Collapsed;
                    operatorDataPanel.Visibility = Visibility.Visible;
                }
            });
        }

        private void StartOperatorUpdateLoop()
        {
            _operatorUpdateCts?.Dispose();
            _operatorUpdateCts = new CancellationTokenSource();
            var token = _operatorUpdateCts.Token;

            _operatorUpdateTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"Обновление в {DateTime.Now}");
                    await _statsService.UpdateIdsAsync(_operatorService.CurrentOperator.PersonnelNumber);
                    await _statsService.UpdateStatsAsync(_operatorService.CurrentOperator.PersonnelNumber, (shiftItems, shiftDowntime, monthItems, monthDowntime, monthPlan, shiftPlan) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (string.IsNullOrEmpty(shiftItems) || string.IsNullOrEmpty(shiftDowntime) || string.IsNullOrEmpty(monthItems) || string.IsNullOrEmpty(monthDowntime) || string.IsNullOrEmpty(monthPlan) || string.IsNullOrEmpty(shiftPlan))
                            {
                                txtNoStatsPrompt.Visibility = Visibility.Visible;
                                statsDataPanel.Visibility = Visibility.Collapsed;
                                lblShiftItems.Content = string.Empty;
                                lblShiftDowntime.Content = string.Empty;
                                lblMonthItems.Content = string.Empty;
                                lblMonthDowntime.Content = string.Empty;
                                lblShiftPlanItems.Content = string.Empty;
                                lblMonthPlanItems.Content += string.Empty;
                            }
                            else
                            {
                                txtNoStatsPrompt.Visibility = Visibility.Collapsed;
                                lblShiftItems.Content = shiftItems;
                                lblShiftDowntime.Content = shiftDowntime;
                                lblMonthItems.Content = monthItems;
                                lblMonthDowntime.Content = monthDowntime;
                                lblShiftPlanItems.Content = shiftPlan;
                                lblMonthPlanItems.Content = monthPlan;
                                statsDataPanel.Visibility = Visibility.Visible;
                            }
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

        private void StartCurrentPipeUpdateLoop()
        {
            _currentPipeUpdateCts?.Dispose();
            _currentPipeUpdateCts = new CancellationTokenSource();
            UpdateNomenclatureAsync();
            var token = _currentPipeUpdateCts.Token;

            _currentPipeUpdateTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await UpdateCurrentPipeCounter();
                    await Task.Delay(5000, token);
                }
            }, token);
        }

        private void StartAllStatsUpdateLoop()
        {
            _allStatsUpdateCts?.Dispose();
            _allStatsUpdateCts = new CancellationTokenSource();
            var token = _allStatsUpdateCts.Token;

            _allStatsUpdateTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await _statsService.UpdateStatsForAllOperatorsAsync((monthlyDowntimeByShift, monthlyOperationCountByShift, monthlyPlanByShift) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Обнуляем общие суммы для каждой итерации
                            decimal totalDowntime = 0;
                            int totalFactItems = 0;
                            double totalPlanItems = 0;

                            // Суммируем данные по всем сменам
                            foreach (var kvp in monthlyDowntimeByShift)
                            {
                                totalDowntime += kvp.Value;
                            }
                            foreach (var kvp in monthlyOperationCountByShift)
                            {
                                totalFactItems += kvp.Value;
                            }
                            foreach (var kvp in monthlyPlanByShift)
                            {
                                totalPlanItems += kvp.Value;
                            }

                            // Обновляем общие метки (если они есть в UI)
                            //lblAllMonthItems.Content = totalFactItems.ToString();
                            //lblAllMonthDowntime.Content = totalDowntime.ToString("F2");
                            // lblAllShiftItems и lblAllShiftDowntime можно убрать или адаптировать, если они не используются
                            // lblAllShiftItems.Content = ""; // Очистить, если не нужно
                            // lblAllShiftDowntime.Content = ""; // Очистить, если не нужно

                            // Обновляем метки для каждой смены в allStatsDataPanel
                            lblShiftAPlanItems.Content = monthlyPlanByShift.GetValueOrDefault(1, 0).ToString();
                            lblShiftAFactItems.Content = monthlyOperationCountByShift.GetValueOrDefault(1, 0).ToString();
                            lblShiftADowntime.Content = monthlyDowntimeByShift.GetValueOrDefault(1, 0).ToString("F2");

                            lblShiftBPlanItems.Content = monthlyPlanByShift.GetValueOrDefault(2, 0).ToString();
                            lblShiftBFactItems.Content = monthlyOperationCountByShift.GetValueOrDefault(2, 0).ToString();
                            lblShiftBDowntime.Content = monthlyDowntimeByShift.GetValueOrDefault(2, 0).ToString("F2");

                            lblShiftVPlanItems.Content = monthlyPlanByShift.GetValueOrDefault(3, 0).ToString();
                            lblShiftVFactItems.Content = monthlyOperationCountByShift.GetValueOrDefault(3, 0).ToString();
                            lblShiftVDowntime.Content = monthlyDowntimeByShift.GetValueOrDefault(3, 0).ToString("F2");

                            lblShiftGPlanItems.Content = monthlyPlanByShift.GetValueOrDefault(4, 0).ToString();
                            lblShiftGFactItems.Content = monthlyOperationCountByShift.GetValueOrDefault(4, 0).ToString();
                            lblShiftGDowntime.Content = monthlyDowntimeByShift.GetValueOrDefault(4, 0).ToString("F2");

                            allStatsDataPanel.Visibility = Visibility.Visible;
                        });
                    });
                    await Task.Delay(5000, token); // Интервал 5 секунд
                }
            }, token);
        }

        private async Task UpdateCurrentPipeCounter()
        {
            try
            {
                using (var conn = new SqlConnection(_runtimeConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT TOP 1 PipeCounter FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter IS NOT NULL ORDER BY PipeCounter DESC", conn);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int pipeCounter))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _currentPipeCounter = pipeCounter;
                            txtCurrentPipe.Content = pipeCounter.ToString();
                        });

                        bool isScrewOn = CheckScrewOnStatus();
                        if (_selectedPipeCounter != _currentPipeCounter)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (_graphUpdateCts != null)
                                {
                                    _graphUpdateCts.Cancel();
                                    _graphUpdateCts.Dispose();
                                    _graphUpdateCts = null;
                                    Debug.WriteLine("MainWindow: Цикл обновления графика остановлен из-за смены PipeCounter.");
                                }
                                _torquePoints?.Clear();
                                _currentGraphPipeCounter = null;
                                _selectedPipeCounter = _currentPipeCounter;
                                _wasCycleStoppedRecently = false; // Сбрасываем флаг при смене трубы
                                _isTextChangeProgrammatic = true;
                                txtPipeCounter.Text = _currentPipeCounter.HasValue ? _currentPipeCounter.Value.ToString() : string.Empty;
                                _isTextChangeProgrammatic = false;
                                btnShowGraph_Click(null, null);
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                lblStatus.Visibility = isScrewOn ? Visibility.Visible : Visibility.Collapsed;
                                if (isScrewOn || _isInScrewOnWindow)
                                {
                                    if (_wasCycleStoppedRecently)
                                    {
                                        _isInScrewOnWindow = true;
                                        if (!_lastScrewOnTrueTime.HasValue)
                                        {
                                            _lastScrewOnTrueTime = DateTime.Now; // Устанавливаем время, если не было ранее
                                        }
                                        Debug.WriteLine("MainWindow: NOT_MN3_SCREW_ON переключилось на false, начато 8-секундное окно.");
                                    }

                                    UpdateGraphAsync();
                                    if (_graphUpdateCts == null || _graphUpdateCts.IsCancellationRequested)
                                    {
                                        _graphUpdateCts = new CancellationTokenSource();
                                        StartGraphUpdateLoop(_graphUpdateCts.Token);
                                        Debug.WriteLine($"MainWindow: Запущен цикл обновления графика для PipeCounter={_selectedPipeCounter}");
                                    }
                                }
                                else if (!isScrewOn && !_isInScrewOnWindow && _graphUpdateCts != null)
                                {
                                    _isInScrewOnWindow = true;
                                    if (!_lastScrewOnTrueTime.HasValue)
                                    {
                                        _lastScrewOnTrueTime = DateTime.Now; // Устанавливаем время, если не было ранее
                                    }
                                    Debug.WriteLine("MainWindow: NOT_MN3_SCREW_ON переключилось на false, начато 8-секундное окно.");
                                }
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtCurrentPipe.Content = string.Empty;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при обновлении текущей трубы: {ex.Message}");
                    txtCurrentPipe.Content = "Ошибка";
                    Debug.WriteLine($"MainWindow: Ошибка обновления PipeCounter: {ex.Message}");
                });
            }
        }
        private void UpdatePipeCounter(int? newPipeCounter)
        {
            if (_selectedPipeCounter != newPipeCounter)
            {
                _selectedPipeCounter = newPipeCounter;
                _lastScrewOnStatus = CheckScrewOnStatus();
                _isInScrewOnWindow = false;
                _lastScrewOnTrueTime = null;
                _wasCycleStoppedRecently = false; // Сбрасываем флаг
                Debug.WriteLine($"MainWindow: Смена PipeCounter на {newPipeCounter}, _lastScrewOnStatus инициализировано как {_lastScrewOnStatus}");
                if (_graphUpdateCts != null)
                {
                    _graphUpdateCts.Cancel();
                    _graphUpdateCts.Dispose();
                    _graphUpdateCts = null;
                    Debug.WriteLine("MainWindow: Цикл обновления графика остановлен при смене PipeCounter.");
                }
            }
        }
        private async void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtPipeCounter.Text, out int pipeCounter))
            {
                MessageBox.Show("Введите корректный номер трубы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM [Pilot].[dbo].[MuftN3_REP] WHERE PipeCounter = @PipeCounter",
                        conn);
                    checkCmd.Parameters.AddWithValue("@PipeCounter", pipeCounter);
                    var count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        MessageBox.Show($"Труба с номером {pipeCounter} не найдена в базе данных.");
                        return;
                    }
                }

                if (_selectedPipeCounter != pipeCounter)
                {
                    _torquePoints?.Clear();
                    _currentGraphPipeCounter = null;
                    _wasCycleStoppedRecently = false; // Сбрасываем флаг при явной смене трубы
                }

                _selectedPipeCounter = pipeCounter;
                UpdatePipeCounter(pipeCounter);
                await UpdateGraphAsync();

                if (_graphUpdateCts != null)
                {
                    _graphUpdateCts.Cancel();
                    _graphUpdateCts.Dispose();
                    _graphUpdateCts = null;
                }

                if (_selectedPipeCounter == _currentPipeCounter)
                {
                    bool isScrewOn = CheckScrewOnStatus();
                    if (isScrewOn || _isInScrewOnWindow)
                    {
                        if (_wasCycleStoppedRecently)
                        {
                            Debug.WriteLine("MainWindow: Запуск цикла через btnShowGraph_Click предотвращён, так как он был остановлен для текущего PipeCounter.");
                            await UpdateGraphAsync(); // Однократное обновление
                            return;
                        }

                        _graphUpdateCts = new CancellationTokenSource();
                        await StartGraphUpdateLoop(_graphUpdateCts.Token);
                        Debug.WriteLine($"MainWindow: Запущен цикл обновления графика для PipeCounter={_selectedPipeCounter}");
                    }
                    else
                    {
                        await UpdateGraphAsync();
                        Debug.WriteLine($"MainWindow: Выполнено однократное обновление графика для PipeCounter={_selectedPipeCounter}, цикл не запущен.");
                    }
                }
                else
                {
                    await UpdateGraphAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Ошибка в btnShowGraph_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (canvasGraph.Children.Count == 0 || !(canvasGraph.Children[0] is CartesianChart chart))
                {
                    MessageBox.Show("График отсутствует. Сначала создайте график.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (chart.Width <= 0 || chart.Height <= 0)
                {
                    chart.Width = canvasGraph.ActualWidth > 0 ? canvasGraph.ActualWidth : 800;
                    chart.Height = canvasGraph.ActualHeight > 0 ? canvasGraph.ActualHeight : 600;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    Title = "Сохранить график как PDF",
                    FileName = $"Graph_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)chart.Width,
                        (int)chart.Height,
                        96, 96,
                        PixelFormats.Pbgra32);

                    chart.Measure(new Size(chart.Width, chart.Height));
                    chart.Arrange(new Rect(new Size(chart.Width, chart.Height)));
                    renderBitmap.Render(chart);

                    using (var memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        using (var pdfDocument = new PdfDocument())
                        {
                            var page = pdfDocument.AddPage();
                            page.Size = PdfSharpCore.PageSize.A4;
                            var gfx = XGraphics.FromPdfPage(page);

                            using (var xImage = XImage.FromStream(() => memoryStream))
                            {
                                double scale = Math.Min(page.Width / xImage.PixelWidth, page.Height / xImage.PixelHeight);
                                double newWidth = xImage.PixelWidth * scale;
                                double newHeight = xImage.PixelHeight * scale;

                                double x = (page.Width - newWidth) / 2;
                                double y = (page.Height - newHeight) / 2;

                                gfx.DrawImage(xImage, x, y, newWidth, newHeight);
                            }

                            pdfDocument.Save(saveFileDialog.FileName);
                            MessageBox.Show($"График успешно сохранен как {saveFileDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка сохранения графика: {ex.Message}");
            }
        }

        private void btnResetGraph_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                canvasGraph.Children.Clear();
                _torquePoints = null;
                _selectedPipeCounter = null;
                txtPipeCounter.Text = string.Empty;
                lblStatus.Visibility = Visibility.Collapsed;
                _currentPipeCounter = null;
                txtCurrentPipe.Content = string.Empty;
                _currentPipeUpdateCts?.Cancel();
                _currentPipeUpdateCts?.Dispose();
                _currentPipeUpdateCts = null;
                _graphUpdateCts?.Cancel();
                _graphUpdateCts?.Dispose();
                _graphUpdateCts = null;
                lblGraphStatus.Content = string.Empty;
                _currentChart = null;
                _currentGraphPipeCounter = null;
                Debug.WriteLine("MainWindow: График сброшен, все точки очищены.");
            });
        }

        private void btnResumeUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPipeUpdateCts == null || _currentPipeUpdateCts.IsCancellationRequested)
            {
                StartCurrentPipeUpdateLoop();
                Debug.WriteLine("MainWindow: btnResumeUpdate_Click: Запущен StartCurrentPipeUpdateLoop");

                // Проверяем, есть ли значение в _currentPipeCounter
                if (!_currentPipeCounter.HasValue)
                {
                    // Загружаем последнее значение PipeCounter из базы данных
                    try
                    {
                        using (var conn = new SqlConnection(_runtimeConnectionString))
                        {
                            conn.Open();
                            var cmd = new SqlCommand("SELECT TOP 1 PipeCounter FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter IS NOT NULL ORDER BY PipeCounter DESC", conn);
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int pipeCounter))
                            {
                                _currentPipeCounter = pipeCounter;
                                Debug.WriteLine($"MainWindow: btnResumeUpdate_Click: Получен PipeCounter={pipeCounter} из базы данных");
                            }
                            else
                            {
                                Debug.WriteLine("MainWindow: btnResumeUpdate_Click: Не удалось получить PipeCounter из базы данных");
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show("Не удалось найти текущий номер трубы в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    txtCurrentPipe.Content = string.Empty;
                                });
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MainWindow: btnResumeUpdate_Click: Ошибка получения PipeCounter: {ex.Message}");
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Ошибка при получении номера трубы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            txtCurrentPipe.Content = "Ошибка";
                        });
                        return;
                    }
                }

                // Устанавливаем _selectedPipeCounter и обновляем график
                Dispatcher.Invoke(() =>
                {
                    _selectedPipeCounter = _currentPipeCounter;
                    txtPipeCounter.Text = _currentPipeCounter.HasValue ? _currentPipeCounter.Value.ToString() : string.Empty;
                    txtCurrentPipe.Content = _currentPipeCounter.HasValue ? _currentPipeCounter.Value.ToString() : string.Empty;
                    Debug.WriteLine($"MainWindow: btnResumeUpdate_Click: Установлен _selectedPipeCounter={_selectedPipeCounter}, вызываем btnShowGraph_Click");
                    btnShowGraph_Click(null, null);
                });

                MessageBox.Show("Обновление текущей трубы возобновлено.");
            }
            else
            {
                MessageBox.Show("Обновление уже активно.");
            }
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Process.Start("explorer.exe", lastFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытия папки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка открытия папки: {ex.Message}");
            }
        }

        private async Task UpdateNomenclatureAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                SELECT TOP 1
                pn.pipe_diameter, 
                pn.pipe_wall, 
                psc.StrengthClass, 
                pt.pipe_thread
                FROM PipeNomenclature.dbo.PipeNomenclature pn
                INNER JOIN PipeNomenclature.dbo.Product_Nomenclature pn2 ON pn2.PipeNom_id = pn.id
                INNER JOIN PipeNomenclature.dbo.pipeStrengthClass psc ON pn2.StrClass_id = psc.id
                INNER JOIN PipeNomenclature.dbo.Product p ON p.ProductNomencl_id = pn2.id
                INNER JOIN PipeNomenclature.dbo.PipeThread pt ON p.Thread_id = pt.id
                INNER JOIN PipeNomenclature.dbo.Productivity pr ON pr.Product_id = p.id
                INNER JOIN PipeNomenclature.dbo.ShiftTask st ON st.productivity_id = pr.id
                INNER JOIN Pilot.dbo.MuftN3_REP mnr ON mnr.Product_id = p.id;";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                double diameter = reader.GetDouble(0);
                                double wall = reader.GetDouble(1);
                                string strength = reader.GetString(2);
                                string thread = reader.IsDBNull(3) ? "N/A" : reader.GetString(3);
                                string nomenclature = $"Диаметр: {diameter} мм, Стенка: {wall} мм, Группа прочности: {strength}, Резьба: {thread}";
                                Dispatcher.Invoke(() => lblNomenclature.Text = nomenclature);
                            }
                            else
                            {
                                Dispatcher.Invoke(() => lblNomenclature.Text = "Сменное задание изменилось, запись пока не добавлена так как труба еще не поступила");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления номенклатуры: {ex.Message}");
                Dispatcher.Invoke(() => lblNomenclature.Text = "Ошибка загрузки");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _comController.StateChanged -= ComController_StateChanged;
            _operatorService.OnOperatorChanged -= OperatorService_OnOperatorChanged;

            _cts?.Cancel();
            _operatorUpdateCts?.Cancel();
            _allStatsUpdateCts?.Cancel();
            _currentPipeUpdateCts?.Cancel();
            _connectionCheckCts?.Cancel();
            _graphUpdateCts?.Cancel();
            _torqueUpdateCts?.Cancel();

            if (_comStatusTimer != null)
            {
                _comStatusTimer.Stop();
                Debug.WriteLine("MainWindow: Таймер остановлен при закрытии окна.");
                _comStatusTimer = null;
            }

            try
            {
                _comController.IsReading = false;
                _comController.Dispose();
                Debug.WriteLine("MainWindow: COMController завершён.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Ошибка при закрытии COM-порта: {ex.Message}");
            }

            try
            {
                if (_plcUpdateTask != null)
                    await Task.WhenAny(_plcUpdateTask, Task.Delay(2000));
                if (_operatorUpdateTask != null)
                    await Task.WhenAny(_operatorUpdateTask, Task.Delay(2000));
                if (_allStatsUpdateTask != null)
                    await Task.WhenAny(_allStatsUpdateTask, Task.Delay(2000));
                if (_currentPipeUpdateTask != null)
                    await Task.WhenAny(_currentPipeUpdateTask, Task.Delay(2000));
                Debug.WriteLine($"MainWindow: Статус задач - PLC: {_plcUpdateTask?.Status}, Operator: {_operatorUpdateTask?.Status}, Stats: {_allStatsUpdateTask?.Status}, Pipe: {_currentPipeUpdateTask?.Status}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Ошибка при завершении задач: {ex.Message}");
            }

            if (s7Client != null && s7Client.Connected)
            {
                s7Client.Disconnect();
                Debug.WriteLine("MainWindow: S7Client отключён.");
            }

            if (_plcDataWindow != null)
            {
                _plcDataWindow.Close();
                Debug.WriteLine("MainWindow: PLCDataWindow закрыт.");
            }

            _cts?.Dispose();
            _operatorUpdateCts?.Dispose();
            _allStatsUpdateCts?.Dispose();
            _currentPipeUpdateCts?.Dispose();
            _connectionCheckCts?.Dispose();
            _graphUpdateCts?.Dispose();
            _torqueUpdateCts?.Dispose();

            try
            {
                if (_conn != null && _conn.State != ConnectionState.Closed)
                    _conn.Close();
                _conn?.Dispose();
                Debug.WriteLine("MainWindow: SQL-соединение закрыто.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Ошибка при закрытии SQL-соединения: {ex.Message}");
            }
        }
    }
}