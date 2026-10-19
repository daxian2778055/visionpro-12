# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `4c87c5b`（2026-10-19，「停相机超时中止后复原界面：快照并回填 bnClose / 自动重连 / timer2」）。上一轮基线是 `1569aae`。中间只有这一个提交，只改了 `Form1.bnClose_Click`，增加 11 行。`BuildInfo` 仍是 `2026.10.18+5ad62de`。本次只更新本报告，不改应用源码。提交说明里的 Release 编译和 178 个单测不是这次重跑的结果。

没有严重项，也没有高项，也没有新的中项。

上一轮指出的中止半状态已经按进入前的快照回填：超时不再拆相机，`bnClose` 设回可点，自动重连和 `timer2` 恢复成点击关闭之前的值。成功关掉相机的路径没有走这段回填，关闭按钮仍要等下次打开成功才亮。安装包仍收 `bin\Release`，`jitDebugging` 仍是 `false`。失败帧完成位没有动。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

## 已修复 / 仍开放

| 上一轮条目 | 状态 | 这次看到的代码 |
| --- | --- | --- |
| 停相机超时中止后 `bnClose`、自动重连、`timer2` 未恢复 | 已修 | `bnClose_Click` 在改这三项之前先记下 `_cameraReconnectEnabled` 和 `timer2.Enabled`（约 14306–14308 行）。`WaitForIdle(2000)` 失败时写回这两个原值，并把 `bnClose.Enabled` 设为 true（约 14354–14356 行），然后弹窗并 `return`。`finally` 仍 `Resume`，并重新启用打开按钮和菜单。成功拆除的后半段仍把 `bnClose` 设为 false（约 14472 行），不会被这次回填碰到 |
| 关相机顺序：停流 → 等回调归零 → 再销毁；停相机超时不强制拆 | 保持，未改坏 | 标题栏 `ReleaseAllCameras` 与 `Dispose` 兜底仍是 `WaitForIdle(2000)` 再 `ReleaseAllCameras(500, 100)`。这两条退出路径超时仍继续销毁。停相机第一次等待超时仍是中止，不执行 `CloseDevice` / `DestroyDevice` |
| 安装包 exe 收 `bin\Release` | 保持 | Setup1 约 7503 行、Setup2 约 10132 行仍是 `bin\Release\WindowsFormsApplication1.exe`，ini 与图标同目录。本提交没改 vdproj |
| `jitDebugging` | 保持已关 | `App.config` 第 13 行仍是 `false`。目标框架仍是 `v4.6` |
| 失败帧完成位 | 仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关 | `IPlcOutputWriter.cs` 不在本提交里 |
| 其余低项 | 未改 | 本提交只动了 `bnClose_Click` 的中止分支 |

没有回退。回填只写在超时分支里，成功关闭不会把重连和 `timer2` 重新打开。

## 中

无。

停相机中止后，关闭按钮可以再点，重连开关和 `timer2` 回到点击前。对话框里的「稍后重试」对得上关闭按钮。

## 低

### 中止后「打开设备」也被重新启用

`finally` 不论成败都把 `bnOpen.Enabled` 设为 true（约 14447 行）。相机其实没关。相机开着时，打开成功的路径是关闭可点、打开禁用（`EnableAllConnectedCameraControls`，约 9013–9014 行）。中止之后两个按钮都可点。

点「打开设备」会进 `bnOpenCore`。槽里已有句柄时先 `ClearCameraSlotOnOpenFailed` 再创建（约 13607–13608 行）。对话框让操作员重试的是关闭，不是打开。这是按钮对和「相机仍开着」不一致，不是中止本身没复原。

回填写在 `MessageBox` 之前。对话框消息泵期间，若 `timer2` 原来是开的（间隔 400ms），它会在检测门尚未 `Resume` 时再跳。`timer2_Tick` 对仍连接、且没有 SDK 断连标志的相机直接返回（约 9316–9328 行），不会把还连着的设备拆掉。已经报断线的槽可能在操作员读对话框时开始重连。点确定之后 `finally` 才 `Resume`。

### 退出路径与安装包、框架

标题栏在 `StopInspectWorkers` 等满 10 秒仍不空闲时取消关闭。其后 `ReleaseAllCameras` 的 2 秒超时仍继续销毁，`Dispose` 兜底相同。正常关窗时槽位已经空了，`ReleaseAllCameras(500, 100)` 对空槽仍会睡眠。停相机在设备已销毁之后的第二次 `WaitForIdle` 超时仍释放缓冲（约 14407 行）；第一次已经归零且不再接收新回调。

两份安装包的主程序和 ini 仍都指向 `bin\Release`。ini 不在 git 里。`jitDebugging` 仍关。`BuildInfo` 仍指向 `5ad62de`。

### 上一轮其余低项没有变化

切方案置位前的窄窗口、NG 预览空 `catch` 与整文件读入、根目录只进日志、统计目录随区域变化、无人调用的 `PollLoop`、结果队列满丢最旧、CI 无超时、三菱编程口演示窗、OK 帧缺端子发出「无」、仓库根杂物，都没有被这个提交改到。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `RepeatForRegisters` 仍不重复字符串、奇数 `long` / `float` 不写最后一格、字数未配只写第 1 格，各只告警一次。测试工程仍未链接该文件。约定写成具体格子之前不要改。

## 建议下一步

1. **中止路径把「打开设备」设回不可点。** 相机没关时与打开成功后的按钮对一致：`bnClose` 可点，`bnOpen` 不可点。回填可以留在弹窗之前；若希望读对话框时不要启动重连，把 `timer2` 和重连开关的恢复挪到 `MessageBox` 之后、`return` 之前即可。上机看一次超时：关闭可再点，打开是灰的，仍连接的相机不会被拆。
2. **退出路径维持「超时仍销毁」。** 不必再改标题栏和 `Dispose`。空槽上的 600ms 睡眠可以以后再省。
3. **安装包用 Visual Studio 重新生成一次**，确认 MSI 里的 exe 和 ini 来自同一次 `bin\Release`。
4. **完成位继续不动。** 发版时把 `BuildInfo` 补到 `4c87c5b`。其余低项不挡这一轮。
