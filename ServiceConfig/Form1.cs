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


namespace ServiceConfig
{
    public partial class Form1 : Form
    {
        private ServiceController controller;
        public Form1()
        {
            InitializeComponent();
            CheckService();
            ConfigInit();
        }

        private void ConfigInit()
        {
            System.Configuration.Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            SlaveSettings slaveSettings = (SlaveSettings)ConfigurationManager.GetSection("SlaveSettings");
            NameValueCollection SerialPortSection = (NameValueCollection)ConfigurationManager.GetSection("SerialPortSettings");

        }

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
    }
}
