# visionpro-12 代码审查报告

审查方式：静态阅读入口、组合根、相机/通讯/授权/MES/数据库路径、测试工程与 CI。未改应用源码，未在本环境编译（主工程依赖本机 Cognex VisionPro 与海康 SDK）。行号以审查时 `master` 工作树为准，后续改动后请用方法名复核。

仓库内未发现已提交的 API Key、数据库口令明文或私钥文件。登录口令以源码字面量形式存在，见下文「严重」项；本报告不抄录这些口令的具体取值。

---

## 1. 项目是什么

这是一套跑在 Windows 工控机上的 **12 路 CCD 视觉检测程序**（WinForms，目标框架 .NET Framework 4.6，x64）。现场形态是：海康工业相机取流，Cognex VisionPro `CogToolBlock`（`.vpp`）做检测，再把 OK/NG 写回 PLC，并把良率与 NG 图落到本地磁盘。

主要组成：

| 部分 | 位置 | 职责 |
| --- | --- | --- |
| 主程序 | `WindowsFormsApplication1/` | 检测、界面、相机、方案、统计 |
| 入口 | `Program.cs` | 单实例互斥、全局异常、崩溃转储、`AppHost.Build()` 后 `Application.Run(new Form1())` |
| 组合根 | `Core/Infrastructure/AppHost.cs` | 配置、日志、相机、统计、通讯、作业服务的单例注册 |
| 相机 | `Core/Camera/CameraController.cs`、`MVCamera.cs` | 12 路 `MyCamera` 的持有与停流/关设备/销毁 |
| 检测与界面 | `Form1.cs` 及 `Form1.*.cs` 分部类 | 运行时主逻辑；设计器约 1.5 万行 |
| PLC | `Services/` 下 Modbus TCP/RTU、FINS；`FormModbus*.cs`、`FormOmron.cs`、`FormMelsecSerial.cs`、`Form3.cs` | 多协议连接、触发与回写 |
| MES / 数据库工具窗 | `FormMES.cs`、`FormMySQL.cs` | HTTP 调试、任意 SQL 控制台 |
| 授权与登录 | `Form1_Load` 一带、`Form5.cs` | `test.ini` / `code.ini` 到期判断；厂家/操作工登录 |
| 权限开通工具 | `GrantIniAccess/` | 给 ini/目录改 ACL，供现场写配置 |
| 安装包工程 | `Setup1/`、`Setup2/` | 部署；`Setup1` 内还提交了 `Newtonsoft.Json.dll` |
| 本机验收 | `verify.ps1` | 有 VisionPro 的机器上 MSBuild Rebuild + `dotnet test` |
| 单元测试 | `WindowsFormsApplication1.Tests/` | 源码链接、不引用主程序集，CI 可跑 |
| 回归脚本 | `tests/` | 依赖已编译的 Debug x64 exe，不在 CI 里 |

数据面（硬编码，未做成可配置根目录）：

- 存图：`Form1.cs` 启动时把 12 路 OK/NG 根目录设为 `E:\fu1ok\` … `E:\fu12ng\`。
- 统计：`AppHost` 注释写明 `StatisticsService` 委托 `RunLog` 写 `E:\生产统计` / `E:\每日统计`。
- 配置：程序目录 `code.ini`、`test.ini`；MES 为 `MESConfig.txt`；MySQL 为 `MySQLConfig.ini`。

运行模型：UI 线程 + 每路检测/取流回调 + PLC 轮询线程。关闭主窗体走 `Form1_FormClosed` → `Environment.Exit(0)`。`Main` 的 `finally` 因此不会跑，收尾改挂 `AppDomain.ProcessExit`（`Program.OnProcessExit`：清崩溃标记、`AppHost.Shutdown()`、日志限时刷盘）。这是有意设计，注释写清楚了。

---

## 2. 已经做得好的地方

近期提交（约第 43–59 轮）是在同一套现场程序上连续补洞，而不是推倒重写。下面这些模式值得保留。

**崩溃与退出可观测。** `Program.cs` 同时挂了 UI 线程异常、`AppDomain` 未处理异常和 `UnobservedTaskException`。进程终止路径用同步 `WriteLogSync`，避免异步队列没刷盘。`CrashMonitor` 做 minidump、上次异常退出提醒，并区分「真崩溃」和「断电/强杀残留标记」，避免每次开机误报。`SessionEnding` 时提前标正常退出。

**日志不再堵检测线程。** `Core/Logging/AsyncFileLogger.cs` 用有界队列，满了丢弃并计数，而不是让 12 路相机互相等磁盘。按天分文件、按大小滚动、保留约 30 天。崩溃路径另有同步直写。

**相机句柄有明确主人。** `CameraController.ReleaseAllCameras` 按「全体停流 → 一次等待 → 全体关设备 → 一次等待 → 销毁句柄」批量释放，避免 12 路各自 `Sleep` 把关窗拖到近 2 秒。`Dispose` 用 0 等待做兜底。`Form1` 关闭路径先优雅释放，再在 `Dispose` 里兜底，并针对 VisionPro RCW 的 `RaceOnRCWCleanup` 做了停止后台活动的清理。

**配置与存图有防呆。** `AtomicFileSave` 先写临时文件再替换，并滚动 `.bak`，避免保存 `.vpp` 写到一半损坏。`SafeNamePart`（`Form1.DisplaySettings.cs`）去掉非法文件名字符、尾部点/空格，并截断长度。存图失败有累计计数和 30 秒限流提示，不再只写一条容易被淹没的日志。

**凭据落盘做过一轮加固（还不完整，见问题列表）。** MySQL 口令用 DPAPI `CurrentUser`（`FormMySQL.Protect`）。MES 保存时对 URL、查询串和疑似凭据头做脱敏/加密（`FormMES` 中 `IsSecretHeader`、`enc:` 前缀）。启动外部程序限制为已存在的 `.exe` / `.application`（`Form1.Camera58Events.cs` 的 `button32_Click`）。

**SQL 窗有一层误操作确认。** `NeedsWriteConfirm` 会拦多语句、`WITH`、`INTO OUTFILE` / `DUMPFILE`、`LOAD_FILE`，写操作要二次确认，查询行数上限 5000。这只是防误触，不是权限边界。

**可测的纯逻辑已经抽出来。** 帧账本、显示预算、渲染耗时、ini 读写、触发队列、Modbus TCP 回环都有 MSTest。测试工程用 `Compile Include` 链接主工程源文件，不依赖 Cognex，所以 GitHub `windows-latest` 能编能跑。CI 注释与 `[TestMethod]` 数量一致，当前为 **169**。

**线程风险有书面台账。** `docs/线程审计-第43轮.md` 把「锁内同步 `Invoke` / `Sleep`」从猜测收成证据和处置状态，并规定以后用方法名定位、不要把会漂移的行号当合同。`ServiceProvider` 把工厂调用放到锁外，避免嵌套 `Resolve` 死锁。

**单实例。** `Program.Main` 用程序集全名做 `Mutex`，重复启动直接退出。

---

## 3. 问题（按严重程度）

### 严重

#### S1. 厂家/操作工口令写死在源码里，并保留万能口令

**位置：** `WindowsFormsApplication1/Form5.cs`，`button1_Click`；同文件 `Authorize`。

登录成功与否由源码中的字符串相等判断，不是 `test.ini` 里读出的 `userCode`。`Form5_Load` 确实读了 `userCode/code1` 与 `userCode/code2`，但点击登录时没有使用这两个值。现场改 ini 不能改口令。

`Authorize` 的注释写明 MES/MySQL 仍保留一个单字符万能口令。厂家会话在 `timer1_Tick` 里不按 30 秒掉线（超时条件是 `mark1 == 0`，厂家登录把 `mark1` 置 1）。操作工会超时，厂家不会。

通过 `Authorize` 才能打开的两个窗体分别是：

- `FormMySQL`：连上库之后执行文本框里的任意 SQL（`btnExec_Click`）。
- `FormMES`：对任意 URL 发 GET/POST/PUT/DELETE（`btnSend_Click`）。

口令随仓库和发行的程序集一起分发，反编译即可得到。这是当前最需要处理的问题。本报告不写出这些字符串。

### 高

#### H1. 权限工具把 Everyone 完全控制授给目标文件，目录模式还会递归

**位置：** `GrantIniAccess/PermissionHelper.cs`

- `ApplyFullAccessToFile` 在清掉 Deny ACE 之后，给当前用户、Users、Authenticated Users、**World（Everyone）**、SYSTEM、Administrators 加 `FullControl`（约 779–795 行）。
- `GrantDirectoryAccess` 对整棵目录树做同样级别的授权（`ApplyFullAccessToDirectoryTree`）。
- 单文件模式对**父目录**收窄成「当前用户 / Administrators / SYSTEM、不可继承」（约 252–259 行），这是对的；**文件本身**仍是 Everyone 完全控制。

`code.ini`、`test.ini`、`MESConfig.txt`、`MySQLConfig.ini` 若被这个工具处理过，本机任意账户都能读改授权数据和通讯配置。工具本身还会 `runas` 提权（`RestartAsAdministrator`）。这是现场排障用的，但默认策略过宽。

#### H2. MES 凭据用本机范围 DPAPI，本机其他 Windows 用户可以解开

**位置：** `FormMES.cs` 的 `Protect` / `Unprotect`（约 300–317 行）。

注释已说明：`DataProtectionScope.LocalMachine` 表示本机任意用户都能解，选这个是为了换 Windows 账户仍能用。读取时先 LocalMachine，失败再回退 CurrentUser。密文写在 exe 目录的 `MESConfig.txt`。再叠加 H1，加密只防「拷到另一台电脑」，不防本机其他账户或备份目录被拷走后在同一台机器上解密。

请求体（Body）按注释故意明文落盘。若报文里带工单号、条码、令牌，会留在磁盘上。

`btnSend_Click` 用 `HttpWebRequest` 访问用户输入的 URL，没有限制主机、协议或内网地址。登录被绕过或厂家口令泄露后，这就是对产线 MES/PLC 网段的任意 HTTP 客户端。未见 `BinaryFormatter` 一类不安全反序列化；JSON 展示是手写缩进，不是多态反序列化。

#### H3. MySQL 窗体是任意 SQL 控制台，默认不走 TLS

**位置：** `FormMySQL.cs`

- `BuildConnString`：未勾选 SSL 时 `SslMode = None`（默认不勾）。口令在连接串里，异常信息若被写进状态栏，可能带服务器返回的细节。
- `btnExec_Click` 把文本框内容直接交给 `MySqlCommand`。`AllowUserVariables = false` 的注释写成「仅允许可信 SQL」，与真实行为不符：它只关用户变量，不限制语句。
- `NeedsWriteConfirm` 是确认框，操作员点「是」后 `DROP`/`DELETE`/写文件类语句仍会执行。关键字前缀判断挡不住注释、大小写混排以外的更多绕法；不要把它当成安全边界。
- `LoadConnSettings` 仍接受旧键 `pwd=` 明文（约 109 行）。升级后旧配置文件里的口令继续生效。
- 保存失败是空 `catch`（`SaveConnSettings`），操作员以为记住了口令，下次打开是空的。

#### H4. 主逻辑仍集中在一个超大 Form 里，12 路复制是主要缺陷来源

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

相机 1–12 的按钮、IO、触发仍是复制粘贴（`Form1.cs` 里成排的 `bnStopGrabN`、`IO输出N`）。`Form1.Camera18Events.cs` 与 `Form1.DisplaySettings.cs` 各有一份「启动关联程序再立刻 `Kill`」的看图代码，行为容易分叉。项目规则（`.codebuddy/rules/refactor-principle.md`）已要求按服务抽、禁止一次性大改；目前 `AppHost` 收口了所有权，热路径仍在 Form 里。

这不是风格问题：下一次只改了其中一路，另外 11 路会静默保持旧行为。第 55 轮「连续运行不计件被顺手删掉」就是这类回归。

### 中

#### M1. 授权数据读不到时故意放行生产

**位置：** `Form1.cs` 中 `_authExpired` 的计算及上方注释（`Form1_Load` 后段，约 3540–3548 行）。

到期且数据有效时会跳过自动开相机。`test.ini` 缺失、读失败或系统时间被判定为不可信时，`_authExpired` 为 false，设备照常生产和自动开相机。注释写明这是为了避免误锁已付费客户，并承认删掉 `test.ini` 不能防拷贝。日志有「fail-open」字样，界面侧主要是挡手动按钮，挡不住启动自动路径。

机器指纹 `GetCPUSerialnumber` 使用 MD5，并混入固定字符串参与哈希（同文件约 3736–3760 行）。这不能当作防克隆手段。

本报告不描述如何构造加密码。

#### M2. 用「打开图片再杀掉进程」来显示 NG 图

**位置：**

- `Form1.DisplaySettings.cs` 的 `chatu_fangfa`（约 1425–1445 行）
- `Form1.Camera18Events.cs`（约 639 行）

`Process.Start(路径.bmp)` 走 Shell 关联，返回的可能是看图程序，也可能是已存在的宿主进程；紧接着 `Kill()`。结果要么图一闪而过，要么误伤用户正在使用的看图/资源管理器进程。`myProc` 为空时 `Kill` 会抛，被外层 catch 吃掉，只记「图片显示」。文件名经过 `SafeNamePart`，路径前缀仍来自 `E:\fuNng\` 加日期，磁盘不存在时整段失败。

#### M3. 相机关闭靠固定 Sleep 等回调退出

**位置：** `CameraController.ReleaseAllCameras`；`Form1.ReleaseAllCameras` 传入 100ms / 50ms。

注释说明瓶颈是等 SDK 回调线程退出。固定睡眠在「回调很快」时白等，在「回调被检测或存图拖住」时不够，关设备与在途回调会交错。`Dispose` 路径把两次等待都设为 0，只保证句柄最终被销毁。连续运行停帧有看门狗（`RecoverStaleTriggers`），这是补救，不是关闭协议。

`_locker_open` 内仍有大量同步 `Invoke` 和 `Thread.Sleep`（线程审计第 43 轮标为中，归台架）。死锁环当时静态排除了，UI 卡顿和持锁窗口还在。审计文档自己声明行号已作废，不要按那份文档的行号改代码。

#### M4. 统计和存图绑死 E: 盘

**位置：** `Form1.cs` 约 605–628 行的 `pathhead_ok` / `pathhead_ng`；`AppHost` 对统计目录的注释；CI 对「无 E 盘则 Inconclusive」的说明。

工控机没有 E:、E: 满、或权限被 H1 一类工具改乱时，良率 CSV 和 NG 图会失败。存图失败已有限流告警；统计失败是否同样能被操作员看见，要在现场确认 `RunLog` 的失败路径（测试覆盖了追加与日期，没有覆盖「盘符不存在」的界面提示）。

#### M5. 目标框架 4.6 已无安全更新；发行配置打开了 JIT 调试

**位置：** `WindowsFormsApplication1.csproj` 的 `TargetFrameworkVersion` 为 `v4.6`；`App.config` 第 9 行 `jitDebugging="true"`。

4.6 自 2016 年起不再接收安全更新。VisionPro 59.2 的引用是绝对路径 `C:\Program Files\Cognex\VisionPro\ReferencedAssemblies\`，升级框架要先确认 Cognex/海康/IO 卡（`IOC0640.dll`）的支持矩阵，不能单独改 csproj。

`jitDebugging="true"` 让未处理异常更容易挂调试器。现场机器上这会暴露调用栈，也让「崩溃提示 + 写报告」这条路径变得不确定。

#### M6. 仓库根上有一份不会被编译的 `Form6.cs`

`WindowsFormsApplication1/Form6.cs`（69 行）才在 csproj 里。根目录 `Form6.cs`（78 行）内容不同，`.gitignore` 只忽略了根目录 `Form1.cs`，没有忽略这份。后续若有人对照根目录文件改检测窗，改动不会进安装包。

### 低

#### L1. 仓库卫生

- 根目录有微信/企业微信截图和若干小 PNG（`企业微信截图_*.png`、`微信图片_*.jpg`、`tu.png`、登录/保存图标）。体积大，也可能带现场画面。
- `Menu.ini` 是一条客户现场绝对路径（含方案 `.vpp`）。
- `新建文本文档.txt` 含相机型号与序列号一类设备标识。
- `reasonix.toml` 含开发机绝对路径，以及读取本机 `AppData` 会话归档的命令。不是口令，但把个人环境写进了 git。
- `Setup1/Newtonsoft.Json.dll`、`WindowsFormsApplication1/IOC0640.dll` 以二进制入库。DLL 没有校验和或版本说明，换包时难以审计。

#### L2. 日志脱敏不完整

MES **保存配置**时会跳过凭据头；**运行中的响应正文**仍完整进 `txtResponse`。MySQL 状态栏使用 `ex.Message`。授权日志已改成只写到期日和剩余天数（`Form1` 中 `AuthDescribe` 一带），这是好的。检测热路径的 `_logger.WriteLog(ex.Message + ...)` 仍可能带路径和 PLC 地址，一般可接受，不要再把连接串或报文正文拼进去。

#### L3. `ServiceProvider` 工厂路径的并发

`TryResolveInternal` 在锁外执行工厂，并发首次解析可能创建两个实例再覆盖字典。当前 `AppHost.Build` 注册的是已构造好的实例，这条路径基本没走到。若以后改成工厂注册，需要补「只保留一个实例并释放另一个」。

---

## 4. 测试覆盖缺口

### 4.1 `WindowsFormsApplication1.Tests`（CI 会跑）

工程说明见 csproj 顶部注释：net48、MSTest、源码链接、HslCommunication 7.0.1 仅用于 Modbus 回环。链接进去的生产代码主要是：

`ClassIni`、`RunLog`、日志子系统、`CommGridHelper`、`FinsIniStore`、`PendingCameraTriggers`、`AtomicFileSave`、`DeadImageScan`、`FrameAccounting`、`RenderBudget`、`RenderCostMeter`、Modbus TCP Runtime/Link。

`[TestMethod]` 合计 169，与 `ci.yml` 注释一致。这些测试把显示降频、帧账本、ini 脏键、原子写、死图扫描钉得比较紧，适合作为回归网。

**没有链接、因此 CI 完全看不到的部分：**

| 缺口 | 为什么重要 |
| --- | --- |
| `Form1` 全部检测/IO/方案切换 | 产量与安全输出的实际位置 |
| `Form5` 登录与 `Authorize` | S1 |
| `FormMySQL.IsQuerySql` / `NeedsWriteConfirm` | 可抽成纯函数，现在测不到 |
| `FormMES` 凭据头判断、`enc:` 读写 | 脱敏规则已写过多轮，没有测试锁住 |
| `SafeNamePart` | 在 `Form1` 分部类上，存图文件名规则无单测 |
| `CameraController` 释放顺序 | 依赖海康 SDK，至少可以把「空槽跳过、重复释放、失败一路不影响其他路」做成不调用 SDK 的测试替身 |
| `GrantIniAccess` | ACL 策略无测试，只能人工看代码 |
| `CrashMonitor`、`Program` 退出顺序 | 崩溃提醒的误报曾经是现场问题 |
| `Form3` TCP、串口三菱、FINS 运行时（ini 段测试除外） | 触发丢了会直接导致漏检 |

Modbus 回环只有 **1** 个 `[TestMethod]`（`ModbusTcpLoopbackTests.cs`），覆盖的是本机回环轮询，不是多连接、断线重连或写失败。

### 4.2 `tests/`

`tests/InspectionRegressionTests.cs` 用断言函数手写了触发队列的早期回调、失败回滚、容量、并发顺序、检测生命周期和 PLC 故障码格式。`tests/Run-InspectionRegression.ps1` 用 MSBuild 旁的 `csc.exe` 去引用 `WindowsFormsApplication1\bin\x64\Debug\WindowsFormsApplication1.exe`。

它不在 `WindowsFormsApplication1.sln` 的测试工程里，`ci.yml` 和 `verify.ps1` 都不会跑它。队列行为与 `PendingCameraTriggersTests` 有重叠；生命周期和 PLC 故障码格式这两段是独有的，目前等于没有自动化门禁。脚本还假设本机 `MSBuild.exe` 在 PATH 上，和 `verify.ps1` 用 vswhere 查找的方式不一致。

含 `E:\` 的用例在无该盘符的 runner 上会 Inconclusive。CI 注释承认这一点，托管构建不能代替现场磁盘路径的验收。

---

## 5. CI / `.github/workflows`

只有 `.github/workflows/ci.yml` 一个工作流：`push` 与 `pull_request` 都触发，`windows-latest`，安装 .NET SDK 9，然后

`dotnet test WindowsFormsApplication1.Tests\WindowsFormsApplication1.Tests.csproj -c Release`。

合理的部分：

- 明确不在托管 runner 上编主工程（Cognex HintPath 不存在），把主工程验收留给 `verify.ps1`。
- `actions/checkout@v4`、`actions/setup-dotnet@v4`，版本没有用浮动的 `@master`。
- 测试与生产源文件是同一份，不会测到一份过期拷贝。

缺口：

1. **同一 PR 会跑两次**（分支 push 一次、PR 同步一次），没有 `concurrency` 取消过期运行。
2. **没有 `permissions: contents: read`**。默认 GITHUB_TOKEN 权限偏大，这个仓库的 CI 只需要读代码。
3. **没有超时**。测试挂死会占满 runner。
4. **不上传测试结果**（trx/日志），失败时只能翻控制台。
5. **没有密钥扫描、依赖审核或二进制清单**。对一个含登录口令字面量、提交了 DLL 和现场截图的仓库，这一项是空的。
6. **主工程、`GrantIniAccess`、`tests/InspectionRegressionTests.cs` 都不在 CI。** 绿灯只代表那 169 个纯逻辑用例。
7. **工作流前 30 行是版本注释**，把每一轮用例增量写进 YAML。数字一旦和代码分叉，审查者会以为 CI 仍在守旧口径。用例数应以测试工程为准，注释不必当变更日志。
8. `verify.ps1` 把 MSBuild 兜底路径写成 `D:\Program Files\Microsoft Visual Studio\2022\Professional\...`，换机器会直接抛「未找到 MSBuild」。vswhere 分支是对的，兜底路径应删掉或改成参数。

---

## 6. 建议的下一步（按优先级）

1. **换掉写死的口令（S1）。** 去掉万能口令；厂家与操作工分开存储口令哈希（带盐，不要 MD5）；`Form5_Load` 读到的 `userCode` 要么真正使用，要么删掉以免误导。MES/MySQL 只接受厂家角色，并且厂家登录也要超时。发版前把历史提交里的旧口令视为已泄露，现场全部改新口令。不要把新口令写回 git。

2. **收紧 `GrantIniAccess`（H1）。** 默认只给当前用户与 Administrators 修改权限；不要给 World/Everyone；不要递归授权，除非操作员输入目录后再次确认。单文件成功后打印最终 ACE，方便现场核对。给「父目录最小权限」和「文件不包含 Everyone」各写一条不依赖 Windows ACL 的策略测试（检查代码路径生成的规则集合）或在 Windows 上做集成测试。

3. **MES 改回仅当前用户能解的保护（H2），并限制请求目标。** `LocalMachine` 与「exe 目录明文文件」不适合放令牌。Body 里若有凭据，保存前剥离。URL 至少拒绝非 http/https。响应里再出现的令牌不要原样留在界面历史里。

4. **MySQL 窗改成最小权限（H3）。** 默认 `SslMode=Required`（内网例外要显式勾选「允许不加密」）。停止读取 `pwd=`。执行账号使用只读数据库用户；写语句从产品里移除，或单独再要一次厂家口令而不是一个 Yes/No。把 `IsQuerySql` / `NeedsWriteConfirm` 改成 `internal static` 并加测试，同时改掉「仅允许可信 SQL」这句不真实的注释。

5. **授权失败要让操作员看见（M1）。** 保持「宁可误放行也不要误锁机」可以，但 fail-open 时主界面应持续显示「授权未确认」，而不是只写日志。不要在本次把判定式改松或改紧；先补日志/界面一致性。

6. **删掉 `Process.Start` + `Kill` 看图（M2）。** NG 图用进程内 `PictureBox` 打开。存图根目录从 `E:\` 改为配置项，缺盘时启动即提示，而不是第一张 NG 才失败。

7. **继续按路数参数化，而不是再复制第 13 份事件（H4）。** 下一刀只做「IO 输出 1–12」或「停止采集按钮」中的一路，用现有 `CameraController` 下标，行为对照后再做下一组。不要在同一轮改检测时序。空 `catch` 至少改成限流日志。

8. **把 `tests/` 并进 MSTest，或让 `verify.ps1` 在主工程编译成功后调用它。** 优先迁「PLC 故障码格式」和「检测生命周期」这两段独有断言。给 `SafeNamePart`、SQL 分类、MES 头名称判断补测试。

9. **收紧 CI。** `permissions: contents: read`；`concurrency` 取消过时运行；`timeout-minutes`；失败时上传 trx。增加一个密钥扫描（只报新增字面量，避免把本报告的讨论误报进来）。删掉或缩短 `ci.yml` 顶部变更日志。主工程在没有 VisionPro 的 runner 上继续跳过，但应在有自托管机时把 `verify.ps1` 接上，否则第 59 轮这种「注释说 169 例」只能靠开发者本机。

10. **清理仓库根目录（L1）。** 截图、`Menu.ini`、`新建文本文档.txt`、根目录 `Form6.cs`、`reasonix.toml` 移出 git 或加入 `.gitignore`。确认这些文件没有被安装工程引用后再删。二进制 DLL 记下版本与来源。

---

## 7. 本次审查没有做的事

- 没有改业务代码，没有旋转口令，没有改 ACL 实现。
- 没有运行 `verify.ps1` 或 `dotnet test`（当前环境不是带 VisionPro 的 Windows 构建机）。169 这个数字来自源码里的 `[TestMethod]` 计数，与 `ci.yml` 注释一致，不代表本次重新执行了测试。
- 没有做动态验证：相机回调是否在 50ms 内退出、E: 盘缺失时统计界面是否提示、厂家登录后 MES 是否仍可在超时后使用。这些需要现场台架。
