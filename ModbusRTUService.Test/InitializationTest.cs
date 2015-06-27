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
        public class FileTest:FileParse{}
        public class ModbusTest : ModbusService { }
        #region Тестирование конфигурации COM-порта
        [TestMethod]
        public void ConfigurationPort()
        {
            
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Assert.IsNotNull(appConfig);

            KeyValueConfigurationCollection SerialPortSection = ((AppSettingsSection)appConfig.GetSection("SerialPortSettings")).Settings;
            Assert.IsNotNull(SerialPortSection);
                
                // Если секция найдена
                if (SerialPortSection != null)
                {
                    // Считываем данные порта
                    // Переменная из конфигурационного файла для обращения к нужному порту
                    string portName = SerialPortSection["PortName"].Value;
                    Debug.Assert(portName == "COM1");
                    // Переменная из конфигурационного файла для установки скорости порта
                    int baudRate = Convert.ToInt32(SerialPortSection["BaudRate"].Value);
                    Assert.AreEqual(baudRate,19200);
                    // Переменная из конфигурационного файла для установки четности порта
                    Parity parity = (Parity)Enum.Parse(typeof(Parity), SerialPortSection["Parity"].Value);
                    Assert.AreEqual(parity,Parity.None);
                    // Переменная из конфигурационного файла для установки битов данных
                    int dataBits = Convert.ToInt16(SerialPortSection["DataBits"].Value);
                    Assert.AreEqual(dataBits, 8);
                    // Переменная из конфигурационного файла для установки стопового бита
                    StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), SerialPortSection["StopBits"].Value);
                    Assert.AreEqual(stopBits, StopBits.One);
                    SerialPort comPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                }
        }
#endregion

        #region Тестирование конфигурации Modbus и сбора данных
        [TestMethod]
        public void ConfigurationSlave()
        {
            Dictionary<byte, ushort[]> AWAUS = new Dictionary<byte,ushort[]>();                // Переменная для аналоговых значений 
            Dictionary<byte, ushort[]> BWAUS = new Dictionary<byte, ushort[]>();
            Dictionary<byte, List<string>> unitAnalogFiles = new Dictionary<byte, List<string>>();  // Список файлов аналоговых сигналов 

            Dictionary<byte, List<string>> unitDiscreteFiles = new Dictionary<byte, List<string>>(); // Список файлов дискретных сигналов

            List<byte> slaveId = new List<byte>();                      // Массив адресов устройств
            IFileParse fileParse = new FileTest();    //Инициализация класса для обработки файлов
            IModbusService mbSlave = new ModbusTest();  //Инициализация класса трансляции данных по Modbus

            // Откртытие конфигурационного файла
            System.Configuration.Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Assert.IsNotNull(appConfig);
            // Поиск секции настроек COM-порта из конфигурационного файла
            SlaveSettings slaveSettings = (SlaveSettings)appConfig.GetSection("SlaveSettings");
            Assert.IsNotNull(slaveSettings);
            // Если секция найдена
            if (slaveSettings != null)
            {
                // Считываем конфигурацию 
                foreach (Slaves slaves in slaveSettings.SlaveFiles)     // Цикл списка устройств 
                {
                    // Добаляем адрес устройства из конфигурации
                    slaveId.Add(slaves.Id);
                    unitAnalogFiles.Add(slaves.Id, new List<string>());
                    unitDiscreteFiles.Add(slaves.Id, new List<string>());
                    int nSlaves = slaveSettings.SlaveFiles.Count;
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
                AWAUS.Add(id,fileParse.AWAUSParse(unitAnalogFiles[id]));
                Assert.IsNotNull(AWAUS[id]);
                BWAUS.Add(id,fileParse.BWAUSParse(unitDiscreteFiles[id]));
                Assert.IsNotNull(BWAUS[id]);
            }
            
            
            foreach (byte id in slaveId)
            {
                    mbSlave.CreateDataStore(id, AWAUS[id], BWAUS[id]);
            }

        }
        #endregion


    }
}
