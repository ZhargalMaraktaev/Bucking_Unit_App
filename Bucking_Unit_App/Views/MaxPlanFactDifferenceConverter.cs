using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Bucking_Unit_App.Views
{
    public class MaxPlanFactDifferenceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length != 8)
                {
                    Debug.WriteLine($"MaxPlanFactDifferenceConverter: Ожидалось 8 значений, получено: {values.Length}");
                    return false;
                }

                var differences = new double[4];
                bool hasValidData = false;

                for (int i = 0; i < 4; i++)
                {
                    string planStr = values[i * 2]?.ToString();
                    string factStr = values[i * 2 + 1]?.ToString();

                    if (string.IsNullOrEmpty(planStr) || string.IsNullOrEmpty(factStr))
                    {
                        Debug.WriteLine($"MaxPlanFactDifferenceConverter: Пустое значение в строке {i} (Смена {(char)('А' + i)}). Plan: '{planStr}', Fact: '{factStr}'");
                        differences[i] = double.MinValue;
                        continue;
                    }

                    if (double.TryParse(planStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double plan) &&
                        double.TryParse(factStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double fact))
                    {
                        differences[i] = plan - fact;
                        hasValidData = true;
                        Debug.WriteLine($"MaxPlanFactDifferenceConverter: Строка {i} (Смена {(char)('А' + i)}), Plan={plan}, Fact={fact}, Diff={differences[i]}");
                    }
                    else
                    {
                        Debug.WriteLine($"MaxPlanFactDifferenceConverter: Не удалось преобразовать в double в строке {i} (Смена {(char)('А' + i)}). Plan: '{planStr}', Fact: '{factStr}'");
                        differences[i] = double.MinValue;
                    }
                }

                if (!hasValidData)
                {
                    Debug.WriteLine("MaxPlanFactDifferenceConverter: Нет валидных данных для сравнения.");
                    return false;
                }

                double maxDifference = differences.Max();
                int maxIndex = Array.IndexOf(differences, maxDifference);

                if (parameter == null || !int.TryParse(parameter.ToString(), out int rowIndex) || rowIndex < 0 || rowIndex > 3)
                {
                    Debug.WriteLine($"MaxPlanFactDifferenceConverter: Неверный параметр строки: {parameter}");
                    return false;
                }

                bool isMax = (rowIndex == maxIndex && maxDifference > 0);
                Debug.WriteLine($"MaxPlanFactDifferenceConverter: MaxDiff={maxDifference}, MaxIndex={maxIndex} (Смена {(char)('А' + maxIndex)}), RowIndex={rowIndex} (Смена {(char)('А' + rowIndex)}), IsMax={isMax}");
                return isMax;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MaxPlanFactDifferenceConverter: Ошибка: {ex.Message}");
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
