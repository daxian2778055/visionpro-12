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

        // ★第26轮#20：最近一次已核对过的"日文件 + 方案列头"组合，避免每条记录都去读文件头
        private string _lastDayHeaderKey = "";
        // ★性能#3（第41轮）：每日明细原每条 ReadAllLines 整个日文件只为取末行序号——O(条数²)，
        //   1万条/日 ≈ 5GB 读放大。单槽缓存以 (路径,长度,mtime) 三元组校验：本进程上次成功追加后文件没人动
        //   → 直接序号+1；外部改写/其它进程追加/本文件被 CreateCsvPath 改头 → 任一变化即回退全量读并重建。
        private string _seqPath;
        private DateTime _seqMtimeUtc;
        private long _seqLength;
        private int _seqLast;
        // ★复盘P2-4：编码不符跳过当日累计行时的 5s 限流日志用（同一 CSV 写入本就在 _lock 内，无需原子；
        //  初值必须为 0——int.MinValue 会让 now-MinValue 溢出为负，限流条件永不成立、日志永不触发）
        private int _encSkipLogTick;

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
            lock (_lock)   // ★第26轮#20：与 WriteDate/WriteDate1 同锁串行（12 路检测线程都会进来）
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
                    _lastDayHeaderKey = strCsvPath2 + "|" + shuju;
                }
                else
                {
                    // ★第26轮#20：原实现只在文件不存在时写表头。当天切了方案（输出列名/列数不同）
                    //   表头不会更新，此后一整天写进去的数据列与表头永久错位。
                    //   现按"文件+列头"做一次性核对：不一致就在末尾追加一行新表头当分界
                    //   （不重写既有数据），并记日志。核对每个组合只做一次，不给每条记录增加读盘。
                    string wantKey = strCsvPath2 + "|" + shuju;
                    if (wantKey != _lastDayHeaderKey)
                    {
                        _lastDayHeaderKey = wantKey;
                        try
                        {
                            string existing;
                            using (var rd = new StreamReader(strCsvPath2, Encoding.Default)) existing = rd.ReadLine() ?? "";
                            string want = "序号,日期," + shuju + "结果";
                            if (existing.Trim() != want.Trim())
                            {
                                using (var ap = new StreamWriter(File.Open(strCsvPath2, FileMode.Append), Encoding.Default))
                                {
                                    ap.WriteLine();
                                    ap.WriteLine(want);
                                }
                                Errorwrite.WriteLog("每日统计 CSV 列头与当前方案不一致，已在文件末尾追加新表头作为分界: " + strCsvPath2);
                            }
                        }
                        catch (Exception exHead)
                        {
                            Errorwrite.WriteLog("每日统计 CSV 列头核对失败（忽略，不影响写数据）!" + exHead.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Errorwrite.WriteLog("日志文件生成出错!" + ex.Message);
            }
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

                            // ★功能修复⑥（第41轮）：编码不匹配按本段注释语义"本次不改写该行"——原实现把
                            //   当日累计行写成 string.Empty = 直接删行（计数归零丢失，下次又从尾部找 → 振荡丢数）。
                            if (encodingMismatch)
                            {
                                // ★复盘P2-4：本路径静默 return 意味着"生产统计当日累计停更"且无迹可循，
                                //   补 5 秒限流日志留痕（每次跳过都记会刷爆日志）。
                                int nowTick = Environment.TickCount;
                                if (nowTick - _encSkipLogTick > 5000)
                                {
                                    _encSkipLogTick = nowTick;
                                    try { Errorwrite.WriteLog("生产统计编码不符，本次未改写当日累计行（5秒限流）: " + strCsvPath); } catch { }
                                }
                                return;
                            }
                            lines[lastIdx] = str + "," + (iok + ing) + "," + iok + "," + ing + "," + sOrg + "," + iok * 1.0f / (iok + ing);

                            WriteAllLinesAtomic(strCsvPath, lines);
                        }
                        else if (encodingMismatch && CountNonEmptyLines(lines) < 35)
                        {
                            // ★复盘P1-2（功能⑧守卫补齐）：encodingMismatch 时 lines 是有损解码结果，整表
                            //   WriteAllLinesAtomic 重写会把历史行固化成 mojibake/'?'——且本分支正是
                            //   "新一天首条记录"的必经路径（每天都走）。退回字节级安全追加：历史字节一律
                            //   不动，只补新行；文件已以换行结尾时不再多拼换行（⑧要修的空行累积在此路径
                            //   同样消除）。超过 35 行仍不追加（保持旧实现按行数滚动的上限语义）。
                            string dayRow = str + "," + (okss + ng1) + "," + okss + "," + ng1 + "," + sOrg + "," + okss * 1.0f / (okss + ng1);
                            AppendLineSafe(strCsvPath, dayRow);
                        }
                        else if (CountNonEmptyLines(lines) < 35)
                        {
                            // ★功能修复⑧（第41轮）：原 StreamWriter 追加恒拼 "\r\n" 前缀，而同日原子重写后文件已以
                            //   换行结尾 → 次日首追加多出一个空行并逐日累积。改整表原子追加（统一收尾换行、断电安全）。
                            //   （走到此处已隐含 !encodingMismatch —— 前一分支已把 mismatch 引到字节级追加。）
                            string[] appended = new string[lines.Length + 1];
                            Array.Copy(lines, appended, lines.Length);
                            appended[lines.Length] = str + "," + (okss + ng1) + "," + okss + "," + ng1 + "," + sOrg + "," + okss * 1.0f / (okss + ng1);
                            WriteAllLinesAtomic(strCsvPath, appended);
                        }
                        else if (!encodingMismatch)
                        {
                            // 超过 35 行且最后一行不是今天：保持旧实现"清空最后一行"的语义（该文件按行数滚动）
                            lines[lastIdx] = string.Empty;
                            WriteAllLinesAtomic(strCsvPath, lines);
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

                    // ★性能#3：缓存命中（同路径+同长度+mtime = 上次成功追加后文件无人动过）直接序号+1，
                    //   不再整文件 ReadAllLines；stat 失败/任一不匹配按未命中走下方全量读（语义与原实现一致）。
                    int seqValue = -1;
                    try
                    {
                        FileInfo fi = new FileInfo(strCsvPath);
                        if (_seqPath == strCsvPath && _seqLength == fi.Length && _seqMtimeUtc == fi.LastWriteTimeUtc)
                            seqValue = _seqLast + 1;
                    }
                    catch { seqValue = -1; }
                    if (seqValue < 0)
                    {
                        string[] lines = File.ReadAllLines(strCsvPath, Encoding.Default);
                        int lastIdx = lines.Length - 1;
                        while (lastIdx >= 0 && string.IsNullOrEmpty(lines[lastIdx])) lastIdx--;

                        // 序号 = 最后一行首个字段 + 1；表头（"序号"/"次序"）视为 0。
                        // 无法解析时不写入（与旧实现抛异常被吞的结果一致）。
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
                    }

                    string jieguo = (okss == 1) ? "OK" : "NG";
                    string s = seqValue + "," + DateTime.Now.ToString("s") + "," + jilu + "," + jieguo;

                    using (StreamWriter sw4 = new StreamWriter(strCsvPath, true, Encoding.Default))
                    {
                        sw4.WriteLine(s);
                    }
                    // ★性能#3：追加成功后把缓存更新为追加后的文件状态（追加失败不更新 → 下次自然回退全量读）
                    try
                    {
                        FileInfo fiW = new FileInfo(strCsvPath);
                        _seqPath = strCsvPath;
                        _seqLength = fiW.Length;
                        _seqMtimeUtc = fiW.LastWriteTimeUtc;
                        _seqLast = seqValue;
                    }
                    catch { }
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

        /// <summary>
        /// 原子重写整份 CSV：先写临时文件再替换正式文件。
        /// 直接 WriteAllLines 会先清空目标文件，写入中途断电/异常会把统计数据清成半截或空文件；
        /// 临时文件方案保证失败(如目标被 Excel 独占打开)时旧内容完好，异常交由调用方记日志。
        /// </summary>
        private static void WriteAllLinesAtomic(string path, string[] lines)
        {
            string tmp = path + ".tmp";
            File.WriteAllLines(tmp, lines, Encoding.Default);
            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        /// <summary>
        /// ★复盘P1-2：字节级安全追加——历史字节一律不动（编码不符/有损解码场景绝不能整表重写），
        /// 只在文件尾补一行新记录；文件未以换行结尾时先补一个换行，消除旧实现恒拼 "\r\n" 前缀
        /// 造成的空行累积（与功能⑧同一收尾口径）。行内容由调用方按 Encoding.Default 组装。
        /// </summary>
        private static void AppendLineSafe(string path, string text)
        {
            byte[] payload = Encoding.Default.GetBytes(text + Environment.NewLine);
            // ★第41轮复盘P0：FileMode.Append 下 Seek 抛 IOException（"无法在追加模式下寻址"），
            //   且 FileAccess.Write 句柄本就不可 ReadByte —— 追加永不发生且被"记录1"吞成日志刷屏。
            //   改 OpenOrCreate + ReadWrite 单句柄：自行寻址到尾判定换行后顺次写入（语义等价追加，
            //   历史字节不动；文件不存在则创建）。
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                if (fs.Length > 0)
                {
                    fs.Seek(-1, SeekOrigin.End);
                    int tail = fs.ReadByte();   // 读完后位置恰为文件尾
                    if (tail != '\n' && tail != '\r')
                        fs.Write(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2);
                }
                fs.Write(payload, 0, payload.Length);
            }
        }
    }
}
