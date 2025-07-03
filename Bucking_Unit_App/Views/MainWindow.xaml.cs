using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics; // Для Process.Start
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
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Windows.Media.Imaging;

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
        private CancellationTokenSource _allStatsUpdateCts; // Новый токен для общей статистики
        private int? _selectedPipeCounter = null;
        private int _selectedYear = DateTime.Now.Year; // Значение по умолчанию
        private int? _currentPipeCounter = null; // Для хранения текущей трубы
        private readonly string _runtimeConnectionString = "Data Source=192.168.11.222,1433;Initial Catalog=Runtime;User ID=UserNotTrend;Password=NotTrend";
        private CancellationTokenSource _currentPipeUpdateCts; // Поле для управления циклом текущей трубы

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
            StartCurrentPipeUpdateLoop(); // Запускаем цикл обновления текущей трубы
            StartAllStatsUpdateLoop(); // Запускаем цикл обновления общей статистики
        }

        private void txtPipeCounter_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Запускаем экранную клавиатуру с запросом повышения привилегий
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "osk.exe",
                    UseShellExecute = true, // Необходимо для Verb
                    Verb = "runas" // Запрос повышения привилегий
                };
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть экранную клавиатуру: {ex.Message}");
            }
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
                // Автоматически обновляем график при выборе трубы
                btnShowGraph_Click(null, null);
            }
        }

        private void cbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbYear.SelectedItem is ComboBoxItem item)
            {
                _selectedYear = int.Parse(item.Content.ToString());
                Console.WriteLine($"Selected year: {_selectedYear}");
                // Автоматически обновляем график при изменении года
                if (_selectedPipeCounter.HasValue)
                    btnShowGraph_Click(null, null);
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
                    lblStatus.Visibility = Visibility.Collapsed; // Скрываем статус при сбросе
                    _currentPipeCounter = null;
                    //_selectedPipeCounter = null;
                    operatorDataPanel.Visibility = Visibility.Collapsed;
                    statsDataPanel.Visibility = Visibility.Collapsed;
                    txtInsertCardPrompt.Visibility = Visibility.Visible; // Показываем подсказку для оператора
                    txtNoStatsPrompt.Visibility = Visibility.Visible; // Показываем подсказку для статистики
                    lblIdCard.Content = string.Empty;
                    lblTabNumber.Content = string.Empty;
                    lblFIO.Content = string.Empty;
                    lblDepartment.Text = string.Empty;
                    lblEmployName.Content = string.Empty;
                    lblShiftItems.Content = string.Empty;
                    lblShiftDowntime.Content = string.Empty;
                    lblMonthItems.Content = string.Empty;
                    lblMonthDowntime.Content = string.Empty;
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
                    operatorDataPanel.Visibility = Visibility.Collapsed;
                    statsDataPanel.Visibility = Visibility.Collapsed;
                    txtInsertCardPrompt.Visibility = Visibility.Visible; // Показываем подсказку для оператора
                    txtNoStatsPrompt.Visibility = Visibility.Visible; // Показываем подсказку для статистики
                }
                else
                {
                    lblIdCard.Content = operatorInfo?.CardNumber ?? string.Empty;
                    lblTabNumber.Content = operatorInfo?.PersonnelNumber ?? string.Empty;
                    lblFIO.Content = operatorInfo?.FullName ?? string.Empty;
                    lblDepartment.Text = operatorInfo?.Department ?? string.Empty;
                    lblEmployName.Content = operatorInfo?.Position ?? string.Empty;
                    txtInsertCardPrompt.Visibility = Visibility.Collapsed; // Скрываем подсказку для оператора
                    operatorDataPanel.Visibility = Visibility.Visible; // Показываем данные оператора
                    // Статистика обновляется в StartOperatorUpdateLoop
                }
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
                            if (string.IsNullOrEmpty(shiftItems) || string.IsNullOrEmpty(shiftDowntime) || string.IsNullOrEmpty(monthItems) || string.IsNullOrEmpty(monthDowntime))
                            {
                                txtNoStatsPrompt.Visibility = Visibility.Visible;
                                statsDataPanel.Visibility = Visibility.Collapsed;
                                lblShiftItems.Content = string.Empty;
                                lblShiftDowntime.Content = string.Empty;
                                lblMonthItems.Content = string.Empty;
                                lblMonthDowntime.Content = string.Empty;
                            }
                            else
                            {
                                txtNoStatsPrompt.Visibility = Visibility.Collapsed;
                                lblShiftItems.Content = shiftItems;
                                lblShiftDowntime.Content = shiftDowntime;
                                lblMonthItems.Content = monthItems;
                                lblMonthDowntime.Content = monthDowntime;
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
            _currentPipeUpdateCts?.Dispose(); // Очищаем предыдущий, если есть
            _currentPipeUpdateCts = new CancellationTokenSource();
            var token = _currentPipeUpdateCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await UpdateCurrentPipeCounter();
                    await Task.Delay(5000, token); // Обновление каждые 5 секунд
                }
            }, token);
        }

        private void StartAllStatsUpdateLoop()
        {
            _allStatsUpdateCts?.Dispose(); // Очищаем предыдущий, если есть
            _allStatsUpdateCts = new CancellationTokenSource();
            var token = _allStatsUpdateCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await _statsService.UpdateStatsForAllOperatorsAsync((dailyDowntime, monthlyDowntime, dailyOps, monthlyOps) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            decimal totalShiftDowntime = 0;
                            decimal totalMonthDowntime = 0;
                            int totalShiftItems = 0;
                            int totalMonthItems = 0;

                            foreach (var kvp in dailyDowntime)
                            {
                                totalShiftDowntime += kvp.Value.IsDayShift ? kvp.Value.DayShiftDowntime : kvp.Value.NightShiftDowntime;
                            }
                            foreach (var kvp in monthlyDowntime)
                            {
                                totalMonthDowntime += kvp.Value.IsDayShift ? kvp.Value.DayShiftDowntime : kvp.Value.NightShiftDowntime;
                            }
                            foreach (var kvp in dailyOps)
                            {
                                totalShiftItems += kvp.Value.ShiftOperationCount;
                            }
                            foreach (var kvp in monthlyOps)
                            {
                                totalMonthItems += kvp.Value.ShiftOperationCount;
                            }

                            lblAllShiftItems.Content = totalShiftItems.ToString();
                            lblAllShiftDowntime.Content = totalShiftDowntime.ToString("F2");
                            lblAllMonthItems.Content = totalMonthItems.ToString();
                            lblAllMonthDowntime.Content = totalMonthDowntime.ToString("F2");
                        });
                    });
                    await Task.Delay(5000, token); // Обновление каждые 5 секунд
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
                    var cmd = new SqlCommand("SELECT [Value] FROM [Runtime].[dbo].[v_Live] WHERE [TagName] = 'NOT_MN3_TUBE_NUM'", conn);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int pipeCounter))
                    {
                        // Обновляем _currentPipeCounter и отображаем сразу
                        Dispatcher.Invoke(() =>
                        {
                            _currentPipeCounter = pipeCounter;
                            txtCurrentPipe.Content = $"Pipe {_currentPipeCounter}";
                        });

                        // Проверяем наличие StartDateTime и EndDateTime в MuftN3_REP
                        using (var pilotConn = new SqlConnection(_connectionString))
                        {
                            await pilotConn.OpenAsync();
                            var checkCmd = new SqlCommand(
                                "SELECT COUNT(*) FROM [Pilot].[dbo].[MuftN3_REP] WHERE PipeCounter = @PipeCounter AND StartDateTime IS NOT NULL AND EndDateTime IS NOT NULL",
                                pilotConn);
                            checkCmd.Parameters.AddWithValue("@PipeCounter", pipeCounter);
                            var count = (int)await checkCmd.ExecuteScalarAsync();

                            Dispatcher.Invoke(() =>
                            {
                                if (count > 0)
                                {
                                    // Скрываем статус, так как данные доступны
                                    lblStatus.Visibility = Visibility.Collapsed;
                                    // Обновляем _selectedPipeCounter и строим график, если труба изменилась
                                    if (_selectedPipeCounter != _currentPipeCounter)
                                    {
                                        _selectedPipeCounter = _currentPipeCounter;
                                        txtPipeCounter.Text = _currentPipeCounter.HasValue ? $"Pipe {_currentPipeCounter.Value}" : string.Empty;
                                        btnShowGraph_Click(null, null);
                                    }
                                }
                                else
                                {
                                    // Показываем статус, если данные отсутствуют
                                    lblStatus.Visibility = Visibility.Visible;
                                    if (_selectedPipeCounter != null && _selectedPipeCounter != pipeCounter)
                                    {
                                        _selectedPipeCounter = null;
                                        txtPipeCounter.Text = string.Empty;
                                        canvasGraph.Children.Clear();
                                    }
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Ошибка при обновлении текущей трубы: {ex.Message}"));
            }
        }

        private async void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_selectedPipeCounter.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите или укажите текущую трубу.");
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
                        new StepLineSeries<ObservablePoint, CircleGeometry> // Используем StepLineSeries для ступенчатого графика
                        {
                            Values = torquePoints,
                            Name = "Крутящий момент",
                            XToolTipLabelFormatter = (chartPoint) =>
                            {
                                double? torqueValueNullable = chartPoint.Model.Y;
                                double torqueValue = torqueValueNullable ?? 0.0;
                                return torqueValue.ToString("F2");
                            },
                            Stroke = new SolidColorPaint(SKColors.Red), // Цвет линии
                            Fill = new SolidColorPaint(SKColors.White), // Цвет заполнения под линией
                            GeometrySize = 0.5f, // Размер точек
                            GeometryFill = new SolidColorPaint(SKColors.Black), // Цвет заполнения точек
                            GeometryStroke = new SolidColorPaint(SKColors.Black) // Цвет обводки точек
                        }
                            },
                            XAxes = xAxes,
                            YAxes = yAxes,
                            LegendPosition = LegendPosition.Top,
                            Width = canvasGraph.ActualWidth,
                            Height = canvasGraph.ActualHeight
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
            try
            {
                // Проверяем, есть ли график для сохранения
                if (canvasGraph.Children.Count == 0 || !(canvasGraph.Children[0] is CartesianChart chart))
                {
                    MessageBox.Show("График отсутствует. Сначала создайте график.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Убедимся, что размеры графика заданы (если не заданы, используем минимальные значения)
                if (chart.Width <= 0 || chart.Height <= 0)
                {
                    chart.Width = 800; // Минимальная ширина
                    chart.Height = 600; // Минимальная высота
                }

                // Открываем диалог для выбора места сохранения (WPF-вариант)
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    Title = "Сохранить график как PDF",
                    FileName = $"Graph_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Создаем RenderTargetBitmap для рендеринга графика
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)chart.Width,
                        (int)chart.Height,
                        96, // DPI X
                        96, // DPI Y
                        PixelFormats.Pbgra32);

                    // Измеряем и располагаем элемент для рендеринга
                    chart.Measure(new Size(chart.Width, chart.Height));
                    chart.Arrange(new Rect(new Size(chart.Width, chart.Height)));
                    renderBitmap.Render(chart);

                    // Преобразуем RenderTargetBitmap в поток
                    using (var memoryStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        // Создаем PDF-документ
                        using (var pdfDocument = new PdfDocument())
                        {
                            // Добавляем новую страницу
                            var page = pdfDocument.AddPage();
                            page.Size = PdfSharpCore.PageSize.A4; // Размер страницы A4
                            var gfx = XGraphics.FromPdfPage(page);

                            // Преобразуем поток в XImage и добавляем в PDF
                            using (var xImage = XImage.FromStream(() => memoryStream))
                            {
                                // Масштабируем изображение, чтобы оно помещалось на странице
                                double scale = Math.Min(page.Width / xImage.PixelWidth, page.Height / xImage.PixelHeight);
                                double newWidth = xImage.PixelWidth * scale;
                                double newHeight = xImage.PixelHeight * scale;

                                // Центрируем изображение на странице
                                double x = (page.Width - newWidth) / 2;
                                double y = (page.Height - newHeight) / 2;

                                // Рисуем изображение на странице
                                gfx.DrawImage(xImage, x, y, newWidth, newHeight);
                            }

                            // Сохраняем PDF
                            pdfDocument.Save(saveFileDialog.FileName);
                            MessageBox.Show($"График успешно сохранен как {saveFileDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetGraph_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                canvasGraph.Children.Clear();
                _selectedPipeCounter = null;
                txtPipeCounter.Text = string.Empty;
                txtCurrentPipe.Content = "Не указано";
                lblStatus.Visibility = Visibility.Collapsed; // Скрываем статус при сбросе
                _currentPipeCounter = null;
                // Останавливаем цикл обновления текущей трубы
                _currentPipeUpdateCts?.Cancel();
                _currentPipeUpdateCts?.Dispose();
                _currentPipeUpdateCts = null;
            });
        }

        private void btnResumeUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Возобновляем цикл обновления текущей трубы
            if (_currentPipeUpdateCts == null || _currentPipeUpdateCts.IsCancellationRequested)
            {
                StartCurrentPipeUpdateLoop();
                MessageBox.Show("Обновление текущей трубы возобновлено.");
            }
            else
            {
                MessageBox.Show("Обновление уже активно.");
            }
        }
    }
}