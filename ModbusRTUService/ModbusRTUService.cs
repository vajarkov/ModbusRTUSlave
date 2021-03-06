﻿using System;
using System.ComponentModel;
using System.ServiceProcess;
using System.Timers;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Install;
using System.Reflection;
using System.Linq;

namespace ModbusRTUService
{
    public partial class ModbusRTUService : ServiceBase
    {

        public EventLog eventLog = new EventLog();      // Переменная для записи в журнал событий
        private System.Timers.Timer timerSrv;           // Таймер периодичности опроса
        // Список файлов аналоговых сигналов
        private Dictionary<byte, List<string>> unitAnalogFiles = new Dictionary<byte, List<string>>();
        // Список файлов дискретных сигналов
        private Dictionary<byte, List<string>> unitDiscreteFiles = new Dictionary<byte, List<string>>();   
        // Переменная для аналоговых значений 
        private Dictionary<byte, ushort[]> AWAUS = new Dictionary<byte, ushort[]>();
        // Переменная для дискретных значений 
        private Dictionary<byte, ushort[]> BWAUS = new Dictionary<byte, ushort[]>();
        // Переменная для копирования файлов
        private Dictionary<string, string> fileCopy = new Dictionary<string, string>();
        // private ushort[] pto_1;                      // Переменная для аналоговых значений
        private IModbusService mbSlave;                 // Класс для трансляции данных в Modbus
        private IFileParse fileParse;                   // Класс для обработки файлов и записи их переменные
        private Thread threadSlave;                     // Поток, в котором будет работать Modbus
        List<byte> slaveId = new List<byte>();          // Массив адресов устройств


        // Инициализация службы
        public ModbusRTUService()
        {
            #region Инициализация компонентов

            InitializeComponent();
            //Отключаем автоматическую запись в журнал
            AutoLog = false;

            // Создаем журнал событий и записываем в него
            if (!EventLog.SourceExists("ModbusRTUService")) //Если журнал с таким названием не существует
            {
                EventLog.CreateEventSource("ModbusRTUService", "ModbusRTUService"); // Создаем журнал
            }
            eventLog.Source = "ModbusRTUService"; //Помечаем, что будем писать в этот журнал



            string exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceConfig.exe");

            // Откртытие конфигурационного файла
            System.Configuration.Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).HasFile ? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) : ConfigurationManager.OpenExeConfiguration(exePath); 
           
            // Поиск секции настроек Modbus Slave из конфигурационного файла
            SlaveSettings slaveSettings = (SlaveSettings)appConfig.GetSection("SlaveSettings");
            
            // Если секция найдена
            if (slaveSettings != null)
            {
                // Считываем конфигурацию 
                foreach (Slaves slaves in slaveSettings.SlaveFiles)     // Цикл списка устройств 
                {
                    // Добаляем адрес устройства из конфигурации
                    slaveId.Add(slaves.Id);
                    
                    // Инициализируем список файлов аналоговых значений
                    unitAnalogFiles.Add(slaves.Id, new List<string>());
                    
                    // Инициализируем список файлов дискретных значений
                    unitDiscreteFiles.Add(slaves.Id, new List<string>());
                    
                    foreach (SlaveElement files in slaves.Slave)        // Цикл файлов для устройства
                    {
                        fileCopy.Add(files.FilePath, files.Source);
                        switch (files.Type)
                        {
                            // Добавление в список файлов файла с аналоговыми значениями
                            case "Analog":
                                unitAnalogFiles[slaves.Id].Add(files.FilePath.Replace("\\",@"\"));
                                break;

                            // Добавление в список файлов файла с дискретными значениями
                            case "Discrete":
                                unitDiscreteFiles[slaves.Id].Add(files.FilePath);
                                break;
                        }

                    }
                }
            }

            fileParse = new FileParse();    //Инициализация класса для обработки файлов
            mbSlave = new ModbusService();  //Инициализация класса трансляции данных по Modbus

            #endregion
        }

        

        // Запуск службы
        protected override void OnStart(string[] args)
        {
            #region Запись в журнал

            eventLog.WriteEntry("Служба запущена");

            #endregion

            #region Инициализация таймера
            //Инициализация таймера
            timerSrv = new System.Timers.Timer();
            //Задание интервала опроса
            timerSrv.Interval = 60000;
            //Включение таймера
            timerSrv.Enabled = true;
            //Добавление обработчика на таймер
            timerSrv.Elapsed += new ElapsedEventHandler(ReadAndModbus);
            //Автоматический взвод таймера 
            timerSrv.AutoReset = true;
            //Старт таймера
            timerSrv.Start();
   
            #endregion
        }

        protected override void OnStop()
        {
            
            #region Запись в журнал

            eventLog.WriteEntry("Служба остановлена");

            #endregion
        }
        // Обработчик таймера
        private void ReadAndModbus(object sender, ElapsedEventArgs e)
        {
            #region Обработка файлов и запись их в переменнные
            fileParse.FileCopy(fileCopy);
            foreach (byte id in slaveId)
            {
                // Проверка нет ли не считанных файлов аналоговых значений
                if (AWAUS.ContainsKey(id))
                    AWAUS[id] = fileAnalogCheck(id, fileParse.AWAUSParse(unitAnalogFiles[id]),unitAnalogFiles[id].Count);
                else
                    AWAUS.Add(id, fileParse.AWAUSParse(unitAnalogFiles[id]));

                // Проверка нет ли не считанных файлов дискретных значений
                if (BWAUS.ContainsKey(id))
                    BWAUS[id] = fileDiscreteCheck(id, fileParse.BWAUSParse(unitDiscreteFiles[id]), unitDiscreteFiles[id].Count);
                else
                    BWAUS.Add(id, fileParse.BWAUSParse(unitDiscreteFiles[id]));
                
            }
           
            #endregion

            #region Перезапуск потока Modbus

            StopSlaveThread();      //Остановска службы, если она запущена
            InitThreads();          //Инициализация данных потока
            threadSlave.Start();    //Запуск потока

            #endregion
        }

        #region Проверка считанных файлов аналоговых значений
        private ushort[] fileAnalogCheck(byte id, ushort[] response, int count) 
        {
            ushort[] checkValues = new ushort[count * 200];
            UInt32 sum;
            int start = 0;
            int end = 0;
            for (int i = 0; i < count; i++)
            {
                sum = 0;
                start = i * 200;
                end = i * 200 + 200;
                for (int j = start; j < end; j++)
                {
                    sum += response[j];
                }
                if (sum == 0)
                    Array.Copy(AWAUS[id], start, checkValues, start, 200);
                else
                    Array.Copy(response, start, checkValues, start, 200);
            }
            return checkValues;
        }
        #endregion

        #region Проверка считанных файлов дискретных значений
        private ushort[] fileDiscreteCheck(byte id, ushort[] response, int count)
        {
            ushort[] checkValues = new ushort[count * 6];
            UInt32 sum;
            int start = 0;
            int end = 0;
            for (int i = 0; i < count; i++)
            {
                sum = 0;
                start = i * 6;
                end = i * 6 + 6;
                for (int j = start; j < end; j++)
                {
                    sum += response[j];
                }
                if (sum == 0)
                    Array.Copy(BWAUS[id], start, checkValues, start, 6);
                else
                    Array.Copy(response, start, checkValues, start, 6);
            }
            return checkValues;
        }
        #endregion

        //Остановка потока
        private void StopSlaveThread()
        {
            #region Остановка потока

            //Если поток инициализирован и запущен
            if (threadSlave != null && threadSlave.IsAlive)
            {
                mbSlave.StopRTU();  //Останавливаем поток
                threadSlave.Join(); //Ждем его завершения
            }

            #endregion
        }

        //Инициализация потока
        private void InitThreads()
        {
            #region Инициализация потока
            //Создаем и заполняем устройства данными из файлов
            foreach (byte id in slaveId)
            {
                mbSlave.CreateDataStore(id, AWAUS[id], BWAUS[id]);
            }
            
            //Передаем потоку функцию из класса ModbusService с номером порта
            threadSlave = new Thread(new ThreadStart(() => mbSlave.StartRTU()));
            //Помечаем поток как фоновый
            threadSlave.IsBackground = true;

            #endregion
        }

    }
}
