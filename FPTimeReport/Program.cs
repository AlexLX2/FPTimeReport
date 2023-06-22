using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tremol;

namespace FPTimeReport
{
    internal class Program
    {
        public static void Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string computerName = Environment.MachineName;

            DateTime now = DateTime.Now;
            DateTime fpdt;

            DateTime firstDayOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            DateTime lastDayOfLastMonth = new DateTime(now.Year, now.Month, 1).AddDays(-1);

            string dateYM = now.Month + "/" + now.Year + Environment.NewLine;
            string dateYMLOG = now.Month - 1 + "/" + now.Year;

            string fileNameLocal = computerName + ".txt";
            string filePathLocalDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Logs\\");
            string filePathLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Logs\\", fileNameLocal);
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RaportDate.txt");
            string[] comPorts = SerialPort.GetPortNames();

            bool exceptionTry = false;
            long maxSize = 10 * 1024 * 1024;
            bool isExistDate = false;

            if (!Directory.Exists(filePathLocalDirectory))
            {
                Directory.CreateDirectory(filePathLocalDirectory);
            }

            if (filePathLocal.Length > maxSize)
            {
                int kolstrok = 100;
                string[] stroki = File.ReadAllLines(filePathLocal, Encoding.UTF8);
                string[] wr = new string[stroki.Length - kolstrok];
                Array.Copy(stroki, kolstrok, wr, 0, wr.Length);
                File.WriteAllLines(filePathLocal, wr);

            }

            if (!File.Exists(path))
            {
                File.AppendAllText(path, "");
            }

            if (comPorts.Length == 0)
            {
                File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " + "Нет доступных COM-портов.\n");
                Environment.Exit(0);
            }
            else
            {
                foreach (string port in comPorts)
                {
                    try
                    {
                        using (FP fp = new FP())
                        {
                            fp.OpenPort(port, 115200);

                            using (StreamReader reader = new StreamReader(path))
                            {
                                string firstLine = reader.ReadLine();
                                if (firstLine != null && firstLine.Trim() == dateYM.Trim())
                                {
                                    isExistDate = true;
                                }
                            }

                            if (!isExistDate)
                            {
                                string LogRaport = "[" + now.ToString() + "] " + "Месячный рапорт вышел " + dateYMLOG + "\n";
                                fp.ReportFiscalByDate(false, false, firstDayOfLastMonth, lastDayOfLastMonth);
                                File.AppendAllText(filePathLocal, LogRaport);
                                File.WriteAllText(path, dateYM);
                            }
                            else
                            {
                                File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " +  "Месячный рапорт за " + dateYMLOG + " уже выходил\n");
                            }

                            fpdt = fp.GetDateTime();
                            TimeSpan difference = now - fpdt;

                            string dataToWrite = "[" + now.ToString() + "] " + "Changed time " + fpdt.ToString() + " to -> " + now.ToString() + "\n";

                            if (difference.TotalMinutes > 4 || difference.TotalMinutes < -4)
                            {
                                fp.SetDateTime(now);
                                File.AppendAllText(filePathLocal, dataToWrite);
                            }
                            else
                            {
                                File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " + "Время на кассовом аппарате установлено корректно " + fpdt.ToString() + ".\n");
                            }

                            exceptionTry = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(FPException))
                        {
                            int err = ((FPException)ex).ErrorCode;
                            if (err == 2)
                            {
                                File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " + "Время не получилось поменять\n");
                                exceptionTry = true;
                            }
                        }
                        else
                        {
                            File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " + ex + "\n");
                        }
                    }
                }
            }

            if (!exceptionTry)
            {
                File.AppendAllText(filePathLocal, "[" + now.ToString() + "] " + "Фискальный принтер не найден\n");
            }
        }

    }
}


// Raports --------------------------------------- // 
//fp.ReportDaily(true, true); // Рапорт Z
//fp.ReportDaily(false, true); // Рапорт X
// Raports --------------------------------------- // 