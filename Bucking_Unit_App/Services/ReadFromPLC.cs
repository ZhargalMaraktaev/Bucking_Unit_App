using Sharp7;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bucking_Unit_App.SiemensPLC.Models;
using static Bucking_Unit_App.SiemensPLC.Models.SiemensPLCModels.DBAddressModel;

namespace Bucking_Unit_App.Services
{
    public class ReadFromPLC
    {
        private readonly S7Client s7Client;
        private readonly Dictionary<string, object> _addresses;
        public SiemensPLCModels.DBAddressModel.TorqueUpperLimitHMI _torqueUpperLimitHMI;
        private readonly string logFilePath = "logs/plc_read.log";

        public event EventHandler<(string AddressKey, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType Result)> ValueRead;
        public event EventHandler<bool> ConnectionStateChanged;

        public ReadFromPLC(S7Client s7Client)
        {
            this.s7Client = s7Client ?? throw new ArgumentNullException(nameof(s7Client));
            _addresses = new Dictionary<string, object>();
            _torqueUpperLimitHMI = new SiemensPLCModels.DBAddressModel.TorqueUpperLimitHMI(s7Client);
            _addresses["TorqueUpperLimitHMI"] = _torqueUpperLimitHMI;
            Directory.CreateDirectory("logs");
            Log("ReadFromPLC инициализирован.");
        }

        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        public bool Connect(string ipAddress, int rack, int slot)
        {
            Log($"Подключение к PLC: {ipAddress}, Rack: {rack}, Slot: {slot}");
            int result = s7Client.ConnectTo(ipAddress, rack, slot);
            bool isConnected = result == 0;
            if (!isConnected)
                Log($"Ошибка подключения: {s7Client.ErrorText(result)}");
            ConnectionStateChanged?.Invoke(this, isConnected);
            return isConnected;
        }

        public void Disconnect()
        {
            Log("Отключение от PLC.");
            s7Client.Disconnect();
            ConnectionStateChanged?.Invoke(this, false);
        }

        public bool IsConnected => s7Client.Connected;

        public void AddAddress(string key, object address)
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
            Log($"Добавлен адрес для чтения: {key}");
        }

        public bool RemoveAddress(string key)
        {
            bool removed = _addresses.Remove(key);
            if (removed)
                Log($"Удалён адрес: {key}");
            return removed;
        }

        public SiemensPLCModels.PLCReadWriteModel.PLCModifiedType Read(string addressKey)
        {
            Log($"Чтение адреса {addressKey}");
            if (!_addresses.TryGetValue(addressKey, out var address))
            {
                Log($"Адрес {addressKey} не найден.");
                return new SiemensPLCModels.PLCReadWriteModel.PLCUnknownError();
            }

            SiemensPLCModels.PLCReadWriteModel.PLCModifiedType result;
            switch (address)
            {
                case SiemensPLCModels.DBAddressModel.DbbDbwDbdAddress dbAddress:
                    result = dbAddress.Read();
                    break;
                case SiemensPLCModels.DBAddressModel.DbxAddress dbxAddress:
                    result = dbxAddress.Read();
                    break;
                case SiemensPLCModels.DBAddressModel.DbxStringAddress dbxStringAddress:
                    result = dbxStringAddress.Read();
                    break;
                case SiemensPLCModels.DBAddressModel.DbwStringAddress dbwStringAddress:
                    result = dbwStringAddress.Read();
                    break;
                case SiemensPLCModels.MAddressModel.XAddress mAddress:
                    result = mAddress.Read();
                    break;
                case SiemensPLCModels.MAddressModel.XStringAddress mStringAddress:
                    result = mStringAddress.Read();
                    break;
                case SiemensPLCModels.MAddressModel.BWDAddress bwdAddress:
                    result = bwdAddress.Read();
                    break;
                default:
                    Log($"Неподдерживаемый тип адреса для {addressKey}");
                    return new SiemensPLCModels.PLCReadWriteModel.PLCUnknownError();
            }

            ValueRead?.Invoke(this, (addressKey, result));
            if (result is SiemensPLCModels.PLCReadWriteModel.PLCFloatResult floatResult)
                Log($"Успешно прочитано значение {floatResult.value} из адреса {addressKey}");
            else if (result is SiemensPLCModels.PLCReadWriteModel.PLCNoConnection)
                Log($"Нет соединения с PLC при чтении адреса {addressKey}");
            else
                Log($"Ошибка чтения адреса {addressKey}: {result.GetType().Name}");

            return result;
        }

        public async Task<SiemensPLCModels.PLCReadWriteModel.PLCModifiedType> ReadAsync(string addressKey, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Read(addressKey), cancellationToken);
        }

        public async Task<Dictionary<string, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType>> ReadMultipleAsync(IEnumerable<string> addressKeys, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType>();
            var multiVar = new S7MultiVar(s7Client);
            var validAddresses = new List<(string Key, SiemensPLCModels.DBAddressModel.DbbDbwDbdAddress Address, byte[] Buffer)>();

            foreach (var key in addressKeys)
            {
                if (_addresses.TryGetValue(key, out var address))
                {
                    if (address is SiemensPLCModels.DBAddressModel.DbbDbwDbdAddress dbAddress)
                    {
                        byte[] buffer = new byte[4];
                        int dbNumber = key == "TorqueUpperLimitHMI" ? 23 : key == "Torque" ? 23 : 0;
                        int offset = key == "TorqueUpperLimitHMI" ? 8 : key == "Torque" ? 12 : 0;
                        multiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, dbNumber, offset, 4, ref buffer);
                        validAddresses.Add((key, dbAddress, buffer));
                        results[key] = null;
                    }
                    else
                    {
                        results[key] = await ReadAsync(key, cancellationToken);
                    }
                }
                else
                {
                    results[key] = new SiemensPLCModels.PLCReadWriteModel.PLCUnknownError();
                    Log($"Адрес {key} не найден для множественного чтения.");
                }
            }

            if (validAddresses.Any())
            {
                int result = await Task.Run(() => multiVar.Read(), cancellationToken);
                if (result == 0)
                {
                    foreach (var (key, address, buffer) in validAddresses)
                    {
                        results[key] = address.Read();
                        ValueRead?.Invoke(this, (key, results[key]));
                        Log($"Успешно прочитано значение из адреса {key}");
                    }
                }
                else
                {
                    foreach (var key in validAddresses.Select(a => a.Key))
                    {
                        results[key] = new SiemensPLCModels.PLCReadWriteModel.PLCNoConnection();
                        Log($"Ошибка множественного чтения для адреса {key}: {s7Client.ErrorText(result)}");
                    }
                }
            }

            return results;
        }

        public async Task StartPeriodicReadAsync(TimeSpan interval, Action<string, SiemensPLCModels.PLCReadWriteModel.PLCModifiedType> onValueRead, CancellationToken cancellationToken = default)
        {
            Log($"Запуск периодического чтения с интервалом {interval.TotalMilliseconds} мс");
            while (!cancellationToken.IsCancellationRequested)
            {
                var results = await ReadMultipleAsync(_addresses.Keys, cancellationToken);
                foreach (var result in results)
                {
                    onValueRead?.Invoke(result.Key, result.Value);
                }
                await Task.Delay(interval, cancellationToken);
            }
            Log("Периодическое чтение остановлено.");
        }
    }
}
