using Sharp7;

namespace Bucking_Unit_App.SiemensPLC.Models
{
    public partial class SiemensPLCModels
    {
        public partial class DBAddressModel
        {
            public abstract class DbxAddress
            {
                private S7Client Client { get; }

                /// <summary>
                /// Объект для управления записью и чтением из адреса ПЛК Siemens.
                /// </summary>
                private PLCReadWriteModel PLCReadWriteModel { get; } = new PLCReadWriteModel();

                /// <summary>
                /// Название адреса.
                /// </summary>
                public abstract string Name { get; }

                /// <summary>
                /// Тип данных значения адреса в памяти.
                /// </summary>
                protected abstract Types Type { get; }

                /// <summary>
                /// Номер блока данных (DB - Data block).
                /// </summary>
                protected abstract int Number { get; }

                /// <summary>
                /// Смещение в байтах внутри блока DB.
                /// </summary>
                protected abstract int Offset { get; }

                /// <summary>
                /// Номер бита в выбранном байте.
                /// </summary>
                protected abstract int Bit { get; }

                /// <summary>
                /// Смещение в байтах в массиве, с которого начинается число.
                /// </summary>
                protected abstract int Pos { get; }

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected abstract int BufferLength { get; }

                public DbxAddress(S7Client client)
                {
                    Client = client;
                }

                public PLCReadWriteModel.PLCModifiedType Read()
                {
                    if (Client == null)
                        return new PLCReadWriteModel.PLCClientVariableIsNull();

                    if (!Client.Connected)
                        return new PLCReadWriteModel.PLCNoConnection();

                    byte[] buffer = new byte[BufferLength];
                    Client.DBRead(Number, Offset, BufferLength, buffer);

                    return PLCReadWriteModel.GetValueAt(buffer, Pos, Bit, Type);
                }

                public int Write<T>(T newValue)
                {
                    try
                    {
                        if (Client == null || !Client.Connected)
                            return -3;

                        byte[] buffer = new byte[BufferLength];

                        int resultSetValueAt = PLCReadWriteModel.SetValueAt(buffer, Pos, Bit, Type, newValue);

                        if (resultSetValueAt > 0)
                        {
                            Client.DBWrite(Number, Offset, BufferLength, buffer);
                        }

                        return resultSetValueAt;
                    }
                    catch
                    {
                        return -3;
                    }
                }
            }

            public abstract class DbxStringAddress
            {
                private S7Client Client { get; }

                /// <summary>
                /// Объект для управления записью и чтением из адреса ПЛК Siemens.
                /// </summary>
                private PLCReadWriteModel PLCReadWriteModel { get; } = new PLCReadWriteModel();

                /// <summary>
                /// Название адреса.
                /// </summary>
                public abstract string Name { get; }

                /// <summary>
                /// Тип данных значения адреса в памяти.
                /// </summary>
                protected abstract Types Type { get; }

                /// <summary>
                /// Номер блока данных (DB - Data block).
                /// </summary>
                protected abstract int Number { get; }

                /// <summary>
                /// Смещение в байтах внутри блока DB.
                /// </summary>
                protected abstract int Offset { get; }

                /// <summary>
                /// Смещение в байтах в массиве, с которого начинается число.
                /// </summary>
                protected abstract int Pos { get; }

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected abstract int MaxLength { get; }

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected int BufferLength { get; }

                public DbxStringAddress(S7Client client)
                {
                    Client = client;
                    BufferLength = MaxLength + 2;
                }

                public PLCReadWriteModel.PLCModifiedType Read()
                {
                    if (Client == null)
                        return new PLCReadWriteModel.PLCClientVariableIsNull();

                    if (!Client.Connected)
                        return new PLCReadWriteModel.PLCNoConnection();

                    byte[] buffer = new byte[BufferLength];
                    Client.DBRead(Number, Offset, BufferLength, buffer);

                    return PLCReadWriteModel.GetValueAt(buffer, Pos, Type);
                }

                public int Write<T>(T newValue)
                {
                    try
                    {
                        if (Client == null || !Client.Connected)
                            return -3;

                        byte[] buffer = new byte[BufferLength];

                        int resultSetValueAt = PLCReadWriteModel.SetValueAt(buffer, Pos, Type, MaxLength, newValue);

                        if (resultSetValueAt > 0)
                        {
                            Client.DBWrite(Number, Offset, BufferLength, buffer);
                        }

                        return resultSetValueAt;
                    }
                    catch
                    {
                        return -3;
                    }
                }
            }

            public abstract class DbbDbwDbdAddress
            {
                private S7Client Client { get; }

                /// <summary>
                /// Объект для управления записью и чтением из адреса ПЛК Siemens.
                /// </summary>
                private PLCReadWriteModel PLCReadWriteModel { get; } = new PLCReadWriteModel();

                /// <summary>
                /// Название адреса.
                /// </summary>
                public abstract string Name { get; }

                /// <summary>
                /// Тип данных значения адреса в памяти.
                /// </summary>
                protected abstract Types Type { get; }

                /// <summary>
                /// Номер блока данных (DB - Data block).
                /// </summary>
                protected abstract int Number { get; }

                /// <summary>
                /// Смещение в байтах внутри блока DB.
                /// </summary>
                protected abstract int Offset { get; }

                /// <summary>
                /// Смещение в байтах в массиве, с которого начинается число.
                /// </summary>
                protected abstract int Pos { get; }

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected abstract int BufferLength {  get; }
                
                public DbbDbwDbdAddress(S7Client client)
                {
                    Client = client;
                }

                public PLCReadWriteModel.PLCModifiedType Read()
                {
                    if (Client == null)
                        return new PLCReadWriteModel.PLCClientVariableIsNull();

                    if (!Client.Connected)
                        return new PLCReadWriteModel.PLCNoConnection();

                    byte[] buffer = new byte[BufferLength];
                    Client.DBRead(Number, Offset, BufferLength, buffer);

                    return PLCReadWriteModel.GetValueAt(buffer, Pos, Type);
                }

                public int Write<T>(T newValue)
                {
                    try
                    {
                        if (Client == null || !Client.Connected)
                            return -3;

                        byte[] buffer = new byte[BufferLength];

                        int resultSetValueAt = PLCReadWriteModel.SetValueAt(buffer, Pos, Type, newValue);

                        if (resultSetValueAt > 0)
                        {
                            Client.DBWrite(Number, Offset, BufferLength, buffer);
                        }

                        return resultSetValueAt;
                    }
                    catch
                    {
                        return -3;
                    }
                }
            }

            public abstract class DbwStringAddress
            {
                private S7Client Client { get; }

                /// <summary>
                /// Объект для управления записью и чтением из адреса ПЛК Siemens.
                /// </summary>
                private PLCReadWriteModel PLCReadWriteModel { get; } = new PLCReadWriteModel();

                /// <summary>
                /// Название адреса.
                /// </summary>
                public abstract string Name { get; }

                /// <summary>
                /// Тип данных значения адреса в памяти.
                /// </summary>
                protected abstract Types Type { get; }

                /// <summary>
                /// Номер блока данных (DB - Data block).
                /// </summary>
                protected abstract int Number { get; }

                /// <summary>
                /// Смещение в байтах внутри блока DB.
                /// </summary>
                protected abstract int Offset { get; }

                /// <summary>
                /// Смещение в байтах в массиве, с которого начинается число.
                /// </summary>
                protected const int Pos = 4;

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected abstract int MaxLength { get; }

                /// <summary>
                /// Размер массива байт, для хранения считываемых данных.
                /// </summary>
                protected int BufferLength { get; }

                public DbwStringAddress(S7Client client)
                {
                    Client = client;
                    BufferLength = (MaxLength * 2) + 4;
                }

                public PLCReadWriteModel.PLCModifiedType Read()
                {
                    /*if (Client == null)
                        return new PLCReadWriteModel.PLCClientVariableIsNull();

                    if (!Client.Connected)
                        return new PLCReadWriteModel.PLCNoConnection();

                    byte[] buffer = new byte[BufferLength];
                    Client.DBRead(Number, Offset, BufferLength, buffer);

                    return PLCReadWriteModel.GetValueAt(buffer, Pos, Type);*/

                    if (Client == null)
                        return new PLCReadWriteModel.PLCClientVariableIsNull();

                    if (!Client.Connected)
                        return new PLCReadWriteModel.PLCNoConnection();

                    byte[] buffer = new byte[BufferLength];
                    Client.DBRead(Number, Offset, BufferLength, buffer);

                    return PLCReadWriteModel.GetValueAt(buffer, Pos, Type);
                }

                public int Write<T>(T newValue)
                {
                    try
                    {
                        /*if (Client == null || !Client.Connected)
                            return -3;

                        byte[] buffer = new byte[BufferLength];

                        int resultSetValueAt = PLCReadWriteModel.SetValueAt(buffer, Pos, Type, MaxLength, newValue);

                        if (resultSetValueAt > 0)
                        {
                            Client.DBWrite(Number, Offset, BufferLength, buffer);
                        }

                        return resultSetValueAt;*/

                        if (Client == null || !Client.Connected)
                            return -3;

                        byte[] buffer = new byte[BufferLength];

                        int resultSetValueAt = PLCReadWriteModel.SetValueAt(buffer, Type, MaxLength, newValue);

                        if (resultSetValueAt > 0)
                        {
                            Client.DBWrite(Number, Offset, BufferLength, buffer);
                        }

                        return resultSetValueAt;
                    }
                    catch
                    {
                        return -3;
                    }
                }
            }
        }
    }
}
