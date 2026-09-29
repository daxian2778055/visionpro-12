using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 显示降频的现场可调参数（★第57轮收窄，code.ini [Display] 段，全部只读值由窗体侧解析后注入）。
    ///
    /// 现场只留**三个可调项**（用户要求"参数越少、配置越方便"）：
    ///   · 频率阈值 RenderProtectHz：实测帧率低于它的路**一帧不丢**，也不参与降频分摊。
    ///     第54轮的 max-min 只在"该路需求 ≤ 水位"时全显，水位=1000/RenderMinIntervalMs/路数，
    ///     12 路 16ms 时只有 5.2fps——10Hz 的触发路本来该全显却被挤，这条把保护写成硬承诺。
    ///   · 权重 RenderWeight 0~100：对**高于阈值**的路降得多狠，而且是**主动降**（不是"等预算不够才降"）。
    ///   · 最大降幅 RenderMaxCutPercent：W=100 时的降幅上限，全带统一的那一个数（现场原话"极致权重时
    ///     80Hz 显示 40Hz" ⇒ 默认 50%）。它是上限而不是"高频带专用值"：需求达到高频带分界（固定 50Hz）
    ///     才取满，阈值~分界之间线性上升、最多只到它的一半（见 CutPermyriad 与下面的派生量）。
    ///
    /// 另外三个量第57轮起**由代码派生、不再开放给现场**，各自都有理由：
    ///   · 高频带分界 = 固定 HighBandHzFixed(50Hz)：它只决定"降幅斜坡在哪里封顶"，现场相机基本都在
    ///     60Hz 以下，这个值常年用不上，却会让人误以为要调。
    ///   · 中频带最大降幅 = 最大降幅的一半：默认本来就是 50/25 的一半关系；若两者同值，
    ///     "频率越高降得越多"这条自相矛盾，所以它是被前一个量决定死的派生值，不是独立自由度。
    ///   · 交互期间隔 = 全局最小间隔的一半、地板 InteractiveFloorMs(8ms)：它的生效条件本来就是
    ///     "严格小于全局间隔"（见 Form1.TryClaimRenderBudget），填得比全局大就是个死框——
    ///     一个只在特定大小关系下才起作用的框不该占现场一格。全局=16 时推出 8，与第56轮默认完全一致；
    ///     全局 ≤ 16 时推出值不小于全局 ⇒ 交互期不再放宽（已经压得够狠，没必要再放宽）。
    ///
    /// 两条与用户原话的出入，写在这里以免日后当成实现走样：
    ///   · "50Hz 以下即使极致权重也只能降频百分之50以下"按**上限**理解，中带取一半：要按字面放宽
    ///     只需改 CutPermyriad 里那一处派生，结构不动。
    ///   · W=0 **不等于**"完全不降频"：封顶（RenderMinIntervalMs）仍在，W=0 只是不主动降高频路，
    ///     退回第54轮的按需求解分摊。要真·不限速请把 RenderMinIntervalMs 设 0（该语义本来就存在）。
    ///     ProtectHz=0 且 W=0 时 RenderBudget 走第54轮原分支，逐帧一致（回退基线/现场总开关）。
    ///
    /// 代价要说清楚：**权重会主动压低高频路的显示率**，即使预算还有富余也一样压——这是"用显示换
    /// CPU/稳定"的设计意图，不是回归；反过来，因为它只减不加，各路**应得速率之和**天然 ≤ 封顶
    /// （实测放行帧数只在估计期临时越顶，一次性、不累积，见 RenderBudget 头部"上限不变"段），
    /// 所以本轮早期草案的"下限不可行时保封顶/保感官"(FloorMode) 与"硬底间隔"两个旋钮被删除：
    /// 主动降幅口径下越顶的分支根本走不到，留着只会让现场多两个不起作用的输入框。
    /// 保护带同样是零和的：低频路"一帧不丢"多显示的那一部分必然来自快路让出的额度，
    /// 默认 ProtectHz=10 不是免费的（数字与对照见 DisplayThrottleWeightTests）。
    /// </summary>
    internal sealed class DisplayThrottleSettings
    {
        // 默认值：权重默认 0（不启用主动降幅）。阈值默认 10 **不是免费的**——封顶是零和的，
        // 保护带先从封顶扣一份额度，剩下的才分摊；饱和时快路必然少显示。只有各路都高于阈值时
        // 保护带为空，分摊结果才与第54轮同形（差别仅剩启动瞬态，见 RenderBudget 头部口径）。
        public const int ProtectHzDefault = 10;
        public const int WeightDefault = 0;
        public const int MaxCutPercentDefault = 50;

        /// <summary>高频带分界（Hz）：第57轮起固定，不再开放给现场（理由见类注释）。</summary>
        public const int HighBandHzFixed = 50;

        /// <summary>交互期间隔的地板（毫秒）：第56轮 P4 实测出来的取值，再小就等于不限速。</summary>
        public const int InteractiveFloorMs = 8;

        public readonly int ProtectHz;          // 阈值（Hz）；0=不设保护带
        public readonly int Weight;             // 0~100
        public readonly int HighBandHz;         // 高频带分界（Hz），恒 = HighBandHzFixed
        // ★第58轮②改名（原 HighMaxCutPercent）：本值即界面上那一格「最大降幅」，是全带统一的削减上限，
        //   不只管高频带——中带恒取其一半，所以别被旧名字误导成"还有另一个高频带专用参数"。
        public readonly int MaxCutPercent;      // W=100 时的最大降幅（%）——现场那一格
        public readonly int MidMaxCutPercent;   // W=100 时中频带降幅（%）——恒 = MaxCutPercent 的一半

        public DisplayThrottleSettings(int protectHz, int weight, int maxCutPercent)
        {
            ProtectHz = protectHz;
            Weight = weight;
            HighBandHz = HighBandHzFixed;
            MaxCutPercent = maxCutPercent;
            MidMaxCutPercent = maxCutPercent / 2;
        }

        /// <summary>第54轮口径（无保护带、无权重）——新参数的总开关位，也是回归基线。</summary>
        public static readonly DisplayThrottleSettings Legacy =
            new DisplayThrottleSettings(0, 0, MaxCutPercentDefault);

        /// <summary>是否退回第54轮分支：阈值与权重同时为 0 才算，缺一律走新口径。</summary>
        public bool IsLegacy { get { return ProtectHz <= 0 && Weight <= 0; } }

        /// <summary>
        /// 交互期间的自动推导：全局间隔的一半，但不低于 InteractiveFloorMs；全局为 0（不限速）时返回 0。
        /// 推出来不小于全局时，调用点那条"交互期必须严格小于全局"的判据自然不成立=交互期不放宽，
        /// 这正是"已经压得够狠就别再放宽"的期望行为，所以这里不需要额外分支。
        /// </summary>
        public static int AutoInteractiveIntervalMs(int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            int half = minIntervalMs / 2;
            return half < InteractiveFloorMs ? InteractiveFloorMs : half;
        }

        /// <summary>
        /// 脏值兜底：越界与不可解析一律回默认，并回报被修正的项数（窗体侧据此写日志，
        /// 让"我改了没生效"当场可见，与本项目其它 ini 读回同一纪律）。
        /// </summary>
        public static int Normalize(int protectHz, int weight, int maxCutPercent,
            out DisplayThrottleSettings result)
        {
            int fixedCount = 0;
            int p = Clamp(protectHz, 0, 1000, ProtectHzDefault, ref fixedCount);
            int w = Clamp(weight, 0, 100, WeightDefault, ref fixedCount);
            int hc = Clamp(maxCutPercent, 0, 90, MaxCutPercentDefault, ref fixedCount);
            result = new DisplayThrottleSettings(p, w, hc);
            return fixedCount;
        }

        private static int Clamp(int value, int min, int max, int dflt, ref int fixedCount)
        {
            if (value < min || value > max) { fixedCount++; return dflt; }
            return value;
        }
    }

    /// <summary>
    /// 显示渲染预算的**分摊**调度器（纯 BCL，不依赖 Cognex，源码链接进测试工程）。
    /// 只做一件事：决定"这一帧显不显示"。**绝不参与检测本身**——被拒的帧照常完成 block.Run、
    /// 判定、计数、存图、PLC 反馈，一路计数不受本类影响（这条边界由
    /// RenderBudgetTests.调度器只做显示裁决_没有任何其它出口 钉住）。
    ///
    /// 三代口径，靠 DisplayThrottleSettings 的开关位共存：
    ///  ①第54轮 max-min（ProtectHz=0 且 Weight=0 时逐帧生效，是回退基线）：
    ///     各路实测帧率折算需求，二分解公共水位 λ，需求 ≤ λ 的路一帧不丢，> λ 的路统一按 1000/λ。
    ///     要解决的问题：旧"全局先到先得 + 被拒 40 次放行一次"会锁相，与赢家同相的路每帧都被拒，
    ///     现场表现为"总盯着某一个相机刷新"（模拟：12 路×3fps、预算根本没用完，11 路显示率 0%）。
    ///     遗留硬伤（本轮治的）：水位是**绝对速率**，快路被按同一个绝对速率压。模拟场景
    ///     1 路 80Hz + 低频邻居时水位 8.3Hz ⇒ 80Hz 那路每 9.6 帧只显示 1 帧（连丢 8~9 帧，感官上是幻灯片）；
    ///     保护线也只有 cap/路数≈5.2fps，10Hz 的触发路并没有被"保护"，只是运气好分得多。
    ///  ②本轮阈值保护带：需求 ≤ RenderProtectHz 的路应得速率 = 自己的需求（一帧不丢），
    ///     并且先从总封顶里扣掉——封顶仍然成立，因为水位是在**剩余额度**上解的。
    ///     进出保护带用 1.25 倍迟滞（≤阈值进、>阈值×1.25 才出），否则实测帧率在阈值附近抖动
    ///     会让同一路反复进出保护，画面一阵流畅一阵卡顿，比不保护更难看。
    ///  ③本轮权重主动降幅：非保护路各自有一条显示**上限**速率 = 需求 ×(1−降幅)，降幅随频率递增
    ///     （中频带线性斜坡到 MidMaxCut、高频带取 HighMaxCut），W=100 时 80Hz 的上限恰为 40Hz。
    ///     第57轮起 HighBandHz 固定 50Hz、MidMaxCut 恒为 HighMaxCut 的一半，现场只留"最大降幅"那一格；
    ///     裁决逻辑本身没变，变的只是这两个量的来源（派生而非填表）。
    ///     与旧草案的"下限"相反，它是**主动降**：预算闲置也一样把快路压到上限，用显示换 CPU 稳定；
    ///     水位 λ 在"各路上限"集合上解（Σ min(上限, λ) = 剩余额度），应得速率 = min(上限, λ)。
    ///     因为只减不加，各路**应得速率之和**天然 ≤ 封顶（实测放行帧数只在估计期临时越顶，
    ///     一次性、不累积，见下方"上限不变"段），所以早期草案的"下限不可行时保封顶/保感官"(FloorMode)
    ///     与"硬底间隔"两个旋钮被删除——主动降幅口径下越顶分支根本走不到，留着只是多两个空输入框。
    ///
    /// 三条目标不能同时成立，代码不假装能：保护低频 / 连丢≤1 / 总量封顶 只能取其二。
    /// 12 路×30fps 时（需求 360/s、封顶 62.5/s）要每路连丢≤1 就得把间隔压到 ≤8ms（CPU 1.9 倍）。
    /// 本轮取的是"保护低频 + 总量封顶"两项：**连丢≤1 被有意让给高频路**——降幅本身就是连丢条数
    /// （降 50% 即隔帧显示=连丢 1，降 75% 即连丢 3），这正是现场要的"频率越高降得越狠"，
    /// 因此不再把它当隐含承诺；只有保护带内的路是硬承诺一帧不丢（连丢 0）。
    ///
    /// 需求估计（本轮 P1 加固，只在新口径分支生效，旧分支保留原估计以免行为漂移）：
    ///   · 上调快、下调慢：帧率变慢（周期变长）若一次跳变超过 1.5 倍直接采信新周期，
    ///     否则 30fps 的路切到 3fps 后仍按 33ms 记 30fps 需求，会白占一整段封顶把别人挤死
    ///     （由 DisplayThrottleWeightTests 的 帧率变慢的跳变一次采信_小幅变化走平滑 复测：
    ///     33ms 稳态后改 300ms 间隔，一次采信；33→40 的小幅变化仍走 3:1 平滑）；
    ///   · 断流即作废：相邻两帧间隔超过普查窗口(2s) 时不再"跳过这一笔样本"，而是把周期估计清零
    ///     重新采样——原实现跳过之后旧值永久残留，正是上一条的成因
    ///     （由 DisplayThrottleWeightTests 的 断流超过普查窗口后周期作废重采样 复测）；
    ///   · 无样本的路不计入分母、且自由放行：原实现按 100fps 记帐（UnknownDemandMilliFps），
    ///     12 路同时冷启动时水位被解到 5.2fps（=封顶/路数）⇒ 首帧之后 192ms 内谁都不放行，
    ///     而那一路其实只要再等一个采样周期就能交出真实需求；改为不计入分母之后，
    ///     交出首个周期样本之前那一路不受虚高水位压制
    ///     （离线模拟实测：12 路×30fps 场景 6 秒全窗口放行 375→384 帧，多出的每路一帧来自启动阶段
    ///     到期表的错位；稳态窗口 [2000,6000) 两代逐路差 ≤1 帧，即差别不随时间累积，由
    ///     DisplayThrottleWeightTests 的 尚无周期样本的路不占分母也不被分摊限速 与
    ///     权重为零时不启用主动降幅_保护带之外与第54轮同形 钉住）。
    ///
    /// 上限不变：RenderMinIntervalMs 仍是唯一的 CPU 旋钮。主动降幅只减不加，所以**解析层面**各路
    /// 应得速率之和不超过 1000/它（水位那轮二分只可能少放、不可能多放，粒度上界=路数×1 毫帧/秒，
    /// 即 62.5fps 封顶下最多多 0.012fps）。
    /// 但**实测放行帧数**在估计期会临时越顶，这条要说清楚别当成无条件承诺：封顶是解析式的
    /// （解出来的水位），不是全局令牌桶，没有谁在放行那一刻再去扣一个总闸。需求还没测出来的路
    /// 不预约额度（见上一条 P1 口径），于是在它交出首个周期样本之前，已经测得的路可以把那份
    /// 闲额度用满。离线模拟实测：「2 路 30fps + 10 路 3fps」场景 6 秒全窗口放行 393 帧 > 封顶 375
    /// 帧；同一场景的稳态窗口 [1000,6000)/[2000,6000) 为 309/248 帧，均在上界（324/262）之内
    /// ——超出量是一次性的（长度=最慢那路的一个采样周期），不随时间累积。断言按稳态窗口写，
    /// 见 DisplayThrottleWeightTests 的 权重拉满也不越过封顶_多场景总量校验。
    /// 因此新口径同时提供 GrantedPerSec/WaterLevelFps/PressureCode/CapUsedPercent 只读实测供现场核对。
    ///
    /// 唯一的份额例外（本轮 P5）：停止态末帧一次性旁路 TryClaimTailFrame。路已退出运行态、
    /// 且这一帧刚被分摊裁决挡掉时，允许破例显示**一次**（每路每次停止一帧、全局最多 12 帧，
    /// 运行态一帧都不破例、并把额度复位给下一次停止）。它是显示层的一次性补救，
    /// 不动到期表也不占别人份额（RenderTailFrameTests 逐帧对照钉住这两条）。
    /// 边界同样不许夸大：只有"走到显示裁决这一步"的帧能救，接收前就被丢的帧没有像素可画，
    /// 因此这不是"保证显示最后一帧"，只是"停止时不把最后一帧也挡掉"。
    ///
    /// 线程约定：检测线程并发调用，全 Volatile/Interlocked，无锁、无分配、不阻塞
    /// （Allocation 是栈上结构体，每次现算，不落共享数组——本类不引入任何堆分配）。
    /// now 一律是 Environment.TickCount（int 毫秒），差值全部走 unchecked 以自洽回绕；
    /// "首帧必放行"由每路的到达过/渲染过标志保证，不依赖任何哨兵时刻值（哨兵在环形坐标上
    /// 必有半周期落在"未来"，机器连续开机 18.7~43.5 天这段启动会让所有路永不刷新）。
    ///
    /// 自身开销：第54轮实测单次约 2.4 微秒（15276 次裁决累计 36.4ms）。本轮新口径每路多解
    /// 两轮二分（每轮遍历 12 路），仍为微秒级；渲染本身一次是毫秒级，差三个数量级。
    /// </summary>
    internal sealed class RenderBudget
    {
        public const int Slots = 12;

        /// <summary>最近这么久之内请求过渲染的路才算"活跃"（参与分摊普查）；停流的路 2 秒后自动让出份额。</summary>
        public const int ActivityWindowMs = 2000;

        /// <summary>速率单位：毫帧/秒（1000 = 1fps），全程整数运算避免浮点与非原子双字写。</summary>
        private const int MilliPerFps = 1000;

        /// <summary>10000 分之几（万分比）单位，用于主动降幅。</summary>
        private const int Permyriad = 10000;

        /// <summary>尚无周期样本的路按此需求记账（旧分支=视作贪心路）。高估贪心路不影响他人：份额只由喂饱路的需求与贪心路条数决定。</summary>
        private const int UnknownDemandMilliFps = 100 * MilliPerFps;

        /// <summary>新分支：该路已到帧但还没有周期样本（既不计入分母，也不限速）。</summary>
        private const int DemandUnknown = -1;

        // ★ 迟滞倍数的分子/分母：出保护带要求 需求 > 阈值 × 5/4。
        private const int TierHysteresisNum = 5;
        private const int TierHysteresisDen = 4;

        private readonly int[] _dueTick = new int[Slots];      // 每路下次应渲染时刻（仅 _granted=1 时有意义）
        private readonly int[] _granted = new int[Slots];      // 该路渲染过一次没有（首帧必放行的依据）
        private readonly int[] _requestTick = new int[Slots];  // 每路最近一次请求时刻（活跃普查，仅 _arrived=1 时有意义）
        private readonly int[] _arrived = new int[Slots];      // 该路到达过一次没有
        private readonly int[] _lastArriveTick = new int[Slots]; // 每路上一次到达时刻（测周期用）
        private readonly int[] _periodMs = new int[Slots];     // 每路实测帧周期（0=尚无样本）

        // ↓ 以下四组只服务新口径与只读诊断，不参与旧口径的任何判据。
        private readonly int[] _tier = new int[Slots];         // 保护带迟滞标记（1=在保护带内）
        private readonly int[] _grantTotal = new int[Slots];   // 累计放行次数（对账用）
        private readonly int[] _grantSec = new int[Slots];     // 当前统计秒内放行次数
        private readonly int[] _grantSecPrev = new int[Slots]; // 上一个统计秒放行次数
        private readonly int[] _grantSecTick = new int[Slots]; // 当前统计秒起点
        // ★第56轮 P5：停止/暂停态末帧一次性旁路。1=本次停止已经补显过一帧（额度用掉），
        //   0=还可以补显一帧；路重新运行时复位成 0（复位在 TryClaimTailFrame 里做，
        //   不在这里初始化——12 个 int 的默认值本来就是 0，未启动的路天然还有一次额度）。
        private readonly int[] _tailUsed = new int[Slots];
        private readonly int[] _tailTotal = new int[Slots];    // 累计补显次数（只读诊断）

        // 参数引用：可见性一律由 Volatile.Read/Write 保证（字段本身不加 volatile——
        // 加了对 Volatile.Read(ref _settings) 这类写法反而只报 CS0420，一个好处都没有）。
        private DisplayThrottleSettings _settings = DisplayThrottleSettings.Legacy;

        // 为什么用"到达过/渲染过"标志而不是某个哨兵时刻值：Environment.TickCount 是回绕的 int，
        // 任何固定常量在半个周期里都位于"未来"——机器连续开机约 18.7~43.5 天这段启动的话，
        // 用 MinValue/4 当"很久以前"会让每路首帧都被判成"还没到期"且永不解锁。
        // 标志位不参与环形比较，回绕前后一律自洽；_dueTick/_requestTick 只在标志置位后才被读取。

        /// <summary>
        /// 注入现场参数（窗体侧解析 ini / 配置窗应用时调用）。返回 1=与当前不同（已生效）、0=相同。
        /// 参数是**不可变对象的一次性换引用**，不是逐字段写：检测线程要么整份看到旧参数、
        /// 要么整份看到新参数，不会看到"阈值已改、权重还没改"的半套组合。
        /// </summary>
        public int ApplyConfig(DisplayThrottleSettings settings)
        {
            if (settings == null) return 0;
            DisplayThrottleSettings old = Volatile.Read(ref _settings);
            if (old == settings) return 0;
            Volatile.Write(ref _settings, settings);
            return 1;
        }

        /// <summary>
        /// 清空某路的帧率账（周期估计与保护带标记）。换触发模式、该路由停转起、切方案时调用：
        /// 30fps 的连续运行切到 1fps 的通讯触发，若留着 33ms 的陈旧周期，它会按 30fps 记账白占封顶。
        /// </summary>
        public int ResetPathAccounting(int slot)
        {
            if (slot < 0 || slot >= Slots) return 0;
            Volatile.Write(ref _periodMs[slot], 0);
            Volatile.Write(ref _tier[slot], 0);
            return 1;
        }

        /// <summary>活跃路数（普查窗口内请求过的路）——只作诊断展示，分摊分母由需求解决定。</summary>
        public int ActivePaths(int now)
        {
            int n = 0;
            for (int i = 0; i < Slots; i++)
            {
                if (Volatile.Read(ref _arrived[i]) == 0) continue;
                if (unchecked(now - Volatile.Read(ref _requestTick[i])) <= ActivityWindowMs) n++;
            }
            return n;
        }

        #region 旧口径（第54轮）——ProtectHz=0 且 Weight=0 时逐帧走这里，一个字节没动

        /// <summary>旧分支需求：从未到达或停流超过普查窗口为 0；尚无周期样本按 100fps 记帐。</summary>
        private int LegacyDemandMilliFps(int slot, int now)
        {
            if (Volatile.Read(ref _arrived[slot]) == 0) return 0;
            if (unchecked(now - Volatile.Read(ref _requestTick[slot])) > ActivityWindowMs) return 0;
            int p = Volatile.Read(ref _periodMs[slot]);
            return p <= 0 ? UnknownDemandMilliFps : MilliPerFps * 1000 / p;
        }

        /// <summary>旧分支：全体需求按 cap 截断后求和（单调递增于 cap，用于二分求份额）。</summary>
        private int LegacyCappedDemandMilliFps(int capMilliFps, int now)
        {
            int sum = 0;
            for (int i = 0; i < Slots; i++)
            {
                int d = LegacyDemandMilliFps(i, now);
                sum += d < capMilliFps ? d : capMilliFps;
            }
            return sum;
        }

        /// <summary>登记一次帧到达（测周期，旧分支口径）。该路首次到达不取样本；间隔非正/超普查窗口也不取（旧值保留）。</summary>
        private void LegacyNoteArrival(int slot, int now)
        {
            int prevArrive = Volatile.Read(ref _lastArriveTick[slot]);
            bool firstEver = Volatile.Read(ref _arrived[slot]) == 0;
            Volatile.Write(ref _lastArriveTick[slot], now);
            Volatile.Write(ref _requestTick[slot], now);
            Volatile.Write(ref _arrived[slot], 1);
            if (firstEver) return;
            int gap = unchecked(now - prevArrive);
            if (gap <= 0 || gap > ActivityWindowMs) return;
            int p = Volatile.Read(ref _periodMs[slot]);
            Volatile.Write(ref _periodMs[slot], p <= 0 ? gap : (p * 3 + gap) / 4);
        }

        /// <summary>旧分支：解出贪心路的公共份额（毫帧/秒）。</summary>
        private int LegacyShareMilliFps(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return int.MaxValue;
            int cap = MilliPerFps * 1000 / minIntervalMs;
            int lo = 0, hi = cap;
            while (hi - lo > 1)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (LegacyCappedDemandMilliFps(mid, now) >= cap) hi = mid;
                else lo = mid;
            }
            return hi;
        }

        /// <summary>旧分支：本路应得间隔。喂饱路取自己周期打 9 折，贪心路取 1000/份额。</summary>
        private int LegacyEntitledMs(int slot, int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            if (slot < 0 || slot >= Slots) return minIntervalMs;
            int share = LegacyShareMilliFps(now, minIntervalMs);
            int own = LegacyDemandMilliFps(slot, now);
            if (own > 0 && own <= share)
            {
                int p = Volatile.Read(ref _periodMs[slot]);
                int fair = p <= 0 ? minIntervalMs : p - p / 10;
                return fair < minIntervalMs ? minIntervalMs : fair;
            }
            int greedy = (MilliPerFps * 1000 + share - 1) / share;
            return greedy < minIntervalMs ? minIntervalMs : greedy;
        }

        #endregion

        #region 新口径：保护带 + 权重主动降幅 + 剩余额度分摊

        /// <summary>
        /// 一次分摊解的全部中间量（栈上结构体，不跨线程共享、不落堆）。字段都是毫帧/秒，
        /// 只有 Pressure/DemotedProtection 是标记。
        /// </summary>
        private struct Allocation
        {
            public int CapMilliFps;             // 全局封顶（1000/RenderMinIntervalMs）
            public int ProtectLimitMilliFps;    // 生效的保护上限（保护带总额超封顶时从最高需求那路开始剥夺）
            public int RemainingCapMilliFps;    // 扣掉保护带之后的剩余额度
            public int ShareMilliFps;           // 水位 λ：在非保护路的**显示上限**集合上解出
            public int GrantedMilliFps;         // 保护带 + 各路 min(上限, λ) 的总额（对账用，≤ 封顶）
            public int Pressure;                // 0=预算闲置（各路都按自己的上限显示还有余）1=水位 λ 在真正限速
            public int DemotedProtection;       // 1=有路因封顶不够而被剥夺保护
        }

        /// <summary>
        /// 新分支需求。0=不参与分摊（从未到达/停流超窗口）；DemandUnknown=已到帧但无周期样本
        /// （不占分母、不限速）；&gt;0=实测需求。
        /// </summary>
        private int DemandMilliFps(int slot, int now)
        {
            if (Volatile.Read(ref _arrived[slot]) == 0) return 0;
            if (unchecked(now - Volatile.Read(ref _requestTick[slot])) > ActivityWindowMs) return 0;
            int p = Volatile.Read(ref _periodMs[slot]);
            return p <= 0 ? DemandUnknown : MilliPerFps * 1000 / p;
        }

        /// <summary>
        /// 降幅（万分比）：保护带 0；中频带从阈值处的 0 线性升到高频带分界处的 MidMaxCut×W/100；
        /// 高频带（≥HighBandHzFixed，第57轮起固定 50Hz）取 HighMaxCut×W/100——80Hz 在 W=100 时恰为
        /// 50%（显示上限 40Hz）。
        /// 百分数 × 权重(0~100) 直接就是万分比，不需要再折算。
        /// 阈值 ≥ 高频带分界属配置写反，此时一律按高频带处理（不给"配了阈值却没保护"的静默）。
        /// </summary>
        private static int CutPermyriad(int demandMilliFps, DisplayThrottleSettings cfg)
        {
            if (cfg.Weight <= 0 || demandMilliFps <= 0) return 0;
            int protectMilli = cfg.ProtectHz * MilliPerFps;
            int highMilli = cfg.HighBandHz * MilliPerFps;
            int span = highMilli - protectMilli;
            if (span <= 0 || demandMilliFps >= highMilli) return cfg.MaxCutPercent * cfg.Weight;
            int over = demandMilliFps - protectMilli;
            if (over < 0) over = 0;
            // 中频带线性斜坡：越接近高频带分界降得越多（"频率越高降频比例越大"）
            return (int)((long)cfg.MidMaxCutPercent * cfg.Weight * over / span);
        }

        /// <summary>本路的显示**上限**速率 = 需求 ×(1−降幅)。降幅非负，故上限恒 ≤ 需求（只减不加）。</summary>
        private static int CeilingMilliFps(int demandMilliFps, DisplayThrottleSettings cfg)
        {
            if (demandMilliFps <= 0) return 0;
            int keep = Permyriad - CutPermyriad(demandMilliFps, cfg);
            if (keep < 0) keep = 0;
            return (int)((long)demandMilliFps * keep / Permyriad);
        }

        /// <summary>保护带总额：标记在保护带内且需求不超过上限 pc 的路，需求之和。</summary>
        private int ProtectedSumMilliFps(int pcMilliFps, int now)
        {
            int sum = 0;
            for (int i = 0; i < Slots; i++)
            {
                if (Volatile.Read(ref _tier[i]) == 0) continue;
                int d = DemandMilliFps(i, now);
                if (d <= 0 || d > pcMilliFps) continue;
                sum += d;
            }
            return sum;
        }

        /// <summary>该路本次是否按保护带对待（在保护带标记内且需求不超过生效上限）。</summary>
        private bool IsProtected(int slot, Allocation a, int now)
        {
            if (Volatile.Read(ref _tier[slot]) == 0) return false;
            int d = DemandMilliFps(slot, now);
            return d > 0 && d <= a.ProtectLimitMilliFps;
        }

        /// <summary>非保护路（含被剥夺保护的路）的集合判据：需求存在且不吃保护带额度。</summary>
        private bool SharesBudgetWith(int slot, int demandMilliFps, Allocation a)
        {
            if (demandMilliFps <= 0) return false;   // 未知需求(−1)与停流(0)都不占分母
            if (Volatile.Read(ref _tier[slot]) != 0 && demandMilliFps <= a.ProtectLimitMilliFps) return false;
            return true;
        }

        /// <summary>Σ min(显示上限, x) —— 在剩余额度上解水位 λ 用（单调递增于 x）。</summary>
        private int CeilCappedSum(int xMilliFps, Allocation a, int now, DisplayThrottleSettings cfg)
        {
            int sum = 0;
            for (int i = 0; i < Slots; i++)
            {
                int d = DemandMilliFps(i, now);
                if (!SharesBudgetWith(i, d, a)) continue;
                int c = CeilingMilliFps(d, cfg);
                sum += c < xMilliFps ? c : xMilliFps;
            }
            return sum;
        }

        /// <summary>非保护路的最大显示上限（水位二分的上界；上限恒 ≤ 需求，故比旧口径的上界更小）。</summary>
        private int MaxShareCeiling(int now, Allocation a, DisplayThrottleSettings cfg)
        {
            int max = 0;
            for (int i = 0; i < Slots; i++)
            {
                int d = DemandMilliFps(i, now);
                if (!SharesBudgetWith(i, d, a)) continue;
                int c = CeilingMilliFps(d, cfg);
                if (c > max) max = c;
            }
            return max;
        }

        /// <summary>
        /// 解一次分摊：封顶 → 保护带（不够就从最高需求那路往下剥夺）→ 剩余额度上的水位 λ
        /// （在非保护路的**显示上限**集合上解 Σ min(上限, λ) = 剩余额度）。全程整数、无堆分配。
        /// 因为上限恒 ≤ 需求、λ 又只会往下压，各路应得速率之和不超过剩余额度一个二分步长
        /// （步长上界=路数×1 毫帧/秒），不存在"越顶"分支。
        /// </summary>
        private Allocation Compute(int now, int minIntervalMs, DisplayThrottleSettings cfg)
        {
            var a = new Allocation();
            int cap = MilliPerFps * 1000 / minIntervalMs;
            a.CapMilliFps = cap;

            // 1) 保护带上限 pc：求最大的 pc 使保护带总额 ≤ 封顶（P(pc) 随 pc 单调不减）。
            //    剥夺顺序从需求最高的"伪保护路"开始——它每帧都要显示最贵，剥夺它换到的预算最多。
            int maxTier = 0;
            for (int i = 0; i < Slots; i++)
            {
                if (Volatile.Read(ref _tier[i]) == 0) continue;
                int d = DemandMilliFps(i, now);
                if (d > maxTier) maxTier = d;
            }
            int lo = 0, hi = maxTier;
            while (hi > lo)
            {
                int mid = lo + (hi - lo + 1) / 2;
                if (ProtectedSumMilliFps(mid, now) <= cap) lo = mid;
                else hi = mid - 1;
            }
            a.ProtectLimitMilliFps = lo;
            int protectedSum = ProtectedSumMilliFps(lo, now);
            a.RemainingCapMilliFps = cap - protectedSum < 0 ? 0 : cap - protectedSum;
            a.DemotedProtection = lo < maxTier ? 1 : 0;
            int remaining = a.RemainingCapMilliFps;

            // 2) 水位 λ：上限之和本就 ≤ 剩余额度 ⇒ 谁都按自己的上限显示（预算闲置，λ 取最大上限即可，
            //    再多也没人能用——降频是主动的，闲置份额不会把谁抬过自己的上限）；
            //    否则解最小的 x 使 Σ min(上限, x) ≥ 剩余额度，λ=x（真正限速）。
            int maxC = MaxShareCeiling(now, a, cfg);
            if (CeilCappedSum(maxC, a, now, cfg) <= remaining)
            {
                a.ShareMilliFps = maxC;
                a.Pressure = 0;
            }
            else
            {
                int xlo = 0, xhi = maxC;
                while (xhi - xlo > 1)
                {
                    int mid = xlo + ((xhi - xlo) >> 1);
                    if (CeilCappedSum(mid, a, now, cfg) >= remaining) xhi = mid;
                    else xlo = mid;
                }
                a.ShareMilliFps = xhi;
                a.Pressure = 1;
            }
            a.GrantedMilliFps = protectedSum + CeilCappedSum(a.ShareMilliFps, a, now, cfg);
            return a;
        }

        /// <summary>
        /// 本路的应得显示速率（毫帧/秒）。0=此刻无需求（停流或从未到达）。
        /// 保护带=自己的需求（一帧不丢）；其余= min(显示上限, 水位 λ)——上限只由降幅决定，
        /// 与预算松紧无关，故 λ 只会往下压、不会把谁抬过自己的上限（min 的两侧都 ≤ 需求）。
        /// </summary>
        private int RateMilliFps(int slot, Allocation a, int now, DisplayThrottleSettings cfg)
        {
            int d = DemandMilliFps(slot, now);
            if (d <= 0) return 0;
            if (IsProtected(slot, a, now)) return d;
            int c = CeilingMilliFps(d, cfg);
            int r = c < a.ShareMilliFps ? c : a.ShareMilliFps;
            if (r < 1) r = 1;   // 避免除零；1 毫帧/秒=1000 秒一次，实际等同于不显示
            return r;
        }

        /// <summary>
        /// 新分支：登记帧到达并维护保护带迟滞。
        /// 周期估计"上调快、下调慢"：变慢一次跳变超过 1.5 倍直接采信（否则陈旧的高需求会白占封顶）；
        /// 相邻两帧间隔超过普查窗口 ⇒ 断流，周期作废重采样（原实现是"跳过这笔样本"，旧值永久残留）。
        /// </summary>
        private void NoteArrival(int slot, int now, DisplayThrottleSettings cfg)
        {
            int prevArrive = Volatile.Read(ref _lastArriveTick[slot]);
            bool firstEver = Volatile.Read(ref _arrived[slot]) == 0;
            Volatile.Write(ref _lastArriveTick[slot], now);
            Volatile.Write(ref _requestTick[slot], now);
            Volatile.Write(ref _arrived[slot], 1);
            if (firstEver) return;

            int gap = unchecked(now - prevArrive);
            if (gap <= 0) return;
            int p = Volatile.Read(ref _periodMs[slot]);
            if (gap > ActivityWindowMs)
            {
                // 断流：作废估计（下一帧起重新采样）。此刻该路需求为"未知"，不受限速。
                Volatile.Write(ref _periodMs[slot], 0);
                Volatile.Write(ref _tier[slot], 0);
                return;
            }
            if (p <= 0) Volatile.Write(ref _periodMs[slot], gap);
            else if (gap > p + p / 2) Volatile.Write(ref _periodMs[slot], gap);
            else Volatile.Write(ref _periodMs[slot], (p * 3 + gap) / 4);

            // 保护带迟滞：≤阈值进保护带，>阈值×1.25 才出（避免阈值附近抖动导致反复进出、画面时好时坏）
            int period = Volatile.Read(ref _periodMs[slot]);
            if (period <= 0) return;
            int protectMilli = cfg.ProtectHz * MilliPerFps;
            int demand = MilliPerFps * 1000 / period;
            if (Volatile.Read(ref _tier[slot]) != 0)
            {
                if ((long)demand * TierHysteresisDen > (long)protectMilli * TierHysteresisNum)
                    Volatile.Write(ref _tier[slot], 0);
            }
            else if (protectMilli > 0 && demand <= protectMilli)
            {
                Volatile.Write(ref _tier[slot], 1);
            }
        }

        /// <summary>新分支：本路应得间隔（ms）。保护带=自己周期打 9 折；未知需求=不限到低于全局间隔；其余按应得速率换算。</summary>
        private int EntitledMsNew(int slot, int now, int minIntervalMs, DisplayThrottleSettings cfg)
        {
            if (minIntervalMs <= 0) return 0;
            if (slot < 0 || slot >= Slots) return minIntervalMs;
            int d = DemandMilliFps(slot, now);
            if (d == DemandUnknown) return minIntervalMs;   // 尚无周期样本：自由放行
            if (d == 0) return minIntervalMs;               // 停流：给个无意义值即可，它不会来请求
            Allocation a = Compute(now, minIntervalMs, cfg);
            int rate = RateMilliFps(slot, a, now, cfg);
            int interval;
            if (IsProtected(slot, a, now) || rate >= d)
            {
                int p = Volatile.Read(ref _periodMs[slot]);
                interval = p <= 0 ? minIntervalMs : p - p / 10;
            }
            else
            {
                interval = (MilliPerFps * 1000 + rate - 1) / rate;
            }
            if (interval < minIntervalMs) interval = minIntervalMs;
            return interval;
        }

        #endregion

        #region 裁决入口

        /// <summary>
        /// 本路应得的渲染间隔（ms）。minIntervalMs&lt;=0 时返回 0（不限速）。
        /// ProtectHz=0 且 Weight=0 时逐帧等价第54轮实现；否则走保护带 + 权重主动降幅口径。
        /// 也是只读诊断入口（"显示分摊"弹窗用它打印各路应得间隔）。
        /// </summary>
        public int EntitledMs(int slot, int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            if (cfg == null || cfg.IsLegacy) return LegacyEntitledMs(slot, now, minIntervalMs);
            return EntitledMsNew(slot, now, minIntervalMs, cfg);
        }

        /// <summary>
        /// 旧口径份额（贪心路的公共速率，毫帧/秒）——保留给回归基线与诊断用；
        /// 新口径的水位请用 WaterLevelFps(now, minIntervalMs)。
        /// </summary>
        public int ShareMilliFps(int now, int minIntervalMs)
        {
            return LegacyShareMilliFps(now, minIntervalMs);
        }

        /// <summary>
        /// 申请渲染本帧。true=可以渲染（调用方负责后续投递）；false=本帧不显示（**不影响检测与计数**）。
        /// 同一路并发申请时只有一个赢家（CAS 到期表），输者按"未到期"处理，下一帧仍在自己时隙内。
        /// </summary>
        public bool TryClaim(int slot, int now, int minIntervalMs)
        {
            if (slot < 0 || slot >= Slots) return false;   // 槽位非法：不记账、不放行
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            bool legacy = cfg == null || cfg.IsLegacy;
            if (legacy) LegacyNoteArrival(slot, now);
            else NoteArrival(slot, now, cfg);
            if (minIntervalMs <= 0) return true;           // 不限速（交互期间或配置为 0）

            int fair = legacy ? LegacyEntitledMs(slot, now, minIntervalMs)
                              : EntitledMsNew(slot, now, minIntervalMs, cfg);
            bool granted = ClaimDueTick(slot, now, fair);
            if (granted) NoteGrant(slot, now);
            return granted;
        }

        /// <summary>
        /// ★第56轮 P5：停止/暂停态的**末帧一次性旁路**。每个到达显示门的帧都问一次（运行态也要问，
        /// 因为复位额度只在运行帧上做），调用时机：TryClaim 已经判完、结果用 claimRejected 带进来。
        ///
        /// 判据只有一条：本路**已不在运行态**（pathRunning=false）且**这一帧刚被份额裁决挡掉**。
        /// 运行态一律回 false 并顺手复位额度（下一回停止还能再补一次），所以稳态零成本、一帧不多放；
        /// 停止态但这一帧本来就要显示，也不动额度——额度要留给真正被挡掉的那一帧。
        /// 为什么停止态要破例：操作员按停止后画面就定住了，定在哪一帧取决于最后一次的裁决运气——
        /// 被挡掉的那一帧已经跑完检测、像素和记录都还在调用方手上，再不画就永远没机会了（后面没有新帧了）。
        ///
        /// 一次性额度（CAS 消费）是必须的：停止之后仍在出图的路（连续采集、逐路停止的在途帧）
        /// 若每帧都破例，等于把整个限速绕过去，正是本轮 P0/P1 要防的反转。上限＝每路每次停止 1 帧、
        /// 全局最多 12 帧。补到的是"停止后第一个被挡掉的帧"，不是"保证最后一帧"——
        /// 排空期若连续几帧都被挡，只有第一帧能补，其余仍旧是旧画面（一次性额度已用掉）。
        ///
        /// 边界（不许夸大）：这里只能救"已经走到显示裁决这一步"的帧。接收前就被丢掉的帧
        /// （取图失败、队列溢出、停止排空）根本没有像素可画，任何显示层手段都补不回来——
        /// 所以这不是"保证显示最后一帧"，只是"停止时不把最后一帧也挡掉"。
        /// 不占用分摊额度：不动 _dueTick，本帧的显示只记进补显账（TailFlushTotal）与放行分桶。
        /// </summary>
        public bool TryClaimTailFrame(int slot, int now, bool pathRunning, bool claimRejected)
        {
            if (slot < 0 || slot >= Slots) return false;
            if (pathRunning)
            {
                Volatile.Write(ref _tailUsed[slot], 0);   // 运行中：把"本次停止的补显额度"还回来
                return false;
            }
            if (!claimRejected) return false;             // 这帧本来就显示，额度留给真正被挡的那帧
            if (Interlocked.CompareExchange(ref _tailUsed[slot], 1, 0) != 0) return false;  // 本次停止已补过
            Interlocked.Increment(ref _tailTotal[slot]);
            NoteGrant(slot, now);   // 它确实渲染了一帧，显示次数里要看得见
            return true;
        }

        /// <summary>本路累计"停止态补显"次数（只读诊断；与份额放行分开计，用来核对是否超预期）。</summary>
        public int TailFlushTotal(int slot)
        {
            if (slot < 0 || slot >= Slots) return 0;
            return Volatile.Read(ref _tailTotal[slot]);
        }

        /// <summary>
        /// 到期表认领（两代口径共用，逻辑与第54轮一致）：从**应得时刻**顺延而不是从当前时刻起算，
        /// 被挤掉的那一帧不白丢，长期速率才严格等于应得速率（按当前时刻起算会把整段间隔白送对方，
        /// 正是最早那版"先到先得"偏心的根源）。再加 slot 毫秒固定错峰，避免各路到期时刻随时间
        /// 收敛到同一毫秒（同瞬齐发会一次打满 UI 线程）。
        /// </summary>
        private bool ClaimDueTick(int slot, int now, int fair)
        {
            int due = Volatile.Read(ref _dueTick[slot]);
            bool armed = Volatile.Read(ref _granted[slot]) != 0;
            if (armed && unchecked(now - due) < 0) return false;   // 还没轮到自己的时隙

            int next = unchecked((armed ? due : now) + fair + slot);
            if (unchecked(next - now) <= 0) next = unchecked(now + fair + slot);
            if (Interlocked.CompareExchange(ref _dueTick[slot], next, due) != due) return false;
            Volatile.Write(ref _granted[slot], 1);
            return true;
        }

        #endregion

        #region 只读诊断（★第56轮 P0：先量再改，全部不参与裁决）

        /// <summary>本路实测帧周期（ms，0=尚无样本）。与相机 SDK 显示的帧率不一致时先怀疑估计。</summary>
        public int MeasuredPeriodMs(int slot)
        {
            if (slot < 0 || slot >= Slots) return 0;
            return Volatile.Read(ref _periodMs[slot]);
        }

        /// <summary>本路实测帧率（四舍五入到 fps；无样本/停流返回 0）。</summary>
        public int MeasuredFps(int slot, int now)
        {
            if (slot < 0 || slot >= Slots) return 0;
            int d = DemandMilliFps(slot, now);
            if (d <= 0) return 0;
            return (d + MilliPerFps / 2) / MilliPerFps;
        }

        /// <summary>本路是否处于保护带（阈值以下一帧不丢）。</summary>
        public int IsPathProtected(int slot, int now, int minIntervalMs)
        {
            if (slot < 0 || slot >= Slots) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            if (cfg == null || cfg.IsLegacy) return 0;
            Allocation a = Compute(now, minIntervalMs, cfg);
            return IsProtected(slot, a, now) ? 1 : 0;
        }

        /// <summary>本路累计放行次数（进程启动以来，环形 int）。</summary>
        public int GrantTotal(int slot)
        {
            if (slot < 0 || slot >= Slots) return 0;
            return Volatile.Read(ref _grantTotal[slot]);
        }

        /// <summary>
        /// 本路上一个统计秒内的放行次数（≈显示 fps）。按秒分桶、只读诊断，
        /// 并发换桶时的偶发竞争只会让计数差 1，不影响任何裁决。
        /// </summary>
        public int GrantedPerSec(int slot)
        {
            if (slot < 0 || slot >= Slots) return 0;
            return Volatile.Read(ref _grantSecPrev[slot]);
        }

        /// <summary>当前生效水位（fps，四舍五入）。旧口径直接回第54轮份额。</summary>
        public int WaterLevelFps(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            if (cfg == null || cfg.IsLegacy)
            {
                int share = LegacyShareMilliFps(now, minIntervalMs);
                return share <= 0 ? 0 : (share + MilliPerFps / 2) / MilliPerFps;
            }
            Allocation a = Compute(now, minIntervalMs, cfg);
            int w = a.ShareMilliFps;
            return w <= 0 ? 0 : (w + MilliPerFps / 2) / MilliPerFps;
        }

        /// <summary>
        /// 分摊压力档：0=按各自显示上限分发后封顶还有余 1=水位 λ 正在限速（想显的比封顶多）。
        /// 主动降幅口径下没有第三档——上限只减不加，越顶不可能发生。旧口径恒为 1。
        /// </summary>
        public int PressureCode(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            if (cfg == null || cfg.IsLegacy) return 1;
            return Compute(now, minIntervalMs, cfg).Pressure;
        }

        /// <summary>是否有路因封顶不够被剥夺了保护带（1=有，弹窗要写明"保护不彻底"的原因）。</summary>
        public int ProtectionDemoted(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            if (cfg == null || cfg.IsLegacy) return 0;
            return Compute(now, minIntervalMs, cfg).DemotedProtection;
        }

        /// <summary>
        /// 各路**应得速率**之和占全局封顶的百分比（0~100，解析值、非实测）。
        /// 用途：现场把权重拉高之后，这一项应当明显低于 100——那就是"用显示换来的 CPU 余量"；
        /// 若仍接近 100，说明限速来自水位（预算真不够），再加权重只会继续压快路。
        /// 旧口径按第54轮份额同样口径折算，便于两代数字直接对比。
        /// </summary>
        public int CapUsedPercent(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            int cap = MilliPerFps * 1000 / minIntervalMs;
            if (cap <= 0) return 0;
            DisplayThrottleSettings cfg = Volatile.Read(ref _settings);
            int granted;
            if (cfg == null || cfg.IsLegacy)
            {
                granted = LegacyCappedDemandMilliFps(LegacyShareMilliFps(now, minIntervalMs), now);
            }
            else
            {
                granted = Compute(now, minIntervalMs, cfg).GrantedMilliFps;
            }
            if (granted < 0) granted = 0;
            int percent = granted * 100 / cap;
            return percent > 100 ? 100 : percent;
        }

        /// <summary>
        /// 放行计数分桶（诊断专用；竞争只会让某一桶差 1 次，绝不参与裁决）。
        /// _grantSecTick 以 0 作"未起始"哨兵只影响诊断——TickCount 回绕后偶发晚一秒起桶，无功能后果。
        /// </summary>
        private void NoteGrant(int slot, int now)
        {
            Interlocked.Increment(ref _grantTotal[slot]);
            int start = Volatile.Read(ref _grantSecTick[slot]);
            if (start == 0) Volatile.Write(ref _grantSecTick[slot], now);
            else if (unchecked(now - start) >= 1000)
            {
                Volatile.Write(ref _grantSecPrev[slot], Volatile.Read(ref _grantSec[slot]));
                Volatile.Write(ref _grantSec[slot], 0);
                Volatile.Write(ref _grantSecTick[slot], now);
            }
            Interlocked.Increment(ref _grantSec[slot]);
        }

        #endregion
    }
}
