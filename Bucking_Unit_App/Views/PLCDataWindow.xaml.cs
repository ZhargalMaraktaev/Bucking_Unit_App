using System;
using System.Windows;
using System.Windows.Media;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.SiemensPLC.Models;
using Sharp7;
using System.Globalization;
using System.Threading.Tasks;
using Bucking_Unit_App.Utilities; // Добавлено для TaskExtensions

namespace Bucking_Unit_App.Views
{
    public partial class PLCDataWindow : Window
    {
        private readonly ReadFromPLC _plcReader;
        private readonly WriteToPLC _plcWriter;
        private readonly S7Client _s7Client; // Используем переданный S7Client

        public PLCDataWindow(ReadFromPLC plcReader, S7Client s7Client)
        {
            InitializeComponent();
            _plcReader = plcReader;
            _s7Client = s7Client; // Используем переданный S7Client
            _plcWriter = new WriteToPLC(_s7Client); // Передаём S7Client в WriteToPLC
            _plcReader.ValueRead += PlcReader_ValueRead;

            // Подключение к PLC (если ещё не подключено)
            TryConnectToPLC();
            UpdateConnectionStatus();

            // Добавление адреса для записи
            _plcWriter.AddAddress("TorqueSetpoint", new SiemensPLCModels.DBAddressModel.TorqueUpperLimitHMI(_s7Client));

            // Запуск периодической проверки состояния подключения
            StartConnectionStatusCheck();

            this.Closing += PLCDataWindow_Closing;
        }

        private void TryConnectToPLC()
        {
            if (!_s7Client.Connected)
            {
                System.Diagnostics.Debug.WriteLine("PLCDataWindow: Попытка подключения к PLC...");
                int result = _s7Client.ConnectTo("192.168.11.241", 0, 1);
                if (result != 0)
                {
                    string errorText = _s7Client.ErrorText(result);
                    MessageBox.Show($"Ошибка подключения к PLC: {errorText}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка подключения к PLC, код: {result}, текст: {errorText}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PLCDataWindow: Успешное подключение к PLC.");
                }
            }
            UpdateConnectionStatus();
        }

        private async void StartConnectionStatusCheck()
        {
            while (!IsClosed)
            {
                if (!_s7Client.Connected)
                {
                    TryConnectToPLC();
                }
                UpdateConnectionStatus();
                await Task.Delay(1000); // Проверяем каждую секунду
            }
        }

        private bool IsClosed { get; set; }

        private void PlcReader_ValueRead(object sender, (string AddressKey, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType Result) e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.AddressKey == "TorqueUpperLimitHMI")
                {
                    lblFrontClamp.Content = e.Result?.ToString() ?? "N/A";
                }
                else if (e.AddressKey == "Torque")
                {
                    lblActualTorque.Content = e.Result?.ToString() ?? "N/A";
                }
            });
        }

        private void UpdateConnectionStatus()
        {
            Dispatcher.Invoke(() =>
            {
                if (_s7Client.Connected)
                {
                    lblConnectionStatus.Content = "Подключено к ПЛК";
                    lblConnectionStatus.Foreground = Brushes.Green;
                }
                else
                {
                    lblConnectionStatus.Content = "Ошибка подключения к ПЛК";
                    lblConnectionStatus.Foreground = Brushes.Red;
                }
            });
        }

        private async void btnWriteTorque_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем и пытаемся восстановить соединение перед записью
                if (!_s7Client.Connected)
                {
                    TryConnectToPLC();
                    if (!_s7Client.Connected)
                    {
                        MessageBox.Show("Нет подключения к PLC. Пожалуйста, проверьте соединение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Diagnostics.Debug.WriteLine("PLCDataWindow: Попытка записи без подключения к PLC.");
                        return;
                    }
                }

                // Используем CultureInfo.InvariantCulture для корректного парсинга чисел
                if (float.TryParse(txtTorqueSetpoint.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out float torqueValue))
                {
                    int result = await _plcWriter.WriteAsync("TorqueSetpoint", torqueValue).TimeoutAfter(5000);
                    if (result == 1) // Код 0 в Sharp7 указывает на успех
                    {
                        MessageBox.Show("Значение TorqueSetpoint успешно записано в PLC.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Успешная запись TorqueSetpoint = {torqueValue}");
                    }
                    else
                    {
                        string errorText = _s7Client.ErrorText(result);
                        MessageBox.Show($"Ошибка записи в PLC: Код {result} ({errorText})", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка записи TorqueSetpoint, код: {result}, текст: {errorText}");
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, введите корректное числовое значение для TorqueSetpoint (используйте точку как разделитель).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Некорректное значение TorqueSetpoint: '{txtTorqueSetpoint.Text}'");
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Ошибка: Операция записи в PLC превысила время ожидания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("PLCDataWindow: Таймаут при записи в PLC.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи в PLC: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Исключение при записи в PLC: {ex.Message}");
            }
        }

        private void PLCDataWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
            _plcReader.ValueRead -= PlcReader_ValueRead;
            if (_s7Client.Connected)
            {
                _s7Client.Disconnect();
                System.Diagnostics.Debug.WriteLine("PLCDataWindow: S7Client отключён.");
            }
        }
    }
}