using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaseDrop
{
    public partial class Main : Form
    {
        // Get the path for exporting
        private static string ApplicationDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string WorkingDirectory = string.Empty;
        private static string ResultDirectory = string.Empty;

        public Main()
        {
            InitializeComponent();
            // Setup
            SetupDirectories();
            // Setup files
            SetupFiles();
        }

        private void SetupFiles()
        {
            // Extract the converters
            File.WriteAllBytes(Path.Combine(WorkingDirectory, "ADPCMEnc.exe"), Properties.Resources.ADPCMEncode);
            File.WriteAllBytes(Path.Combine(WorkingDirectory, "XWMAEnc.exe"), Properties.Resources.xWMAEncode);
        }

        private void SetupDirectories()
        {
            // Make directories
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "BaseDrop");
            // Make it
            if (!Directory.Exists(WorkingDirectory))
            {
                // Make it
                Directory.CreateDirectory(WorkingDirectory);
            }
            // Result
            ResultDirectory = Path.Combine(ApplicationDirectory, "exported_files");
            // Make it
            if (!Directory.Exists(ResultDirectory))
            {
                // Make it
                Directory.CreateDirectory(ResultDirectory);
            }
            // Make specific dirs
            if (!Directory.Exists(Path.Combine(ResultDirectory, "normal")))
            {
                // Make it
                Directory.CreateDirectory(Path.Combine(ResultDirectory, "normal"));
            }
            if (!Directory.Exists(Path.Combine(ResultDirectory, "bo_ready")))
            {
                // Make it
                Directory.CreateDirectory(Path.Combine(ResultDirectory, "bo_ready"));
            }
        }

        private void FormatXWMA_Click(object sender, EventArgs e)
        {
            // Uncheck ADPCM
            this.FormatADPCM.isChecked = false;
        }

        private void FormatADPCM_Click(object sender, EventArgs e)
        {
            // Uncheck XWMA
            this.FormatXWMA.isChecked = false;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up
            if (Directory.Exists(WorkingDirectory))
            {
                // Delete
                try
                {
                    // Clean it up
                    Directory.Delete(WorkingDirectory, true);
                }
                catch
                {
                    // Nothing
                }
            }
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            // Get points
            int x = this.PointToClient(new Point(e.X, e.Y)).X;
            int y = this.PointToClient(new Point(e.X, e.Y)).Y;
            // Check
            if (x >= ConverterBox.Location.X && x <= ConverterBox.Location.X + ConverterBox.Width && y >= ConverterBox.Location.Y && y <= ConverterBox.Location.Y + ConverterBox.Height)
            {
                // Convert them
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Show converter
                if (files != null && files.Length > 0)
                {
                    // Show it
                    new Converter(this, files, ResultDirectory, WorkingDirectory, this.FormatADPCM.isChecked, this.FormatXWMA.isChecked, this.FormatLooping.isChecked).ShowDialog();
                }
            }
        }

        private void Main_DragOver(object sender, DragEventArgs e)
        {
            // Get points
            int x = this.PointToClient(new Point(e.X, e.Y)).X;
            int y = this.PointToClient(new Point(e.X, e.Y)).Y;
            // Check
            if (x >= ConverterBox.Location.X && x <= ConverterBox.Location.X + ConverterBox.Width && y >= ConverterBox.Location.Y && y <= ConverterBox.Location.Y + ConverterBox.Height)
            {
                // Allow it
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                // Nope
                e.Effect = DragDropEffects.None;
            }
        }
    }
}
