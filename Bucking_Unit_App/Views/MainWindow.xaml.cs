using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.COM_Controller;
using System.Windows.Controls;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.Models;
using Bucking_Unit_App.Interfaces;

namespace Bucking_Unit_App.Views
{

    // MainWindow.xaml.cs
    // MainWindow.xaml.cs
    public partial class MainWindow : Window
    {
        private readonly string _connectionString = "Data Source=192.168.11.222,1433;Initial Catalog=Pilot;User ID=UserNotTrend;Password=NotTrend";
        private readonly SqlConnection _conn;
        private readonly COMController _comController;
        private readonly OperatorService _operatorService;
        private readonly StatsService _statsService;
        private CancellationTokenSource _operatorUpdateCts;
        private readonly Dictionary<int, string> _pipeCounters = new Dictionary<int, string>(); // Для хранения номеров труб

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
            LoadPipeCountersAsync(); // Асинхронная загрузка
        }

        private async void LoadPipeCountersAsync()
        {
            await Task.Run(() =>
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var cmd = new SqlCommand("SELECT DISTINCT PipeCounter FROM Pilot.dbo.MuftN3_REP WHERE PipeCounter IS NOT NULL", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int pipeCounter = reader.GetInt32(0);
                    _pipeCounters[pipeCounter] = $"Pipe {pipeCounter}";
                }
            });
            Console.WriteLine($"Loaded {_pipeCounters.Count} pipe counters.");
            Dispatcher.Invoke(() =>
            {
                cbPipeCounter.ItemsSource = _pipeCounters.Select(kvp => new KeyValuePair<int, string>(kvp.Key, kvp.Value));
                cbPipeCounter.DisplayMemberPath = "Value";
                cbPipeCounter.SelectedValuePath = "Key";
            });
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
                // Сброс статистики при удалении карты
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
                txtFIO.Text = operatorInfo?.FullName ?? "Не указано";
                txtIdCard.Text = operatorInfo?.CardNumber ?? "Не указано";
                txtTabNumber.Text = operatorInfo?.PersonnelNumber ?? "Не указано";
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

        private void cbPipeCounter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Опционально: обновление графика при смене выбора
        }

        private void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? pipeCounter = cbPipeCounter.SelectedValue as int?;
                Console.WriteLine($"Selected PipeCounter: {pipeCounter}");
                var graphService = new GraphService(_connectionString, pipeCounter);
                var graphWindow = graphService.GenerateGraphWindow();
                graphWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении графика: {ex.Message}");
            }
        }
    }
}