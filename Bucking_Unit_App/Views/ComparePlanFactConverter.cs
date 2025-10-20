using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Bucking_Unit_App.Views
{
    public class ComparePlanFactConverter : IMultiValueConverter
    {
        public ComparePlanFactConverter()
        {
            // Конструктор без параметров — добавьте здесь любую инициализацию по умолчанию, если нужно
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length != 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                {
                    Debug.WriteLine("ComparePlanFactConverter: Недостаточно значений или значения не установлены.");
                    return false;
                }

                string planStr = values[0]?.ToString();
                string factStr = values[1]?.ToString();

                if (string.IsNullOrEmpty(planStr) || string.IsNullOrEmpty(factStr))
                {
                    Debug.WriteLine($"ComparePlanFactConverter: Одно из значений пустое или null. Plan: '{planStr}', Fact: '{factStr}'");
                    return false;
                }

                if (double.TryParse(planStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double plan) &&
                    double.TryParse(factStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double fact))
                {
                    Debug.WriteLine($"ComparePlanFactConverter: План={plan}, Факт={fact}, Результат={plan > fact}");
                    return plan > fact;
                }

                Debug.WriteLine($"ComparePlanFactConverter: Не удалось преобразовать значения в double. Plan: '{planStr}', Fact: '{factStr}'");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ComparePlanFactConverter: Ошибка: {ex.Message}");
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
