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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;

namespace lmutil_parser_win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String fileName = "";
        Lmutil_location lmutil_location;
        public MainWindow()
        {
            InitializeComponent();
            textBox_result.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            lmutil_location=deserializeLmutil();
            if (lmutil_location == null)
            {
                textBox_path.Text = "lmutil location unknown";
            }
            textBox_args.Text = "lmstat -c \" % SE_LICENSE_SERVER % \" -A";
            textBox_path.IsReadOnly = true;
            textBox_result.IsReadOnly = true;
        }

        private void button_chooseLog_Click(object sender, RoutedEventArgs e)
        {
            //Opens a file dialog, then creates a new Lmutil_location object and serializes it
       
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "exe files (*.exe) |*.exe|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                //serialize
                String tempFileName = openFileDialog.FileName;

                Lmutil_location obj = new Lmutil_location();
                obj.Location = tempFileName;

                serializeLmutil(obj);

                textBox_result.Text = tempFileName + "\n";
            }
            else
            {
                return;
            }
        }

        private void button_reset_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(@"krong.bin");
            textBox_path.Text = "";
        }

        private void button_launch_Click(object sender, RoutedEventArgs e)
        {
            //check if lmutil location is known
            //check if file exists
            String lmutilFile = @"krong.bin";
            if (!File.Exists(lmutilFile))
            {
                //Opens an Error message and a file dialog, then creates a new Lmutil_location object and serializes it
                MessageBox.Show("Please point to the location of lmutil.exe");

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "exe files (*.exe) |*.exe|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == true)
                {
                    //serialize
                    String tempFileName = openFileDialog.FileName;

                    Lmutil_location obj = new Lmutil_location();
                    obj.Location = tempFileName;

                    serializeLmutil(obj);

                    textBox_result.Text = tempFileName + "\n";
                }
                else
                {
                    return;
                }
            }

            //deserialize and use path object 
            lmutil_location = deserializeLmutil();

            //parse args
            StreamWriter outStream = new StreamWriter("log.txt");
            Process process = new Process();
            process.StartInfo.FileName = lmutil_location.Location;
            process.StartInfo.Arguments = textBox_args.Text;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    outStream.WriteLine(e.Data);
                }
            });

            process.Start();

            process.BeginOutputReadLine();

            process.WaitForExit();
            process.Close();

            outStream.Close();

            //write to textbox from log
            textBox_result.Text = "complete lmutil output can be found in log.txt";

            StreamReader inStream = new StreamReader("log.txt");
            String line;
            while((line=inStream.ReadLine()) != null)
            {
                textBox_result.AppendText(processLine(line));
            }
        }

        public String processLine(String input)
        {
            String output = "";
            //return input; //-- DEBUG
            //pattern 1
            String pattern = @"\bUsers of (\w+): .*";
            MatchCollection matches = Regex.Matches(input, pattern);
            if(matches.Count()>0)
            {
                output += "\n\n Users of: " + matches.First().Groups[1];
                return output;
            }

            //pattern 2
            pattern = @"\s*(\w+) (\w+) (\S|\.)* \(.*\) \(.*\)";
            matches = Regex.Matches(input, pattern);
            if (matches.Count() > 0)
            {
                output += "\n   User: " + matches.First().Groups[1] +" Machine: " + matches.First().Groups[2];
            }

            return output;
        }

        public void serializeLmutil(Lmutil_location toSerialize)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("krong.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, toSerialize);
            stream.Close();
        }
        
        public Lmutil_location deserializeLmutil()
        {
            if (!File.Exists(@"krong.bin"))
            {
                return null;
            }
            IFormatter formatter2 = new BinaryFormatter();
            Stream stream2 = new FileStream("krong.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
            Lmutil_location lmutil = (Lmutil_location)formatter2.Deserialize(stream2);
            stream2.Close();
            textBox_path.Text = lmutil.Location;
            return lmutil;
        }
    }


    [Serializable]
    public class Lmutil_location
    {
        private String location = "";

        public string Location    // the Name property
        {
            get => location;
            set => location = value;
        }
    }
}
