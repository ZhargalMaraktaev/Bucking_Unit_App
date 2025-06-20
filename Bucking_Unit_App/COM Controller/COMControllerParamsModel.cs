// ===== COMControllerParamsModel.cs =====
using System.Drawing;
using System.IO.Ports;

namespace Bucking_Unit_App.COM_Controller
{
    public class COMControllerParamsModel
    {
        // Возможные коды ошибок
        public enum ErrorCodes
        {
            ReaderConnecting = 0,
            ReadingSuccessful = 1,
            ConnectionError = -1,
            ReadingError = -2,
            SpecificError = -3,
            UnknownError = -4
        }

        // Состояния COM-порта и считывателя
        public enum COMStates
        {
            ReaderConnecting = 0,
            Detected = 1,
            Removed = 2,
            None = 3
        }

        // Статусы со строками и цветом для отображения
        public class COMStatus
        {
            public static class ReaderConnecting
            {
                public static string Text { get; } = "Считыватель подключен. Если карта находится в считывателе, уберите ее и вставьте заного.";
                public static Color ForeColor { get; } = Color.Red;
            }

            public static class CardDetected
            {
                public static string Text { get; } = "Карта вставлена.";
                public static Color ForeColor { get; } = Color.Green;
            }

            public static class CardRemoved
            {
                public static string Text { get; } = "Карта не обнаружена.";
                public static Color ForeColor { get; } = Color.Red;
            }

            public static class ConnectionError
            {
                public static string Text { get; } = "Ошибка подключения к считывателю.";
                public static Color ForeColor { get; } = Color.Red;
            }

            public static class ReadingError
            {
                public static string Text { get; } = "Ошибка. Переподключите считыватель.";
                public static Color ForeColor { get; } = Color.Red;
            }

            public static class UnknownError
            {
                public static string Text { get; } = "Ошибка. Уберите карту из считывателя и вставьте снова.";
                public static Color ForeColor { get; } = Color.Red;
            }
        }

        // Настройки COM-порта
        public string PortName { get; }
        public int BaudRate { get; }
        public Parity Parity { get; }
        public int DataBits { get; }
        public StopBits StopBits { get; }

        // Конструктор инициализирует настройки
        public COMControllerParamsModel(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.Parity = parity;
            this.DataBits = dataBits;
            this.StopBits = stopBits;
        }
    }
}