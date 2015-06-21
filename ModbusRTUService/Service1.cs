using System;
using System.ComponentModel;
using System.ServiceProcess;
using System.Timers;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ModbusRTUService
{
    public partial class ModbusRTUService : ServiceBase
    {

        public EventLog eventLog = new EventLog();      // Переменная для записи в журнал событий
        private System.Timers.Timer timerSrv;           // Таймер периодичности опроса
        List<string>[] unitAnalogFiles;  // Список файлов аналоговых сигналов для Slave 3
        //List<string>[] unitA4Files = new List<string>();  // Список файлов аналоговых сигналов для Slave 4
        List<string>[] unitDiscreteFiles;                      // Список файлов дискретных сигналов для Slave 3
        //List<string> unitB4Files = new List<string>();  // Список файлов дискретных сигналов для Slave 4
        private ushort[][] AWAUS = null;                        // Переменная для аналоговых значений Slave 3 из файла AWAUS_UNIT3 
        private ushort[][] BWAUS = null;                        // Переменная для дискретных значений Slave 3 из файла BWAUS_UNIT3
        //private ushort[] AWAUS4;                        // Переменная для аналоговых значений Slave 4 из файла AWAUS_UNIT4
        //private ushort[] BWAUS4;                        // Переменная для дискретных значений Slave 4 из файла BWAUS_UNIT3
        // private ushort[] pto_1;                      // Переменная для аналоговых значений pto_1
        private IModbusService mbSlave;                 // Класс для трансляции данных в Modbus
        private IFileParse fileParse;                   // Класс для обработки файлов и записи их переменные
        private Thread threadSlave;                     // Поток, в котором будет работать Modbus
        List<byte> slaveId = null;                             // Массив 


        // Инициализация службы
        public ModbusRTUService()
        {
            #region Инициализация компонентов

            InitializeComponent();


            // Откртытие конфигурационного файла
            System.Configuration.Configuration appConfig =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Поиск секции настроек COM-порта из конфигурационного файла
            SlaveSettings slaveSettings =
                    (SlaveSettings)ConfigurationManager.GetSection("SlaveSettings");

            // Если секция найдена
            if (slaveSettings != null)
            {
                // Инициализируем массивы для аналоговых значений
                unitAnalogFiles = new List<string>[slaveSettings.SlaveItems.Count];
                
                // Инициализируем массивы для аналоговых значений
                unitDiscreteFiles = new List<string>[slaveSettings.SlaveItems.Count];
                
                // Считываем конфигурацию 
                
                foreach (Slaves slaves in slaveSettings.SlaveItems)     // Цикл списка устройств 
                {
                    // Добаляем адрес устройства из конфигурации
                    slaveId.Add(slaves.Id);
                    foreach (SlaveElement files in slaves.Slave)        // Цикл файлов для устройства
                    {
                        switch (files.Type)
                        {
                            // Добавление в список файлов файла с аналоговыми значениями
                            case "Analog":
                                unitAnalogFiles[slaves.Id].Add(files.FilePath);
                                break;
                            
                            // Добавление в список файлов файла с дискретными значениями
                            case "Discrete": 
                                unitDiscreteFiles[slaves.Id].Add(files.FilePath);
                                break;
                        }

                    }
                }
            }

            //Отключаем автоматическую запись в журнал
            AutoLog = false;

            // Создаем журнал событий и записываем в него
            if (!EventLog.SourceExists("ModbusRTUService")) //Если журнал с таким названием не существует
            {
                EventLog.CreateEventSource("ModbusRTUService", "ModbusRTUService"); // Создаем журнал
            }
            eventLog.Source = "ModbusRTUService"; //Помечаем, что будем писать в этот журнал

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
            timerSrv = new System.Timers.Timer(10000);
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

            foreach (byte id in slaveId)
            {
                AWAUS[id] = fileParse.AWAUSParse(unitAnalogFiles[id]);
                BWAUS[id] = fileParse.BWAUSParse(unitDiscreteFiles[id]);
            }
           
            #endregion

            #region Перезапуск потока Modbus

            StopSlaveThread();      //Остановска службы, если она запущена
            InitThreads();          //Инициализация данных потока
            threadSlave.Start();    //Запуск потока

            #endregion
        }

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
                mbSlave.CreateDataStore(id, ref AWAUS[id], ref BWAUS[id]);
            }
            
            //Передаем потоку функцию из класса ModbusService с номером порта
            threadSlave = new Thread(new ThreadStart(() => mbSlave.StartRTU()));
            //Помечаем поток как фоновый
            threadSlave.IsBackground = true;

            #endregion
        }

    }
}
