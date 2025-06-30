using System.Drawing;
using System.Text.Json.Serialization;

namespace Bucking_Unit_App.Models
{
    public class Employee1CModel
    {
        public enum ErrorCodes
        {
            EmployeeFound = 0,
            EmployeeNotFound = -1,
            SpecificError = -2,
            UnknownError = -3,
        }

        public class Employee1CStatus
        {
            public static class EmployeeNotFound
            {
                public static string Text { get; } = "Пользователь не найден.";
                public static Color ForeColor { get; } = Color.Red;
            }

            public static class UnknownError
            {
                public static string Text { get; } = "Ошибка получения данных пользователя.";
                public static Color ForeColor { get; } = Color.Red;
            }
        }

        public string? CardNumber { get; set; }
        [JsonPropertyName("ТабельныйНомер")]
        public string? PersonnelNumber { get; set; }

        [JsonPropertyName("Сотрудник")]
        public string? FullName { get; set; }

        [JsonPropertyName("Подразделение")]
        public string? Department { get; set; }

        [JsonPropertyName("Должность")]
        public string? Position { get; set; }

        public int ErrorCode { get; set; } = 0;
        public string? ErrorText { get; set; }
    }
}
