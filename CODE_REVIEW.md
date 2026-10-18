# visionpro-12 代码审查报告（复审，相对上次）

审查基线：已合并 `origin/master`（`075ed66`，版本号 `2026.10.18+5ad62de`，第 63 轮）。上一份报告的基线是 `3904819`。本次只改本文件，不改应用源码。未在本环境编译；提交说明里的 178 个单测和检测回归 8 场景不是本次重跑的结果。

## 复审说明（相对上次）

所有者决定仍然有效，**不升回严重或高**：

- `Form5.cs` 的厂家/操作工字面量口令和单字符万能口令是有意保留的，放在「已知产品选择」。公开仓库仍能看到它们，且它们打开 MES/MySQL 菜单。哈希、去掉万能口令只在对客户正式发版前做。本报告不抄录口令取值。
- `FormMES` / `FormMySQL` 仍是测试工具。任意 HTTP、任意 SQL、LocalMachine DPAPI、默认不走 TLS 只算「若打进客户安装包则残留」。

第 63 轮针对上一份报告文首的三条新问题改了两处代码（`Form1.cs`、`NgImagePreviewForm.cs`）。**没有回退。当前没有「严重」项。**

### 已修（Fixed）

| 上次条目 | 现在 |
| --- | --- |
| 中：`RouteNoProtoSchemeSwitch` 的按连接锁在 `xinghao_qiehuan` 返回时就放开，盖不住后台卸载那几十秒 | 已补上第二层。`Form1.RouteNoProtoSchemeSwitch`（约 9668–9676 行）在排队之前先看 `_switchingScheme`。该标志在 `SchemeSwitchPreface`（约 12119 行）里置真，发生在 `xinghao_qiehuan` 把卸载 `Task.Run` 出去（约 12189 行）**之前**，并在后台任务收尾 / `RollbackSchemeSwitch`（约 12080 行）时置假。按连接的 `_noProtoSwitchLock` 仍在 `xinghao_qiehuan` 返回后放开（约 9699–9702 行），注释已改成只覆盖「请求已接收、前言还没跑」那一段，不再声称与 FINS 锁同口径。同连接在几十秒卸载期间的重复请求会打 5 秒限流日志后返回。 |
| 低：NG 预览在 UI 线程整文件解码 | 已挪到后台。`NgImagePreviewForm.LoadFrom` 只在 UI 上写「正在读取…」并 `++_loadSeq`，`DecodeInBackground` 做 `File.Exists` / `ReadAllBytes` / `new Bitmap`。回投 `ApplyDecoded` 时序号对不上就丢掉位图，避免旧图盖新图。`BeginInvoke` 失败时在后台线程释放位图。 |
| 低：可配根目录不检查盘是否存在 | 启动日志已点名。`RootReachNote`（约 557–568 行）在 `InitializeJobManager` 的后台线程里对存图根做 `Directory.Exists`；统计根与存图根不同才再探一次（约 646–652 行）。盘不在、目录未建或无权限都写成「当前访问不到」，异常降级成「可达性未确认」，不抛到启动流程外。 |

更早已经落地、本轮未再改、仍然有效：窗体内 NG 预览替代 `Process.Start`+`Kill`；`PathLayout` 可配根目录且默认 `E:\`；检测回归进 `verify.ps1`；FINS 连接 2–4 不再丢 `SchemePath`；单测 178。

### 仍开放（Still open）

| 级别 | 条目 | 本轮 |
| --- | --- | --- |
| 高 | `GrantIniAccess` 给 Everyone 完全控制 | 未改。`PermissionHelper.cs` 约 793、850 行仍是 `WorldSid` + `FullControl`，并清除 Deny。 |
| 高 | `Form1` / 12 路复制与空 `catch` | 未收敛。`Form1.cs` 现 **14638** 行（上一份 14601）。`DataChange` 仍逐路抄写。空 `catch { }` 没有成批删除。 |
| 中 | 授权 fail-open 只写日志 | 未改。`_authNotVerifiedFailOpen` 约 3598 行赋值，约 2408 行只 `WriteLog`，主界面没有持续提示。判定式仍是「读不到就放行」。 |
| 中 | 相机关闭固定 Sleep | 未改。`Form1.ReleaseAllCameras` 约 8514 行仍是 `ReleaseAllCameras(100, 50)`。 |
| 中 | .NET Framework 4.6、`jitDebugging="true"` | 未改。`App.config` 第 9 行；两个 csproj 的 `TargetFrameworkVersion` 仍是 `v4.6`。 |
| 中 | 安装包取 `bin\Debug` 的 ini | 未改。`Setup1.vdproj` 约 4480 行 `bin\Debug\test.ini`，约 7503 行主程序是 `obj\Release\WindowsFormsApplication1.exe`。`Setup2.vdproj` 同样引用 Debug 的 `code.ini` / `test.ini`。 |
| 低 | CI 卫生、仓库根目录杂物、游离 `Form6.cs` | 未改。`ci.yml` 仍无 `permissions` / `concurrency` / 超时。`verify.ps1` 的 MSBuild 兜底路径仍写死 D 盘 Professional。 |

### 新问题（New）

没有新的高或中缺陷。第 63 轮留下的残留都是低：

1. **两路连接抢在 `_switchingScheme` 置真之前。** 检查在接收线程上，置真在随后的 UI `Invoke` 里。连接 A、B 若同时看到标志为假，会各拿各的 `_noProtoSwitchLock` 并都调用 `xinghao_qiehuan`。后到的被全局 `qiehuanzhong` 挡住，打的是约 13299 行那句**不限流**的「切方案请求被忽略：已有切换在途」。不会双开卸载。几十秒窗口已经由 `_switchingScheme` 盖住，这只是置真前的窄窗口。
2. **`sourceLink` 越界仍直接 `return`（约 9667 行），没有日志。** 连接号按现有 1–4 用不到；数组长度 16。
3. **NG 预览最外层仍是空 `catch`。** `ShowPreview` / `ShowPreviewCore`（约 94、116 行）失败不写日志。解码失败会写到信息条。没有读入大小上限，异常大的文件仍可能在线程池上占内存。提交说明写明本轮没有实机连点两张图，静态走查与序号设计一致，台架仍要看「正在读取…」是否被图替换。
4. **根目录不可达只进日志，不进主界面。** `Directory.Exists` 对「目录还没建」也是假，首次把根配到尚不存在的文件夹会报「访问不到」，即使稍后存图会自己建子目录。默认 `E:\` 在盘存在时是「可达」，不会误报。死掉的 UNC 会让 `Directory.Exists` 在 `InitializeJobManager` 那条后台线程上等到 SMB 超时，相机初始化会跟着晚，界面本身不卡。

### 回退（Regressed）

没有。第 63 轮没有改回 `Process.Start`+`Kill`、没有把存图根改回只写死 `E:\`、没有放宽 Everyone ACL 以外的权限（ACL 根本没动）、没有把检测回归从 `verify.ps1` 拿掉。

---

## 1. 项目是什么

12 路 CCD 检测程序（WinForms，.NET Framework 4.6，x64）。海康相机取流，Cognex VisionPro `.vpp` 检测，结果写回 PLC（Modbus TCP/RTU、Omron FINS、三菱串口、无协议串口/TCP）。图像和良率 CSV 的根目录由 ini 配置，默认 `E:\`。

| 部分 | 位置 | 职责 |
| --- | --- | --- |
| 入口 | `Program.cs` | 单实例、全局异常、崩溃转储、`AppHost.Build()` |
| 相机 | `Core/Camera/CameraController.cs` | 12 路停流 / 关设备 / 销毁 |
| 主界面 | `Form1.cs`（14638 行）及 `Form1.*.cs` | 检测、IO、方案切换 |
| 路径 | `Services/PathLayout.cs`、`Form1.RootReachNote` | 根目录解析；启动时写是否访问得到 |
| NG 预览 | `NgImagePreviewForm.cs` | 后台解码，UI 只贴图 |
| 调试窗 | `FormMES.cs`、`FormMySQL.cs` | 仍编进主 exe，见产品选择 |
| 权限工具 | `GrantIniAccess/` | 改 ini/目录 ACL |
| 安装 | `Setup1/`、`Setup2/` | 仍引用 Debug ini |
| 验收 | `verify.ps1` | 主工程 + 178 单测 + 检测回归 |
| CI | `.github/workflows/ci.yml` | 只跑单测 |

---

## 2. 已经做得好的地方

崩溃转储、有界异步日志、相机句柄归属、`AtomicFileSave`、`SafeNamePart`、外部程序只启动已存在的 `.exe` / `.application`、单实例、源码链接测试，都还在。

第 63 轮的修法本身是收口而不是再抄一份：在途用已有的 `_switchingScheme`，不新增第二套「切换完成」标志；预览用序号丢弃过期位图，并在窗体已释放时释放 GDI 对象；可达性探测放在原本就存在的后台初始化线程上，失败不抛。注释把上一轮写过头的「与 FINS 锁同口径」改回来了。

---

## 3. 当前问题（按级别）

### 高

#### H1. 权限工具把 Everyone 完全控制授给目标文件，目录模式还会递归

**位置：** `GrantIniAccess/PermissionHelper.cs` 约 779–795 行（文件，`WorldSid` 约 793 行）、约 847–852 行（目录，可继承）。

清掉 Deny 之后，当前用户、Users、Authenticated Users、Everyone、SYSTEM、Administrators 都有 `FullControl`。单文件模式对父目录收窄了（约 252–259 行），文件本身没有。`code.ini` / `test.ini` 被处理过后，本机任意账户都能改授权数据。这是当前最高优先级。

#### H2. 主逻辑仍按 12 路复制，空 catch 仍然很密

`Form1.cs` 14638 行，`Form1.Designer.cs` 14994 行，`Form1.Camera58Events.cs` 6199 行。`DataChange`（约 9500 行一带）仍是 12 段几乎相同的触发。空 `catch { }` 仍集中在 `Form1.cs`、`Form3.cs` 和三个 PLC 大窗。本轮只加了在途判断和一行可达性日志，没有减少复制。

### 中

#### M1. 授权数据读不到时按设计放行，操作员只能在日志里看到

**位置：** `Form1.cs` 约 3573–3598 行计算 `_authExpired` / `_authNotVerifiedFailOpen`；约 2404–2408 行自动开相机，fail-open 时只写「按 fail-open 设计仍自动开相机」。

不要改判定式。正式发版前在标志为真时让主界面持续显示「授权未确认，已按设计继续运行」。

#### M2. 相机关闭靠固定 Sleep 等回调退出

**位置：** `CameraController.ReleaseAllCameras`；`Form1.ReleaseAllCameras` 约 8514 行传入 100ms / 50ms。`Dispose` 兜底仍是 0 等待。

#### M3. 目标框架 4.6 已无安全更新；发行配置打开了 JIT 调试

**位置：** 两个 csproj 的 `TargetFrameworkVersion`；`App.config` 第 9 行 `jitDebugging="true"`。升级要先确认 VisionPro、海康 SDK 和 `IOC0640.dll`。

#### M4. 安装项目把本机 Debug 配置打进包

**位置：** `Setup1/Setup1.vdproj` 约 4480 行 `bin\Debug\test.ini`、约 6028 行 `code.ini`、约 7503 行 `obj\Release\WindowsFormsApplication1.exe`。`Setup2/Setup2.vdproj` 约 4739、8964 行同样取 Debug ini。ini 不在 git 里。打包机 Debug 目录里的授权文件会进 MSI。

### 低

#### L1. 第 63 轮的残留

见文首「新问题」。无协议双连接窄窗口、越界连接号无日志、预览空 `catch` 与无大小上限、根目录不可达只写日志且死 UNC 会拖住初始化线程。

#### L2. CI 与仓库卫生

`ci.yml` 在 `windows-latest` 上跑 178 个 `[TestMethod]`，不编 Cognex 主工程，这是对的。仍缺最小权限、取消过期运行、超时。同一 PR 会因 `push` 和 `pull_request` 跑两次。工作流顶部仍是逐轮用例注释。

根目录微信截图、`Menu.ini`（客户路径）、`新建文本文档.txt`、`reasonix.toml`、根目录游离 `Form6.cs`（与 csproj 里那份不是同一个文件）还在。

---

## 4. 已知产品选择 / 发版前再收

### P1. 登录口令保持源码字面量

**位置：** `Form5.cs` 的 `button1_Click` 与 `Authorize`。`Form5_Load` 读了 `userCode`，点击登录未使用。操作工约 30 秒掉线，厂家不按这个超时掉线。

正式发版前：带盐哈希、去掉万能口令。新口令不要写回 git。在那之前不必为审查意见改 `Form5.cs`。

### P2. MES / MySQL 是测试工具，随主程序一起发行

**位置：** `FormMES.cs`、`FormMySQL.cs`；菜单在 `Form1.Camera58Events.cs` 的 `mesToolStripMenuItem_Click`、`mysqlToolStripMenuItem_Click`。`Setup1` 打的是这份主 exe。

正式发版前三选一：`#if DEBUG`、独立工具工程、安装项目排除菜单。真正连接客户主机时再收紧 DPAPI 和 TLS。

---

## 5. 测试覆盖

CI 链接的仍是纯逻辑：`ClassIni`、`RunLog`、日志、`CommGridHelper`、`FinsIniStore`、`PendingCameraTriggers`、`AtomicFileSave`、`DeadImageScan`、`FrameAccounting`、`RenderBudget`、`RenderCostMeter`、Modbus TCP、`PathLayout`。`[TestMethod]` 仍是 **178**。第 63 轮的三处改动都在 `Form1.cs` / `NgImagePreviewForm.cs`，测试工程链不进来，所以用例数不变。提交说明也写了：证据是编译和静态走查，没有新断言。

`tests/InspectionRegressionTests.cs` 的 8 个场景仍只在完整 `verify.ps1` 里跑，不在 `ci.yml`。

仍然看不到：`GrantIniAccess`、`CameraController` 释放时序、`RouteNoProtoSchemeSwitch` 与 `_switchingScheme` 的衔接、`NgImagePreviewForm` 的序号丢弃。

---

## 6. 建议的下一步

1. **收紧 `GrantIniAccess`（H1）。** 不要给 Everyone；不要默认递归。父目录那套最小权限扩到文件本身。
2. **按路数收口 `DataChange` 的 12 段触发（H2）。** 下一刀只做一组。空 `catch` 从新代码开始改成限流日志。
3. **fail-open 补界面提示，不改判定（M1）。** 根目录「访问不到」可以复用同一行状态文字，不必只留在日志里（L1）。
4. **正式发版清单（P1、P2、M3、M4）。** 口令哈希并去掉万能口令；调试窗不进客户包；关掉 `jitDebugging`；vdproj 不要再取 `bin\Debug` 的 ini。
5. **相机关闭等回调结束（M2）。** 仍归台架，不要和界面改动捆在一轮。
6. **给无协议在途补一条可测的纯函数，或至少把「双连接窄窗口」接受为已知。** 若要消掉约 13299 行的不限流日志，把 `_switchingScheme` 的判断挪进与 `qiehuanzhong` 同一临界区，而不是接收线程上的先读后排队。
7. **CI 加上 `permissions`、`concurrency`、超时（L2）。** 台架看一次 NG 连点是否换图、死 UNC 启动是否拖住初始化。

---

## 7. 本次没有做的事

- 没有改业务代码。
- 没有运行 `verify.ps1` 或 `dotnet test`。
- 没有实机确认预览换图、无 E 盘时的启动日志措辞，或 fail-open 时主画面是否仍像正常生产。
