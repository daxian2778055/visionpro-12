# visionpro-12 代码审查报告（复审）

## 复审说明

本版按所有者反馈改过分级。初审把两件**产品选择**写成了必须马上修的严重/高危安全缺陷，复审撤销该定性。应用源码未改。审查仍是静态阅读，未在本环境编译（主工程依赖本机 Cognex VisionPro 与海康 SDK）。行号以当前分支工作树为准，后续改动后用方法名复核。

**所有者决定（本报告视为权威）：**

1. `Form5.cs` 里厂家/操作工的字面量口令，以及单字符万能口令，是**有意保留**的。不列入严重或高。放到文末「已知产品选择」：当前可接受；仓库公开，且这组口令打开 MES/MySQL 菜单，所以残留风险要写明。哈希存储、去掉万能口令，只在对客户正式发版前做。
2. `FormMES`、`FormMySQL` 目前是**测试/调试工具**，不是生产功能。任意 HTTP、任意 SQL 不再升为高危缺陷。若它们随检测主程序打进同一个安装包，残留风险仍在；建议正式包用 `#if DEBUG`、独立工具工程，或不收入发布包。`LocalMachine` DPAPI、MySQL 默认不走 TLS，留到真正写入凭据和主机时再收紧，不是当前优先缺陷。

**复审后的级别：没有「严重」项。**

| 级别 | 内容 |
| --- | --- |
| 高 | 权限工具给 Everyone 完全控制；`Form1` 与相机分部类的复制和空 `catch` |
| 中 | 授权 fail-open 的界面可见性、NG 图 `Process.Start`+`Kill`、存图/统计绑死 `E:\`、相机关闭靠固定 Sleep、.NET 4.6 与 `jitDebugging`、安装包混用 Debug ini、CI 盖不住主程序 |
| 低 | 仓库卫生、根目录游离 `Form6.cs`、日志脱敏、服务容器工厂并发 |
| 已知产品选择 | 字面量登录口令；MES/MySQL 调试窗及其 DPAPI/TLS |

复审又对过一遍入口、授权门控、看图、盘符、框架、CI 和仓库根目录。没有新的严重缺陷。初审漏记一条发布风险：`Setup1`/`Setup2` 的 vdproj 指向 `bin\Debug` 下的 `test.ini`/`code.ini`（这些文件不在 git 里），主程序输出却指向 `obj\Release` 的 exe。见 M6。

仓库内未发现已提交的 API Key、数据库口令明文或私钥文件。登录口令的具体取值不写入本报告。

---

## 1. 项目是什么

这是一套跑在 Windows 工控机上的 **12 路 CCD 视觉检测程序**（WinForms，目标框架 .NET Framework 4.6，x64）。现场形态是：海康工业相机取流，Cognex VisionPro `CogToolBlock`（`.vpp`）做检测，再把 OK/NG 写回 PLC，并把良率与 NG 图落到本地磁盘。

| 部分 | 位置 | 职责 |
| --- | --- | --- |
| 主程序 | `WindowsFormsApplication1/` | 检测、界面、相机、方案、统计 |
| 入口 | `Program.cs` | 单实例互斥、全局异常、崩溃转储、`AppHost.Build()` 后 `Application.Run(new Form1())` |
| 组合根 | `Core/Infrastructure/AppHost.cs` | 配置、日志、相机、统计、通讯、作业服务的单例注册 |
| 相机 | `Core/Camera/CameraController.cs`、`MVCamera.cs` | 12 路 `MyCamera` 的持有与停流/关设备/销毁 |
| 检测与界面 | `Form1.cs` 及 `Form1.*.cs` 分部类 | 运行时主逻辑；设计器约 1.5 万行 |
| PLC | `Services/` 下 Modbus TCP/RTU、FINS；`FormModbus*.cs`、`FormOmron.cs`、`FormMelsecSerial.cs`、`Form3.cs` | 多协议连接、触发与回写 |
| 调试窗 | `FormMES.cs`、`FormMySQL.cs` | 菜单项 `mesToolStripMenuItem`、`mysqlToolStripMenuItem`，编进主 exe。当前定位是测试工具，见「已知产品选择」 |
| 授权与登录 | `Form1_Load` 一带、`Form5.cs` | `test.ini` / `code.ini` 到期判断；厂家/操作工登录 |
| 权限开通工具 | `GrantIniAccess/` | 给 ini/目录改 ACL，供现场写配置 |
| 安装包工程 | `Setup1/`、`Setup2/` | 部署；`Setup1` 内还提交了 `Newtonsoft.Json.dll` |
| 本机验收 | `verify.ps1` | 有 VisionPro 的机器上 MSBuild Rebuild + `dotnet test` |
| 单元测试 | `WindowsFormsApplication1.Tests/` | 源码链接、不引用主程序集，CI 可跑 |
| 回归脚本 | `tests/` | 依赖已编译的 Debug x64 exe，不在 CI 里 |

数据面（硬编码，未做成可配置根目录）：

- 存图：`Form1.cs` 启动时把 12 路 OK/NG 根目录设为 `E:\fu1ok\` … `E:\fu12ng\`（约 605 行起）。全库只有这一处给 `pathhead_ok` 赋值。
- 统计：`AppHost` 注释写明 `StatisticsService` 委托 `RunLog` 写 `E:\生产统计` / `E:\每日统计`。
- 配置：程序目录 `code.ini`、`test.ini`。MES/MySQL 的配置文件只在调试窗使用。

运行模型：UI 线程 + 每路检测/取流回调 + PLC 轮询线程。关闭主窗体走 `Form1_FormClosed` → `Environment.Exit(0)`。`Main` 的 `finally` 因此不会跑，收尾改挂 `AppDomain.ProcessExit`（`Program.OnProcessExit`：清崩溃标记、`AppHost.Shutdown()`、日志限时刷盘）。这是有意设计，注释写清楚了。

---

## 2. 已经做得好的地方

近期提交（约第 43–59 轮）是在同一套现场程序上连续补洞，而不是推倒重写。下面这些模式值得保留。

**崩溃与退出可观测。** `Program.cs` 同时挂了 UI 线程异常、`AppDomain` 未处理异常和 `UnobservedTaskException`。进程终止路径用同步 `WriteLogSync`，避免异步队列没刷盘。`CrashMonitor` 做 minidump、上次异常退出提醒，并区分「真崩溃」和「断电/强杀残留标记」，避免每次开机误报。`SessionEnding` 时提前标正常退出。

**日志不再堵检测线程。** `Core/Logging/AsyncFileLogger.cs` 用有界队列，满了丢弃并计数，而不是让 12 路相机互相等磁盘。按天分文件、按大小滚动、保留约 30 天。崩溃路径另有同步直写。

**相机句柄有明确主人。** `CameraController.ReleaseAllCameras` 按「全体停流 → 一次等待 → 全体关设备 → 一次等待 → 销毁句柄」批量释放，避免 12 路各自 `Sleep` 把关窗拖到近 2 秒。`Dispose` 用 0 等待做兜底。`Form1` 关闭路径先优雅释放，再在 `Dispose` 里兜底，并针对 VisionPro RCW 的 `RaceOnRCWCleanup` 做了停止后台活动的清理。

**配置与存图有防呆。** `AtomicFileSave` 先写临时文件再替换，并滚动 `.bak`，避免保存 `.vpp` 写到一半损坏。`SafeNamePart`（`Form1.DisplaySettings.cs`）去掉非法文件名字符、尾部点/空格，并截断长度。存图失败有累计计数和 30 秒限流提示，不再只写一条容易被淹没的日志。

**外部程序启动有白名单。** `Form1.Camera58Events.cs` 的 `button32_Click` 只启动磁盘上真实存在的 `.exe` / `.application`。

**可测的纯逻辑已经抽出来。** 帧账本、显示预算、渲染耗时、ini 读写、触发队列、Modbus TCP 回环都有 MSTest。测试工程用 `Compile Include` 链接主工程源文件，不依赖 Cognex，所以 GitHub `windows-latest` 能编能跑。CI 注释与 `[TestMethod]` 数量一致，当前为 **169**。

**线程风险有书面台账。** `docs/线程审计-第43轮.md` 把「锁内同步 `Invoke` / `Sleep`」从猜测收成证据和处置状态，并规定以后用方法名定位、不要把会漂移的行号当合同。`ServiceProvider` 把工厂调用放到锁外，避免嵌套 `Resolve` 死锁。

**单实例。** `Program.Main` 用程序集全名做 `Mutex`，重复启动直接退出。

**调试窗已经做了一部分误操作防护**（不改变「它们是测试工具」的定位）：MySQL 口令写入用 DPAPI `CurrentUser`；写语句有二次确认，并拦截多语句、`INTO OUTFILE` / `DUMPFILE`、`LOAD_FILE`；查询行数上限 5000。MES 保存配置时对疑似凭据头做了加密。这些在正式当生产接口之前够用，见「已知产品选择」。

---

## 3. 问题（按严重程度）

复审后不再设「严重」。

### 高

#### H1. 权限工具把 Everyone 完全控制授给目标文件，目录模式还会递归

**位置：** `GrantIniAccess/PermissionHelper.cs`

- `ApplyFullAccessToFile` 在清掉 Deny ACE 之后，给当前用户、Users、Authenticated Users、**World（Everyone）**、SYSTEM、Administrators 加 `FullControl`（约 779–795 行，`WorldSid` 在约 793 行）。
- 目录自身同样把 `WorldSid` 放进可继承 ACE（约 847–852 行）。`GrantDirectoryAccess` 再对子树调用这套授权。
- 单文件模式对**父目录**收窄成「当前用户 / Administrators / SYSTEM、不可继承」（约 252–259 行）。这一步是对的。**文件本身**仍是 Everyone 完全控制。

`code.ini`、`test.ini` 若被这个工具处理过，本机任意账户都能读改授权数据。工具还会 `runas` 提权（`RestartAsAdministrator`）。它是现场排障用的，默认策略仍然过宽，和登录口令是否写死无关。这是当前最高优先级的代码问题。

#### H2. 主逻辑集中在一个超大 Form 里，12 路复制是主要缺陷来源

**规模（行数，含设计器）：**

| 文件 | 行数 |
| --- | --- |
| `Form1.cs` | 14470 |
| `Form1.Designer.cs` | 14994 |
| `Form1.Camera58Events.cs` | 6199 |
| `MVCamera.cs` | 4865 |
| `FormModbus.cs` / `FormModbusRtu.cs` / `FormOmron.cs` | 4343 / 4044 / 3731 |
| `Form3.cs` | 2449 |
| `Form1` 其余分部（显示、主题、错误、相机 1–4 与 9–12） | 约 5400 |

空 `catch { }` 很密：仅 `Form1.cs` 约 165 处，`Form3.cs` 约 48 处，三个 PLC 大窗合计约 70 处。很多是 UI 跨线程或 SDK 调用的「不要崩」，副作用是相机打开失败、写 PLC 失败、存图失败可以没有日志。

相机 1–12 的按钮、IO、触发仍是复制粘贴（`Form1.cs` 里成排的 `bnStopGrabN`、`IO输出N`）。`Form1.Camera18Events.cs` 与 `Form1.DisplaySettings.cs` 各有一份「启动关联程序再立刻 `Kill`」的看图代码。项目规则（`.codebuddy/rules/refactor-principle.md`）已要求按服务抽、禁止一次性大改；目前 `AppHost` 收口了所有权，热路径仍在 Form 里。

这是可维护性风险，不是风格意见：下一次只改了其中一路，另外 11 路会静默保持旧行为。第 55 轮「连续运行不计件被顺手删掉」就是这类回归。

### 中

#### M1. 授权数据读不到时按设计放行，但操作员主要只能在日志里看到

**位置：** `Form1.cs` 中 `_authExpired` 的计算（`Form1_Load` 后段，约 3540–3549 行）以及自动开相机（约 2354–2364 行）。跨日重判在 `Form1.DisplaySettings.cs` 约 1636 行，口径一致。

注释写明这是**有意 fail-open**：`test.ini` 缺失、读失败或系统时间不可信时，`_authExpired` 为 false，自动开相机和随后的 `button1_Click` 照常走。目的是避免误锁已付费客户。真正到期且读到有效日期时，会跳过自动开相机。日志有「按 fail-open 设计仍自动开相机」。

复审不要求改判定式。残留缺口是：界面「加密中」只挡手动按钮，挡不住这条自动路径，主画面没有持续的「授权未确认」提示。正式发版前应让操作员不用翻 Log 就能看见当前是放行状态。本报告不描述如何构造加密码。

机器指纹 `GetCPUSerialnumber`（同文件约 3736–3760 行）使用 MD5，并混入固定字符串。不能当作防克隆手段。

#### M2. 用「打开图片再杀掉进程」来显示 NG 图

**位置：**

- `Form1.DisplaySettings.cs` 的 `chatu_fangfa`（约 1425–1445 行）
- `Form1.Camera18Events.cs`（约 639 行）

`Process.Start(路径.bmp)` 走 Shell 关联。返回的可能是看图程序，也可能是已存在的宿主进程；紧接着 `Kill()`。结果要么图一闪而过，要么误伤用户正在使用的看图程序。`myProc` 为空时 `Kill` 会抛，被外层 catch 吃掉，只记「图片显示」。文件名经过 `SafeNamePart`，路径前缀仍是 `E:\fuNng\` 加日期。

#### M3. 统计和存图绑死 E: 盘

**位置：** `Form1.cs` 约 605–628 行的 `pathhead_ok` / `pathhead_ng`；`AppHost` 对统计目录的注释。全库没有第二处覆盖这些存图根目录。

工控机没有 E:、E: 满、或权限被 H1 改乱时，良率 CSV 和 NG 图会失败。存图失败已有限流告警。CI 注释写明含 `E:\` 的用例在无该盘符的 runner 上 Inconclusive，托管构建不能代替现场盘符验收。

#### M4. 相机关闭靠固定 Sleep 等回调退出

**位置：** `CameraController.ReleaseAllCameras`；`Form1.ReleaseAllCameras` 传入 100ms / 50ms。

注释说明瓶颈是等 SDK 回调线程退出。固定睡眠在回调很快时白等，在回调被检测或存图拖住时不够。`Dispose` 路径把两次等待都设为 0，只保证句柄最终被销毁。连续运行停帧有看门狗（`RecoverStaleTriggers`），这是补救，不是关闭协议。

`_locker_open` 内仍有大量同步 `Invoke` 和 `Thread.Sleep`（线程审计第 43 轮标为中，归台架）。死锁环当时静态排除了，UI 卡顿和持锁窗口还在。审计文档自己声明行号已作废，不要按那份文档的行号改代码。

#### M5. 目标框架 4.6 已无安全更新；发行配置打开了 JIT 调试

**位置：** `WindowsFormsApplication1.csproj` 与 `GrantIniAccess.csproj` 的 `TargetFrameworkVersion` 均为 `v4.6`；`App.config` 第 9 行 `jitDebugging="true"`。

4.6 不再接收安全更新。VisionPro 的引用是绝对路径 `C:\Program Files\Cognex\VisionPro\ReferencedAssemblies\`，升级框架要先确认 Cognex、海康和 IO 卡（`IOC0640.dll`）的支持矩阵，不能单独改 csproj。

`jitDebugging="true"` 让未处理异常更容易挂调试器。现场机器上这会暴露调用栈，也让「崩溃提示 + 写报告」这条路径变得不确定。发布配置里应关掉。

#### M6. 安装项目把本机 Debug 配置打进包（初审漏记）

**位置：**

- `Setup1/Setup1.vdproj`：`SourcePath` 指向 `WindowsFormsApplication1\bin\Debug\test.ini` 与 `bin\Debug\code.ini`；主程序输出指向 `WindowsFormsApplication1\obj\Release\WindowsFormsApplication1.exe`。
- `Setup2/Setup2.vdproj`：同样引用 `bin\Debug\code.ini`。

这两个 ini **没有**提交进 git（`bin/` 被忽略），所以不是仓库泄密。风险在打包机：谁最后在 Debug 目录留下授权文件，谁的 `test.ini`/`code.ini` 就会进 MSI，并且和 Release 的 exe 混在一次安装里。正式出包前应改为明确的发布目录，不要从 `bin\Debug` 取配置。

主 exe 里编进了 MES/MySQL 菜单（`Form1.Camera58Events.cs` 的 `mesToolStripMenuItem_Click`、`mysqlToolStripMenuItem_Click`）。因此当前安装包会带上这两个调试窗。处理方式见「已知产品选择」，不单列成安全高危。

#### M7. CI 只守住 169 个纯逻辑用例

详见第 5 节。主工程、`GrantIniAccess`、`tests/InspectionRegressionTests.cs` 都不在 `.github/workflows/ci.yml` 里。绿灯不代表检测、相机或安装包可用。这是流程缺口，不是现场缺陷。

### 低

#### L1. 公开仓库里的现场痕迹

- 根目录有微信/企业微信截图和若干小 PNG（`企业微信截图_*.png`、`微信图片_*.jpg`、`tu.png`、登录/保存图标）。
- `Menu.ini` 是一条客户现场绝对路径（含方案 `.vpp`）。
- `新建文本文档.txt` 含相机型号与序列号一类设备标识。
- `reasonix.toml` 含开发机绝对路径，以及读取本机 `AppData` 会话归档的命令。
- `Setup1/Newtonsoft.Json.dll`、`WindowsFormsApplication1/IOC0640.dll` 以二进制入库，没有版本说明。

这些不是口令。仓库公开时，客户路径和截图会随 clone 扩散。清理成本低，建议顺手做，不挡功能开发。

#### L2. 仓库根上有一份不会被编译的 `Form6.cs`

`WindowsFormsApplication1/Form6.cs`（69 行）才在 csproj 里。根目录 `Form6.cs`（78 行）内容不同。`.gitignore` 只忽略了根目录 `Form1.cs`。对照根目录文件改检测窗不会进安装包。

#### L3. 日志脱敏不必在调试窗上升级

授权日志已改成只写到期日和剩余天数（`Form1` 中 `AuthDescribe` 一带）。检测热路径的 `_logger.WriteLog(ex.Message + ...)` 可能带路径和 PLC 地址，一般可接受。不要再把连接串或报文正文拼进检测日志。调试窗的响应正文留在界面上，与「测试工具」的用途一致。

#### L4. `ServiceProvider` 工厂路径的并发

`TryResolveInternal` 在锁外执行工厂，并发首次解析可能创建两个实例再覆盖字典。当前 `AppHost.Build` 注册的是已构造好的实例，这条路径基本没走到。以后若改成工厂注册，需要只保留一个实例并释放另一个。

---

## 4. 已知产品选择 / 发版前再收

本节不是当前必须修改的缺陷。正式对客户发版前再收。

### P1. 登录口令保持源码字面量

**位置：** `WindowsFormsApplication1/Form5.cs` 的 `button1_Click` 与 `Authorize`。`Form5_Load` 读取 `userCode/code1`、`userCode/code2`，点击登录时没有使用这两个值，现场改 ini 不能改口令。`timer1_Tick` 里操作工约 30 秒掉线（`mark1 == 0`），厂家登录把 `mark1` 置 1，不按这个超时掉线。

所有者确认这是当前阶段的产品选择。残留风险：

- GitHub 仓库是公开的，字面量会随历史提交和发行程序集分发。反编译也能看到。
- 同一组口令打开主界面上的 MES、MySQL 菜单（`Form1.Camera58Events.cs` 约 1802–1836 行）。调试窗还在主程序里时，知道口令的人可以在现场机器上打开它们。

**正式发版前再做：** 口令改为带盐哈希；去掉万能口令；要么真正使用 ini 中的 `userCode`，要么删掉这两个读取，避免误以为改配置就能改口令。新口令不要写回 git。在那之前不必为了审查意见去改 `Form5.cs`。

### P2. MES / MySQL 是测试工具，随主程序一起发行

**位置：** `FormMES.cs`、`FormMySQL.cs`；菜单在 `Form1.Camera58Events.cs`。`Setup1` 打包的是这一份主 exe，所以调试窗会进安装包。

当前能力（供发版时决定收不收，不作为本轮缺陷）：

- MES：`btnSend_Click` 对文本框 URL 发 GET/POST/PUT/DELETE，不限制主机或协议。保存凭据用 `DataProtectionScope.LocalMachine`（`Protect`，约 300–317 行），本机其他 Windows 用户可以解开 `MESConfig.txt`。请求体按注释明文落盘。
- MySQL：`btnExec_Click` 执行文本框 SQL。未勾选 SSL 时 `SslMode = None`。仍读取旧键 `pwd=`。`NeedsWriteConfirm` 是确认框，点「是」后写语句会执行。注释里「仅允许可信 SQL」只对应 `AllowUserVariables = false`，说重了。

**现在：** 当作内部调试入口可以留着。LocalMachine DPAPI 和默认不加密，等真正拿它们连客户主机、写入真实口令时再收紧，不排进当前优先队列。

**正式发版前再做（三选一即可）：** `#if DEBUG` 编译掉菜单；拆成单独的工具工程；安装项目排除这两个入口。若调试窗继续进客户包，再考虑只读数据库账号、默认 TLS，以及把 MES 保护范围改回当前用户。

---

## 5. 测试覆盖缺口

### 5.1 `WindowsFormsApplication1.Tests`（CI 会跑）

工程说明见 csproj 顶部注释：net48、MSTest、源码链接、HslCommunication 7.0.1 仅用于 Modbus 回环。链接进去的生产代码主要是：

`ClassIni`、`RunLog`、日志子系统、`CommGridHelper`、`FinsIniStore`、`PendingCameraTriggers`、`AtomicFileSave`、`DeadImageScan`、`FrameAccounting`、`RenderBudget`、`RenderCostMeter`、Modbus TCP Runtime/Link。

`[TestMethod]` 合计 169，与 `ci.yml` 注释一致。这些测试把显示降频、帧账本、ini 脏键、原子写、死图扫描钉得比较紧，适合作为回归网。

**没有链接、因此 CI 完全看不到的部分：**

| 缺口 | 为什么重要 |
| --- | --- |
| `Form1` 全部检测/IO/方案切换 | 产量与安全输出的实际位置（H2） |
| `CameraController` 释放顺序 | 依赖海康 SDK；至少可以把「空槽跳过、重复释放、失败一路不影响其他路」做成不调用 SDK 的测试替身（M4） |
| `SafeNamePart` | 在 `Form1` 分部类上，存图文件名规则无单测 |
| `GrantIniAccess` | ACL 策略无测试（H1） |
| `CrashMonitor`、`Program` 退出顺序 | 崩溃提醒的误报曾经是现场问题 |
| `Form3` TCP、串口三菱、FINS 运行时（ini 段测试除外） | 触发丢了会直接导致漏检 |

Modbus 回环只有 **1** 个 `[TestMethod]`（`ModbusTcpLoopbackTests.cs`）。调试窗和 `Form5` 不要求在本轮补安全测试；发版前若保留它们，再给 SQL 分类和凭据头判断补测试。

### 5.2 `tests/`

`tests/InspectionRegressionTests.cs` 用手写断言覆盖触发队列的早期回调、失败回滚、容量、并发顺序、检测生命周期和 PLC 故障码格式。`tests/Run-InspectionRegression.ps1` 用 MSBuild 旁的 `csc.exe` 引用 `WindowsFormsApplication1\bin\x64\Debug\WindowsFormsApplication1.exe`。

它不在解决方案的测试工程里，`ci.yml` 和 `verify.ps1` 都不会跑它。队列行为与 `PendingCameraTriggersTests` 有重叠；生命周期和 PLC 故障码格式是独有的，目前没有自动化门禁。脚本假设 `MSBuild.exe` 在 PATH 上，和 `verify.ps1` 用 vswhere 查找的方式不一致。

---

## 6. CI / `.github/workflows`

只有 `.github/workflows/ci.yml`：`push` 与 `pull_request` 都触发，`windows-latest`，安装 .NET SDK 9，然后

`dotnet test WindowsFormsApplication1.Tests\WindowsFormsApplication1.Tests.csproj -c Release`。

合理的部分：

- 明确不在托管 runner 上编主工程（Cognex HintPath 不存在），把主工程验收留给 `verify.ps1`。
- `actions/checkout@v4`、`actions/setup-dotnet@v4`。
- 测试与生产源文件是同一份。

缺口（对应 M7）：

1. 同一 PR 会跑两次，没有 `concurrency` 取消过期运行。
2. 没有 `permissions: contents: read`。这个工作流只需要读代码。
3. 没有超时，没有上传 trx。
4. 没有密钥扫描或二进制清单。公开仓库加上提交的 DLL，这一项是空的。它不替代 P1：口令是所有者决定保留的，扫描应避免把本报告误报成新事故。
5. 主工程、`GrantIniAccess`、`tests/` 都不在 CI。
6. 工作流前 30 行是版本注释。用例数应以测试工程为准。
7. `verify.ps1` 的 MSBuild 兜底路径写成 `D:\Program Files\Microsoft Visual Studio\2022\Professional\...`。vswhere 失败时换机器会直接抛。兜底路径应删掉或改成参数。

---

## 7. 建议的下一步（按复审后的优先级）

1. **收紧 `GrantIniAccess`（H1）。** 默认只给当前用户与 Administrators 修改权限；不要给 World/Everyone；不要递归授权，除非操作员再次确认。单文件成功后能看出最终授权对象。这是现在最值得改的一处代码。

2. **按路数参数化，而不是再复制一份相机事件（H2）。** 下一刀只做「IO 输出 1–12」或「停止采集按钮」中的一组，用现有 `CameraController` 下标，行为对照后再做下一组。不要在同一轮改检测时序。空 `catch` 至少改成限流日志。

3. **NG 图改在进程内显示，存图根目录做成配置（M2、M3）。** 删掉 `Process.Start` 加 `Kill`。`E:\` 缺失时启动就提示。

4. **fail-open 保持判定，补一条界面提示（M1）。** 不改「读不到授权就放行生产」。主界面在 `_authNotVerifiedFailOpen` 为真时持续显示「授权未确认，已按设计继续运行」。

5. **正式发版清单（P1、P2、M5、M6），现在不必改。** 口令改哈希并去掉万能口令；MES/MySQL 不进客户包（`#if DEBUG`、独立工程或安装项目排除）；`jitDebugging` 在发布配置关掉；vdproj 不要再取 `bin\Debug` 的 ini，也不要 Debug 配置配 Release exe。框架升级等 Cognex/海康矩阵确认后再动。

6. **相机关闭等回调结束，而不是固定 Sleep（M4）。** 用在途回调计数或 SDK 提供的停流完成信号。台架上对照关窗是否还丢句柄。`_locker_open` 里的 `Sleep` 仍按线程审计归台架，不要和界面改动捆在一轮。

7. **把 `tests/` 里独有的断言并进 MSTest，或让 `verify.ps1` 在主工程编译成功后调用它。** 优先是 PLC 故障码格式和检测生命周期。`SafeNamePart` 可以顺手加测。

8. **收紧 CI（M7）。** `permissions: contents: read`；`concurrency`；`timeout-minutes`；失败时上传 trx。缩短 `ci.yml` 顶部变更日志。有自托管且装了 VisionPro 的机器时，再把 `verify.ps1` 接上。

9. **清理仓库根目录（L1、L2）。** 截图、`Menu.ini`、`新建文本文档.txt`、根目录 `Form6.cs`、`reasonix.toml` 移出 git 或加入 `.gitignore`。先确认安装工程没有引用它们。

---

## 8. 本次复审没有做的事

- 没有改业务代码，没有改口令、ACL 或调试窗。
- 没有运行 `verify.ps1` 或 `dotnet test`。169 来自 `[TestMethod]` 计数，与 `ci.yml` 注释一致。
- 没有动态验证：相机回调是否在 50ms 内退出、E: 盘缺失时界面是否提示、fail-open 时主画面是否仍显示「加密中」。这些需要现场台架。
