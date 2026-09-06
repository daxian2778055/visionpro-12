using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 生产统计服务。收口原 Form1 中散落的 <see cref="RunLog"/> 调用（向 E:\生产统计 / E:\每日统计 写入 CSV 良率统计）。
    /// <para>
    /// 内部委托既有的 <see cref="RunLog"/> 实现，保持 CSV 写入行为完全不变；Form1 只通过本服务记录统计，
    /// 不再直接持有 <see cref="RunLog"/> 实例，便于后续统一路径配置与单测。
    /// </para>
    /// </summary>
    public sealed class StatisticsService
    {
        private readonly RunLog _runLog = new RunLog();

        /// <summary>按年份创建「生产统计 / 每日统计」目录（path 为型号/工件编号）。</summary>
        public void CreateDirectoryCsvPath(string path)
        {
            _runLog.CreateDirectoryCsvPath(path);
        }

        /// <summary>若当月/当日 CSV 不存在则写入表头（shuju 为每日统计的明细列头）。</summary>
        public void CreateCsvPath(string path, string shuju)
        {
            _runLog.CreateCsvPath(path, shuju);
        }

        /// <summary>在「生产统计」月度汇总 CSV 中记录一次 OK/NG 结果。</summary>
        public void WriteDate(int ok, int ng, string path)
        {
            _runLog.WriteDate(ok, ng, path);
        }

        /// <summary>在「每日统计」明细 CSV 中追加一条记录（jilu 为结果明细，ok=1 表示 OK）。</summary>
        public void WriteDate1(string jilu, string path, int ok)
        {
            _runLog.WriteDate1(jilu, path, ok);
        }
    }
}
