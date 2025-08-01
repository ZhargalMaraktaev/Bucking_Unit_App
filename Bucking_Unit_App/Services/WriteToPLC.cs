using Sharp7;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bucking_Unit_App.SiemensPLC.Models;

namespace Bucking_Unit_App.Services
{
    public class WriteToPLC
    {
        private readonly S7Client s7Client;
        private readonly Dictionary<string, object> _addresses;
        private readonly string logFilePath = "logs/plc_write.log";
        private static readonly object _lock = new object();

        public event EventHandler<bool> ConnectionStateChanged;

        public WriteToPLC(S7Client s7Client)
        {
            this.s7Client = s7Client ?? throw new ArgumentNullException(nameof(s7Client));
            _addresses = new Dictionary<string, object>();
            Directory.CreateDirectory("logs");
            Log("WriteToPLC инициализирован.");
        }

        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        public bool Connect(string ipAddress, int rack, int slot)
        {
            lock (_lock)
            {
                Log($"Подключение к PLC: {ipAddress}, Rack: {rack}, Slot: {slot}");
                int result = s7Client.ConnectTo(ipAddress, rack, slot);
                bool isConnected = result == 0;
                if (!isConnected)
                    Log($"Ошибка подключения: {s7Client.ErrorText(result)}");
                ConnectionStateChanged?.Invoke(this, isConnected);
                return isConnected;
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                Log("Отключение от PLC.");
                s7Client.Disconnect();
                ConnectionStateChanged?.Invoke(this, false);
            }
        }

        public bool IsConnected => s7Client.Connected;

        public void AddAddress(string key, object address)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Ключ адреса не может быть пустым.", nameof(key));
                if (address == null)
                    throw new ArgumentNullException(nameof(address));
                if (!(address is SiemensPLCModels.DBAddressModel.DbbDbwDbdAddress ||
                      address is SiemensPLCModels.DBAddressModel.DbxAddress ||
                      address is SiemensPLCModels.DBAddressModel.DbxStringAddress ||
                      address is SiemensPLCModels.DBAddressModel.DbwStringAddress ||
                      address is SiemensPLCModels.MAddressModel.XAddress ||
                      address is SiemensPLCModels.MAddressModel.XStringAddress ||
                      address is SiemensPLCModels.MAddressModel.BWDAddress))
                    throw new ArgumentException("Неподдерживаемый тип адреса.", nameof(address));
                _addresses[key] = address;
                Log($"Добавлен адрес для записи: {key}");
            }
        }

        public bool RemoveAddress(string key)
        {
            lock (_lock)
            {
                bool removed = _addresses.Remove(key);
                if (removed)
                    Log($"Удалён адрес: {key}");
                return removed;
            }
        }

        public int Write<T>(string addressKey, T value)
        {
            lock (_lock)
            {
                Log($"Запись значения {value} в адрес {addressKey}, соединение: {s7Client.Connected}");
                if (!_addresses.TryGetValue(addressKey, out var address))
                {
                    Log($"Адрес {addressKey} не найден.");
                    return -2;
                }

                int result;
                try
                {
                    switch (address)
                    {
                        case SiemensPLCModels.DBAddressModel.DbbDbwDbdAddress dbAddress:
                            result = dbAddress.Write(value);
                            break;
                        case SiemensPLCModels.DBAddressModel.DbxAddress dbxAddress:
                            result = dbxAddress.Write(value);
                            break;
                        case SiemensPLCModels.DBAddressModel.DbxStringAddress dbxStringAddress:
                            result = dbxStringAddress.Write(value);
                            break;
                        case SiemensPLCModels.DBAddressModel.DbwStringAddress dbwStringAddress:
                            result = dbwStringAddress.Write(value);
                            break;
                        case SiemensPLCModels.MAddressModel.XAddress mAddress:
                            result = mAddress.Write(value);
                            break;
                        case SiemensPLCModels.MAddressModel.XStringAddress mStringAddress:
                            result = mStringAddress.Write(value);
                            break;
                        case SiemensPLCModels.MAddressModel.BWDAddress bwdAddress:
                            result = bwdAddress.Write(value);
                            break;
                        default:
                            Log($"Неподдерживаемый тип адреса для {addressKey}");
                            return -2;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Исключение при записи в адрес {addressKey}: {ex.Message}");
                    return -3;
                }

                if (result == 1)
                    Log($"Успешно записано значение {value} в адрес {addressKey}");
                else
                    Log($"Ошибка записи в адрес {addressKey}: Код {result}");
                return result;
            }
        }

        public async Task<int> WriteAsync<T>(string addressKey, T value, CancellationToken cancellationToken = default)
        {
            int maxRetries = 3;
            int retryDelayMs = 500;
            for (int i = 0; i < maxRetries; i++)
            {
                int result = await Task.Run(() => Write(addressKey, value), cancellationToken);
                if (result == 1) // Успешная запись
                    return result;
                Log($"Попытка записи {i + 1}/{maxRetries} не удалась для {addressKey}, код: {result}");
                await Task.Delay(retryDelayMs, cancellationToken);
            }
            Log($"Не удалось записать значение в {addressKey} после {maxRetries} попыток.");
            return -3;
        }
    }
}