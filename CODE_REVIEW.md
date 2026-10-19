# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `98e0398`（2026-10-19，「回审落地：关 jitDebugging / 安装包统一 Release 输出 / 两条关相机路径」）。上一轮基线是 `075ed66`（版本号仍写着 `2026.10.18+5ad62de`，这次提交没有改 `BuildInfo`）。相对上一轮只有这一个代码提交，改了 4 个文件：`App.config`、`Form1.cs`、`Setup1.vdproj`、`Setup2.vdproj`。本次只更新本报告，不改应用源码。提交说明里的「主工程 Release 真编译通过、dotnet test 178/178」不是这次复审重跑的结果；vdproj 与相机关闭没有在本环境实机验证。

没有严重项，也没有高项。

检测主链、连接 1 的通讯锁、切方案全局互斥、崩溃落盘，与上一轮相同，这一个提交没有碰它们。`jitDebugging` 已关掉。两份安装包里的 Debug 输出路径已经清掉，都改指向 Release。关相机只改到一半：停相机按钮的睡眠移出了锁；标题栏真正拆设备的函数没有改，改的是窗体 `Dispose` 里的兜底清理。

失败帧完成位的代码与测试都没有动。状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** 口径没定之前，不要按「必须把完成位写满」去改。

按所有者决定忽略，不列入开放问题、也不建议修改：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess` 对 Everyone 的完全控制；授权失败只打日志。`Form5` 口令、`FormMES` / `FormMySQL` 维持原产品选择。

## 已修复 / 仍开放

| 上一轮条目 | 状态 | 这次看到的代码 |
| --- | --- | --- |
| 中：关相机两条路径。先停流、等回调停稳再销毁；睡眠移出锁外 | 部分已修 | 停相机 `bnClose_Click`：`Thread.Sleep(30)` 已挪到 `_cameraLock` 外面（约 14351–14368 行），顺序是持锁停流、锁外等 30ms、再持锁关闭并销毁。`WaitForIdle(2000)` 超时后仍继续拆设备（约 14333–14334 行），这一点没改。标题栏 `Form1_FormClosing` 仍调用 `ReleaseAllCameras()`（约 8483 行），里面仍是 `CameraController.ReleaseAllCameras(100, 50)` 再在销毁之后 `Sleep(300)`（约 8513–8516 行）。提交改的是 `Dispose` → `SafeCleanupBeforeDispose`（约 8348 行）：由 `ReleaseAllCameras(0, 0)` 之后盲等 500ms，改成 `ReleaseAllCameras(500, 100)`，等待发生在停流/关设备之后、销毁之前。同文件注释写明相机已在 `FormClosing` → `ReleaseAllCameras()` 里释放，正常关窗时这 600ms 作用在已经清空的槽位上 |
| 中：安装包 Setup1/Setup2 混用 Debug | 已修配置代次；exe 目录仍是残留 | 两个 vdproj 的 `test.ini` / `code.ini` / `biao.ico` 都改为 `bin\Release`。Setup2 的 `Menu.ini` 同样改为 `bin\Release`，主程序由 `obj\Debug` 改为 `obj\Release`。应用文件路径里已没有 `bin\Debug` 或 `obj\Debug`。两包主程序仍是 `obj\Release\WindowsFormsApplication1.exe`（Setup1 约 7503 行，Setup2 约 10132 行），ini/图标在 `bin\Release`。vdproj 的 Debug/Release 配置节点是安装项目自身的配置，不是在收调试版程序 |
| 中：`jitDebugging="true"`，以及 .NET 4.6 | `jitDebugging` 已修；框架按上次建议未升 | `App.config` 第 13 行 `jitDebugging="false"`。`TargetFrameworkVersion` 仍是 `v4.6`，`supportedRuntime` 仍是 `v4.6` |
| 中：失败帧完成位（奇数 long/float 不写最后一格；字数未配只写第一格；测试未链接） | 仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关 | `Services/IPlcOutputWriter.cs` 的 `RepeatForRegisters` / `NotifyOddRegisters` / `NotifyRegsUnset` 与 `075ed66` 相同。测试工程仍未编译该文件 |
| 低：切方案置位前窄窗口、越界连接号无日志 | 未改 | 不在本提交里 |
| 低：NG 预览空 `catch`、整文件读入 | 未改 | 提交说明写明未做 |
| 低：根目录不可达只进日志；死 UNC 拖初始化 | 未改 | 不在本提交里 |
| 低：统计目录名随系统区域 | 未改 | 不在本提交里 |
| 低：无人调用的 `PollLoop` | 未改 | 不在本提交里 |
| 低：结果队列满仍丢最旧 | 未改 | 不在本提交里 |
| 低：CI 无超时/并发取消；`verify.ps1` 的 MSBuild 兜底写死 D 盘 | 未改 | 不在本提交里 |
| 低：三菱编程口演示窗；OK 帧缺端子发出「无」；仓库杂物 | 未改 | 不在本提交里 |

没有回退。没有把 `jitDebugging` 重新打开，也没有把安装包改回 Debug。

## 中

### 关相机：睡眠出锁已落地，两条路径仍不一致，超时后仍拆

现场影响：点「关闭相机」时，若 2 秒内回调没排空，代码仍会关掉并销毁句柄，海康回调可能打到已销毁设备。标题栏关闭的第一次拆设备仍用 100ms + 50ms 的固定等待，不是这次改长的 500ms + 100ms。

停相机按钮已经按「睡眠不占锁」改完。`bnClose_Click` 在 `StopAccepting` 之后 `WaitForIdle(2000)`，超时只记「窗口已极窄」然后进入循环。每一路在抓流时：持 `_cameraLock` 调用 `MV_CC_StopGrabbing_NET`，放开锁，`Sleep(30)`，再持锁 `CloseDevice` / `DestroyDevice`。重连和打开相机的入口在拆除期间是禁用的。其它线程不会再被这 30ms 按在锁里。超时后继续拆，以及拆完 `Resume`（程序还要重新开相机），都保持原样。

标题栏关闭的顺序没变。`Form1_FormClosing` 确认关闭后 `StopInspectWorkers`（含最多 10 秒的 `WaitForIdle`，等不到就取消关闭、不拆相机），然后 `ReleaseAllCameras()`：`ReleaseAllCameras(100, 50)` 在停流后等 100ms、关设备后等 50ms、再销毁，接着再 `Sleep(300)` 才释放缓冲。`CameraController` 的三阶段等待本来就在销毁之前；这次没有把 100/50 改成与兜底路径相同的 500/100。

`Dispose` 里的 `SafeCleanupBeforeDispose` 才是这次改等待顺序的地方。正常关窗时，`FormClosing` 已经把 12 路槽位置空，随后 `Application.Exit` 再次进入 `FormClosing` 会因 `_isFormClosing` 直接返回，然后才 `Dispose`。兜底函数对空槽仍会执行 `Sleep(500)` 和 `Sleep(100)`（`CameraController.ReleaseAllCameras` 只要毫秒数大于 0 就睡眠，不看有没有相机）。这 600ms 加在退出尾部，保护的是「`FormClosing` 没跑到 `ReleaseAllCameras`」的情况，例如关闭过程抛异常后最外层 `catch` 把 `e.Cancel` 设为 false、窗体仍被关掉。那条异常路径上，等待从「销毁之后」挪到了「销毁之前」，这是实改。

两条日常路径（标题栏、停相机按钮）没有收成同一个「停流 → 等到回调计数归零 → 再销毁」的函数。停相机超时后仍拆。提交说明也写了这两处没有真机验证。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。**

`InspectionFailureOutput.RepeatForRegisters` 的行为与上一轮相同：`int` / `long` / `float` 故障值是 `999`；字符串格式不重复；`long` / `float` 寄存器数为奇数时最后一格不写，`NotifyOddRegisters` 只记一次；`block[2]` 不是正整数时 `NotifyRegsUnset` 只记一次，只写第 1 格。调用点仍在 `FormModbus.WriteCameraOutput`、`FormModbusRtu`、`FormOmron` 的失败分支。测试工程没有链接 `IPlcOutputWriter.cs`。

这不是本提交的遗漏。所有者还在理解现场约定。在约定写成「某一格必须由软件写满」之前，保持现在的「不越块尾、只告警」是符合当前说明的。

## 低

### 安装包：Release 已对齐，exe 仍取 obj

两份 vdproj 不再把 `bin\Debug` 的 ini 或 `obj\Debug` 的 exe 打进包。Setup1 与 Setup2 的主程序都是 `obj\Release\WindowsFormsApplication1.exe`，`code.ini` / `test.ini` / `biao.ico` 都是 `bin\Release`（Setup2 的 `Menu.ini` 也是）。

`obj\Release` 是编译中间目录，平时双击运行的是后生成事件拷到 `bin\Release` 的那份。没有额外改写 exe 时，两个目录里的主程序应是同一次编译的同一份文件。ini 仍然不在 git 里，包里是打包机 `bin\Release` 当时留下的那一份。vdproj 需要在装了安装项目组件的 Visual Studio 里重新生成才能确认 MSI，本环境没有做这一步。

### 框架仍是 4.6

`jitDebugging` 已关，未处理 UI 异常回到 `Program.cs` 里的 `Application.ThreadException` / `AppDomain.UnhandledException`。目标框架没动，与「等 Cognex / 海康 / IO 卡再升」一致。4.6 本身仍无安全更新，这是已知等待，不是这次回退。

版本字符串仍是 `2026.10.18+5ad62de`。运行中的关于页不会显示 `98e0398`。

### 上一轮低项均未改

本提交没有碰到这些文件。行为与上一轮报告相同，这里只保留现场含义：

- 两条无协议连接若抢在 `_switchingScheme` 置真之前各走一次 `xinghao_qiehuan`，后到的被 `qiehuanzhong` 挡住，打一条不限流的「切方案请求被忽略」。不会双开卸载。`sourceLink` 越界（约 9667 行）仍直接返回。
- `NgImagePreviewForm.ShowPreview` / `ShowPreviewCore` 仍是空 `catch`；解码仍 `File.ReadAllBytes` 整文件。第 63 轮的连点台架提交说明里就没做，这次也没有补。
- `RootReachNote` 只写启动日志。目录未建也会写成「访问不到」。死 UNC 会让 `InitializeJobManager` 那条后台线程等到 SMB 超时，相机初始化跟着晚。
- `RunLog` 的月份、日期文件夹用 `ToString("Y")` / `ToString("m")`，随系统区域变化。中文区域下与现有测试一致。
- `ModbusTcpRuntime.PollLoop`（以及 RTU、FINS 的对应循环）仍没有调用方。连接 2–4 仍是子窗自己的 `_ioSync`。把 `ApplyExtraLinkRuntime` / `CreateLinkRuntime` 接上之前，读、写、重连还没有每连接的同一把锁。
- `CameraWorkQueue` 满时仍丢最旧。结果队列容量 64，注释写「不丢」，实现会丢。风险在 PLC 写长时间阻塞时。
- `ci.yml` 仍无 `permissions` / `concurrency` / 超时。`verify.ps1` 的 MSBuild 兜底仍是 D 盘 Professional。检测回归仍只在本机 `verify.ps1` 第 3 步。178 个单测的编译集仍不含 `Form1`、`CameraController`、`NgImagePreviewForm`、`IPlcOutputWriter`。
- 菜单「三菱Fx编程口」仍打开可手工读写的 `FormMelsecSerial`。OK 帧读输出端子抛错时，待发字符串仍是「无」（NG 帧仍是 `Reject`）。仓库根的游离 `Form6.cs`、`新建文本文档.txt`、`Menu.ini` 本机路径、`reasonix.toml` 还在。

## 建议下一步

1. **关相机收成一条顺序。** 标题栏实际拆设备的 `Form1.ReleaseAllCameras` 与停相机按钮对齐：停流，等到 `InspectionLifecycle` 的在途计数归零，再关闭并销毁，睡眠放在锁外。`bnClose_Click` 在 `WaitForIdle` 超时后改为停止拆除并提示，不要继续 `DestroyDevice`。`SafeCleanupBeforeDispose` 的 500/100 留作 `FormClosing` 没拆完时的兜底即可；正常关窗不必在空槽上再睡 600ms。上机看标题栏关闭和按钮关相机各一次。
2. **安装包 exe 改收 `bin\Release`。** 与 ini、图标同一目录、同一次输出。发版前用 Visual Studio 重新生成安装项目。ini 仍不入库，包里用哪一台机器的 `bin\Release` 要在发版记录里写明。
3. **完成位先不动。** 状态是所有者澄清中：客户端自定写寄存器，与客户免编程无关。约定写成具体格子之前，保持现在的不越块尾。约定定了再改 `RepeatForRegisters`，并把 `IPlcOutputWriter.cs` 链进测试工程。
4. **框架继续等 SDK。** `jitDebugging` 已关。上机故意制造一次未处理 UI 异常，确认弹出的是现有崩溃报告而不是调试器对话框。`BuildInfo` 仍指向 `5ad62de`，下次发版按原规矩补上 `98e0398`。
5. **低项不挡这一轮。** 切方案窄窗口、NG 预览、根目录日志、区域格式、休眠的 `PollLoop`、结果队列丢旧、CI 卫生、三菱演示窗，都没有被这个提交改到，也没有新的证据表明它们在正常节拍下会漏检。
