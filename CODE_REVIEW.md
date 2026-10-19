# visionpro-12 代码审查报告

审查基线：`origin/master` 为 `075ed66`（版本号 `2026.10.18+5ad62de`，BuildTime `2026-10-18 23:47:29`，第 63 轮）。该提交已是本审查分支的祖先，无需再合并。本次只改本文件，不改应用源码。未在本环境重跑编译；提交说明中的 178 个单测与检测回归 8 场景沿用第 63 轮的记录，不是这次重跑的结果。

本轮按所有者划定的范围做静态走查：正确性、竞态、资源释放、PLC/通讯、相机生命周期、方案切换、统计与路径、崩溃与退出、日志、CI/验收（不含主窗体体积）、安装包与运行配置是否对得上、死代码、回归与边界。重点看了 `Services/`、`Core/`、`FormModbus` / `FormModbusRtu` / `FormOmron` / `Form3` / `FormMelsecSerial`、`CameraController`、切方案路由，以及测试是否盖住这些风险。

当前没有「严重」项，也没有「高」项。

## 复审范围

| 项 | 内容 |
| --- | --- |
| 代码 | `075ed66` 上的检测、通讯、相机、安装与测试工程 |
| 不列入开放问题 | 见下一节。那些项只在该节出现，下文不建议修改 |
| 产品选择（不升级） | `Form5` 口令；`FormMES` / `FormMySQL` 测试工具 |
| 证据 | 静态阅读调用链与锁。`Form1`、`NgImagePreviewForm`、`CameraController` 不在测试工程的编译集里，第 63 轮三处改动没有新的断言 |

12 路 CCD（WinForms，.NET Framework 4.6，x64）。海康取流，Cognex VisionPro `.vpp` 检测，结果回写 PLC（Modbus TCP、Modbus RTU、Omron FINS、无协议串口/TCP）。三菱 FX 编程口是菜单里的手工读写窗，不在检测回写链上。图像与良率 CSV 的根目录由 ini 配置，默认 `E:\`。

| 部分 | 位置 | 职责 |
| --- | --- | --- |
| 入口 | `Program.cs` | 单实例互斥、未处理异常、崩溃转储、`AppHost.Build()` |
| 相机 | `Core/Camera/CameraController.cs` | 12 路停流、关设备、销毁句柄 |
| 检测门 | `Core/Threading/InspectionLifecycle.cs` | 暂停进入、在途计数、等待排空 |
| 主流程 | `Form1` 及 `Form1.*.cs` | 取流回调、`getrecord`、切方案、关窗 |
| 通讯 | `FormModbus`、`FormModbusRtu`、`FormOmron`、`Form3` | 连接 1 自有轮询；连接 2–4 为独立子窗 |
| 路径 | `Services/PathLayout.cs`、`RunLog.cs` | 根目录与按日 CSV |
| NG 图 | `NgImagePreviewForm.cs` | 进程内预览，解码在后台 |
| 安装 | `Setup1/`、`Setup2/` | vdproj 引用本机输出 |
| 验收 | `verify.ps1`、`.github/workflows/ci.yml` | 本机全量；托管 CI 只跑单测 |

## 按所有者决定忽略

下列三项本轮不列为开放问题，也不建议修改：

1. `Form1` 的体积、12 路复制、空 `catch` 密度，以及巨型窗体的可维护性。
2. `GrantIniAccess` 里对 Everyone 的完全控制 ACL（`PermissionHelper.cs` 仍使用 `WorldSid` + `FullControl`）。
3. 授权失败时只写日志、主界面不挂横幅的放行行为。

同时保持既有产品决定，不升级：

- `Form5.cs` 的厂家/操作工字面量口令和单字符万能口令是有意保留的产品选择。公开仓库里看得到这些字面量，它们会打开 MES/MySQL 菜单。哈希、去掉万能口令只在对客户正式发版前再收。本报告不抄录口令取值。
- `FormMES` / `FormMySQL` 是测试工具。任意 HTTP、任意 SQL、LocalMachine DPAPI、默认不走 TLS，只在「打进客户安装包」时算残留。收口方式是 `#if DEBUG`、独立工具，或发版时不编进主程序，不把现有行为当成必须修的缺陷。

## 本轮核对过、行为已经对齐的部分

这些路径按现有注释和调用顺序是自洽的，不单列成问题：

- 崩溃：`Program.CurrentDomain_UnhandleException` 用同步 `WriteLogSync` 写原因、现场和 minidump，并弹出报告路径。`ProcessExit` 覆盖 `Environment.Exit` 时的日志收尾。上次非正常退出由 `CrashMonitor.HadPreviousCrash` 区分「真崩溃」和「断电/强杀残留标记」。
- 单实例：`Program.Main` 用程序集全名做互斥，第二个进程直接退出。
- 检测门：`ImageCallBack` 在 `TryEnter` 失败时记账后返回，成功路径在 `finally` 里 `Exit`。`getrecord` 同样成对。同槽 `recordBusy` 占用中丢弃本次，通讯触发靠 PLC 超时判 NG，并复位 `trriger`，避免回图按钮被粘住。
- 标题栏关闭会先排空检测。`Form1_FormClosing` 在确认关闭后调用 `StopInspectWorkers`：`StopAccepting`，各检测线程 `Join(300)`，再 `WaitForIdle(10000)`。返回 false（10 秒后仍有检测事务）时中止关闭、回滚 `_disposingFlag`，不拆相机。返回 true 之后才 `ReleaseAllCameras`。
- 切方案的长卸载被 `_switchingScheme` 盖住。`SchemeSwitchPreface` 在把卸载 `Task.Run` 出去之前置真；`RouteNoProtoSchemeSwitch` 在按连接排队之前先看这个标志，重复请求 5 秒限流后返回。全局 `qiehuanzhong` 的 CAS 保证只有一路真正卸载。FINS 连接 2–4 走 `RaiseSchemeSwitch`，无协议连接 2–4 不再改写连接 1 的 `lujing`。
- 连接 1 的 Modbus/FINS/RTU 轮询、回写、心跳、手动读写共用该窗体的 `_ioSync`。手动操作 `TryEnter` 约 1 秒，拿不到就提示链路正忙，`MessageBox` 在释放锁之后。失败帧数值格式写 `999`（`InspectionFailureOutput.ForFormat`）。无协议目标连接已释放时跳过发送并记日志，靠 PLC 超时判 NG，不回落到连接 1。
- 结果回写与 IO 脉冲分开：`_cameraResultWork` 容量 64，`_cameraOutWork` 容量 4。统计不进这两条队列。
- `SerialPortGuard` 覆盖 Modbus-RTU 与无协议连接 2 及以上的 ini 占用；连接 1 无协议串口在运行时 `TryAcquire`。`CommCameraGuard` 允许不同协议共用相机，结果只回到触发那条连接。
- 存图/统计根由 `PathLayout` 解析，默认 `E:\`。单测把 `RunLog.Root` 指到临时目录，不依赖 E 盘。NG 预览解码在后台，`_loadSeq` 丢掉过期位图。
- 轮询 `Stop` 在 `Join(1000)` 超时后保留线程引用并记日志，再次 `Start` 不会再起第二条轮询线程。
- 托管 CI 的 178 个 `[TestMethod]` 覆盖路径解析、帧账本、显示预算、ini、原子保存、待触发队列等纯逻辑。检测回归 8 场景在 `verify.ps1` 第 3 步（需要已编译的主程序和 csc）。

## 当前问题

### 严重

无。

### 高

无。

### 中

#### M1. 两条关相机路径用固定等待代替「原生回调已退出」

现场影响：关主窗口或点「关闭相机」时，若海康回调线程还在用转换缓冲，可能打到已销毁句柄或已 `FreeHGlobal` 的内存，表现为关窗/停相机瞬间闪退。停相机时持全局相机锁睡眠，会让同时进行的打开/重连抢锁失败。

标题栏关闭的托管侧已经排空，见上一节。排空之后的释放仍是定时等待：

- `Form1.ReleaseAllCameras` 调用 `CameraController.ReleaseAllCameras(100, 50)`：12 路一起 `StopGrabbing`，`Sleep(100)`，一起 `CloseDevice`，`Sleep(50)`，再 `DestroyDevice`。回到 `Form1` 后再 `Thread.Sleep(300)`，注销 `cbImage` / `cbException`，然后在 `m_BufForSaveImageLock` 下 `FreeHGlobal`。
- `CameraController.ReleaseAllCameras` 不加锁，也不读 `InspectionLifecycle` 的在途计数。调用方必须自己保证没有人还在用设备。`Dispose` 会以 `(0, 0)` 调用同一方法（不等待）；当前工程里没有 `_cameraCtrl.Dispose` 的调用点，这条零等待路径现在走不到。

「关闭相机」是另一条路径，`Form1.bnClose_Click`：

- 先停重连并泵消息，再 `StopAccepting`，`WaitForIdle(2000)`。超时只记「窗口已极窄」，然后继续 `CloseDevice` / `DestroyDevice`。
- 每一路若仍在抓流，在持有 `_cameraLock` 的情况下 `MV_CC_StopGrabbing_NET` 后 `Thread.Sleep(30)`，再关闭并销毁。其它线程对这把锁是短超时 `TryEnter`，这几十毫秒会让它们失败。12 路都在抓时，UI 线程在锁内累计睡眠约 360ms，再加上 SDK 调用。
- 拆完再 `WaitForIdle(2000)` 后释放缓冲；方法末尾 `Resume`，因为程序还要继续跑，可以重新打开相机。
- 这条路径不调用 `CameraController.ReleaseAllCameras`。

`Form1_FormClosing` 的最外层 `catch` 在异常时把 `e.Cancel` 设为 false。若异常发生在 `ReleaseAllCameras` 之前（`StopInspectWorkers` 内部大多已吞掉异常，抛出的概率低），窗体仍会关掉，有序拆相机不会跑完。

关主窗口时检测卡住会拒绝退出，这是对的。剩下的窗口是：托管计数已经归零之后，海康原生回调是否还在像素缓冲里，只靠 100+50+300 毫秒；以及停相机按钮在 2 秒超时后仍然拆句柄。

#### M2. 安装工程拿走的 exe 与 ini 不是同一套输出

现场影响：用 vdproj 打出来的 MSI，许可/方案 ini 来自打包机 `bin\Debug`，主程序不一定是刚编出来的 Release。`test.ini` / `code.ini` 不在 git 里，包里是谁最后在本机编译留下的那一份。客户机可能跑到调试构建，或带上打包机的试验配置。

| 工程 | 主程序 | ini |
| --- | --- | --- |
| `Setup1/Setup1.vdproj` | `WindowsFormsApplication1\obj\Release\WindowsFormsApplication1.exe`（约 7503 行） | `bin\Debug\test.ini`（约 4480 行）、`bin\Debug\code.ini`（约 6028 行） |
| `Setup2/Setup2.vdproj` | `WindowsFormsApplication1\obj\Debug\WindowsFormsApplication1.exe`（约 10132 行） | `bin\Debug\code.ini`（约 4739 行）、`bin\Debug\test.ini`（约 8964 行） |

`obj\` 下的 exe 是编译中间产物，后生成事件拷到 `bin\` 的那份才是平时双击运行的程序。Setup2 直接收 `obj\Debug`，打出来的包是 Debug 构建。

#### M3. 目标框架停在 4.6，并且打开了 JIT 调试

`WindowsFormsApplication1.csproj` 与 `GrantIniAccess.csproj` 的 `TargetFrameworkVersion` 都是 `v4.6`。`App.config` 第 9 行是 `jitDebugging="true"`，`supportedRuntime` 也是 `v4.6`。

现场影响：4.6 已退出支持，机器上不再收到该版本的安全更新。`jitDebugging="true"` 会让未处理的 UI 异常走 JIT 调试对话框，而不是稳定进入 `Program.cs` 里注册的 `Application.ThreadException`。工控机没有调试器时，操作员看到的是系统错误框或进程直接退出，`CrashMonitor` 准备的说明框和同步日志不一定按设计走完。

升框架要等 Cognex VisionPro 59.2、海康 MvCamCtrl、IOC0640 这几家的支持说明。在此之前可以单独评估关掉 `jitDebugging`，让未处理异常进现有的崩溃报告路径。本报告不建议在未确认依赖的情况下改 `TargetFrameworkVersion`。

#### M4. 失败帧整块回写在奇数 long/float、以及未配置字数时留空寄存器

`InspectionFailureOutput`（`Services/IPlcOutputWriter.cs`）：

- `ForFormat`：`int` / `long` / `float`（忽略大小写和空白）得到 `"999"`，其它格式得到 `"Reject"`。
- `RepeatForRegisters`：字符串格式只写一个值，避免按地址横扫踩到相邻块。`long` / `float` 且寄存器数为奇数时，`NotifyOddRegisters` 只告警一次，最后一个寄存器不写。注释写明该寄存器通常是完成位。
- `NotifyRegsUnset`：`block[2]` 解析不出正整数时只写第 1 个寄存器，同样只告警一次。三处调用在 `FormModbus.WriteCameraOutput`、`FormModbusRtu`、`FormOmron` 的失败分支。

现场影响：方案把完成位放在奇数长度的 long/float 块末尾时，NG 帧把数据写成 999，完成位保持上一件的值。PLC 若用完成位判断「本件已写完」，会把上一件的完成状态套到这一件上。字数没配时只有第一格是故障值，后面的格子同样是旧值。告警进日志且进程内只出现一次，产线不一定有人盯着那一行。

字符串不重复是有意的，避免写越界。缺的是「完成位单独补写」的现场约定，代码注释已把决定留给现场。测试工程没有链接 `IPlcOutputWriter.cs`，奇数寄存器和缺字数这两条没有断言。

### 低

#### L1. 切方案置位前的窄窗口，以及越界连接号无日志

`Form1.RouteNoProtoSchemeSwitch` 在接收线程上看 `_switchingScheme`，置真发生在随后 UI 线程的 `SchemeSwitchPreface` 里。两条无协议连接若在标志仍为 false 时各拿到自己的 `_noProtoSwitchLock`，会各调用一次 `xinghao_qiehuan`。后到的被 `qiehuanzhong` 的 CAS 拒绝，日志是 `xinghao_qiehuan` 的 else 分支（约 13299 行）那句不限流的「切方案请求被忽略：已有切换在途」。不会双开卸载。几十秒的卸载窗口已经由 `_switchingScheme` 盖住。

`sourceLink < 0` 或 `>= 16` 时该方法直接 `return`（约 9667 行），没有日志。现有连接号是 1–4，数组长度 16，正常配置走不到。

`Form3.DispatchReceivedFrame` 的注释仍写「由 Form1.timer10 完成切型」。连接 2–4 的实现已经改为 `RaiseSchemeSwitch`，注释和代码不一致，不影响运行。

#### L2. NG 预览失败被吃掉，且整文件读入没有大小上限

`NgImagePreviewForm.ShowPreview`（约 94 行）和 `ShowPreviewCore`（约 116 行）仍是空 `catch`。解码失败会写到信息条；窗体创建或 `Show` 失败则什么都不留。`DecodeInBackground` 对路径做 `File.ReadAllBytes`，异常大的文件会在线程池上占用内存。第 63 轮提交说明写明没有实机连点：台架仍要确认「正在读取…」会被图替换，以及连点两张时旧图不盖新图。序号丢弃和 `BeginInvoke` 失败时释放位图，静态看是对齐的。

#### L3. 根目录不可达只进启动日志；死 UNC 会拖住初始化线程

`Form1.RootReachNote`（约 557–568 行）在 `InitializeJobManager` 的后台线程里对存图根做 `Directory.Exists`。统计根与存图根不同才再探一次。盘不在、目录未建、无权限都写成「当前访问不到」，异常降级成「可达性未确认」。主界面没有对应提示。

`Directory.Exists` 对「目录还没建」也返回 false。把根配到尚不存在的文件夹时，启动日志会写访问不到，即使稍后存图会自己建子目录。默认 `E:\` 在盘存在时是「可达」。

死掉的 UNC 会让 `Directory.Exists` 在这条后台线程上等到 SMB 超时。界面消息泵还在，相机和方案初始化会跟着晚。

#### L4. 统计目录名用了当前区域的标准日期格式

`RunLog.CreateDirectoryCsvPath` / `CreateCsvPath` / `WriteDate` 用 `DateTime.ToString("Y")` 和 `ToString("m")`。在 zh-CN 下分别是「2026年10月」和「10月19日」这一类，测试按这个口径锁定，并且不再依赖 E 盘。格式是 .NET 的标准说明符，随系统区域变化。工控机区域一直是中文时，目录名稳定。若有人把系统区域改成 en-US，新的 CSV 会进另一套文件夹名，旧月份的统计看起来像断档。这是区域约定，不是算错良率。

#### L5. 多连接 `PollLoop` 已写好，但没有任何调用方把它启动起来

`ModbusTcpRuntime.PollLoop`、`ModbusRtuRuntime.PollLoop`、`FinsRuntime.PollLoop` 在 `Context != null` 时由 `Start` 拉起。注释写明：这条循环里的读写与 `OnReconnect` 不共锁；连接 1 的 `Fins_duxie` 才每次持 `_ioSync`。直接复用宿主的 `_ioSync` 会锁错对象。

能把 `Context` 设上并 `Start` 的入口是 `FormModbus.ApplyExtraLinkRuntime`、`FormModbusRtu.ApplyExtraLinkRuntime`，以及 `FormOmron.CreateLinkRuntime`（连接 2 及以上会 `new FinsLinkContext`）。这三处在当前工程里都没有调用方。现场的连接 2–4 是 `FormCommManager` 登记的独立子窗，各自跑自己的 `Fins_duxie` 和自己的 `_ioSync`。

因此这不是现在产线上的并发写。它是一条接上就会和重连抢同一个套接字的未完成路径。`FormOmron.ReloadFinsLinks` 只清理历史上可能残留的运行时，不创建新的 `PollLoop`。

#### L6. 结果队列在实现上仍会丢最旧的任务

`CameraWorkQueue.Enqueue` 在容量满时 `TryTake` 掉最旧的一项再放入新任务，并用 5 秒限流记日志。IO 脉冲走容量 4，注释写明可丢，PLC 超时判 NG，这是设计。

结果回写走容量 64 的 `_cameraResultWork`。`Form1` 里的注释写「不丢结果」。类本身没有「满则等待或拒绝」的模式，64 格满了同样丢最旧。PLC 写阻塞、心跳和结果叠在同一路工人线程上时，较早一帧的回写会被丢掉，后一帧的 OK 仍可能写出去。PLC 若按件超时，丢掉的那件会判 NG；若完成位协议把后一帧当成前一件的补写，就会错件。正常节拍下 64 格很难满，风险在 PLC 半死、写操作卡住的时候。`Dispose` 最多 `Join(2000)`，超时后工人线程仍是后台线程，进程退出时不一定把队列写完。

#### L7. 托管 CI 与本机验收的缺口

`.github/workflows/ci.yml` 在 `push` 和 `pull_request` 上都跑 `dotnet test`，没有 `permissions`、`concurrency` 和 job 超时。同一提交进 PR 时会跑两遍。注释里的 178 与测试工程里的 `[TestMethod]` 数量一致。

`verify.ps1` 的 `Find-MSBuild` 在 vswhere 失败后落到 `D:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe`。换机器或 Community/BuildTools 安装时，这条兜底会找不到编译器。

检测回归（`tests\Run-InspectionRegression.ps1`，反射加载主程序）因为 Cognex 绝对路径，进不了托管 CI。它在完整的 `verify.ps1` 第 3 步里。`-SkipMainBuild` 会跳过并说明原因。

测试编译集没有 `Form1`、`CameraController`、`NgImagePreviewForm`、切方案锁的生存期，也没有 `IPlcOutputWriter`。第 63 轮改的三处因此没有自动化断言，证据是编译、静态顺序和提交说明里的回归记录。

#### L8. 菜单上的三菱演示窗、OK 帧缺端子时发出「无」、以及仓库里的死代码和杂物

- `FormMelsecSerial` 由 `CommunicationService` 创建，菜单「三菱Fx编程口」（`Form1.Camera58Events` 约 1855 行）把它显示出来。窗体标题在英文模式下是 “Melsec Read PLC Demo”，上面是任意地址的读/写。它不参加 `getrecord` 的结果回写。操作员打开后能对串口 PLC 写线圈和寄存器。若产线不用这条口，它只是多出来的入口；若现场把它当成检测通道，结果不会从检测线程写回去。
- OK 帧读取 VisionPro 输出端子抛错时，TCP/串口/FINS/Modbus 的待发字符串被设成 `"无"`（`Form1.getrecord` 一带，约 5891、5910、5932、5943、5954 行），并记日志。NG 帧在三元表达式里直接取 `"Reject"`，不会进这个 `catch`。缺端子的合格帧会把字面量「无」写给 PLC，而不是故障码。PLC 不认这个字符串时通常超时判 NG（偏安全）；若字符串通道把「无」当成有效文本，这一件的记录会对不上。
- 根目录的 `Form6.cs` 与 `WindowsFormsApplication1/Form6.cs` 并存。`新建文本文档.txt` 里是相机型号和序列号一类文本。`Menu.ini` 指向本机客户目录下的 `.vpp`。`reasonix.toml` 在仓库根。这些都不参与检测，但是公开仓库里的杂物。
- `SerialPortGuard.ScanIniOwners` 的注释已经说明：无协议连接 1 的串口不在 ini 扫描里，只在运行时占用。保存配置时的冲突提示因此看不到连接 1 已经打开的 COM。运行时 `TryAcquire` 仍会拒绝第二次打开。

## 详细结论

整体可以在装有 VisionPro 和海康 SDK 的工控机上作为产线程序使用。检测主链、连接 1 的通讯锁、切方案的全局互斥、失败帧的数值故障码、存图根可配置、崩溃落盘，这几块在第 63 轮之后是对齐的。本轮没有发现会在正常节拍下稳定漏检、双开卸载或把结果写错连接的缺陷。没有需要立刻停线的严重或高问题。

扎实的部分：单实例和崩溃报告；相机回调与 `getrecord` 的进入/退出成对；标题栏关闭先等最多 10 秒，等不到就取消关闭而不是硬拆；停相机先关门再拆，只是超时后仍继续拆；切方案用 `_switchingScheme` 盖住几十秒卸载，无协议与 FINS 的连接 2–4 都回到同一入口且不改写连接 1 的路径；连接 1 的读/写/重连/心跳同一把锁，手动框不在锁内弹窗；结果与 IO 分队列；路径与帧账本有 178 个单测，检测回归挂在本机 `verify.ps1` 上。NG 预览不再在 UI 线程上整文件解码。

建议优先处理的事项（均不在忽略清单里）：

1. **安装包收口（M2）。** Setup1 改收 `bin\Release` 的 exe，并与将要随包发出的 `code.ini` / `test.ini` 放在同一配置。Setup2 现在收的是 `obj\Debug` 的 exe，若这份安装包还会发给客户，应改成与 Setup1 同一套 Release 输出，或停止用它发版。ini 不入库，发版前要明确「包里的 ini 从哪台机器的哪一次编译来」。
2. **关相机的等待口径（M1）。** 停相机在 `WaitForIdle` 超时后仍销毁设备，并且在 `_cameraLock` 内 `Sleep`。标题栏路径在托管计数归零后仍靠 450ms 睡眠覆盖海康原生回调。两处可以共用「先停流、等待回调静止、再关设备、最后释放缓冲」的顺序，睡眠留在锁外。`FormClosing` 的最外层异常不要在资源还没释放时把关闭标成允许。
3. **失败帧的完成位（M4）。** 和现场确认 long/float 奇数块的最后一格是不是完成位。若是，NG 时除了数据格写 999，完成位要按约定单独写一次；字数未配置时应在界面上能看见，而不是进程内只记一行日志。这两条适合补进测试工程（`IPlcOutputWriter.cs` 只依赖 BCL 和日志接口）。
4. **崩溃对话框（M3）。** 在不升框架的前提下，评估 `App.config` 的 `jitDebugging`。目标是未处理 UI 异常进 `Program` 里已有的同步日志和崩溃报告。框架版本等 VisionPro / 海康 / IO 卡的支持范围再动。

可以排后、但值得知道的残留：

- 两条无协议连接抢在 `_switchingScheme` 置真之前时，后到的请求打一条不限流日志，不会双开卸载（L1）。
- NG 预览的空 `catch` 和整文件读入，以及第 63 轮未做的连点台架（L2）。
- 根目录不可达只在启动日志里，死 UNC 拖初始化线程（L3）。
- 统计月份文件夹随系统区域变化（L4）。
- 没人调用的 `PollLoop` 一旦接上，会在没有每连接 I/O 锁的情况下和重连并发（L5）。现在的连接 2–4 不走这条路径。
- PLC 写长时间阻塞时，容量 64 的结果队列仍会丢最旧一帧（L6）。
- CI 没有超时和并发取消；检测回归只在本机 `verify.ps1`；`verify.ps1` 的 MSBuild 兜底写死 D 盘 Professional（L7）。
- 三菱编程口演示窗能手工写 PLC；OK 帧缺输出端子时发出「无」；仓库根有游离 `Form6.cs` 和本机路径（L8）。

已接受、不在本轮行动项里的风险：`Form5` 口令维持产品选择，发版前再收；`FormMES` / `FormMySQL` 维持测试工具，只在打包时决定是否带进客户安装包；主窗体体积、Everyone 完全控制、授权失败只记日志，按所有者决定保持原样。
