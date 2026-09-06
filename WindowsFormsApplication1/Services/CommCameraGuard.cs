using System.Collections.Generic;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 多连接“相机归属”防呆（阶段 5）。
    ///
    /// 规则（按现场确认，2026-09-04）：
    ///   · <b>物理相机 1..12</b>：同一协议内，一台相机的触发与反馈一旦被某条连接配置，
    ///     其它连接<b>禁止</b>再配置这台相机（触发与反馈任一有值即视为已占用）；
    ///   · <b>相机 13 = 功能槽，不是物理相机</b>：
    ///       触发侧（chufa）= “切换方案”入口 —— 方案切换是整条产线的全局动作，
    ///       同一协议内只允许一条连接配置，其它连接禁止再配；
    ///       反馈侧（fankui）= “心跳” —— 各连接与各自 PLC 保活，互不占用，每连接可独立配置；
    ///   · <b>不同协议之间互不影响</b>：FINS 连接 2 配了相机 1 后，Modbus-TCP 仍可配相机 1。
    ///
    /// 为什么需要：结果回写只回写给“触发它的那条连接”。若同一协议内两条连接都绑了同一台相机，
    /// 回写目标就不唯一、会互相覆盖，所以在“保存配置”这一步直接拦住，而不是等现场出怪现象。
    ///
    /// 接入点：FormOmron 相机触发/反馈下拉的 26 个保存入口统一调用
    ///   <see cref="FindOwner"/> 判占用、占用则回滚并提示；FormFinsLinkConfig 等整段保存
    ///   路径调用 <see cref="CheckFins"/>。
    /// </summary>
    public static class CommCameraGuard
    {
        /// <summary>相机 13 是功能槽（触发侧=方案切换 / 反馈侧=心跳），不是物理相机。</summary>
        public const int SchemeSwitchCamera = 13;

        /// <summary>某相机绑定值是否视为“占用”该相机（空白/“无”/“（无绑定）”不算）。</summary>
        public static bool IsBoundValue(string v)
        {
            if (v == null) return false;
            v = v.Trim();
            return v.Length > 0 && v != "无" && v != "（无绑定）";
        }

        /// <summary>
        /// 查询同协议内某相机/功能槽当前被哪条连接占用（0 = 空闲，当前连接可绑定）。
        /// 占用条件：
        ///   相机 1..12：其它连接里该相机的触发（chufa）或反馈（fankui）任一有值即占用；
        ///   相机 13：仅其它连接的触发侧（方案切换）有值才算占用，反馈侧（心跳）每连接独立、不计占用。
        /// </summary>
        public static int FindOwner(ClassIni ini, int selfLinkId, int cameraNo)
        {
            if (ini == null) return 0;
            for (int link = 1; link <= FinsIniStore.MaxLinks; link++)
            {
                if (link == selfLinkId) continue;
                var cfg = FinsIniStore.Load(ini, link);
                var cam = cfg.CameraBindings.Find(c => c.CameraNo == cameraNo);
                if (cam == null) continue;
                bool occupied = cameraNo == SchemeSwitchCamera
                    ? IsBoundValue(cam.Chufa)                       // 方案切换槽只看触发侧
                    : IsBoundValue(cam.Chufa) || IsBoundValue(cam.Fankui);
                if (occupied)
                    return link;
            }
            return 0;
        }

        /// <summary>取占用连接的显示名（FINS-2 / 自定义名）。</summary>
        public static string OwnerDisplayName(ClassIni ini, int ownerLink)
        {
            if (ini == null || ownerLink < 1) return "FINS-" + ownerLink;
            var cfg = FinsIniStore.Load(ini, ownerLink);
            return string.IsNullOrEmpty(cfg.Name) ? "FINS-" + ownerLink : cfg.Name;
        }

        /// <summary>
        /// 检查 FINS 协议内的相机/功能槽冲突（整段保存用）。
        /// </summary>
        /// <param name="ini">test.ini（调用前应保证能看到其它连接最新配置）</param>
        /// <param name="selfLinkId">当前正在保存的连接号（自身不参与冲突判断）</param>
        /// <param name="selfBindings">本连接将要保存的相机绑定表（含相机 1..13 每一行的触发/反馈）</param>
        /// <returns>冲突说明；null 表示无冲突，可以保存。</returns>
        public static string CheckFins(ClassIni ini, int selfLinkId, IEnumerable<FinsCameraBindingConfig> selfBindings)
        {
            if (ini == null || selfBindings == null) return null;

            // 本连接想占用的物理相机（1..12，触发或反馈任一有值即要占用这台相机）
            var physical = new List<int>();
            // 本连接是否要占用“方案切换”入口（相机 13 的触发侧）
            bool wantsScheme = false;
            foreach (var c in selfBindings)
            {
                if (c.CameraNo == SchemeSwitchCamera)
                {
                    if (IsBoundValue(c.Chufa)) wantsScheme = true;
                    continue;   // 13 反馈侧（心跳）各连接独立，不产生占用
                }
                if (c.CameraNo < 1 || c.CameraNo > 12) continue;
                if ((IsBoundValue(c.Chufa) || IsBoundValue(c.Fankui))
                    && !physical.Contains(c.CameraNo))
                    physical.Add(c.CameraNo);
            }
            if (physical.Count == 0 && !wantsScheme) return null;

            for (int link = 1; link <= FinsIniStore.MaxLinks; link++)
            {
                if (link == selfLinkId) continue;
                FinsLinkConfig cfg = FinsIniStore.Load(ini, link);
                foreach (var cam in cfg.CameraBindings)
                {
                    if (cam.CameraNo == SchemeSwitchCamera)
                    {
                        // 心跳（13 反馈侧）不占用任何资源；仅触发侧占用“方案切换”
                        if (wantsScheme && IsBoundValue(cam.Chufa))
                        {
                            string name = OwnerDisplayName(ini, link);
                            return "“切换方案”触发块已经由 FINS 连接 " + link + "（" + name + "）配置。\r\n\r\n"
                                 + "方案切换是整条产线的全局动作，同一协议内只允许一条连接负责切方案（各连接的心跳可独立配置，互不影响）。\r\n"
                                 + "如要改为本连接，请先选中 FINS 连接 " + link + "，把相机 13 的“切换方案”触发下拉选成“（无绑定）”释放。";
                        }
                        continue;
                    }
                    if (!physical.Contains(cam.CameraNo)) continue;
                    if (!IsBoundValue(cam.Chufa) && !IsBoundValue(cam.Fankui)) continue;

                    string n2 = OwnerDisplayName(ini, link);
                    return "相机 " + cam.CameraNo + " 已经由 FINS 连接 " + link + "（" + n2 + "）配置了触发/反馈。\r\n\r\n"
                         + "同一协议内，一台相机只能归属一条连接（不同协议之间互不影响）。\r\n"
                         + "如要改为本连接，请先选中 FINS 连接 " + link + "，把相机 " + cam.CameraNo + " 那整行清空并保存。";
                }
            }
            return null;
        }
    }
}
