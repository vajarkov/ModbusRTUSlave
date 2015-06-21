using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Configuration;
using System.IO.Ports;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using ModbusRTUService;


namespace ModbusRTUService.Test
{
    [TestClass]
    public class InitializationTest
    {
        #region Тестирование конфигурации COM-порта
        [TestMethod]
        public void ConfigurationPort()
        {
            
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Assert.IsNotNull(appConfig);

            NameValueCollection SerialPortSection =
                (NameValueCollection)ConfigurationManager.GetSection("SerialPortSettings");
            Assert.IsNotNull(SerialPortSection);
                
                // Если секция найдена
                if (SerialPortSection != null)
                {
                    // Считываем данные порта
                    // Переменная из конфигурационного файла для обращения к нужному порту
                    string portName = SerialPortSection["PortName"];
                    Debug.Assert(portName == "COM1");
                    // Переменная из конфигурационного файла для установки скорости порта
                    int baudRate = Convert.ToInt32(SerialPortSection["BaudRate"]);
                    Assert.AreEqual(baudRate,19200);
                    // Переменная из конфигурационного файла для установки четности порта
                    Parity parity = (Parity)Enum.Parse(typeof(Parity), SerialPortSection["Parity"]);
                    Assert.AreEqual(SerialPortSection["Parity"],"None");
                    // Переменная из конфигурационного файла для установки битов данных
                    int dataBits = Convert.ToInt16(SerialPortSection["DataBits"]);
                    Assert.AreEqual(dataBits, 8);
                    // Переменная из конфигурационного файла для установки стопового бита
                    StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), SerialPortSection["StopBits"]);
                    SerialPort comPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                }
        }
#endregion

        #region Тестирование конфигурации Modbus и сбора данных
        [TestMethod]
        public void ConfigurationSlave()
        {
            ushort[][] AWAUS = null;                // Переменная для аналоговых значений 
            ushort[][] BWAUS = null;
            Dictionary<int, List<string>> unitAnalogFiles = null;  // Список файлов аналоговых сигналов 

            Dictionary<int, List<string>> unitDiscreteFiles = null; // Список файлов дискретных сигналов

            List<byte> slaveId = null;                      // Массив адресов устройств
            IFileParse fileParse = new FileParse();    //Инициализация класса для обработки файлов
            IModbusService mbSlave = new ModbusService();  //Инициализация класса трансляции данных по Modbus

            // Откртытие конфигурационного файла
            System.Configuration.Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Assert.IsNotNull(appConfig);
            // Поиск секции настроек COM-порта из конфигурационного файла
            SlaveSettings slaveSettings = (SlaveSettings)ConfigurationManager.GetSection("SlaveSettings");
            Assert.IsNotNull(slaveSettings);
            // Если секция найдена
            if (slaveSettings != null)
            {
                // Инициализируем массивы для аналоговых значений
                unitAnalogFiles = new Dictionary<int, List<string>>();

                // Инициализируем массивы для аналоговых значений
                unitDiscreteFiles = new Dictionary<int, List<string>>();
                slaveId = new List<byte>();
                // Считываем конфигурацию 
                foreach (Slaves slaves in slaveSettings.SlaveItems)     // Цикл списка устройств 
                {
                    // Добаляем адрес устройства из конфигурации
                    slaveId.Add(slaves.Id);
                    unitAnalogFiles.Add(slaves.Id, new List<string>());
                    unitDiscreteFiles.Add(slaves.Id, new List<string>());
                    int nSlaves = slaveSettings.SlaveItems.Count;
                    Debug.Assert(nSlaves > 1);
                    foreach (SlaveElement files in slaves.Slave)        // Цикл файлов для устройства
                    {
                        switch (files.Type)
                        {
                            // Добавление в список файлов файла с аналоговыми значениями
                            case "Analog":
                                unitAnalogFiles[slaves.Id].Add(files.FilePath);
                                StringAssert.Contains(files.FilePath, "AWAUS");
                                break;

                            // Добавление в список файлов файла с дискретными значениями
                            case "Discrete":
                                unitDiscreteFiles[slaves.Id].Add(files.FilePath);
                                StringAssert.Contains(files.FilePath, "BWAUS");
                                break;
                        }

                    }
                }
            }
            foreach (byte id in slaveId)
            {
                AWAUS[id] = fileParse.AWAUSParse(unitAnalogFiles[id]);
                BWAUS[id] = fileParse.BWAUSParse(unitDiscreteFiles[id]);
            }
            
            
            foreach (byte id in slaveId)
            {
                mbSlave.CreateDataStore(id, ref AWAUS[id], ref BWAUS[id]);
            }
        }
        #endregion


    }
}
