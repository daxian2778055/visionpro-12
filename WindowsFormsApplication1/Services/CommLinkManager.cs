using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯连接管理器（多实例改造 阶段2）。
    ///
    /// 作用：把所有连接实例登记到一处，按 LinkId 索引。
    ///       后续“触发来自哪个连接”“结果回写给哪个连接”都通过它查找目标。
    ///
    /// 当前阶段仍只有 1 个 Modbus TCP 实例被登记，因此行为与改造前完全一致；
    /// 但它已经具备容纳第 2、第 3 个连接的能力（阶段5 配置多段化后即可启用）。
    /// </summary>
    public class CommLinkManager
    {
        private readonly object _sync = new object();
        private readonly List<ICommLink> _links = new List<ICommLink>();

        /// <summary>登记（或按 LinkId 替换）一个连接实例。</summary>
        public void Register(ICommLink link)
        {
            if (link == null) return;
            lock (_sync)
            {
                int idx = _links.FindIndex(l => l.LinkId == link.LinkId);
                if (idx >= 0)
                    _links[idx] = link;
                else
                    _links.Add(link);
            }
        }

        /// <summary>注销指定连接，返回是否确实移除了一个。</summary>
        public bool Unregister(int linkId)
        {
            lock (_sync)
            {
                return _links.RemoveAll(l => l.LinkId == linkId) > 0;
            }
        }

        /// <summary>按 LinkId 取连接，不存在返回 null。</summary>
        public ICommLink Get(int linkId)
        {
            lock (_sync)
            {
                return _links.FirstOrDefault(l => l.LinkId == linkId);
            }
        }

        /// <summary>取全部连接的副本（避免外部遍历时集合被改动）。</summary>
        public List<ICommLink> GetAll()
        {
            lock (_sync)
            {
                return _links.ToList();
            }
        }

        /// <summary>按协议类型取连接集合，如 GetByProtocol("modbustcp")。</summary>
        public List<ICommLink> GetByProtocol(string protocol)
        {
            if (string.IsNullOrEmpty(protocol)) return new List<ICommLink>();
            lock (_sync)
            {
                return _links
                    .Where(l => string.Equals(l.Protocol, protocol, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>分配一个当前未占用的 LinkId（用于界面“新增连接”）。</summary>
        public int NextLinkId()
        {
            lock (_sync)
            {
                if (_links.Count == 0) return 1;
                return _links.Max(l => l.LinkId) + 1;
            }
        }

        /// <summary>已登记的连接数量。</summary>
        public int Count
        {
            get { lock (_sync) { return _links.Count; } }
        }

        /// <summary>关闭并断开所有连接（退出程序时调用）。</summary>
        public void CloseAll()
        {
            List<ICommLink> snapshot = GetAll();
            foreach (ICommLink link in snapshot)
            {
                try { link.Close(); }
                catch { }
            }
        }
    }
}
