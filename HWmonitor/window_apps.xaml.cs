using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace HWmonitor
{
    /// <summary>
    /// Interaction logic for window_apps.xaml
    /// </summary>
    public partial class window_apps : Window
    {
        List<string> apps = new List<string>();

        public window_apps()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(new Action(() => AppsSearch()));
        }

        ManagementObjectSearcher searcher;

        void AppsSearch()
        {            
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Product");

            foreach (var item in searcher.Get())
            {
                lb_appList.Items.Add(item["Name"] + " " + item["Version"]);
                apps.Add(Convert.ToString(item["Name"]));
            }     
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text File|*.txt";
            saveFileDialog1.Title = "Save";

            if (saveFileDialog1.ShowDialog() == true)
            {
                File.WriteAllLines(saveFileDialog1.FileName, apps);
            }
        }
    }
}
