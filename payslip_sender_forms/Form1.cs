using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace payslip_sender_forms
{
    public partial class payslipForm : Form
    {
        static string source_file = "";
        public payslipForm()
        {
            InitializeComponent();
            progressBar1.Maximum = 10000;
            // To report progress from the background worker we need to set this property
            backgroundWorker1.WorkerReportsProgress = true;
            // This event will be raised when we call ReportProgress
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            // Start the BackgroundWorker.
            backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fbd = new OpenFileDialog();
            //FolderBrowserDialog fbd = new FolderBrowserDialog();
            //fbd.RootFolder = Environment.SpecialFolder.Desktop;
            //fbd.Description = "Load the payslip for the present month";            

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //string path = Path.GetDirectoryName(fbd.FileName);
                //string fullPath = path + "/" + fbd.FileName;
                source_file = fbd.FileName;
                MessageBox.Show(source_file);
                label1.Text = source_file;
            }
               
        }

        private void button3_Click(object sender, EventArgs e)
        {                        

            if (string.IsNullOrEmpty(source_file))
                MessageBox.Show("You must select a payslip document");
            else
            {
                string extension = System.IO.Path.GetExtension(source_file);
                if (extension != ".pdf")
                {
                    MessageBox.Show("document must be PDF");
                }
                else
                {
                    //variables

                    String numberSplit = "C:/numberSplit/";

                    PdfCopy copy;

                    //create PdfReader object
                    PdfReader reader = new PdfReader(source_file);

                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        //create Document object
                        Document document = new Document();
                        copy = new PdfCopy(document, new FileStream(numberSplit + i + ".pdf", FileMode.Create));
                        //open the document 
                        document.Open();
                        //add page to PdfCopy 
                        copy.AddPage(copy.GetImportedPage(reader, i));
                        //close the document object
                        document.Close();

                        // write into listbox
                        listBox1.Items.Add(numberSplit + i + ".pdf");
                        listBox1.Refresh();
                        listBox1.SelectedIndex = listBox1.Items.Count - 1;
                        listBox1.SelectedIndex = -1;

                        progressBar1.Value = i * progressBar1.Maximum / reader.NumberOfPages;
                        // Report progress.
                        backgroundWorker1.ReportProgress(i);
                        // Wait 100 milliseconds.

                        splitInfoLabel.Text = "Slpitting Payslip into numeric naming based pages";
                        Thread.Sleep(100);

                        LabelTotal.Text = i.ToString() + " of " + reader.NumberOfPages; //show number of count in lable
                        int presentage = (i * 100) / reader.NumberOfPages;
                        LabelPresentage.Text = presentage.ToString() + " %"; //show precentage in lable
                        Application.DoEvents(); //keep form active in every loop

                        //Console.WriteLine(i);

                    }

                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {

                        String ippis_result = "C:/numberSplit/" + i + ".pdf";
                        string newresult = ParsePdf(ippis_result);

                        if (newresult != "" || newresult != "Oracl")
                        {

                            try
                            {
                                File.Move("C:/numberSplit/" + i + ".pdf", "C:/ippisSplit/" + newresult + ".pdf");
                            }
                            catch
                            {

                            }

                        }
                        splitInfoLabel.Text = "Slpitting Payslip into ippis naming based pages";
                        Thread.Sleep(100);

                    }

                }

                
            }
            
        }

        public static string ParsePdf(string filename)        
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("fileName");

            using (PdfReader textreader = new PdfReader(filename))
            {
                StringBuilder sb = new StringBuilder();

                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                for (int page = 0; page < textreader.NumberOfPages; page++)
                {
                    string text = PdfTextExtractor.GetTextFromPage(textreader, page + 1, strategy);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.Append(Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text))));
                    }
                }
                string sb_final = sb.ToString();
                int new_file_name_index = sb_final.IndexOf("Number");
                int startValue = new_file_name_index + 8;
                string new_file_name = sb_final.Substring(startValue, 6);

                return new_file_name;                
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Change the value of the ProgressBar to the BackgroundWorker progress.
            progressBar1.Value = e.ProgressPercentage;
            // Set the text.
            this.Text = e.ProgressPercentage.ToString();
        }

        public static void Send(string from, string password, string to, string Message, string subject, string host, int port, string file)
        {

            MailMessage email = new MailMessage();
            email.From = new MailAddress(from);
            email.To.Add(to);
            email.Subject = subject;
            email.Body = Message;
            SmtpClient smtp = new SmtpClient(host, port);
            smtp.UseDefaultCredentials = false;
            NetworkCredential nc = new NetworkCredential(from, password);
            smtp.Credentials = nc;
            smtp.EnableSsl = true;

            email.IsBodyHtml = true;
            email.Priority = MailPriority.Normal;
            email.BodyEncoding = Encoding.UTF8;

            if (file.Length > 0)
            {
                Attachment attachment;
                attachment = new Attachment(file);
                email.Attachments.Add(attachment);
            }

            // smtp.Send(email);
            smtp.SendCompleted += new SendCompletedEventHandler(SendCompletedCallBack);
            string userstate = "sending ...";
            smtp.SendAsync(email, userstate);

        }

        private static void SendCompletedCallBack(object sender, AsyncCompletedEventArgs e)
        {
            string result = "";
            if (e.Cancelled)
            {
                //MessageBox.Show(string.Format("{0} send canceled.", e.UserState), "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine(string.Format("{0} send canceled.", e.UserState), "Message");
                //updateDB(globalIppis, "Cancelled");
            }
            else if (e.Error != null)
            {
                //MessageBox.Show(string.Format("{0} {1}", e.UserState, e.Error), "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine(string.Format("{0} {1}", e.UserState, e.Error), "Message");
                //updateDB(globalIppis, "Not Sent");
            }
            else
            {
                //MessageBox.Show("your message is sended", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine(string.Format("{0} {1}", "your message is sended", "Message"));
                //updateDB(globalIppis, "Sent");
            }

        }

        private static string dbaseAccess(string ippis, string token)
        //public static void dbaseAccess()
        {
            //IEnumerable<StaffData> query = DBAccess.StaffDatas.Select();
            string finalValue;
            using (var context = new NominalDataEntities())
            {
                var stdQuery = (from d in context.StaffDatas
                                select new
                                {
                                    Id = d.Id,
                                    PinNo = d.PinNo,
                                    IppisNo = d.IppisNo,
                                    Fullname = d.Fullname,
                                    EmailAddress = d.EmailAddress,
                                    Department = d.Department,
                                    Comment = d.Comment
                                }).Where(x => x.IppisNo == ippis).FirstOrDefault();

                /*foreach (var q in stdQuery)
                {
                    Console.WriteLine("PinNo : " + q.PinNo + ", IppisNo : " + q.IppisNo + ", Department : " + q.Department);
                }*/


                //Console.ReadLine();

                if (stdQuery != null && token == "email")
                    finalValue = stdQuery.EmailAddress;
                else if (stdQuery != null && token == "name")
                    finalValue = stdQuery.Fullname;
                else
                    finalValue = null;

            }

            return finalValue;
        }

        public static void updateDB(string ippis, string message)
        {
            using (var context2 = new NominalDataEntities())
            {
                //var result = context2.StaffDatas.SingleOrDefault(b => b.IppisNo == ippis);
                var result = context2.StaffDatas.FirstOrDefault(b => b.IppisNo == ippis);
                if (result != null)
                {
                    result.Comment = message;
                    context2.SaveChanges();
                }
            }
        }

        public static bool IsValidEmail(string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }        

    }
}
