using System;
using System.IO;
using System.Text;

namespace WindowsFormsApplication1
{
    class RunLog
    {
        private int processCount;
        private int iTemp;
        private string sOrg;
        private int IOK;
        private int ING;
        private int sumend;
        private int sumline;
        private int sumline1;
        int iTemp1;
        int Sumss;
        ErrorLog Errorwrite = new ErrorLog();

        /*按照年份创建文件夹*/
        public void CreateDirectoryCsvPath(string path22)
        {
            try
            {
                string strYear = DateTime.Now.Year.ToString();
                string strMoth = DateTime.Now.ToString("Y");
                string strDirectoryCsvPath = @"E:\生产统计\" + "\\" + path22 + "\\" + strYear;
                string strDirectoryCsvPath1 = @"E:\每日统计\" + "\\" + path22 + "\\" + strYear + "\\" + strMoth;
                if (!Directory.Exists(strDirectoryCsvPath))
                {
                    Directory.CreateDirectory(strDirectoryCsvPath);
                }
                if (!Directory.Exists(strDirectoryCsvPath1))
                {
                    Directory.CreateDirectory(strDirectoryCsvPath1);
                }
            }
            catch (Exception ex)
            {
                Errorwrite.WriteLog("日志文件路径生成出错！" + ex.Message);
            }
        }

        /*按照月份创建csv*/
        public void CreateCsvPath(string path22, string shuju)
        {
            try
            {
                string strYear = DateTime.Now.Year.ToString();
                string strMoth = DateTime.Now.ToString("Y");
                string strDay = DateTime.Now.ToString("m");
                string strCsvPath = @"E:\生产统计\" + path22 + "\\" + strYear + "\\" + strMoth + ".csv";
                string strCsvPath2 = @"E:\每日统计\" + path22 + "\\" + strYear + "\\" + strMoth + "\\" + strDay + ".csv";
                if (!File.Exists(strCsvPath))
                {
                    using (new FileStream(strCsvPath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) { }
                    using (StreamWriter sw = new StreamWriter(File.OpenWrite(strCsvPath), Encoding.Default))
                    {
                        sw.BaseStream.Seek(0, SeekOrigin.Begin);
                        sw.Write("日期,总量,OK,NG,型号,合格率");
                    }
                }
                if (!File.Exists(strCsvPath2))
                {
                    using (new FileStream(strCsvPath2, FileMode.OpenOrCreate, FileAccess.ReadWrite)) { }
                    using (StreamWriter sw2 = new StreamWriter(File.OpenWrite(strCsvPath2), Encoding.Default))
                    {
                        sw2.BaseStream.Seek(0, SeekOrigin.Begin);
                        sw2.WriteLine("序号,日期," + shuju + "结果");
                    }
                }
            }
            catch (Exception ex)
            {
                Errorwrite.WriteLog("日志文件生成出错!" + ex.Message);
            }
        }

        public void WriteDate(int okss, int ng1, string path22)
        {
            try
            {
                string strYear = DateTime.Now.Year.ToString();
                string strMoth = DateTime.Now.ToString("Y");
                string str = DateTime.Now.ToString("m");
                string strCsvPath = @"E:\生产统计\" + path22 + "\\" + strYear + "\\" + strMoth + ".csv";
                string str1;
                string[] strs;
                sOrg = "mode";

                int temp;
                int iflag;
                using (FileStream fs = new FileStream(strCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs, Encoding.Default))
                {
                    strs = new string[5];
                    temp = 0;
                    iflag = 0;
                    processCount = 0;
                    sumend = 0;
                    sumline = 0;
                    sumline1 = 0;
                    while ((str1 = reader.ReadLine()) != null)
                    {
                        sumline = 0;
                        if (str1 != "")
                            sumline1++;
                        strs = str1.Split(',');
                        byte[] strbyte = Encoding.Default.GetBytes(str1);
                        temp = 0;
                        iflag++;
                        for (int i = 0; i < strbyte.Length; i++)
                        {
                            if ((int)strbyte[i] == 63)
                            {
                                processCount += 2;
                                temp += 2;
                                sumline++;
                            }
                            else if ((int)strbyte[i] != 63 && (int)strbyte[i] != 0)
                            {
                                processCount++;
                                temp++;
                            }
                        }
                        if (sumline > 20)
                            sumend = 1;
                    }
                    iTemp = processCount - temp + (iflag - 1) * 2;
                    processCount = 0;
                }

                try
                {
                    if (strs[0] == str)
                    {
                        if (strs[4].Contains(sOrg))
                        {
                            if (okss == 1)
                            {
                                IOK = int.Parse(strs[2]) + 1;
                                ING = int.Parse(strs[3]);
                            }
                            else
                            {
                                IOK = int.Parse(strs[2]);
                                ING = int.Parse(strs[3]) + 1;
                            }
                        }
                        else
                        {
                            if (okss == 1)
                            {
                                IOK = int.Parse(strs[2]) + 1;
                                ING = int.Parse(strs[3]);
                            }
                            else
                            {
                                IOK = int.Parse(strs[2]);
                                ING = int.Parse(strs[3]) + 1;
                            }
                            sOrg = strs[4] + sOrg;
                        }
                        string s;
                        if (sumend == 0)
                        {
                            s = str + "," + (IOK + ING) + "," + IOK + "," + ING + "," + sOrg + "," + IOK * 1.0f / (IOK + ING);
                        }
                        else
                        {
                            s = "";
                        }
                        using (StreamWriter sw = new StreamWriter(File.OpenWrite(strCsvPath), Encoding.Default))
                        {
                            if (sumend == 0)
                                sw.BaseStream.Seek(iTemp, SeekOrigin.Begin);
                            else
                                sw.BaseStream.Seek(iTemp - 20, SeekOrigin.Begin);
                            sw.Write(s);
                        }
                    }
                    else
                    {
                        if (sumline1 < 35)
                        {
                            using (StreamWriter sw1 = new StreamWriter(strCsvPath, true, Encoding.Default))
                            {
                                string s = "\r\n" + str + "," + (okss + ng1) + "," + okss + "," + ng1 + "," + sOrg + "," + okss * 1.0f / (okss + ng1);
                                sw1.WriteLine(s);
                            }
                        }
                        else
                        {
                            using (StreamWriter sw = new StreamWriter(File.OpenWrite(strCsvPath), Encoding.Default))
                            {
                                sw.BaseStream.Seek(iTemp - 20, SeekOrigin.Begin);
                                sw.Write("");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Errorwrite.WriteLog(ex.Message + "记录1");
                }
            }
            catch (Exception ex)
            {
                Errorwrite.WriteLog(ex.Message + "记录2");
            }
        }

        public void WriteDate1(string jilu, string path22, int okss)
        {
            string jieguo;
            string strYear = DateTime.Now.Year.ToString();
            string strMoth = DateTime.Now.ToString("Y");
            string str = DateTime.Now.ToString("m");
            string strCsvPath = @"E:\每日统计\" + path22 + "\\" + strYear + "\\" + strMoth + "\\" + str + ".csv";
            string str1;
            string[] strs;
            using (FileStream fs = new FileStream(strCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs, Encoding.Default))
            {
                strs = new string[5];
                processCount = 0;
                int temp = 0;
                int iflag = 0;

                while ((str1 = reader.ReadLine()) != null)
                {
                    strs = str1.Split(',');
                    byte[] strbyte = Encoding.Default.GetBytes(str1);
                    iflag++;

                    for (int i = 0; i < strbyte.Length; i++)
                    {
                        if ((int)strbyte[i] == 63)
                        {
                            processCount += 2;
                            temp += 2;
                        }
                        else
                        {
                            processCount++;
                            temp++;
                        }
                    }
                }
                iTemp1 = processCount - temp + (iflag - 1) * 2;
                processCount = 0;
            }

            try
            {
                if (strs[0] == "次序" || strs[0] == "序号")
                    strs[0] = "0";

                Sumss = int.Parse(strs[0]) + 1;
                string strsecond = DateTime.Now.ToString("s");
                if (okss == 1)
                    jieguo = "OK";
                else
                    jieguo = "NG";
                string s = Sumss + "," + strsecond + "," + jilu + "," + jieguo;

                using (StreamWriter sw4 = new StreamWriter(strCsvPath, true, Encoding.Default))
                {
                    sw4.WriteLine(s);
                }
            }
            catch
            {
            }
        }
    }
}
