using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using ModbusRTUService;
using System.Configuration;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Diagnostics;
using System.Collections.Specialized;
using System.IO.Ports;


namespace ServiceConfig
{
    public partial class SAID : Form
    {
        private Configuration appConfig;        // Переменная для чтения конфигурации
        private ServiceController controller;   // Переменная для работы со службой
        private SlaveSettings slaveSettings;    // Переменная для конфигурации файлов с данными
        private AppSettingsSection SerialPortSection;  // Переменная для конфигурации порта
        private EventLog events = new EventLog();

        public SAID()
        {
            InitializeComponent();
            CheckService();
            ConfigInit();
            ComboBoxInit(cbPort, SerialPort.GetPortNames(), "PortName");
            ComboBoxInit(cbBaudRate, new string[] { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400" }, "BaudRate");
            ComboBoxInit(cbParity, Enum.GetNames(typeof (Parity)), "Parity");
             ///foreach (string parity in Enum.GetNames(typeof(Parity)))/
             ///{
             ///   cbParity.Items.Add(parity);
             ///}
             ///cbParity.SelectedItem = SerialPortSection["Parity"].Value;
            
           ComboBoxInit(cbStopBit, Enum.GetNames(typeof (StopBits)), "StopBits");

           ComboBoxInit(cbDataBits, new string[] { "4", "5", "6", "7", "8", }, "DataBits");
            
          //  foreach (string databits in new string[] { "4", "5", "6", "7", "8" })
           // {
          //      cbDataBits.Items.Add(databits);
          //  }
          //  cbDataBits.SelectedItem = SerialPortSection["DataBits"].Value;
            
        }

        private void ComboBoxInit(ComboBox cbItem, string[] strItems, string section)
        {
            foreach (string item in strItems)
            {
                cbItem.Items.Add(item);
            }
            cbItem.SelectedItem = SerialPortSection.Settings[section].Value;
        }

        #region Конфигурация программы
        private void ConfigInit()
        {
            appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            slaveSettings = (SlaveSettings)appConfig.GetSection("SlaveSettings");
            SerialPortSection = (AppSettingsSection)appConfig.GetSection("SerialPortSettings");

        }
        #endregion

        #region Установка службы ModbusRTUService
        private void bInst_Click(object sender, EventArgs e)
        {
           
           string[] args = {"ModbusRTUService.exe"};
           if(!ServiceIsExisted("ModbusRTUService"))
           {
               try
               {
                    ManagedInstallerClass.InstallHelper(args);
               }
               catch (Exception ex)
               {
                   MessageBox.Show(ex.Message);
                   return;
               }
           }
           CheckService(); // Проверяем установлена ли служба и ее статус
        }
        #endregion

        #region Проверить установлена ли служба ModbusRTUService
        private bool ServiceIsExisted(string p)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController s in services)
            {
                if (s.ServiceName == p)
                    return true;
            }
            return false;
        }
        #endregion

        #region Запуск службы
        private void bStart_Click(object sender, EventArgs e)
        {
            if (ServiceIsExisted("ModbusRTUService"))
            {
                controller.Start();
                bStop.Enabled = true;
                bStart.Enabled = false;
            }
        }
        #endregion

        #region Остановка службы
        private void bStop_Click(object sender, EventArgs e)
        {
            if (ServiceIsExisted("ModbusRTUService"))
            {
                controller.Stop();
                bStop.Enabled = false;
                bStart.Enabled = true;
            }
        }
        #endregion

        #region Расстановка действий при установленной службе
        private void CheckService()
        {
            // Если служба установлена
            if (ServiceIsExisted("ModbusRTUService"))
            {
                // Проверяем статус службы и выставляем действия
                CheckStatus();
                bDel.Enabled = true;    // Кнокпа "Установить" активна
                bInst.Enabled = false;  // Кнокпа "Удалить" заблокирована
            }
            else
            {
                bDel.Enabled = false;   // Кнокпа "Установить" заблокирована
                bInst.Enabled = true;   // Кнокпа "Удалить" активна
                bStop.Enabled = false;  // Кнокпа "Стоп" заблокирована
                bStart.Enabled = false; // Кнокпа "Старт" заблокирована
            }
        }
        #endregion

        #region Проверить запущена ли служба ModbusRTUService
        private void CheckStatus()
        {
            // Cоздаем переменную с указателем на службу
            controller = new ServiceController("ModbusRTUService");
            // Усли служба запущена
            if (controller.Status == ServiceControllerStatus.Running)
            {
                bStop.Enabled = true;   // Кнокпа "Стоп" активна
                bStart.Enabled = false; // Кнокпа "Старт" заблокирована
            }
            else
            {
                bStop.Enabled = false;  // Кнокпа "Стоп" заблокирована
                bStart.Enabled = true;  // Кнокпа "Старт" активна
            }

        }
        #endregion

        #region Удаление службы
        private void bDel_Click(object sender, EventArgs e)
        {
            string[] args = { "/u","ModbusRTUService.exe" };
            if (ServiceIsExisted("ModbusRTUService"))
            {
                try
                {
                    ManagedInstallerClass.InstallHelper(args);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            CheckService(); // Проверяем установлена ли служба и ее статус
        }
        #endregion

        #region Сохранение параметров COM-порта
        private void bCOMSave_Click(object sender, EventArgs e)
        {
            if (!SerialPortSection.SectionInformation.IsProtected)
            {
                SerialPortSection.SectionInformation.ForceSave = true;
                SerialPortSection.Settings["PortName"].Value = cbPort.SelectedText;
                SerialPortSection.Settings["BaudRate"].Value = cbBaudRate.SelectedText;
                SerialPortSection.Settings["Parity"].Value = cbParity.SelectedText;
                SerialPortSection.Settings["DataBits"].Value = cbDataBits.SelectedText;
                SerialPortSection.Settings["StopBits"].Value = cbStopBit.SelectedText;
                
                //ConfigurationManager.RefreshSection("SerialPortSettings");
                appConfig.Save(ConfigurationSaveMode.Modified, true);
                //appConfig;
            }
        }
        #endregion

        #region Обновление ошибок 
        private void bRefreshEvent_Click(object sender, EventArgs e)
        {
            gvEvents.Rows.Clear();
            if (!EventLog.SourceExists("ModbusRTUService"))
            {
                EventLog.CreateEventSource("ModbusRTUService", "ModbusRTUService"); // Создаем журнал
            }
            events.Log = "ModbusRTUService";
            events.Source = "ModbusRTUService";
            if (events.Entries.Count > 0)
            {
                foreach (EventLogEntry entry in events.Entries)
                {
                    gvEvents.Rows.Add(entry.TimeGenerated, entry.Message);
                    if (entry.EntryType == EventLogEntryType.Error)
                    {
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["DateEvent"].Style.BackColor = System.Drawing.Color.Red;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["DateEvent"].Style.ForeColor = System.Drawing.Color.White;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["EventMessage"].Style.BackColor = System.Drawing.Color.Red;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["EventMessage"].Style.ForeColor = System.Drawing.Color.White;

                    }
                    else
                    {
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["DateEvent"].Style.BackColor = System.Drawing.Color.White;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["DateEvent"].Style.ForeColor = System.Drawing.Color.Black;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["EventMessage"].Style.BackColor = System.Drawing.Color.White;
                        gvEvents.Rows[gvEvents.Rows.Count - 1].Cells["EventMessage"].Style.ForeColor = System.Drawing.Color.Black;
                    }
                }
                gvEvents.Sort(DateEvent, ListSortDirection.Descending);
            }
        }
        #endregion

        #region Просмотр ошибки
        private void gvEvents_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            MessageBox.Show(gvEvents.Rows[e.RowIndex].Cells["EventMessage"].Value.ToString(), gvEvents.Rows[e.RowIndex].Cells["DateEvent"].Value.ToString());
        }
        #endregion
    }
}
