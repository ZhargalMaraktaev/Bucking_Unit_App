using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Bucking_Unit_App.Views
{
    public class GradientBackgroundConverter : IMultiValueConverter
    {
        public GradientBackgroundConverter()
        {
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Проверка входных данных: ожидаем 8 значений (план и факт для А, Б, В, Г)
                if (values.Length != 8)
                {
                    Debug.WriteLine($"GradientBackgroundConverter: Ожидалось 8 значений, получено: {values.Length}");
                    return Brushes.Transparent;
                }
                if (!int.TryParse(parameter?.ToString(), out int rowIndex) || rowIndex < 0 || rowIndex > 3)
                {
                    Debug.WriteLine($"GradientBackgroundConverter: Неверный параметр строки: {parameter}");
                    return Brushes.Transparent;
                }

                // Вычисление разниц план-факт для всех смен
                var differences = new double[4];
                bool hasValidData = false;
                for (int i = 0; i < 4; i++)
                {
                    string planStr = values[i * 2]?.ToString();
                    string factStr = values[i * 2 + 1]?.ToString();
                    if (string.IsNullOrEmpty(planStr) || string.IsNullOrEmpty(factStr))
                    {
                        Debug.WriteLine($"GradientBackgroundConverter: Пустое значение в строке {i} (Смена {(char)('А' + i)}). Plan: '{planStr}', Fact: '{factStr}'");
                        differences[i] = 0;
                        continue;
                    }
                    if (double.TryParse(planStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double plan) &&
                        double.TryParse(factStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double fact))
                    {
                        differences[i] = plan - fact;
                        hasValidData = true;
                        Debug.WriteLine($"GradientBackgroundConverter: Строка {i} (Смена {(char)('А' + i)}), Plan={plan}, Fact={fact}, Diff={differences[i]}");
                    }
                    else
                    {
                        Debug.WriteLine($"GradientBackgroundConverter: Не удалось преобразовать в double в строке {i} (Смена {(char)('А' + i)}). Plan: '{planStr}', Fact: '{factStr}'");
                        differences[i] = 0;
                    }
                }

                if (!hasValidData)
                {
                    Debug.WriteLine("GradientBackgroundConverter: Нет валидных данных для сравнения.");
                    return Brushes.Transparent;
                }

                // Текущая разница для строки
                double currentDiff = differences[rowIndex];
                if (currentDiff == 0)
                {
                    return Brushes.Transparent;
                }

                // Находим максимальную и минимальную разницу для нормализации
                double maxDiff = Math.Max(differences.Max(), 0);
                double minDiff = Math.Min(differences.Min(), 0);

                if (currentDiff > 0)
                {
                    // Положительная разница: градиент от #FFF5F5 (255, 245, 245) до #8B0000 (139, 0, 0)
                    if (maxDiff == 0)
                    {
                        return Brushes.Transparent;
                    }
                    double ratio = currentDiff / maxDiff; // Нормализация [0, 1]
                    byte r = (byte)(255 - (255 - 139) * (1 - ratio)); // От 255 до 139
                    byte g = (byte)(245 * (1 - ratio));               // От 245 до 0
                    byte b = (byte)(245 * (1 - ratio));               // От 245 до 0
                    Debug.WriteLine($"GradientBackgroundConverter: Row {rowIndex} (Смена {(char)('А' + rowIndex)}), Diff={currentDiff}, Red Gradient, Color=({r},{g},{b})");
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
                else
                {
                    // Отрицательная разница: градиент от #F5FFF5 (245, 255, 245) до #006400 (0, 100, 0)
                    if (minDiff == 0)
                    {
                        return Brushes.Transparent;
                    }
                    double ratio = currentDiff / minDiff; // Нормализация [0, 1]
                    byte r = (byte)(245 * (1 - ratio));               // От 245 до 0
                    byte g = (byte)(255 - (255 - 100) * (1 - ratio)); // От 255 до 100
                    byte b = (byte)(245 * (1 - ratio));               // От 245 до 0
                    Debug.WriteLine($"GradientBackgroundConverter: Row {rowIndex} (Смена {(char)('А' + rowIndex)}), Diff={currentDiff}, Green Gradient, Color=({r},{g},{b})");
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GradientBackgroundConverter: Ошибка: {ex.Message}");
                return Brushes.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}