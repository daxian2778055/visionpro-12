# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `9510fc5`（2026-10-19，「停相机中止时打开设备保持不可点」）。上一轮基线是 `4c87c5b`。中间只有这一个提交，只改了 `Form1.bnClose_Click` 的中止标记和 `finally`。`BuildInfo` 仍是 `2026.10.18+5ad62de`。本次只更新本报告，不改应用源码。提交说明里的 Release 编译和 178 个单测不是这次重跑的结果。

没有严重项，也没有高项，也没有中项。

上一轮留下的低项已经落地：停相机超时没拆成相机时，「打开设备」保持不可点，「关闭相机」可再点。成功拆除的路径仍会重新启用打开按钮，并把关闭按钮设灰。安装包仍收 `bin\Release`，`jitDebugging` 仍是 `false`。失败帧完成位没有动。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

## 已修复 / 仍开放

| 上一轮条目 | 状态 | 这次看到的代码 |
| --- | --- | --- |
| 停相机超时中止后「打开设备」仍可点 | 已修 | `StopAccepting` 之后声明 `abortTearDown`（约 14341 行）。超时分支在回填重连、`timer2`、`bnClose` 之前把它设为 true（约 14357 行）。`finally` 只有 `!abortTearDown` 才把 `bnOpen.Enabled` 设为 true（约 14453 行）。方法开头已经把 `bnOpen` 设成 false，中止又 `return`，后面那段成功路径的 `bnOpen.Enabled = true`（约 14477 行）不会执行 |
| 关相机顺序与超时策略 | 保持 | 停相机第一次 `WaitForIdle(2000)` 超时仍不 `CloseDevice` / `DestroyDevice`。标题栏 `ReleaseAllCameras` 与 `Dispose` 兜底仍是先等回调，再 `ReleaseAllCameras(500, 100)`，超时仍继续销毁 |
| 安装包 | 保持 `bin\Release` | Setup1 约 7503 行、Setup2 约 10132 行仍是 `bin\Release\WindowsFormsApplication1.exe`，ini 与图标同目录。本提交没改 vdproj |
| `jitDebugging` | 保持已关 | `App.config` 第 13 行仍是 `false`。目标框架仍是 `v4.6` |
| 失败帧完成位 | 仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关 | `IPlcOutputWriter.cs` 不在本提交里 |
| 其余低项 | 未改 | 本提交只动了 `bnClose_Click` 的中止标记 |

没有回退。`abortTearDown` 的初值是 false，只有超时分支把它设为 true。拆除过程中抛异常时仍会重新启用「打开设备」，这是原来的幸存路径，不是这次中止。

## 中

无。

中止之后的按钮对与「相机仍开着」一致：关闭可点，打开不可点。菜单「打开」恢复的是选 `.vpp` 方案（`打开ToolStripMenuItem_Click`，约 11892 行），不是打开相机。

## 低

### 读对话框时重连定时器可能已经恢复

回填 `_cameraReconnectEnabled` 和 `timer2` 仍在 `MessageBox` 之前（约 14358–14361 行）。`timer2` 间隔 400ms。对话框消息泵期间，仍连接且没有 SDK 断连标志的相机会在 `timer2_Tick` 里直接返回；已经报断线的槽可能在操作员点确定之前开始重连。检测门要等 `finally` 的 `Resume` 才重新接收回调。这不改变「关闭可再点、打开保持灰色」。

### 退出路径、安装包、框架

标题栏在 `StopInspectWorkers` 等满 10 秒仍不空闲时取消关闭。其后 2 秒超时仍继续销毁，`Dispose` 兜底相同。空槽上 `ReleaseAllCameras(500, 100)` 仍会睡眠。停相机在设备已经销毁之后的第二次 `WaitForIdle` 超时仍释放缓冲。

两份安装包的主程序和 ini 仍指向 `bin\Release`。ini 不在 git 里。`jitDebugging` 仍关。`BuildInfo` 仍指向 `5ad62de`，关于页不会显示 `9510fc5`。

### 上一轮其余低项没有变化

切方案置位前的窄窗口、NG 预览空 `catch` 与整文件读入、根目录只进日志、统计目录随区域变化、无人调用的 `PollLoop`、结果队列满丢最旧、CI 无超时、三菱编程口演示窗、OK 帧缺端子发出「无」、仓库根杂物，都没有被这个提交改到。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `RepeatForRegisters` 仍不重复字符串、奇数 `long` / `float` 不写最后一格、字数未配只写第 1 格，各只告警一次。测试工程仍未链接该文件。约定写成具体格子之前不要改。

## 建议下一步

1. **这一轮相机按钮不用再改。** 上机看一次停相机超时：关闭可再点，打开设备是灰的，相机没有被拆。若希望读对话框时不要启动重连，把重连和 `timer2` 的回填挪到 `MessageBox` 之后、`return` 之前。
2. **安装包用 Visual Studio 重新生成一次**，确认 MSI 里的 exe 和 ini 来自同一次 `bin\Release`。
3. **完成位继续不动。** 发版时把 `BuildInfo` 补到 `9510fc5`。其余低项不挡这一轮。
