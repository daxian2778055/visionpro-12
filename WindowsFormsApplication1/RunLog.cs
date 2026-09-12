using System;
using System.IO;
using System.Text;

namespace WindowsFormsApplication1
{
    class RunLog
    {
        ErrorLog Errorwrite = new ErrorLog();

        // 多路相机检测线程都会调用 WriteDate/WriteDate1 写同一批 CSV，必须串行化，
        // 否则并发读写同一文件会抛出 IOException 且计数互相覆盖。
        private readonly object _lock = new object();

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

        /// <summary>
        /// 在「生产统计」月度汇总 CSV 中记录一次 OK/NG 结果。
        /// <para>
        /// ★ S5 修复：改为"读取全部行 → 定位最后一行 → 整体重写"，彻底废弃原先按字节偏移
        /// <c>Seek(iTemp - 20)</c> 覆盖写的做法。旧做法依赖"按字符数估算字节偏移"，
        /// 当计数从 9→10 变长时会把后续内容写坏/截断。
        /// </para>
        /// <para>该文件按设计最多约 35 行，整体重写代价可忽略。</para>
        /// </summary>
        public void WriteDate(int okss, int ng1, string path22)
        {
            lock (_lock)
            {
                try
                {
                    string strYear = DateTime.Now.Year.ToString();
                    string strMoth = DateTime.Now.ToString("Y");
                    string str = DateTime.Now.ToString("m");
                    string strCsvPath = @"E:\生产统计\" + path22 + "\\" + strYear + "\\" + strMoth + ".csv";

                    if (!File.Exists(strCsvPath))
                    {
                        Errorwrite.WriteLog("生产统计文件不存在，跳过本次记录: " + strCsvPath);
                        return;
                    }

                    string[] lines = File.ReadAllLines(strCsvPath, Encoding.Default);
                    if (lines.Length == 0) return;

                    // 定位最后一条非空行（等价于旧实现"取最后一次 ReadLine 的结果"）
                    int lastIdx = lines.Length - 1;
                    while (lastIdx >= 0 && string.IsNullOrEmpty(lines[lastIdx])) lastIdx--;
                    if (lastIdx < 0) return;

                    string[] strs = lines[lastIdx].Split(',');
                    if (strs.Length < 5) return;   // 结构异常，避免越界

                    // 保留旧实现的"编码不匹配"保护：整份文件出现大量无法按默认代码页表示的字符时
                    // （Encoding.Default 会替换成 '?'），本次不改写该行。
                    bool encodingMismatch = HasTooManyUnmappableChars(lines);

                    string sOrg = "mode";
                    try
                    {
                        if (strs[0] == str)
                        {
                            int iok;
                            int ing;
                            if (strs[4].Contains(sOrg))
                            {
                                iok = int.Parse(strs[2]) + (okss == 1 ? 1 : 0);
                                ing = int.Parse(strs[3]) + (okss == 1 ? 0 : 1);
                            }
                            else
                            {
                                iok = int.Parse(strs[2]) + (okss == 1 ? 1 : 0);
                                ing = int.Parse(strs[3]) + (okss == 1 ? 0 : 1);
                                sOrg = strs[4] + sOrg;
                            }

                            lines[lastIdx] = encodingMismatch
                                ? string.Empty
                                : str + "," + (iok + ing) + "," + iok + "," + ing + "," + sOrg + "," + iok * 1.0f / (iok + ing);

                            File.WriteAllLines(strCsvPath, lines, Encoding.Default);
                        }
                        else if (CountNonEmptyLines(lines) < 35)
                        {
                            using (StreamWriter sw1 = new StreamWriter(strCsvPath, true, Encoding.Default))
                            {
                                string s = "\r\n" + str + "," + (okss + ng1) + "," + okss + "," + ng1 + "," + sOrg + "," + okss * 1.0f / (okss + ng1);
                                sw1.WriteLine(s);
                            }
                        }
                        else if (!encodingMismatch)
                        {
                            // 超过 35 行且最后一行不是今天：保持旧实现"清空最后一行"的语义（该文件按行数滚动）
                            lines[lastIdx] = string.Empty;
                            File.WriteAllLines(strCsvPath, lines, Encoding.Default);
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
        }

        /// <summary>
        /// 在「每日统计」明细 CSV 中追加一条记录（jilu 为结果明细，okss=1 表示 OK）。
        /// <para>
        /// ★ S5 修复：删除旧实现为计算一个"从未被使用的字节偏移 iTemp1"而做的整文件逐字节扫描；
        /// 文件不存在时明确记日志，而不是抛出 FileNotFoundException 被内层空 catch 静默吞掉。
        /// </para>
        /// </summary>
        public void WriteDate1(string jilu, string path22, int okss)
        {
            try
            {
                lock (_lock)
                {
                    string strYear = DateTime.Now.Year.ToString();
                    string strMoth = DateTime.Now.ToString("Y");
                    string str = DateTime.Now.ToString("m");
                    string strCsvPath = @"E:\每日统计\" + path22 + "\\" + strYear + "\\" + strMoth + "\\" + str + ".csv";

                    if (!File.Exists(strCsvPath))
                    {
                        Errorwrite.WriteLog("每日统计文件不存在，跳过本次记录: " + strCsvPath);
                        return;
                    }

                    string[] lines = File.ReadAllLines(strCsvPath, Encoding.Default);
                    int lastIdx = lines.Length - 1;
                    while (lastIdx >= 0 && string.IsNullOrEmpty(lines[lastIdx])) lastIdx--;

                    // 序号 = 最后一行首个字段 + 1；表头（"序号"/"次序"）视为 0。
                    // 无法解析时不写入（与旧实现抛异常被吞的结果一致）。
                    int seqValue = -1;
                    if (lastIdx >= 0)
                    {
                        string[] strs = lines[lastIdx].Split(',');
                        string first = (strs.Length > 0) ? strs[0] : null;
                        if (first == "次序" || first == "序号") first = "0";
                        int parsed;
                        if (!string.IsNullOrEmpty(first) && int.TryParse(first, out parsed))
                            seqValue = parsed + 1;
                    }
                    if (seqValue < 0) return;

                    string jieguo = (okss == 1) ? "OK" : "NG";
                    string s = seqValue + "," + DateTime.Now.ToString("s") + "," + jilu + "," + jieguo;

                    using (StreamWriter sw4 = new StreamWriter(strCsvPath, true, Encoding.Default))
                    {
                        sw4.WriteLine(s);
                    }
                }
            }
            catch (Exception ex)
            {
                try { Errorwrite.WriteLog("每日记录失败:" + ex.Message); } catch { }
            }
        }

        /// <summary>
        /// 旧实现用"某行按默认代码页转换后 '?' 字节数 > 20"判定文件编码与预期不符
        /// （'?' 是 Encoding.Default 对无法映射字符的替换结果）。这里保留同一判据。
        /// </summary>
        private static bool HasTooManyUnmappableChars(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;

                byte[] bytes = Encoding.Default.GetBytes(line);
                int marks = 0;
                for (int j = 0; j < bytes.Length; j++)
                {
                    if (bytes[j] == (byte)'?') marks++;
                }
                if (marks > 20) return true;
            }
            return false;
        }

        private static int CountNonEmptyLines(string[] lines)
        {
            int n = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i])) n++;
            }
            return n;
        }
    }
}
