// Подключаемые пространства имён
using System.Collections.Concurrent; // Для потокобезопасной очереди сообщений
using System.Diagnostics; // Для записи в журнал событий Windows
using System.IO.Ports; // Для работы с COM-портами
using System.Text; // Для работы с буфером строк
using System.Text.RegularExpressions; // Для поиска шаблонов в строках

namespace Bucking_Unit_App.COM_Controller
{
    // Класс для управления COM-считывателем
    public class COMController
    {
        // Событие, которое вызывается при изменении состояния считывателя
        public event EventHandler<COMEventArgs.ReadingDataEventArgs>? StateChanged;

        // Параметры подключения к COM-порту
        private COMControllerParamsModel ComControllerParamsModel { get; }

        // Время между попытками переподключения
        public int TimeToReconnect { get; set; } = 500;

        // Свойство, активна ли сейчас операция чтения
        public bool IsReading
        {
            get { return isReading; }
            set
            {
                if (isReading == value)
                    return;

                if (value)
                {
                    // Если не удалось инициализировать порт — выдаём ошибку
                    if (!InitializeSerialPort())
                    {
                        isReading = false;

                        CleanupSerialPort();

                        messageQueue.Clear();
                        currentBuffer.Clear();

                        StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(null, COMControllerParamsModel.COMStates.None)
                        {
                            ErrorCode = (int)COMControllerParamsModel.ErrorCodes.ConnectionError,
                            ErrorText = "Ошибка подключения к считывателю."
                        });
                    }
                    else
                    {
                        isReading = value;

                        // Запускаем фоновую задачу для чтения данных
                        Task.Run(() => ProcessQueue());
                    }
                }
                else
                {
                    isReading = value;
                    CleanupSerialPort();
                    messageQueue.Clear();
                    currentBuffer.Clear();
                }
            }
        }

        private bool isReading = false;
        private SerialPort? serialPort;
        private ConcurrentQueue<string> messageQueue;
        private StringBuilder currentBuffer;

        // Конструктор: принимает модель параметров порта
        public COMController(COMControllerParamsModel comControllerParamsModel)
        {
            this.ComControllerParamsModel = comControllerParamsModel;
            this.messageQueue = new ConcurrentQueue<string>();
            this.currentBuffer = new StringBuilder();
        }

        // Обработчик входящих данных с порта
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                    return;

                string incoming = serialPort.ReadExisting();

                lock (currentBuffer)
                {
                    currentBuffer.Append(incoming);
                    string buffer = currentBuffer.ToString();
                    int newlineIndex;

                    // Делим буфер по строкам
                    while ((newlineIndex = buffer.IndexOf('\n')) >= 0)
                    {
                        string line = buffer.Substring(0, newlineIndex).Trim('\r', '\n');
                        messageQueue.Enqueue(line);
                        buffer = buffer.Substring(newlineIndex + 1);
                    }

                    currentBuffer.Clear();
                    currentBuffer.Append(buffer);
                }
            }
            catch (Exception ex)
            {
                StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(null, COMControllerParamsModel.COMStates.None)
                {
                    ErrorCode = (int)COMControllerParamsModel.ErrorCodes.SpecificError,
                    ErrorText = $"Ошибка считывания данных.\n{ex.Message}"
                });
            }
        }

        // Фоновая обработка данных из очереди
        private void ProcessQueue()
        {
            Regex regex = new Regex(@"\d{1,},\d+"); // шаблон: числа с запятой

            string? detectedCardIdStr = null;
            bool thereWasAConnectionError = false;

            try
            {
                while (isReading)
                {
                    // Переподключение, если порт отвалился
                    while (isReading && (serialPort == null || !serialPort.IsOpen))
                    {
                        try
                        {
                            thereWasAConnectionError = true;
                            StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(null, COMControllerParamsModel.COMStates.None)
                            {
                                ErrorCode = (int)COMControllerParamsModel.ErrorCodes.ReadingError,
                                ErrorText = "Ошибка считывания данных. Возможно, считыватель был отключен."
                            });

                            CleanupSerialPort();

                            if (!InitializeSerialPort())
                                Thread.Sleep(TimeToReconnect);
                        }
                        catch
                        {
                            Thread.Sleep(TimeToReconnect);
                        }
                    }

                    if (!isReading)
                        break;

                    // Сообщаем, что подключение восстановлено
                    if (thereWasAConnectionError)
                    {
                        thereWasAConnectionError = false;
                        StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(detectedCardIdStr, COMControllerParamsModel.COMStates.ReaderConnecting)
                        {
                            ErrorCode = (int)COMControllerParamsModel.ErrorCodes.ReaderConnecting
                        });
                    }

                    // Чтение данных
                    if (messageQueue.TryDequeue(out var serialPortData))
                    {
                        string? cardIdStr = regex.Match(serialPortData).Value;

                        if (cardIdStr.Length > 0)
                        {
                            StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(cardIdStr, COMControllerParamsModel.COMStates.Detected)
                            {
                                ErrorCode = (int)COMControllerParamsModel.ErrorCodes.ReadingSuccessful
                            });

                            detectedCardIdStr = cardIdStr;
                        }
                        else
                        {
                            StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(detectedCardIdStr, COMControllerParamsModel.COMStates.Removed)
                            {
                                ErrorCode = (int)COMControllerParamsModel.ErrorCodes.ReadingSuccessful
                            });

                            detectedCardIdStr = null;
                        }
                    }

                    Thread.Sleep(50); // Предотвращаем перегрузку CPU
                }
            }
            catch
            {
                isReading = false;
                CleanupSerialPort();
                messageQueue.Clear();
                currentBuffer.Clear();

                StateChanged?.Invoke(this, new COMEventArgs.ReadingDataEventArgs(null, COMControllerParamsModel.COMStates.None)
                {
                    ErrorCode = (int)COMControllerParamsModel.ErrorCodes.UnknownError,
                    ErrorText = "Неизвестная ошибка считывания данных."
                });
            }
        }

        // Инициализация подключения к порту
        private bool InitializeSerialPort()
        {
            try
            {
                serialPort = new SerialPort(ComControllerParamsModel.PortName, ComControllerParamsModel.BaudRate, ComControllerParamsModel.Parity, ComControllerParamsModel.DataBits, ComControllerParamsModel.StopBits);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
            }
            catch
            {
                EventLog.WriteEntry(".NET Runtime", "Ошибка поключения к считывателю. Error InitializeSerialPort.", EventLogEntryType.Error);
                return false;
            }

            return serialPort != null ? serialPort.IsOpen : false;
        }

        // Очистка и отключение COM-порта
        private void CleanupSerialPort()
        {
            if (serialPort != null)
            {
                serialPort.DataReceived -= SerialPort_DataReceived;

                if (serialPort.IsOpen)
                    serialPort.Close();

                serialPort.Dispose();
                serialPort = null;
            }
        }
    }
}
