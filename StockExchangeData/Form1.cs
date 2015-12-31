using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace StockExchangeData
{
    public partial class Form1 : Form
    {

        private DateTime stock_data;
        private string stock_type;

        public Form1()
        {
            InitializeComponent();

            //domyslne wart. w combobox
            Dictionary<string, string> stockType = new Dictionary<string, string>();
            stockType.Add("1","Indeksy");
            stockType.Add("10","Akcje");
            stockType.Add("13","Obligacje");
            stockType.Add("17","Prawa Poboru");
            stockType.Add("35","Kontrakty terminowe");
            stockType.Add("37","Prawa Do Akcji");
            stockType.Add("48","Certyfikaty Inwestycyjne");
            stockType.Add("53","Warranty");
            stockType.Add("54","Jednostki indeksowe");
            stockType.Add("66","Opcje");
            stockType.Add("161","Produkty strukturyzowane");
            stockType.Add("241","ETF");

            comboBox1.DataSource = new BindingSource(stockType, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";
            comboBox1.SelectedIndex = 1;

            //domyslna data
            dateTimePicker1.Value = DateTime.Today.AddDays(-1);
            dateTimePicker2.Value = DateTime.Today.AddDays(-1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
            progressBar1.Visible = false;
            label5.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();

            string page;
            page = "http://www.gpw.pl/notowania_archiwalne_full?type=";

            List<List<string>> list = new List<List<string>>();
            List<List<string>> listTransposed = new List<List<string>>();

            list = downloadPage(page, stock_data, stock_type);
            listTransposed = transposeList(list);

            string date = getDate(page, stock_data, stock_type);
            displayListViewItems(listTransposed, listView1, date);
        }

        #region Page scraping
        private void saveToStream(string stock_type, DateTime stock_data, string path)
        {
            string page, path_main;
            page = "http://www.gpw.pl/notowania_archiwalne_full?type=";

            List<List<string>> list = new List<List<string>>();
            List<List<string>> listTransposed = new List<List<string>>();

            list = downloadPage(page, stock_data, stock_type);
            listTransposed = transposeList(list);

            string date = getDate(page, stock_data, stock_type);

            //This test is added only once to the file.
            if (!File.Exists(path))
            {
                //Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Data;Nazwa;Kod ISIN;Waluta;Kurs otwarcia;Kurs max;Kurs min;Kurs zamkniecia;Zmiana dzienna(%);Wolumen(szt.);Liczba transakcji;Wartosc obrotu(tys.)");
                }
            }
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                foreach (var sublist in listTransposed)
                {
                    sw.Write(stock_data.ToString("yyyy-MM-dd") + ";");
                    foreach (var value in sublist)
                    {
                        sw.Write(value + ";");
                    }
                    sw.WriteLine();
                }
            }
        }

        private string savePath()
        {
            string path;

            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    path = dialog.SelectedPath;
                else
                    path = @"c:\temp\";
            }

            return path;
        }
        private int counter(string url, string xpath)
        {
            int output = 0;

            HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);

            output = doc.DocumentNode.SelectNodes(xpath).Count;

            return output;
        }
        private string display(List<List<string>> list)
        {
            string output = string.Empty;

            foreach (var sublist in list)
            {
                foreach (var value in sublist)
                {
                    output += value + "\t ";
                }
                output += "\n";
            }

            return output;
        }
        private void displayListViewItems(List<List<string>> list, ListView lv, string date)
        {
            //ListViewItem lvi = new ListViewItem(date);
            //lvi.SubItems.Add("123");
            //lv.Items.Add(lvi);
            var items = lv.Items;
            foreach (var sublist in list)
            {

                ListViewItem lvi = new ListViewItem(date);
                foreach (var value in sublist)
                {
                    lvi.SubItems.Add(value);
                }
                lv.Items.Add(lvi);
            }
        }
        private string display_irow(List<List<string>> list, int i)
        {
            string output = string.Empty;

            foreach (var sublist in list)
            {
                output += sublist[i] + "\n";
            }

            return output;
        }

        private List<List<string>> downloadPage(string url, DateTime stockdate, string stock_type)
        {
            List<List<string>> list = new List<List<string>>();
            bool connectionProblem = false;
            int iterations = 1;

            do
            {
                connectionProblem = false;

                try
                {
                    HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
                    HtmlAgilityPack.HtmlDocument doc = web.Load(url + stock_type + "&date=" + stockdate.ToString("yyyy-MM-dd"));

                    for (int i = 1; i <= 11; i++)
                    {
                        List<string> sublist = new List<string>();
                        foreach (HtmlNode item in doc.DocumentNode.SelectNodes(".//td[" + i + "]"))
                        {
                            if (item != null)
                            {
                                //uzycie wyrażeń regularnych pozwala na pominięcie spacji przy &nbsp;
                                sublist.Add(Regex.Replace(item.InnerText, @"<[^>]+>|&nbsp;", "").Trim());
                            }
                        }

                        list.Add(sublist);
                    }
                }
                catch (NullReferenceException)
                {
                    //stock_data = stock_data.AddDays(1);
                    //downloadPage(url, stock_data);
                }
                catch (Exception ex)
                {
                    iterations++;
                    connectionProblem = true;

                    if (iterations >= 15)
                    {
                        MessageBox.Show("Zanotowano wyjątek!\n\n" + ex.Message.ToString());
                        break;
                    }
                }
            } while (connectionProblem);
            
            return list;
        }
        private string getDate(string url, DateTime stockdate, string stock_type)
        {
            string output = string.Empty;
            bool connectionProblem = false;
            int iterations = 1;

            do
            {
                connectionProblem = false;

                try
                {
                    HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
                    HtmlAgilityPack.HtmlDocument doc = web.Load(url + stock_type + "&date=" + stockdate.ToString("yyyy-MM-dd"));
                    output = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[2]/div[1]").InnerText;
                }
                catch (NullReferenceException) { }
                catch (Exception ex)
                {
                    iterations++;
                    connectionProblem = true;

                    if (iterations >= 15)
                    {
                        MessageBox.Show("Zanotowano wyjątek!\n\n" + ex.Message.ToString());
                        break;
                    }
                }
            } while (connectionProblem);

            return output;
        }

        private List<List<string>> transposeList(List<List<string>> lists)
        {
            var longest = lists.Any() ? lists.Max(l => l.Count) : 0;
            List<List<string>> outer = new List<List<string>>(longest);

            for (int i = 0; i < longest; i++)
                outer.Add(new List<string>(lists.Count));

            for (int j = 0; j < lists.Count; j++)
                for (int i = 0; i < longest; i++)
                    outer[i].Add(lists[j].Count > i ? lists[j][i] : default(string));

            return outer;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            stock_data = dateTimePicker1.Value;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            stock_type = comboBox1.SelectedValue.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //default filename
            string datastamp = DateTime.Today.ToString("yyyy-MM-dd");


            //declare new SaveFileDialog + set it's initial properties
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Wybierz miejsce do zapisu pliku",
                    FileName = "dane gieldowe " + datastamp + ".csv",
                    Filter = "CSV (*.csv)|*.csv",
                    FilterIndex = 0,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                //show the dialog + display the results in a msgbox unless cancelled
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string[] headers = listView1.Columns
                               .OfType<ColumnHeader>()
                               .Select(header => header.Text.Trim())
                               .ToArray();

                    string[][] items = listView1.Items
                                .OfType<ListViewItem>()
                                .Select(lvi => lvi.SubItems
                                    .OfType<ListViewItem.ListViewSubItem>()
                                    .Select(si => si.Text).ToArray()).ToArray();

                    string table = string.Join(";", headers) + Environment.NewLine;
                    foreach (string[] a in items)
                    {
                        //a = a_loopVariable;
                        table += string.Join(";", a) + Environment.NewLine;
                    }
                    table = table.TrimEnd('\r', '\n');
                    System.IO.File.WriteAllText(sfd.FileName, table);
                }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string datastample = DateTime.Today.ToString("yyyy-MM-dd");

            progressBar1.Visible = true;
            label5.Text = string.Empty;
            label5.Visible = true;
            
            string path = savePath() + @"\dane gieldowe "+ datastample +".csv";
            backgroundWorker1.RunWorkerAsync(path);
        }
        #endregion

        //First of all -> change properties in backgroundWorker1 -> WorkerReportsProgress = true, WorkerSupportsCancellation = true
        //Secondly -> add events -> DoWork, ProgressChanged, RunWorkerCompleted.
        #region Background work
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //this is updated from doWork. Its where UI components are updated
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            DateTime dateFrom, dateTo, today, dateIter;
            dateFrom = dateTimePicker2.Value;
            dateIter = dateFrom;
            today = DateTime.Today;
            dateTo = today.AddDays(-1);
            
            int counter;
            counter = dateTo.Subtract(dateFrom).Days;

            string path = (string)e.Argument; //przesłanie ścieżki do pliku
            double progress = 0;

            //main loop
            for (int i = 0; i <= counter; i++)
            {
                progress = ((double)i / (double)counter) * 100;
                backgroundWorker1.ReportProgress((int)Math.Round(progress,0),dateIter.ToString("yyyy-MM-dd"));
                saveToStream(stock_type, dateIter, path);
                dateIter = dateIter.AddDays(1);
            }


            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedtime = String.Format("{0:00}h:{1:00}m:{2:00}s", ts.Hours, ts.Minutes, ts.Seconds);
            e.Result = elapsedtime;
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //there should be no GUI component method
            //receives updates after Thread.Sleep(100)
            progressBar1.Refresh();
            progressBar1.Value = e.ProgressPercentage;
            label5.Text = "Pobieram dane za " + e.UserState;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //called when the heavy operation in background is over. Can also accept GUI components

            MessageBox.Show("Zakończono.\nCzas pobierania danych: "+ e.Result);
            progressBar1.Visible = false;
        }
        #endregion

        private void label1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Kontakt:\nkodi1911@wp.pl");
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();

        }
    }
}
