using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.SiemensPLC.Models;
using Sharp7;
using System.Globalization;
using System.Threading.Tasks;
using Bucking_Unit_App.Utilities;
using System.Windows.Controls;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using static Bucking_Unit_App.SiemensPLC.Models.SiemensPLCModels.DBAddressModel;

namespace Bucking_Unit_App.Views
{
    public partial class PLCDataWindow : Window
    {
        private readonly ReadFromPLC _plcReader;
        private readonly WriteToPLC _plcWriter;
        private readonly S7Client _s7Client;
        private readonly string _xmlFilePath = "logs/plc_parameters.xml";
        private string _currentXmlFilePath; // Поле для хранения текущего пути к XML-файлу

        public PLCDataWindow(ReadFromPLC plcReader, S7Client s7Client)
        {
            InitializeComponent();
            _plcReader = plcReader;
            _s7Client = s7Client;
            _plcWriter = new WriteToPLC(_s7Client);
            _plcReader.ValueRead += PlcReader_ValueRead;
            _currentXmlFilePath = _xmlFilePath; // Инициализация по умолчанию
            UpdateCurrentXmlFileLabel(); // Установить начальное значение Label

            // Добавление адресов для чтения и записи
            var parameters = new[]
            {
                "TorqueUpperLimitHMI", "IdleTorqueHMI","StopTorqueHMI",
                "TorqueLowerLimitHMI", "QuantityHMI", "StartingTorqueHMI",
                "CuringRotationCount", "StatusValue", "StatusTime",
                "FeedDelayTimeHMI", "ReturnDelayTimeHMI"
            };

            foreach (var param in parameters)
            {
                _plcReader.AddAddress(param, CreateAddressModel(param, _s7Client));
                _plcWriter.AddAddress(param, CreateAddressModel(param, _s7Client));
            }

            this.Closing += PLCDataWindow_Closing;
            Directory.CreateDirectory("logs"); // Убедимся, что папка logs существует
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
                default: throw new ArgumentException($"Неизвестный параметр: {param}");
            }
        }

        private void PlcReader_ValueRead(object sender, (string AddressKey, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType Result) e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var label = FindName($"lbl{e.AddressKey}") as System.Windows.Controls.Label;
                    if (label != null)
                    {
                        if (e.Result is SiemensPLCModels.PLCReadWriteModel.PLCFloatResult floatResult)
                        {
                            label.Content = floatResult.value.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            label.Content = e.Result?.ToString() ?? "N/A";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка обновления интерфейса для {e.AddressKey}: {ex.Message}");
                }
            });
        }

        private async void btnReadParameters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_s7Client.Connected)
                {
                    MessageBox.Show("Нет подключения к ПЛК. Пожалуйста, проверьте соединение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("PLCDataWindow: Попытка чтения без подключения к ПЛК.");
                    return;
                }

                var parameters = new[]
                {
                    "TorqueUpperLimitHMI", "IdleTorqueHMI","StopTorqueHMI",
                    "TorqueLowerLimitHMI", "QuantityHMI", "StartingTorqueHMI",
                    "CuringRotationCount", "StatusValue", "StatusTime",
                    "FeedDelayTimeHMI", "ReturnDelayTimeHMI"
                };

                // Чтение параметров с ПЛК
                var results = await _plcReader.ReadMultipleAsync(parameters);
                var parameterValues = new List<Parameter>();

                // Сохранение в XML
                foreach (var param in parameters)
                {
                    var result = results[param];
                    if (result is SiemensPLCModels.PLCReadWriteModel.PLCFloatResult floatResult)
                    {
                        parameterValues.Add(new Parameter { Name = param, Value = floatResult.value });
                    }
                    else
                    {
                        parameterValues.Add(new Parameter { Name = param, Value = null });
                        System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Не удалось прочитать {param}: {result.GetType().Name}");
                    }
                }

                SaveParametersToXml(parameterValues, _xmlFilePath);
                _currentXmlFilePath = _xmlFilePath; // Обновляем текущий путь
                UpdateCurrentXmlFileLabel(); // Обновляем Label
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Параметры сохранены в {_xmlFilePath}");

                // Обновление интерфейса из XML
                var loadedParameters = LoadParametersFromXml(_xmlFilePath);
                Dispatcher.Invoke(() =>
                {
                    foreach (var param in loadedParameters)
                    {
                        var label = FindName($"lbl{param.Name}") as Label;
                        var textBox = FindName($"txt{param.Name}") as TextBox;
                        if (label != null)
                        {
                            label.Content = param.Value.HasValue ? param.Value.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
                        }
                        if (textBox != null)
                        {
                            textBox.Text = param.Value.HasValue ? param.Value.Value.ToString(CultureInfo.InvariantCulture) : "";
                        }
                    }
                });

                MessageBox.Show($"Параметры успешно считаны и сохранены в {_xmlFilePath}.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении параметров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка при чтении параметров: {ex.Message}");
            }
        }

        private async void btnWriteParameters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_s7Client.Connected)
                {
                    MessageBox.Show("Нет подключения к ПЛК. Пожалуйста, проверьте соединение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("PLCDataWindow: Попытка записи параметров без подключения к ПЛК.");
                    return;
                }

                var parameters = new[]
                {
                    "TorqueUpperLimitHMI", "IdleTorqueHMI","StopTorqueHMI",
                    "TorqueLowerLimitHMI", "QuantityHMI", "StartingTorqueHMI",
                    "CuringRotationCount", "StatusValue", "StatusTime",
                    "FeedDelayTimeHMI", "ReturnDelayTimeHMI"
                };

                var parameterValues = new List<Parameter>();
                bool allValid = true;

                // Сбор значений из текстовых полей и валидация
                foreach (var param in parameters)
                {
                    var textBox = FindName($"txt{param}") as TextBox;
                    if (textBox == null)
                    {
                        MessageBox.Show($"Поле ввода для {param} не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        allValid = false;
                        break;
                    }

                    if (float.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                    {
                        parameterValues.Add(new Parameter { Name = param, Value = value });
                    }
                    else
                    {
                        MessageBox.Show($"Пожалуйста, введите корректное числовое значение для {param} (используйте точку как разделитель).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Некорректное значение {param}: '{textBox.Text}'");
                        allValid = false;
                        break;
                    }
                }

                if (!allValid)
                {
                    return;
                }

                // Сохранение в XML
                SaveParametersToXml(parameterValues, _xmlFilePath);
                _currentXmlFilePath = _xmlFilePath; // Обновляем текущий путь
                UpdateCurrentXmlFileLabel(); // Обновляем Label
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Параметры сохранены в {_xmlFilePath} перед записью в ПЛК");

                // Запись в ПЛК из XML
                var loadedParameters = LoadParametersFromXml(_xmlFilePath);
                bool allWritten = true;

                foreach (var param in loadedParameters)
                {
                    if (param.Value.HasValue)
                    {
                        int result = await _plcWriter.WriteAsync(param.Name, param.Value.Value).TimeoutAfter(5000);
                        if (result != 1)
                        {
                            string errorText;
                            switch (result)
                            {
                                case -1:
                                    errorText = "Нет соединения с ПЛК";
                                    break;
                                case -2:
                                    errorText = "Адрес не найден";
                                    break;
                                case -3:
                                    errorText = "Исключение при записи";
                                    break;
                                default:
                                    errorText = _s7Client.ErrorText(result);
                                    break;
                            }
                            MessageBox.Show($"Ошибка записи {param.Name} в ПЛК: Код {result} ({errorText})", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка записи {param.Name}, код: {result}, текст: {errorText}");
                            allWritten = false;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Успешная запись {param.Name} = {param.Value.Value}");
                        }
                    }
                }

                if (allWritten)
                {
                    MessageBox.Show($"Все параметры успешно записаны в ПЛК из {_xmlFilePath}.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Ошибка: Операция записи параметров в ПЛК превысила время ожидания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("PLCDataWindow: Таймаут при записи параметров.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи параметров в ПЛК: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Исключение при записи параметров: {ex.Message}");
            }
        }

        private void btnSaveParameters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = new[]
                {
                    "TorqueUpperLimitHMI", "IdleTorqueHMI","StopTorqueHMI",
                    "TorqueLowerLimitHMI", "QuantityHMI", "StartingTorqueHMI",
                    "CuringRotationCount", "StatusValue", "StatusTime",
                    "FeedDelayTimeHMI", "ReturnDelayTimeHMI"
                };

                var parameterValues = new List<Parameter>();
                bool allValid = true;

                // Сбор значений из текстовых полей и валидация
                foreach (var param in parameters)
                {
                    var textBox = FindName($"txt{param}") as TextBox;
                    if (textBox == null)
                    {
                        MessageBox.Show($"Поле ввода для {param} не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        allValid = false;
                        break;
                    }

                    if (float.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                    {
                        parameterValues.Add(new Parameter { Name = param, Value = value });
                    }
                    else
                    {
                        parameterValues.Add(new Parameter { Name = param, Value = null });
                    }
                }

                if (!allValid)
                {
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "XML-файлы (*.xml)|*.xml",
                    Title = "Сохранить параметры",
                    FileName = $"PLC_Parameters_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    SaveParametersToXml(parameterValues, saveFileDialog.FileName);
                    _currentXmlFilePath = saveFileDialog.FileName; // Обновляем текущий путь
                    UpdateCurrentXmlFileLabel(); // Обновляем Label
                    MessageBox.Show($"Параметры успешно сохранены в {saveFileDialog.FileName}.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Параметры сохранены в {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении параметров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка при сохранении параметров: {ex.Message}");
            }
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

        private void PLCDataWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
            _plcReader.ValueRead -= PlcReader_ValueRead;
            System.Diagnostics.Debug.WriteLine("PLCDataWindow: Закрытие окна.");
        }

        private bool IsClosed { get; set; }

        [Serializable]
        public class Parameter
        {
            public string Name { get; set; }
            public float? Value { get; set; }
        }

        private void SaveParametersToXml(List<Parameter> parameters, string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Parameter>));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, parameters);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка сохранения параметров в XML: {ex.Message}");
                throw;
            }
        }

        private List<Parameter> LoadParametersFromXml(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Файл {filePath} не существует.");
                    return new List<Parameter>();
                }

                var serializer = new XmlSerializer(typeof(List<Parameter>));
                using (var reader = new StreamReader(filePath))
                {
                    return (List<Parameter>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PLCDataWindow: Ошибка загрузки параметров из XML: {ex.Message}");
                return new List<Parameter>();
            }
        }

        private void UpdateCurrentXmlFileLabel()
        {
            Dispatcher.Invoke(() =>
            {
                lblCurrentXmlFile.Content = $"Текущий XML-файл: {_currentXmlFilePath ?? "Не выбран"}";
            });
        }
    }
}