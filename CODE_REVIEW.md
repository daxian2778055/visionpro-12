# visionpro-12 代码审查报告（复审，相对上次）

审查基线：已合并 `origin/master`（`3904819`，版本号 `2026.10.18+dd353e5`）。对照的上一份报告写于 `eb98774`（2026-10-04 附近）之后的第一次复审。本次只改本文件，不改应用源码。未在本环境编译；178 个用例与检测回归 8 场景以提交说明为准，不是本次重跑的结果。

## 复审说明（相对上次）

所有者决定仍然有效，**不升回严重/高**：

- `Form5.cs` 的厂家/操作工字面量口令和单字符万能口令是有意保留的，放在「已知产品选择」。公开仓库仍能看到它们，且它们打开 MES/MySQL 菜单。哈希、去掉万能口令只在对客户正式发版前做。本报告不抄录口令取值。
- `FormMES` / `FormMySQL` 仍是测试工具。任意 HTTP、任意 SQL、LocalMachine DPAPI、默认不走 TLS 只算「若打进客户安装包则残留」，不是当前必须修的缺陷。

自上次审查以来，`master` 上的第 60–62 轮和几笔回审修复对上了上次的一部分中优先级项，并修了三处切方案/渲染缺陷。**当前没有「严重」项。**

### 已修（Fixed）

| 上次条目 | 现在 |
| --- | --- |
| NG 图 `Process.Start` 后立刻 `Kill`（当时 M2） | 已改为窗体内预览。`Form1.DisplaySettings.cs` 的 `chatu_fangfa` 调用 `NgImagePreviewForm.ShowPreview`。`Form1.Camera18Events.cs` 里那份重复的 `listBox3_MouseDown_1`（同样 Start+Kill，且设计器并未绑定它）已删除。列表点击仍走 `Form1.Camera58Events.cs` 的 `listBox3_MouseDown` → `chatu_fangfa`。全库已无对图片的 `Process.Start`+`Kill`。 |
| 存图/统计绑死 `E:\`（当时 M3） | 根目录改为 ini：`[camera] pathhead_root`、`statistics_root`。解析在 `Services/PathLayout.cs`，默认仍是 `E:\`，不配置时子目录名与旧版一致。启动写一行生效值，并把「没配」和「配了但去空白/引号后是空的」分开（`Form1.RootSourceNote`）。`RunLog.Root` 跟着统计根走。`PathLayoutTests` 钉住默认拼出的 `E:\fuNok\`。`RunLogWriteDateTests` 把根指到临时目录，不再因没有 E 盘而 Inconclusive。 |
| `tests/` 不在任何门禁里 | `verify.ps1` 完整模式增加第 3 步，调用 `tests/Run-InspectionRegression.ps1`。该脚本可接收 `-AssemblyPath`，找不到 PATH 上的 MSBuild 时改走 vswhere。`-SkipMainBuild` 时明确跳过，避免拿旧 exe 假绿。CI 注释写明托管 runner 编不了主工程，所以这 8 个场景仍不进 `ci.yml`。 |
| （上次未单列，本轮顺带核实）切方案与渲染 | FINS 连接 2–4 的切方案改走 `RaiseSchemeSwitch`，不再把 `SchemePath` 丢在 `RaiseSelectionChanged`（`FormCommManager.cs`）。无协议连接 2–4 不再改写连接 1 的 `lujing`（`Form3.RaiseSchemeSwitch` → `Form1.RouteNoProtoSchemeSwitch`）。同路径判据抽到 `PathLayout.SchemePathEquals` 并有测试。`RenderBudget.IsPathProtected` 在 `minIntervalMs <= 0` 时直接返回，避免「帧计数归因」整除零。三条协议的「有意跳过切方案」补了 5 秒限流日志（`LogSchemeSwitchSkip`）。 |

### 仍开放（Still open）

| 级别 | 条目 | 变化 |
| --- | --- | --- |
| 高 | `GrantIniAccess` 给 Everyone 完全控制 | 未改。`PermissionHelper.cs` 约 793、850 行仍是 `WorldSid` + `FullControl`，并清除 Deny。 |
| 高 | `Form1` / 相机分部类复制与空 `catch` | 未收敛。`Form1.cs` 现 14601 行，空 `catch { }` 仍约 165 处。`Form3.cs` 从约 48 处增到 49 处（新的 `RaiseSchemeSwitch` / `RaiseCameraTrigger` 也是空 catch）。 |
| 中 | 授权 fail-open 只写日志 | 未改。`_authNotVerifiedFailOpen` 仍只在 `Form1.cs` 约 2387 行写日志，主界面没有持续提示。判定式保持所有者已接受的「读不到就放行」。 |
| 中 | 相机关闭固定 Sleep | 未改。`Form1.ReleaseAllCameras` 仍调用 `ReleaseAllCameras(100, 50)`；`Dispose` 路径仍是 `(0, 0)`。 |
| 中 | .NET Framework 4.6、`jitDebugging="true"` | 未改。`App.config` 第 9 行，两个 csproj 的 `TargetFrameworkVersion` 仍是 `v4.6`。 |
| 中 | 安装包取 `bin\Debug` 的 ini | 未改。`Setup1.vdproj` 约 4480、6028 行指向 `bin\Debug\test.ini` 与 `code.ini`，约 7503 行主程序是 `obj\Release\WindowsFormsApplication1.exe`。`Setup2.vdproj` 约 4739、8964 行同样取 Debug 的 `code.ini` / `test.ini`。文件本身不在 git 里。 |
| 低 | CI 工作流卫生、仓库根目录杂物、游离 `Form6.cs` | 检测回归已进本机 `verify.ps1`，所以「没有任何门禁跑到 `tests/`」这条降为低。`ci.yml` 仍无 `permissions` / `concurrency` / 超时，顶部仍是逐轮用例注释（现写 178）。`verify.ps1` 的 MSBuild 兜底路径仍是 `D:\Program Files\Microsoft Visual Studio\2022\Professional\...`。截图、`Menu.ini`、`新建文本文档.txt`、根目录 `Form6.cs`、`reasonix.toml` 还在。 |

### 新问题（New）

1. **中：无协议「在途切方案」锁在真正卸载开始前就放开。** `Form1.RouteNoProtoSchemeSwitch`（约 9650–9667 行）在 `Invoke` 调用 `xinghao_qiehuan` 返回后的 `finally` 里把 `_noProtoSwitchLock[sourceLink]` 置 0。`xinghao_qiehuan`（约 12110 行）只在 UI 线程做校验和前言，随后自己再 `Task.Run` 做最多约 10 秒的排空和卸载（约 12152 行），调用本身先返回。因此这把按连接的锁盖不住那几十秒；注释写的「与 FINS `GetSwitchLock` 同口径」并没有对齐。全局 `qiehuanzhong` 的 CAS 仍会挡住第二次真正卸载，所以当前不会双开切方案。后果是：在途窗口内的重复请求不会打出「上一次切方案尚未结束」那行限流日志，锁的保护时间只是前言那一段。应在内部 `Task.Run` 结束（含中止/回滚）后再释放 `_noProtoSwitchLock`，或直接复用已经持有的 `qiehuanzhong`，不要另做一把提前放开的锁。
2. **低：NG 预览在 UI 线程整文件解码。** `NgImagePreviewForm.LoadFrom` 用 `File.ReadAllBytes` 再 `new Bitmap`，跑在 UI 线程上。大 BMP 会卡住检测界面。`ShowPreviewCore` 最外层是空 `catch`（约 105 行），`Show` 失败时没有日志。功能方向是对的，这是换掉 `Kill` 之后的残留。
3. **低：可配根目录仍不检查盘是否存在，配置窗也没有输入格。** `PathLayout.NormalizeRoot` 不访问磁盘。缺盘、无权、路径打错时，要等存图失败的限流告警或去翻启动日志里那一行「存图/统计根目录」。注释已说明这两键只读、不写回。默认行为与改前一致，不算回退；上次建议的「启动时若目录不可用就在界面提示」还没做。

`Form3.DispatchReceivedFrame` 的注释仍写「由 Form1.timer10 完成切型」（约 1653 行），代码已经改成 `RaiseSchemeSwitch`。不影响行为，改注释时一并改掉即可。

---

## 1. 项目是什么

12 路 CCD 检测程序（WinForms，.NET Framework 4.6，x64）。海康相机取流，Cognex VisionPro `.vpp` 检测，结果写回 PLC（Modbus TCP/RTU、Omron FINS、三菱串口、无协议串口/TCP），图像和良率 CSV 落到可配置根目录（默认 `E:\`）。

| 部分 | 位置 | 职责 |
| --- | --- | --- |
| 入口 | `Program.cs` | 单实例、全局异常、崩溃转储、`AppHost.Build()` |
| 组合根 | `Core/Infrastructure/AppHost.cs` | 相机、统计、日志、通讯、作业单例 |
| 相机 | `Core/Camera/CameraController.cs` | 12 路停流 / 关设备 / 销毁 |
| 主界面 | `Form1.cs`（14601 行）及 `Form1.*.cs` | 检测、IO、方案切换 |
| 路径 | `Services/PathLayout.cs` | 存图根、统计根、方案路径等价 |
| NG 预览 | `NgImagePreviewForm.cs` | 进程内看图，替代外部进程 |
| 调试窗 | `FormMES.cs`、`FormMySQL.cs` | 菜单仍编进主 exe，见产品选择 |
| 权限工具 | `GrantIniAccess/` | 改 ini/目录 ACL |
| 安装 | `Setup1/`、`Setup2/` | vdproj；仍引用 Debug ini |
| 验收 | `verify.ps1` | 主工程 Rebuild + 178 单测 + 检测回归 |
| CI | `.github/workflows/ci.yml` | 只跑单测 |

关闭主窗体仍是 `Form1_FormClosed` → `Environment.Exit(0)`，收尾在 `Program.OnProcessExit`。

---

## 2. 已经做得好的地方

上次列出的崩溃转储、有界异步日志、相机句柄归属、`AtomicFileSave`、`SafeNamePart`、外部程序白名单（`button32_Click` 只启动已存在的 `.exe` / `.application`）、单实例互斥、源码链接测试，都还在。

本轮新增、值得保留：

- **路径解析是纯函数且有测试。** 空串、NUL、成对引号、尾斜杠、UNC、子目录拼接、方案路径「该相等 / 不该相等」都在 `PathLayoutTests`。默认根下的目录名与硬编码时代逐字一致，避免现场静默改道。
- **NG 图不再拉起系统看图器。** 预览窗单实例换图，按字节读入后从内存解码，不长期占文件句柄。Esc 关闭。
- **切方案失败变得可从日志里看见。** 同路径、在途、未持锁不再完全静默（无协议在途锁的覆盖时间见「新问题」）。
- **本机验收把检测回归接上了**，并且跳过主工程编译时不会误跑旧 exe。

---

## 3. 当前问题（按级别）

### 高

#### H1. 权限工具把 Everyone 完全控制授给目标文件，目录模式还会递归

**位置：** `GrantIniAccess/PermissionHelper.cs`

`ApplyFullAccessToFile` 清掉 Deny 之后，给当前用户、Users、Authenticated Users、**World（Everyone）**、SYSTEM、Administrators 加 `FullControl`（约 779–795 行，`WorldSid` 约 793 行）。目录自身把 `WorldSid` 放进可继承 ACE（约 847–852 行），`GrantDirectoryAccess` 再走到子树。

单文件模式对**父目录**收窄为当前用户 / Administrators / SYSTEM 且不可继承（约 252–259 行）。文件本身仍是 Everyone 完全控制。`code.ini`、`test.ini` 被这个工具处理过后，本机任意账户都能改授权数据。工具还会 `runas` 提权。这是当前最高优先级，本轮没有动过。

#### H2. 主逻辑仍按 12 路复制，空 catch 仍然很密

`Form1.cs` 14601 行，`Form1.Designer.cs` 14994 行，`Form1.Camera58Events.cs` 6199 行，`Form3.cs` 2470 行。空 `catch { }`：`Form1.cs` 约 165，`Form1.Camera58Events.cs` 38，`Form3.cs` 49，三个 PLC 大窗合计仍约 70。

本轮的无协议切方案、12 路触发字符比较（`DataChange` 约 9500–9614 行）仍是逐路抄写。`AppHost` 收口了服务所有权，热路径还在 Form 里。只改一路、其余路保持旧行为，仍是主要回归来源。

### 中

#### M1. 授权数据读不到时按设计放行，操作员只能在日志里看到

**位置：** `Form1.cs` 约 3573–3577 行计算 `_authExpired` / `_authNotVerifiedFailOpen`；约 2383–2388 行在未过期时自动开相机，fail-open 时只 `WriteLog`。

判定式不要改。缺口仍是：界面「加密中」挡的是手动按钮，挡不住自动开相机和随后的自动运行。正式发版前在 `_authNotVerifiedFailOpen` 为真时让主界面持续显示「授权未确认，已按设计继续运行」。

#### M2. 相机关闭靠固定 Sleep 等回调退出

**位置：** `CameraController.ReleaseAllCameras`；`Form1.ReleaseAllCameras`（约 8486–8493 行）传入 100ms / 50ms。`Dispose` 兜底是 0 等待。

回调被存图或检测拖住时，关设备与在途回调仍会交错。连续运行停帧看门狗还在，那是补救不是关闭协议。`_locker_open` 内的同步 `Invoke` 和 `Sleep` 仍以 `docs/线程审计-第43轮.md` 为准，行号已作废。

#### M3. 目标框架 4.6 已无安全更新；发行配置打开了 JIT 调试

**位置：** 两个 csproj 的 `TargetFrameworkVersion`；`App.config` 第 9 行 `jitDebugging="true"`。

升级要先确认 VisionPro 59.2、海康 SDK 和 `IOC0640.dll`。发布配置应关掉 JIT 调试，避免现场未处理异常去挂调试器。

#### M4. 安装项目把本机 Debug 配置打进包

**位置：** `Setup1/Setup1.vdproj` 的 `bin\Debug\test.ini`、`bin\Debug\code.ini`，以及 `obj\Release\WindowsFormsApplication1.exe`。`Setup2/Setup2.vdproj` 同样引用这两份 Debug ini。

ini 不在 git 中。打包机 Debug 目录里留下的授权文件会进 MSI，并和 Release exe 混装。正式出包应改为明确的发布目录。主 exe 里仍有 MES/MySQL 菜单，客户包是否带上它们见产品选择。

#### M5. 无协议切方案的按连接锁提前释放

见文首「新问题」第 1 条。`Form1.cs` 的 `RouteNoProtoSchemeSwitch` 与 `xinghao_qiehuan`。全局门控仍在，不会因此双开卸载；锁和日志的口径与注释不符。

### 低

#### L1. NG 预览在 UI 线程解码大图；最外层失败无日志

`NgImagePreviewForm.cs` 的 `LoadFrom`、`ShowPreviewCore`。见文首「新问题」第 2 条。

#### L2. 根目录可配，但缺盘不提示、界面不能改

`PathLayout.cs`、`Form1.InitializeJobManager` 约 616–632 行。见文首「新问题」第 3 条。默认 `E:\` 是故意保持的。

#### L3. CI 与本机脚本的剩余缺口

`ci.yml` 在 `windows-latest` 上跑 `dotnet test`（178 个 `[TestMethod]`，与注释一致）。合理之处没变：不在托管 runner 上编 Cognex 主工程；checkout / setup-dotnet 使用 v4。

仍缺：`permissions: contents: read`、`concurrency` 取消过期运行、超时、失败 trx。同一 PR 会因 `push` + `pull_request` 跑两次。工作流前几十行仍是逐轮变更日志。`verify.ps1` 的 `Find-MSBuild` 在 vswhere 失败后仍写死 D 盘 Professional 路径。

检测回归不进 CI 是 Cognex 依赖造成的，本机 `verify.ps1` 已经覆盖，不再单独升为中。

#### L4. 公开仓库里的现场痕迹

根目录微信/企业微信截图、`Menu.ini`（客户绝对路径）、`新建文本文档.txt`（相机标识）、`reasonix.toml`（开发机路径）、`Setup1/Newtonsoft.Json.dll` 与 `IOC0640.dll` 无版本说明。根目录 `Form6.cs`（78 行）与 `WindowsFormsApplication1/Form6.cs`（69 行，才进 csproj）内容不同。`.gitignore` 只忽略了根目录 `Form1.cs`。

---

## 4. 已知产品选择 / 发版前再收

### P1. 登录口令保持源码字面量

**位置：** `Form5.cs` 的 `button1_Click` 与 `Authorize`。`Form5_Load` 读了 `userCode`，点击登录未使用。`timer1_Tick` 里操作工约 30 秒掉线，厂家不按这个超时掉线。

正式发版前：带盐哈希、去掉万能口令、删掉或真正使用 ini 里的 `userCode`。新口令不要写回 git。在那之前不必为审查意见改 `Form5.cs`。

### P2. MES / MySQL 是测试工具，随主程序一起发行

**位置：** `FormMES.cs`、`FormMySQL.cs`；菜单在 `Form1.Camera58Events.cs` 的 `mesToolStripMenuItem_Click`、`mysqlToolStripMenuItem_Click`（仍经 `Form5.Authorize`）。`Setup1` 打的是这份主 exe。

MES 对文本框 URL 发请求，凭据保存用 `DataProtectionScope.LocalMachine`。MySQL 执行文本框 SQL，默认 `SslMode.None`，仍读旧键 `pwd=`。`NeedsWriteConfirm` 是确认框，不是权限边界。

正式发版前三选一：`#if DEBUG`、独立工具工程、安装项目排除菜单。真正拿它们连客户主机时，再收紧 DPAPI 范围和 TLS。现在不排进优先队列。

---

## 5. 测试覆盖

CI 会跑的链接源码仍是：`ClassIni`、`RunLog`、日志、`CommGridHelper`、`FinsIniStore`、`PendingCameraTriggers`、`AtomicFileSave`、`DeadImageScan`、`FrameAccounting`、`RenderBudget`、`RenderCostMeter`、Modbus TCP、以及新增的 `PathLayout`。

`[TestMethod]` 合计 **178**（上次 169）。新增主要在 `PathLayoutTests`（9）以及 `RunLogWriteDateTests` 改为不依赖 E 盘。

仍然看不到：`Form1` 检测/IO/方案切换本体（含 `RouteNoProtoSchemeSwitch` 与 `xinghao_qiehuan` 的锁寿命）、`CameraController`、`GrantIniAccess`、`NgImagePreviewForm`、`CrashMonitor`。`Form5` 与调试窗按产品选择，本轮不要求补安全测试。

`tests/InspectionRegressionTests.cs` 的 8 个场景（早期回调、失败回滚、容量、并发顺序、生命周期、PLC 故障码）现在由完整 `verify.ps1` 执行，仍不在 `ci.yml`。队列行为与 `PendingCameraTriggersTests` 有重叠；生命周期和故障码格式是独有的。

---

## 6. 建议的下一步

1. **收紧 `GrantIniAccess`（H1）。** 不要给 World/Everyone；不要默认递归。父目录那套「最小权限、不可继承」扩到文件本身。
2. **按路数收口 `DataChange` 里 12 段触发，而不是再复制（H2）。** 下一刀只做一组。空 `catch` 改成限流日志，从新加的 `RaiseSchemeSwitch` 开始。
3. **把 `_noProtoSwitchLock` 的释放挪到切方案后台任务结束之后（M5）。** 与 `RollbackSchemeSwitch` / 成功路径同一处复位。补一条注释，写明全局 `qiehuanzhong` 才是互斥，这把锁只防同连接重复排队。
4. **fail-open 补界面提示，不改判定（M1）。** 缺盘的存图根同样：启动时 `Directory.Exists` 失败就在主界面写一句，不必先做配置窗（L2）。
5. **正式发版清单（P1、P2、M3、M4）。** 口令哈希并去掉万能口令；调试窗不进客户包；关掉 `jitDebugging`；vdproj 不要再取 `bin\Debug` 的 ini。
6. **相机关闭等回调结束（M2）。** 仍归台架，不要和界面改动捆在一轮。
7. **NG 预览解码移出 UI 线程，或限制读入大小（L1）。** 失败写日志，不要空 catch。
8. **CI 加上 `permissions`、`concurrency`、超时（L3）。** 缩短 `ci.yml` 顶部注释。`verify.ps1` 去掉写死的 D 盘 MSBuild 路径。
9. **清理仓库根目录（L4）。** 确认安装工程不引用后再移出 git。

---

## 7. 本次没有做的事

- 没有改业务代码，没有改口令、ACL、安装项目或调试窗。
- 没有运行 `verify.ps1` 或 `dotnet test`。178 与「检测回归 8 场景」来自 `3904819` 的提交说明和源码中的 `[TestMethod]` 计数。
- 没有在台架上确认：大 BMP 预览是否卡住界面、无 E 盘时启动日志是否足够、fail-open 时主画面是否仍像正常生产。
