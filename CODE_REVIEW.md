# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `1569aae`（2026-10-19，「关相机三条路径收成停流→等回调归零→再销毁；关相机超时不再继续销毁；安装包 exe 改收 bin\Release」）。上一轮基线是 `98e0398`。中间只有这一个提交，改了 `Form1.cs`、`Setup1.vdproj`、`Setup2.vdproj`。`BuildInfo` 仍是 `2026.10.18+5ad62de`。本次只更新本报告，不改应用源码。提交说明里的 Release 编译和 178 个单测不是这次重跑的结果；关相机和 vdproj 没有在本环境实机验证。

没有严重项，也没有高项。

上一轮点名的三件事里，安装包 exe 已改到 `bin\Release`，`jitDebugging` 仍是 `false`。关相机的主顺序已经收成「先等托管回调归零，再停流、等待、销毁」；停相机按钮超时不再强拆。新的缺口在中止之后：关闭按钮保持禁用，自动重连和 `timer2` 保持关闭，对话框却让现场重试关闭。

失败帧完成位的代码与测试都没有动。状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。**

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

## 已修复 / 仍开放

| 上一轮条目 | 状态 | 这次看到的代码 |
| --- | --- | --- |
| 关相机：两条路径收成「停流 → 回调归零 → 再销毁」，超时不再强拆 | 顺序已收口；停相机超时不再强拆；中止后的按钮和重连未恢复 | 标题栏 `Form1.ReleaseAllCameras`（约 8517–8523 行）先 `StopAccepting` + `WaitForIdle(2000)`，再 `CameraController.ReleaseAllCameras(500, 100)`。销毁后的 `Sleep(300)` 已删除。`Dispose` 里的 `SafeCleanupBeforeDispose`（约 8348–8351 行）同一顺序。停相机 `bnClose_Click`（约 14340–14348 行）超时则记日志、弹窗并 `return`，不执行后面的 `CloseDevice` / `DestroyDevice`。退出路径的 2 秒超时仍继续销毁，提交说明写明是有意的。中止分支没有把 `bnClose`、`_cameraReconnectEnabled`、`timer2` 恢复到点击前 |
| 安装包 exe 改收 `bin\Release` | 已修 | Setup1 约 7503 行、Setup2 约 10132 行都是 `bin\Release\WindowsFormsApplication1.exe`。`code.ini` / `test.ini` / `biao.ico` 仍是 `bin\Release`（Setup2 的 `Menu.ini` 也是）。两包应用文件路径里已没有 `obj\` 或 `bin\Debug` |
| `jitDebugging` | 保持已修 | `App.config` 第 13 行仍是 `false`。目标框架仍是 `v4.6` |
| 失败帧完成位 | 仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关 | `Services/IPlcOutputWriter.cs` 不在本提交里。测试工程仍未链接 |
| 上一轮低项（切方案窄窗口、NG 预览、根目录日志、区域格式、休眠 `PollLoop`、结果队列丢旧、CI、三菱演示窗、缺端子发「无」、仓库杂物） | 未改 | 本提交没有碰到这些文件 |

没有回退。没有把安装包改回 `obj`，也没有重新打开 `jitDebugging`。

## 中

### 停相机超时中止后，界面停在「相机已关」的按钮态，相机其实还开着

现场影响：回调 2 秒内没排空时，软件不再拆句柄，这是对的。弹窗写「未断开相机，请稍后重试」。此时「关闭相机」是灰的，自动重连是关的，操作员按提示再点关闭是点不到的。

`bnClose_Click` 一进来就把 `_cameraReconnectEnabled` 设为 false、停掉 `timer2`、禁用 `bnClose`，并禁用打开/保存/设置。这些是为了防止拆除过程中重开相机。超时 `return` 在 `try` 里，`finally` 会跑：`Resume` 放行回调，并重新启用 `bnOpen` 和那几个菜单。`finally` 不恢复 `bnClose`，也不恢复重连开关和 `timer2`。

`return` 之后，方法后半段那组按钮赋值不会执行。成功关掉相机时，那里把 `bnClose.Enabled` 设为 false（约 14461 行），因为设备已经关了，要等下次打开成功才把关闭按钮设回 true（`EnableAllConnectedCameraControls`，约 9014 行；打开流程约 13685 行）。中止路径设备没关，却留下和「已经关完」一样的关闭按钮，同时把「打开设备」设成可点。

重连要等 `EnableCameraReconnect`（约 8963 行，方案已加载且点击运行之后）才会重新打开。中止发生在正在检测、相机仍在出图时，断线不会再自动重连，直到有人再走一遍运行或打开。

标题栏关闭不是这条缺口。`Form1_FormClosing` 仍先 `StopInspectWorkers`：最多等 10 秒，等不到就取消关闭、不拆相机。等得到之后，`ReleaseAllCameras` 再等 2 秒；这次超时仍继续 `ReleaseAllCameras(500, 100)`。提交说明认为进程即将结束，退出路径中止没有意义。`FormClosing` 在 10 秒那一步已经把「检测还没停」挡在拆设备之前，后一段 2 秒超时继续销毁，只覆盖「10 秒时已经归零、随后又有人进了检测门」的窗口。`SafeCleanupBeforeDispose` 同样超时仍销毁。正常关窗时标题栏已经把槽位清空，`Dispose` 里对空槽仍会 `Sleep(500)` + `Sleep(100)`。

停相机在第一次等待成功、设备已销毁之后，还有一次 `WaitForIdle(2000)`（约 14396 行）。这次超时仍 `FreeHGlobal`。第一次已经归零且 `StopAccepting` 还关着，新的 `ImageCallBack` 进不了门，这条很难再撞上。

## 低

### 安装包路径已对齐，包里的 ini 仍是打包机上的那一份

两份 vdproj 的主程序、`code.ini`、`test.ini`、图标都指向 `bin\Release`。和上一轮相比，exe 不再取 `obj\Release`。ini 仍不在 git 里，MSI 里是生成安装项目时 `bin\Release` 目录里实际存在的文件。vdproj 需要在装了安装项目组件的 Visual Studio 里重新生成，本环境没有做。

### 框架与版本号

`jitDebugging="false"` 仍在。`TargetFrameworkVersion` 仍是 `v4.6`，继续等 Cognex / 海康 / IO 卡。`BuildInfo` 仍指向 `5ad62de`，关于页不会显示 `98e0398` 或 `1569aae`。

### 上一轮低项没有变化

- 两条无协议连接抢在 `_switchingScheme` 置真之前，后到的被 `qiehuanzhong` 挡住，打一条不限流日志，不会双开卸载。`sourceLink` 越界仍直接返回。
- `NgImagePreviewForm` 的 `ShowPreview` / `ShowPreviewCore` 仍是空 `catch`，解码仍整文件 `ReadAllBytes`。
- `RootReachNote` 只写启动日志。目录未建也会写成访问不到。死 UNC 会拖住 `InitializeJobManager` 那条后台线程。
- `RunLog` 的月份、日期文件夹随系统区域变化。中文区域下与现有测试一致。
- `PollLoop` 仍没有调用方。连接 2–4 仍用子窗自己的 `_ioSync`。
- 结果队列容量 64，满时仍丢最旧。风险在 PLC 写长时间阻塞时。
- `ci.yml` 仍无超时和并发取消。`verify.ps1` 的 MSBuild 兜底仍是 D 盘 Professional。178 个单测仍不含 `Form1`、`CameraController`、`NgImagePreviewForm`、`IPlcOutputWriter`。
- 菜单「三菱Fx编程口」仍是可手工读写的演示窗。OK 帧缺输出端子时待发字符串仍是「无」。仓库根的游离 `Form6.cs`、`新建文本文档.txt`、`Menu.ini` 本机路径、`reasonix.toml` 还在。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。**

`RepeatForRegisters` 仍是：数值故障值 `999`；字符串不重复；奇数 `long` / `float` 不写最后一格并只告警一次；字数未配只写第 1 格并只告警一次。约定写成具体格子之前不要改。

## 建议下一步

1. **补停相机的中止恢复。** `WaitForIdle` 超时 `return` 之前，把 `bnClose` 设回 true，并把 `_cameraReconnectEnabled` 和 `timer2` 恢复成进入 `bnClose_Click` 之前的值。`finally` 里不要把「打开设备」留成可点、同时「关闭相机」留成灰色。弹窗里的「稍后重试」才能点到同一个按钮。上机看一次超时（或临时把等待改短）确认相机仍开、关闭按钮可再点、断线重连仍在。
2. **退出路径保持现在的「超时仍销毁」。** 标题栏在 `StopInspectWorkers` 的 10 秒失败时已经取消关闭。不必再让 `ReleaseAllCameras` 的 2 秒超时中止退出。若要少等，可让 `CameraController.ReleaseAllCameras` 在 12 路都是空槽时跳过 `Sleep`，避免 `Dispose` 在已经拆完的相机上再睡 600ms。
3. **安装包用 Visual Studio 重新生成一次。** 确认 MSI 里的 exe 和 ini 都来自同一次 `bin\Release`。ini 不入库，发版记录里写明用的是哪台机器的输出。
4. **完成位继续不动。** 口径是客户端自定写寄存器，与客户免编程无关。
5. **发版时把 `BuildInfo` 补到 `1569aae`。** 低项不挡这一轮。
