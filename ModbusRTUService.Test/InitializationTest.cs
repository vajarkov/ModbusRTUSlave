using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Configuration;
using System.IO.Ports;
using System.IO;
using System.Reflection;

namespace ModbusRTUService.Test
{
    [TestClass]
    public class InitializationTest
    {
        [TestMethod]
        public void Configuration()
        {
            //ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
            //configFile.ExeConfigFilename = "ModbusRTUService.Test.dll.config";
            //Assert.IsTrue(File.Exists(configFile.ExeConfigFilename));
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            Assert.IsTrue(appConfig.HasFile);

            AppSettingsSection SerialPortSection =
                (AppSettingsSection)ConfigurationManager.GetSection("SerialPortSettings");
            Assert.IsNotNull(SerialPortSection);
                
                // Если секция найдена
                if (SerialPortSection != null)
                {
                    // Считываем данные порта
                    // Переменная из конфигурационного файла для обращения к нужному порту
                    string portName = SerialPortSection.Settings["PortName"].Value;
                    Debug.Assert(portName != "COM1");
                    // Переменная из конфигурационного файла для установки скорости порта
                    int baudRate = Convert.ToInt32(SerialPortSection.Settings["BaudRate"].Value);
                    Assert.AreEqual(baudRate,19200);
                    // Переменная из конфигурационного файла для установки четности порта
                    Parity parity = (Parity)Enum.Parse(typeof(Parity), SerialPortSection.Settings["Parity"].Value);
                    // Переменная из конфигурационного файла для установки битов данных
                    int dataBits = Convert.ToInt16(SerialPortSection.Settings["DataBits"].Value);
                    // Переменная из конфигурационного файла для установки стопового бита
                    StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), SerialPortSection.Settings["StopBits"].Value);
                }
        }

       
    }
}
