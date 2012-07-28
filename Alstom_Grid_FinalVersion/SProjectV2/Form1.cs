using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TVA.Collections;
using TVA.Configuration;
using TVA.Historian;
using TVA.Historian.Files;
using TVA.IO;

namespace SProjectV2
{
    public partial class Form1 : Form
    {
        public class MetadataWrapper
        {
            private MetadataRecord m_metadata;

            /// <summary>
            /// Creates a new instance of the <see cref="MetadataWrapper"/> class.
            /// </summary>
            /// <param name="metadata">The <see cref="MetadataRecord"/> to be wrapped.</param>
            public MetadataWrapper(MetadataRecord metadata)
            {
                m_metadata = metadata;
            }

            /// <summary>
            /// Determines whether the measurement represented by this metadata record
            /// should be exported to CSV by the Historian Data Viewer.
            /// </summary>
            public bool Export
            {
                get;
                set;
            }

            /// <summary>
            /// Determines whether the measurement represented by this metadata record
            /// should be displayed on the Historian Data Viewer graph.
            /// </summary>
            public bool Display
            {
                get;
                set;
            }
            public bool Data
            {
                get;
                set;
            }
            /// <summary>
            /// Gets the point number of the measurement.
            /// </summary>
            public int PointNumber
            {
                get
                {
                    return m_metadata.HistorianID;
                }
            }

            /// <summary>
            /// Gets the name of the measurement.
            /// </summary>
            public string Name
            {
                get
                {
                    return m_metadata.Name;
                }
            }

            /// <summary>
            /// Gets the description of the measurement.
            /// </summary>
            public string Description
            {
                get
                {
                    return m_metadata.Description;
                }
            }

            /// <summary>
            /// Gets the first alternate name for the measurement.
            /// </summary>
            public string Synonym1
            {
                get
                {
                    return m_metadata.Synonym1;
                }
            }

            /// <summary>
            /// Gets the second alternate name for the measurement.
            /// </summary>
            public string Synonym2
            {
                get
                {
                    return m_metadata.Synonym2;
                }
            }

            /// <summary>
            /// Gets the third alternate name for the measurement.
            /// </summary>
            public string Synonym3
            {
                get
                {
                    return m_metadata.Synonym3;
                }
            }

            /// <summary>
            /// Gets the system name.
            /// </summary>
            public string System
            {
                get
                {
                    return m_metadata.SystemName;
                }
            }

            /// <summary>
            /// Gets the low range of the measurement.
            /// </summary>
            public Single LowRange
            {
                get
                {
                    return m_metadata.Summary.LowRange;
                }
            }

            /// <summary>
            /// Gets the high range of the measurement.
            /// </summary>
            public Single HighRange
            {
                get
                {
                    return m_metadata.Summary.HighRange;
                }
            }

            /// <summary>
            /// Gets the engineering units used to measure the values.
            /// </summary>
            public string EngineeringUnits
            {
                get
                {
                    return m_metadata.AnalogFields.EngineeringUnits;
                }
            }

            /// <summary>
            /// Gets the unit number of the measurement.
            /// </summary>
            public int Unit
            {
                get
                {
                    return m_metadata.UnitNumber;
                }
            }

            /// <summary>
            /// Returns the wrapped <see cref="MetadataRecord"/>.
            /// </summary>
            /// <returns>The wrapped metadata record.</returns>
            public MetadataRecord GetMetadata()
            {
                return m_metadata;
            }
        }
        public static ICollection<ArchiveFile> m_archiveFiles;
        public static List<MetadataWrapper> m_metadata;
        public static string m_startTime = "";
        public static string m_endTime = "";
        public List<string> PMUnames = new List<string>();
        private struct csvParam
        {
            public string filePath;
            public Form1 myForm;
        };

        public Form1()
        {
            InitializeComponent();
            Text = "VisualData";
            dateTimePicker1.CustomFormat = "MM/dd/yy - HH.mm.ss";
            dateTimePicker2.CustomFormat = "MM/dd/yy - HH.mm.ss";
            dateTimePicker1.Update();
            dateTimePicker2.Update();
            treeView1.PathSeparator = "\\";
        }

        private static ArchiveFile OpenArchiveFile(string fileName)
        {
            Console.WriteLine("Inside OpenArchiveFile function..");

            string m_archiveLocation = FilePath.GetDirectoryName(fileName);

            //string m_archiveLocation = "C:\\Program Files\\openPDC\\Archive\\";
            Console.WriteLine("This is the m_archiveLocation: {0}", m_archiveLocation);
            string instance = fileName.Substring(0, fileName.LastIndexOf('_'));
            ArchiveFile file = new ArchiveFile();
            file.FileName = fileName;
            file.FileAccessMode = FileAccess.Read;

            file.StateFile = new StateFile();
            file.StateFile.FileAccessMode = FileAccess.Read;
            file.StateFile.FileName = string.Format("{0}_startup.dat", instance);

            file.IntercomFile = new IntercomFile();
            file.IntercomFile.FileAccessMode = FileAccess.Read;
            file.IntercomFile.FileName = string.Format("{0}scratch.dat", m_archiveLocation);

            file.MetadataFile = new MetadataFile();
            file.MetadataFile.FileAccessMode = FileAccess.Read;
            file.MetadataFile.FileName = string.Format("{0}_dbase.dat", instance);
            file.MetadataFile.LoadOnOpen = true;

            Console.WriteLine("OpenArchiveFile has finished");
            file.Open();
            return file;
        }

        private static void ClearArchives()
        {
            foreach (ArchiveFile file in m_archiveFiles)
                file.Close();

            m_archiveFiles.Clear();
            m_metadata.Clear();
        }

        private static void OpenArchives(string[] fileNames)
        {
            ClearArchives();
            Console.WriteLine("Inside OpenArchives function...");
            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName))
                    m_archiveFiles.Add(OpenArchiveFile(fileName));
            }


        }

        private static void OpenArchive(string fileName)
        {
            Console.WriteLine("Inside OpenArchive function.....");
            OpenArchives(new string[] { fileName });
        }

        private void exportToCSV(string path, System.ComponentModel.BackgroundWorker worker,
System.ComponentModel.DoWorkEventArgs e)
        {
            bool exit = false;
            string csvFilePath = path;
            string startTime = dateTimePicker1.Value.ToString();
            string endTime = dateTimePicker2.Value.ToString();
            if (csvFilePath != null)
            {

                // Progress Bar setup 
                int updateCounter = 0;  //used to determine when to update progress bar
                int updateOnCount = progressBar1.Step; //when updateCounter reaches updateOnCount, update progress bar
                int progressBarMax = getProgressBarMax(startTime, endTime);
                worker.ReportProgress(0, progressBarMax); //informs main thread of progress bar max value

                TextWriter writer = new StreamWriter(new FileStream(csvFilePath, FileMode.Create, FileAccess.Write));
                writer.WriteLine("Exported data to CSV");
                writer.WriteLine(("Start date: " + startTime));
                writer.WriteLine(("End date: " + endTime));
                writer.WriteLine("\nIf Using Microsoft Excel\nTo switch from rows to columns: \n1) highlight all the data\n2) copy the selection\n3) right-click in an open cell below the selection\n4) choose paste special and check the 'transpose' box\n\n\n");
                bool firstTimeThrough = true;
                foreach (ArchiveFile file in m_archiveFiles)
                {
                    foreach (MetadataRecord record in file.MetadataFile.Read())
                    {
                        if (firstTimeThrough == true)
                        {
                            writer.Write("Date & Time,");
                            foreach (IDataPoint point in file.ReadData(record.HistorianID, startTime, endTime))
                            {
                                writer.Write((point.Time.ToString() + ","));
                            }
                            firstTimeThrough = false;
                            writer.WriteLine();
                        }
                        foreach (string checkedItem in checkedListBox1.CheckedItems)
                        {
                            if (checkedItem.ToString() == record.Description)
                            {
                                if (record.GeneralFlags.Enabled)
                                {
                                    writer.Write(record.Name);
                                    writer.Write(" ");
                                    writer.Write(record.Description);
                                    writer.Write(",");
                                    foreach (IDataPoint point in file.ReadData(record.HistorianID, startTime, endTime))
                                    {
                                        if (worker.CancellationPending)
                                        {
                                            e.Cancel = true;
                                            exit = true;
                                            break;
                                        }
                                        writer.Write((point.Value + ","));
                                        updateCounter++;
                                        if (updateCounter >= updateOnCount)
                                        {
                                            worker.ReportProgress(0); //update progress bar
                                            updateCounter = 0;
                                        }
                                    }
                                    writer.WriteLine();
                                    if (exit)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                writer.Close();
            }
        }







        private void processDir(string dirName)
        {
            string name = "";
            bool found = false;
            string[] fileEntries = Directory.GetFiles(dirName);
            foreach (string fileName in fileEntries)
            {
                if (fileName.IndexOf("archive.d") != -1)
                {
                    name = fileName;
                    found = true;
                }
            }

            if (found)
            {
                int numseries = 0;
                progressBar1.Value = 0;
                m_archiveFiles = new List<ArchiveFile>();
                m_metadata = new List<MetadataWrapper>();
                OpenArchive(name);
                int count = 0;
                foreach (ArchiveFile file in m_archiveFiles)
                {
                    progressBar1.Maximum = file.MetadataFile.RecordsInMemory;
                    foreach (MetadataRecord record in file.MetadataFile.Read())
                    {
                        if (record.GeneralFlags.Enabled)
                        {
                            chart1.Series.Add(record.Description);
                            chart1.Series[numseries].ChartType = SeriesChartType.Line;
                            chart1.Series[numseries].Enabled = false;
                            checkedListBox1.Items.Add(record.Description);
                            m_startTime = dateTimePicker1.Value.ToString();
                            m_endTime = dateTimePicker2.Value.ToString();
                            foreach (IDataPoint point in file.ReadData(record.HistorianID, m_startTime, m_endTime))
                            {

                                chart1.Series[numseries].Points.AddXY(point.Time.ToString(), point.Value);

                            }
                            numseries++;
                            if (count < progressBar1.Maximum)
                            {
                                progressBar1.Value = progressBar1.Value + 1;
                                count++;
                            }
                        }
                    }
                }
                ClearArchives();
            } // close if(found)
        } // close processDir

        private void addPMUnames(string dirName)
        {
            string name = "";
            bool found = false;
            List<string> PMUs = new List<string>();
            string[] fileEntries = Directory.GetFiles(dirName);
            foreach (string fileName in fileEntries)
            {
                if (fileName.IndexOf("archive.d") != -1)
                {
                    name = fileName;
                    found = true;
                }
            }

            if (found)
            {
                m_archiveFiles = new List<ArchiveFile>();
                m_metadata = new List<MetadataWrapper>();
                OpenArchive(name);



                foreach (ArchiveFile fileName in m_archiveFiles)
                {
                    foreach (MetadataRecord record in fileName.MetadataFile.Read())
                    {

                        if (record.GeneralFlags.Enabled)
                        {
                            PMUs.Add(record.SystemName);
                        }
                    }
                }

                foreach (string PMU in PMUs.Distinct())
                {
                    treeView1.Nodes[0].Nodes.Add(PMU);
                }
                ClearArchives();
            }
        }

        private string GetCsvFilePath()
        {


            saveFileDialog1.Filter = "CSV files|*.csv";
            saveFileDialog1.DefaultExt = "csv";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.CheckPathExists = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog1.FileName;
            }
            else
            {
                return null;
            }
        }






        private void SelectRoot_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                treeView1.Nodes.Add(folderBrowserDialog1.SelectedPath);
                addPMUnames(folderBrowserDialog1.SelectedPath);
            }
        }

        private void GraphData_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0)
            {
                MessageBox.Show("No root has been selected.\nPlease select a root.", "Root error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (dateTimePicker1.Value > dateTimePicker2.Value)
            {
                MessageBox.Show("End date is before start date.\nPlease set end date to after start date.", "Time date error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                statusLabel.Text = "Reading data and rendering chart...";
                while (chart1.Series.Count != 0)
                {
                    chart1.Series.RemoveAt(0);
                }

                while (chart1.Legends.Count != 0)
                {
                    chart1.Legends.RemoveAt(0);
                }

                while (chart1.ChartAreas.Count != 0)
                {
                    chart1.ChartAreas.RemoveAt(0);
                }

                for (int i = 0; i <= checkedListBox1.Items.Count - 1; i++)
                {
                    if (checkedListBox1.GetItemChecked(i))
                    {
                        getData(checkedListBox1.Items[i].ToString());
                    }
                }
                MessageBox.Show("Chart Update Complete");
                statusLabel.Text = "";
                progressBar1.Value = 0; //end progress bar
            }
        }

        private void UpdateControl_Click(object sender, EventArgs e)
        {
            PMUnames.Clear();

            while (checkedListBox1.Items.Count != 0)
            {
                checkedListBox1.Items.RemoveAt(0);
            }

            if (treeView1.Nodes.Count != 0)
            {
                for (int i = 0; i <= treeView1.Nodes[0].Nodes.Count - 1; i++)
                {
                    if (treeView1.Nodes[0].Nodes[i].Checked)
                    {
                        PMUnames.Add(treeView1.Nodes[0].Nodes[i].Text);
                    }
                }

                string name = "";
                bool found = false;
                string[] fileEntries = Directory.GetFiles(treeView1.Nodes[0].Text);
                foreach (string fileName in fileEntries)
                {
                    if (fileName.IndexOf("archive.d") != -1)
                    {
                        name = fileName;
                        found = true;
                    }
                }

                if (found)
                {
                    m_archiveFiles = new List<ArchiveFile>();
                    m_metadata = new List<MetadataWrapper>();
                    OpenArchive(name);



                    foreach (ArchiveFile fileName in m_archiveFiles)
                    {
                        foreach (MetadataRecord record in fileName.MetadataFile.Read())
                        {

                            if (record.GeneralFlags.Enabled)
                            {
                                if (PMUnames.Contains(record.SystemName))
                                {
                                    checkedListBox1.Items.Add(record.Description);
                                }
                            }
                        }
                    }
                    ClearArchives();
                }
            }
            else
            {
                MessageBox.Show("No root has been selected.\nPlease select a root.", "Root error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void CSV_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0)
            {
                MessageBox.Show("No root has been selected.\nPlease select a root.", "Root error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (dateTimePicker1.Value > dateTimePicker2.Value)
            {
                MessageBox.Show("End date is before start date.\nPlease set end date to after start date.", "Time date error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                string[] fileEntries = Directory.GetFiles(treeView1.Nodes[0].FullPath);
                string name = "";
                foreach (string fileName in fileEntries)
                {
                    if (fileName.IndexOf("archive.d") != -1)
                    {
                        name = fileName;
                    }
                }

                if (name != "")
                {
                    csvParam param = new csvParam();
                    param.myForm = this;
                    param.filePath = GetCsvFilePath();
                    if (param.filePath != null)
                    {
                        statusLabel.Text = "Exporting data to CSV...";
                        OpenArchive(name); //cleared on thread done event
                        this.Cancel.Enabled = true;
                        this.CSV.Enabled = false;
                        backgroundWorker1.RunWorkerAsync(param);
                    }
                }
            }
        }





        private void pieGraph(string dataType)
        {
            double totalPoints = 0;
            double temp;
            Dictionary<double, double> dict = new Dictionary<double, double>();
            m_startTime = dateTimePicker1.Value.ToString();
            m_endTime = dateTimePicker2.Value.ToString();

            // Progress Bar setup 
            int updateCounter = 0;  //used to determine when to update progress bar
            int updateOnCount = progressBar1.Step; //when updateCounter reaches updateOnCount, update progress bar
            progressBar1.Maximum = getProgressBarMax(m_startTime, m_endTime);

            foreach (ArchiveFile fileName in m_archiveFiles)
            {
                foreach (MetadataRecord record in fileName.MetadataFile.Read())
                {
                    if (record.GeneralFlags.Enabled)
                    {
                        if (record.Description == dataType)
                        {
                            foreach (IDataPoint point in fileName.ReadData(record.HistorianID, m_startTime, m_endTime))
                            {
                                // Update progress bar
                                updateCounter++;
                                if (updateCounter >= updateOnCount)
                                {
                                    progressBar1.PerformStep(); //update progress bar
                                    updateCounter = 0;
                                }

                                totalPoints++;
                                if (!dict.ContainsKey(point.Value))
                                {
                                    // if point.Value isn't in dict, add it with 1 occurance
                                    dict.Add(point.Value, 1);
                                }
                                else
                                {
                                    // point.Value already exists in the dict, so add another occurance of it
                                    temp = dict[point.Value];
                                    temp++;
                                    dict[point.Value] = temp;
                                }
                            }

                            ChartArea temp1 = chart1.ChartAreas.Add(dataType);
                            Series temp2 = chart1.Series.Add(dataType);
                            Legend temp3 =  chart1.Legends.Add(dataType);

                            temp3.DockedToChartArea = dataType;
                            temp3.IsDockedInsideChartArea = false;
                            temp2.ChartArea = dataType;
                            temp2.Legend = dataType;


                            chart1.Series[dataType].ChartType = SeriesChartType.Pie;
                            chart1.Series[dataType]["PieLabelStyle"] = "Inside";
                            chart1.ChartAreas[dataType].Area3DStyle.Enable3D = true;
                            chart1.Legends[dataType].Enabled = true;
                            chart1.Legends[dataType].Title = dataType;
                            foreach (double numKey in dict.Keys.ToList())
                            {
                                temp = (dict[numKey] / totalPoints) * 100; // % of occurance
                                chart1.Series[dataType].Points.AddXY(numKey.ToString(), temp);
                                chart1.Series[dataType].Enabled = true;
                                chart1.Series[dataType].Label = "#PERCENT{0.00 %}";
                                chart1.Series[dataType].LegendText = "#VALX";
                            }
                        }
                    }
                }
            }
            // Indicate finished, reset progress bar in graphdata_click
            progressBar1.Value = progressBar1.Maximum;
        }
 
        private void getData(string dataType)
        {
            string name = "";
            bool found = false;
            string[] fileEntries = Directory.GetFiles(treeView1.Nodes[0].Text);
            foreach (string fileName in fileEntries)
            {
                if (fileName.IndexOf("archive.d") != -1)
                {
                    name = fileName;
                    found = true;
                }
            }

            if (found)
            {
                m_archiveFiles = new List<ArchiveFile>();
                m_metadata = new List<MetadataWrapper>();
                OpenArchive(name);
                //choose graph based off checkbox list

                if (LineGraphB.Checked)
                {
                    if (chart1.ChartAreas.Count == 0)
                    {
                        chart1.ChartAreas.Add("temp");
                        chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
                        chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
                        chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
                        chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
                    }
                    lineGraph(dataType);
                }else if(PieChartB.Checked){
                    pieGraph(dataType);
                }else if(BarGraphB.Checked){
                    if (chart1.ChartAreas.Count == 0)
                    {
                        chart1.ChartAreas.Add("temp");
                    }
                    columnChart(dataType);
                }else{
                    MessageBox.Show("No graph has been chosen.\nPlease choose a graph type.", "Graph Type Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                ClearArchives();
            }
        }

        private void lineGraph(string dataType)
        {
            m_startTime = dateTimePicker1.Value.ToString();
            m_endTime = dateTimePicker2.Value.ToString();

            // Progress Bar setup 
            int updateCounter = 0;  //used to determine when to update progress bar
            int updateOnCount = progressBar1.Step; //when updateCounter reaches updateOnCount, update progress bar
            progressBar1.Maximum = getProgressBarMax(m_startTime, m_endTime);

            foreach (ArchiveFile fileName in m_archiveFiles)
            {
                foreach (MetadataRecord record in fileName.MetadataFile.Read())
                {

                    if (record.GeneralFlags.Enabled)
                    {
                        if (record.Description == dataType)
                        {
                            chart1.Series.Add(record.Description);
                            chart1.Legends.Add(record.Description);
                            chart1.Series.FindByName(record.Description).ChartType = SeriesChartType.Line;
                            chart1.Series.FindByName(record.Description).Enabled = true;
                            foreach (IDataPoint point in fileName.ReadData(record.HistorianID, m_startTime, m_endTime))
                            {
                                // Update progress bar
                                updateCounter++;
                                if (updateCounter >= updateOnCount)
                                {
                                    progressBar1.PerformStep(); //update progress bar
                                    updateCounter = 0;
                                }

                                chart1.Series.FindByName(record.Description).Points.AddXY(point.Time.ToString(), point.Value);
                            }
                        }
                    }
                }
            }
            // Indicate finished, reset progress bar in graphdata_click
            progressBar1.Value = progressBar1.Maximum;
        }

        private void columnChart(string dataType)
        {
            m_startTime = dateTimePicker1.Value.ToString();
            m_endTime = dateTimePicker2.Value.ToString();

            // Progress Bar setup 
            int updateCounter = 0;  //used to determine when to update progress bar
            int updateOnCount = progressBar1.Step; //when updateCounter reaches updateOnCount, update progress bar
            progressBar1.Maximum = getProgressBarMax(m_startTime, m_endTime);

            foreach (ArchiveFile file in m_archiveFiles)
            {
                foreach (MetadataRecord record in file.MetadataFile.Read())
                {
                    if (record.GeneralFlags.Enabled && record.Description == dataType)
                    {
                        chart1.Series.Add(record.Description);
                        chart1.Legends.Add(record.Description);
                        chart1.Series.FindByName(record.Description).ChartType = SeriesChartType.Column;
                        chart1.Series.FindByName(record.Description).Enabled = true;
                        foreach (IDataPoint point in file.ReadData(record.HistorianID, m_startTime, m_endTime))
                        {
                            // Update progress bar
                            updateCounter++;
                            if (updateCounter >= updateOnCount)
                            {
                                progressBar1.PerformStep(); //update progress bar
                                updateCounter = 0;
                            }

                            if (point.Value != 0)
                            {
                                chart1.Series.FindByName(record.Description).Points.AddXY(point.Time.ToString(), point.Value);

                            }
                        }
                    }
                }
            }
            // Indicate finished, reset progress bar in graphdata_click
            progressBar1.Value = progressBar1.Maximum;
        }



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            System.ComponentModel.BackgroundWorker worker;
            worker = (System.ComponentModel.BackgroundWorker)sender;
            csvParam param = (csvParam)e.Argument;
            param.myForm.exportToCSV(param.filePath, worker, e);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null)
            {
                progressBar1.Maximum = (int)e.UserState;
            }
            else
            {
                progressBar1.PerformStep();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show("Error: " + e.Error.Message);
            else if (e.Cancelled)
                MessageBox.Show("Export Cancelled");
            else
                MessageBox.Show("Export Complete");
            this.CSV.Enabled = true;
            this.Cancel.Enabled = false;
            progressBar1.Value = 0;
            statusLabel.Text = "";
            ClearArchives();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.backgroundWorker1.CancelAsync();
        }


        // Pre-condition: archive files must be open
        // Return Value: the maximum value that should be assigned to progressBar.Maximum
        private int getProgressBarMax(string startTime, string endTime)
        {
            // Progress Bar setup 
            int progressBarMax = 0;
            foreach (ArchiveFile file in m_archiveFiles)
            {
                foreach (MetadataRecord record in file.MetadataFile.Read())
                {
                    foreach (string checkedItem in checkedListBox1.CheckedItems)
                    {
                        if ((checkedItem.ToString() == record.Description) && record.GeneralFlags.Enabled)
                        {
                            progressBarMax += file.ReadData(record.HistorianID, startTime, endTime).Count();
                        }
                    }
                }
            }
            return progressBarMax;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
