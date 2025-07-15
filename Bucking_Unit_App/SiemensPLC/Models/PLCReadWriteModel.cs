using Sharp7;
using System.Globalization;
using System.Text;

namespace Bucking_Unit_App.SiemensPLC.Models
{
    public partial class SiemensPLCModels
    {
        public class PLCReadWriteModel
        {
            public abstract record PLCModifiedType;
            public record PLCClientVariableIsNull : PLCModifiedType;
            public record PLCNoConnection : PLCModifiedType;
            public record PLCUnknownError : PLCModifiedType;
            public record PLCUnknownTypeResult : PLCModifiedType;
            public record PLCTypeConversionError : PLCModifiedType;
            public record PLCBoolResult(bool value) : PLCModifiedType;
            public record PLCByteResult(byte value) : PLCModifiedType;
            public record PLCWordResult(ushort value) : PLCModifiedType;
            public record PLCDWordResult(uint value) : PLCModifiedType;
            public record PLCUShortResult(ushort value) : PLCModifiedType;
            public record PLCShortResult(short value) : PLCModifiedType;
            public record PLCUIntResult(uint value) : PLCModifiedType;
            public record PLCIntResult(int value) : PLCModifiedType;
            public record PLCULongResult(ulong value) : PLCModifiedType;
            public record PLCLongResult(long value) : PLCModifiedType;
            public record PLCFloatResult(float value) : PLCModifiedType;
            public record PLCDoubleResult(double value) : PLCModifiedType;
            public record PLCASCIIStringResult(string value) : PLCModifiedType;
            public record PLCWStringResult(string value) : PLCModifiedType;


            private PLCModifiedType ConvertValue<T>(T value, Types targetType)
            {
                try
                {
                    if (value == null)
                        return new PLCTypeConversionError();

                    switch (targetType)
                    {
                        case Types.Bit: return new PLCBoolResult(Convert.ToBoolean(value));
                        case Types.Byte: return new PLCByteResult(ConvertValueToByte(value));
                        case Types.Word: return new PLCWordResult(Convert.ToUInt16(value));
                        case Types.Dword: return new PLCDWordResult(Convert.ToUInt32(value));
                        case Types.Uint: return new PLCUShortResult(Convert.ToUInt16(value));
                        case Types.Int: return new PLCShortResult(Convert.ToInt16(value));
                        case Types.Udint: return new PLCUIntResult(Convert.ToUInt32(value));
                        case Types.Dint: return new PLCIntResult(Convert.ToInt32(value));
                        case Types.Ulint: return new PLCULongResult(Convert.ToUInt64(value));
                        case Types.Lint: return new PLCLongResult(Convert.ToInt64(value));
                        case Types.Real: return new PLCFloatResult(Convert.ToSingle(value));
                        case Types.Lreal: return new PLCDoubleResult(Convert.ToDouble(value));
                        case Types.StringASCII: return new PLCASCIIStringResult(value.ToString()!);
                        case Types.WString: return new PLCWStringResult(value.ToString()!);
                        default: return new PLCUnknownTypeResult();
                    }
                }
                catch
                {
                    return new PLCTypeConversionError();
                }
            }
            
            private byte ConvertValueToByte<T>(T value)
            {
                if (value is string str)
                {
                    // Удаляем возможные префиксы (0x, &h) и пробелы
                    string cleanStr = str.Trim()
                                       .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                                       .Replace("&h", "", StringComparison.OrdinalIgnoreCase);

                    if (byte.TryParse(cleanStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte result))
                    {
                        return result; // Успешный парсинг десятичного числа в виде строки (например, "200")
                    }
                    else if (byte.TryParse(cleanStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexResult))
                    {
                        return hexResult; // Успешный парсинг HEX (например, "C8" → 200)
                    }
                    else
                    {
                        throw new FormatException($"Не удалось преобразовать '{str}' в byte.");
                    }
                }
                else if (value is IConvertible convertible) // Если T — это число (int, short и т. д.)
                {
                    try
                    {
                        return convertible.ToByte(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        throw new OverflowException($"Значение {value} выходит за пределы диапазона byte (0–255).");
                    }
                }
                else
                {
                    throw new InvalidCastException($"Тип {typeof(T)} не поддерживается для преобразования в byte.");
                }
            }

            private void SetWStringAt(byte[] buffer, int maxLength, string value)
            {
                try
                {
                    BitConverter.GetBytes((ushort)maxLength).CopyTo(buffer, 0);         // Макс. длина (байты 0-1)
                    BitConverter.GetBytes((ushort)value.Length).CopyTo(buffer, 2);   // Тек. длина (байты 2-3)
                    Encoding.BigEndianUnicode.GetBytes(value).CopyTo(buffer, 4);     // Символы (с байта 4)
                }
                catch (Exception ex)
                {
                    buffer = new byte[buffer.Length];

                    Exception newException = new Exception(ex.Message, ex);
                    throw newException;
                }
            }

            private string GetWStringAt(byte[] buffer, int pos)
            {
                try
                {
                    // Получаем текущую длину строки (байты 2-3)
                    int actualLength = BitConverter.ToUInt16(buffer, 2);

                    // Извлекаем символы UTF-16 (начиная с 4-го байта)
                    return Encoding.BigEndianUnicode.GetString(buffer, pos, actualLength * 2);
                }
                catch (Exception ex)
                {
                    buffer = new byte[buffer.Length];

                    Exception newException = new Exception(ex.Message, ex);
                    throw newException;
                }
            }

            public PLCModifiedType GetValueAt(byte[] buffer, int pos, Types type)
            {
                try
                {
                    switch (type)
                    {
                        case Types.Byte: return new PLCByteResult(S7.GetByteAt(buffer, pos));
                        case Types.Word: return new PLCWordResult(S7.GetWordAt(buffer, pos));
                        case Types.Dword: return new PLCDWordResult(S7.GetDWordAt(buffer, pos));
                        case Types.Uint: return new PLCUShortResult(S7.GetUIntAt(buffer, pos));
                        case Types.Int: return new PLCShortResult(S7.GetIntAt(buffer, pos));
                        case Types.Udint: return new PLCUIntResult(S7.GetUDIntAt(buffer, pos));
                        case Types.Dint: return new PLCIntResult(S7.GetDIntAt(buffer, pos));
                        case Types.Ulint: return new PLCULongResult(S7.GetULIntAt(buffer, pos));
                        case Types.Lint: return new PLCLongResult(S7.GetLIntAt(buffer, pos));
                        case Types.Real: return new PLCFloatResult(S7.GetRealAt(buffer, pos));
                        case Types.Lreal: return new PLCDoubleResult(S7.GetLRealAt(buffer, pos));
                        case Types.StringASCII: return new PLCASCIIStringResult(S7.GetStringAt(buffer, pos));
                        case Types.WString: return new PLCWStringResult(GetWStringAt(buffer, pos));
                        default: return new PLCUnknownTypeResult();
                    }
                }
                catch
                {
                    return new PLCUnknownError();
                }
            }

            public PLCModifiedType GetValueAt(byte[] buffer, int pos, int bit, Types type)
            {
                try
                {
                    switch (type)
                    {
                        case Types.Bit: return new PLCBoolResult(S7.GetBitAt(buffer, pos, bit));
                        default: return new PLCUnknownTypeResult();
                    }
                }
                catch
                {
                    return new PLCUnknownError();
                }
            }
            
            public int SetValueAt<T>(byte[] buffer, int pos, Types type, T newValue)
            {
                try
                {
                    switch (ConvertValue(newValue, type))
                    {
                        case PLCTypeConversionError:
                            return -1;
                        case PLCUnknownTypeResult:
                            return -2;
                        case PLCByteResult result: S7.SetByteAt(buffer, pos, result.value);
                            return 1;
                        case PLCWordResult result: S7.SetWordAt(buffer, pos, result.value);
                            return 1;
                        case PLCDWordResult result: S7.SetDWordAt(buffer, pos, result.value);
                            return 1;
                        case PLCUShortResult result: S7.SetUIntAt(buffer, pos, result.value);
                            return 1;
                        case PLCShortResult result: S7.SetIntAt(buffer, pos, result.value);
                            return 1;
                        case PLCUIntResult result: S7.SetUDIntAt(buffer, pos, result.value);
                            return 1;
                        case PLCIntResult result: S7.SetDIntAt(buffer, pos, result.value);
                            return 1;
                        case PLCULongResult result: S7.SetULintAt(buffer, pos, result.value);
                            return 1;
                        case PLCLongResult result: S7.SetLIntAt(buffer, pos, result.value);
                            return 1;
                        case PLCFloatResult result: S7.SetRealAt(buffer, pos, result.value);
                            return 1;
                        case PLCDoubleResult result: S7.SetLRealAt(buffer, pos, result.value);
                            return 1;
                        default:
                            return -2;
                    }
                }
                catch
                {
                    return -3;
                }
            }

            public int SetValueAt<T>(byte[] buffer, int pos, int bit, Types type, T newValue)
            {
                try
                {
                    switch (ConvertValue(newValue, Types.Bit))
                    {
                        case PLCTypeConversionError:
                            return -1;
                        case PLCUnknownTypeResult:
                            return -2;
                        case PLCBoolResult result:
                            S7.SetBitAt(buffer, pos, bit, result.value);
                            return 1;
                        default:
                            return -2;
                    }
                }
                catch
                {
                    return -3;
                }
            }

            public int SetValueAt<T>(byte[] buffer, int pos, Types type, int maxLength, T newValue)
            {
                try
                {
                    switch (ConvertValue(newValue, type))
                    {
                        case PLCASCIIStringResult result:
                            S7.SetStringAt(buffer, pos, maxLength, result.value);
                            return 1;
                        default:
                            return -2;
                    }
                }
                catch
                {
                    return -3;
                }
            }

            public int SetValueAt<T>(byte[] buffer, Types type, int maxLength, T newValue)
            {
                try
                {
                    switch (ConvertValue(newValue, type))
                    {
                        case PLCWStringResult result:
                            SetWStringAt(buffer, maxLength, result.value);
                            return 1;
                        default:
                            return -2;
                    }
                }
                catch
                {
                    return -3;
                }
            }
        }
    }
}
