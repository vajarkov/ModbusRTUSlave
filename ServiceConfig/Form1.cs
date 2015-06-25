using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using ModbusRTUService;
using System.Configuration.Assemblies;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Diagnostics;


namespace ServiceConfig
{
    public partial class Form1 : Form
    {
        private ServiceController controller;
        public Form1()
        {
            InitializeComponent();
            
        }

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
           bDel.Enabled = true;
           bInst.Enabled = false;
           ControllerInit();
        }

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

        private void bStart_Click(object sender, EventArgs e)
        {
            if (ServiceIsExisted("ModbusRTUService"))
            {
                controller.Start();
                bStop.Enabled = true;
                bStart.Enabled = false;
            }
        }

        private void bStop_Click(object sender, EventArgs e)
        {
            if (ServiceIsExisted("ModbusRTUService"))
            {
                controller.Stop();
                bStop.Enabled = false;
                bStart.Enabled = true;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

            if (ServiceIsExisted("ModbusRTUService"))
            {
                ControllerInit();
                bDel.Enabled = true;
                bInst.Enabled = false;
            }
            else
            {
                bInst.Enabled = true;
                bStop.Enabled = false;
                bStart.Enabled = false;
                bInst.Enabled = false;
            }
        }

        private void ControllerInit()
        {
            controller = new ServiceController("ModbusRTUService");
            if (controller.Status == ServiceControllerStatus.Running)
            {
                bStop.Enabled = true;
                bStart.Enabled = false;
            }
            else
            {
                bStop.Enabled = false;
                bStart.Enabled = true;
            }

        }

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
            if (ServiceIsExisted("ModbusRTUService"))
            {
                ControllerInit();
                bDel.Enabled = true;
                bInst.Enabled = false;
            }
            else
            {
                bInst.Enabled = true;
                bDel.Enabled = false;
                bStop.Enabled = false;
                bStart.Enabled = false;
            }
            
            
        }
    }
}
