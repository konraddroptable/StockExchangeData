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
using System.Net;

namespace StockExchangeData
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
        }



        private void saveToStream(string path)
        {
            string page, path_main;
            page = "http://www.gpw.pl/karta_spolki/PLPKO0000016";

            List<List<string>> list = new List<List<string>>();
            List<List<string>> listTransposed = new List<List<string>>();

            list = downloadPage(page);
            listTransposed = transposeList(list);

            //This test is added only once to the file.
            if (!File.Exists(path))
            {
                //Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Nazwa;Skrót;Nazwa pełna;Adres siedziby;Województwo;Prezes zarządu;Numer telefonu;Numer faksu;Strona www;Statut");
                }
            }
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                foreach (var sublist in listTransposed)
                {
                    foreach (var value in sublist)
                    {
                        sw.Write(value + ";");
                    }
                    sw.WriteLine();
                }
            }
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

        private List<List<string>> downloadPage(string url)
        {
            List<List<string>> list = new List<List<string>>();


            //HttpClient client = new HttpClient();
            //HttpResponseMessage response = await client.GetAsync(url);

            //HtmlDocument doc = new HtmlDocument();
            //doc.Load(await response.Content.ReadAsStreamAsync());
            Uri uri = new Uri(url + "/#dane_podstawowe");


            //HttpWebRequest request = (HttpWebRequest)
            //    WebRequest.Create(uri);
            //HttpWebResponse response = (HttpWebResponse)
            //    request.GetResponse();

            HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url + "/#dane_podstawowe");
            

            

            try
            {
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
            return list;
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

        private void button4_Click(object sender, EventArgs e)
        {
            string datastample = DateTime.Today.ToString("yyyy-MM-dd");
            string path = savePath() + @"\dane gieldowe " + datastample + ".csv";
            saveToStream(path);
        }




    }
}
