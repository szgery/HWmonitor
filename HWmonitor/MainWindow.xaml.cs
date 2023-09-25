using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using System.Management;
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Security.Permissions;
using System.Net.NetworkInformation;

namespace HWmonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> productList = new List<string>();

        //help from stackoverflow.com
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            int mag = (int)Math.Log(value, 1024);

            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        Computer thisComputer;
        ManagementObjectSearcher search;

        
        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]

        public static bool isAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void AdminRelauncher()
        {
            if (!isAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;

                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("This program must be run as an administrator! \n\n" + ex.ToString());
                }
            }
        }

        public MainWindow()
        {
            try
            {
                if (isAdmin() == true)
                {
                    InitializeComponent();
                    Timer();

                    thisComputer = new Computer() { CPUEnabled = true, RAMEnabled = true, GPUEnabled = true };
                    thisComputer.Open();

                    GetHardwareDatas();
                }
                else
                {
                    AdminRelauncher();
                }
            }catch (Exception ex)
            {
                MessageBox.Show("Error: ", ex.ToString());
            }
            
        }

        public void Timer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_tick;
            timer.Start();
        }        

        private void Timer_tick(object sender, EventArgs e)
        {
            search = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            String temp = "";
            float temp1 = 0;
            int load = 0;
            int gpuPerc = 0;

            foreach (var item in thisComputer.Hardware)
            {
                if (item.HardwareType == HardwareType.CPU)
                {
                    item.Update();                    
                    foreach (var sensor in item.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            temp += (int)sensor.Value;                            
                            temp1 = (float)sensor.Value;
                        }                        
                    }                    
                }

                if (item.HardwareType == HardwareType.CPU)
                {
                    item.Update();
                    foreach (var asd in item.Sensors)
                    {
                        if (asd.SensorType == SensorType.Load)
                        {
                            load = (int)asd.Value;
                        }
                       // System.Threading.Thread.Sleep(1000);
                    }                    
                }

                if (item.HardwareType == HardwareType.RAM)
                {
                    item.Update();

                    foreach (IHardware subHw in item.SubHardware)
                    {
                        subHw.Update();
                    }

                    foreach (var sensor in item.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            memLevel.Value = Math.Round(Convert.ToDouble(sensor.Value), 1);
                        }
                    }
                }

                if (item.HardwareType == HardwareType.GpuAti || item.HardwareType == HardwareType.GpuNvidia)
                {
                    item.Update();
                    foreach (var sensor in item.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            gpuLevel.Value = sensor.Value.Value;
                            gpuPerc = (int)sensor.Value;                            
                        }
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            gpuTempLevel.Maximum = 85;
                            gpuTempLevel.Value = sensor.Value.Value;
                            gpuTempText.Text = sensor.Value.Value.ToString() + " °C";
                        }
                    }
                }
            }
            

            currTemp.Text = temp.ToString() + " °C";
            cputemp.Value = temp1;
            cpuLoad.Value = load;
            cpuLoadText.Text = load.ToString() + "%";
            gpuLevelPercentage.Text = gpuPerc.ToString() + "%";
        }

        List<string> saveList = new List<string>();

        public void GetHardwareDatas()
        {
            CPU();

            MotherBoard();
            Memory();
            NetworkAdapter();
            Disks();
            GPU();
            Products();           
        }

        void CPU()
        {
            string cpu = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var item in search.Get())
            {
                cpuname.Items.Add(item["Name"].ToString() + " " + item["MaxClockSpeed"] + "MHz");
                cpuLoad.Value = Convert.ToDouble(item["LoadPercentage"]);
                cpu = "Processor: " + item["Name"].ToString() + " " + item["MaxClockSpeed"] + "MHz";


            }
            saveList.Add(cpu);
        }
        void MotherBoard()
        {
            string mb = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (var item in search.Get())
            {
                motherboardTextBox.Content = item["Manufacturer"].ToString();
                motherboardTextBox.Content += " " + item["Product"].ToString();

                mb = "Motherboard: " + item["Manufacturer"].ToString() + " " + item["Product"].ToString();
            }
            saveList.Add(mb);
        }

        void Memory()
        {
            string mem = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");

            foreach (var item in search.Get())
            {
                memorytype.Items.Add(item["Name"].ToString() + " " + SizeSuffix(Convert.ToInt64(item["Capacity"].ToString())));
                mem = "Memory: " + item["Name"].ToString() + " " + SizeSuffix(Convert.ToInt64(item["Capacity"].ToString()));
            }
            saveList.Add(mem);
        }

        void NetworkAdapter()
        {
            string NwA = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter");
            foreach (var item in search.Get())
            {
                networkAdapter.Items.Add(item["Manufacturer"] + " " + item["Name"]);;                
                NwA = "Network adapter: " + item["Manufacturer"].ToString() + " " + item["Name"];
            }
            saveList.Add(NwA);
        }

        void Disks()
        {
            string disks = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (var item in search.Get())
            {
                diskListBox.Items.Add(item["Model"] + " " + SizeSuffix(Convert.ToInt64(item["Size"].ToString())));

                disks = "Disks: " + item["Model"] + " " + SizeSuffix(Convert.ToInt64(item["Size"].ToString())) + "/r/n";
            }
            saveList.Add(disks);
        }

        void GPU()
        {
            string gpu = "";

            search = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var item in search.Get())
            {
                gpuListBox.Items.Add(item["Name"].ToString());
                gpu = "GPU: " + item["Name"].ToString();
            }
            saveList.Add(gpu);
        }

        void Products()
        {

            search = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
            foreach (var item in search.Get())
            {
                appList.Items.Add(item["Name"] + " " + item["Version"]);
                productList.Add(item["Name"] + " " + item["Version"]);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text File|*.txt";
            saveFileDialog1.Title = "Save";
            

            if (saveFileDialog1.ShowDialog() == true)
            {
                foreach (var item in saveList)
                {
                    File.AppendAllLines(saveFileDialog1.FileName, saveList);
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text File|*.txt";
            saveFileDialog1.Title = "Save";


            if (saveFileDialog1.ShowDialog() == true)
            {
                foreach (var item in productList)
                {
                    File.AppendAllLines(saveFileDialog1.FileName, productList);
                }
            }
        }

        private void diskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
