# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `7cb91d7`（2026-10-19，「停相机中止：先弹窗后复原重连/timer2」）。上一轮基线是 `9510fc5`。中间只有这一个提交，只调整了 `Form1.bnClose_Click` 超时分支里四行语句的顺序。`BuildInfo` 仍是 `2026.10.18+5ad62de`。本次只更新本报告，不改应用源码。提交说明里的 Release 编译和 178 个单测不是这次重跑的结果。

没有严重项，也没有高项，也没有中项。

上一轮留下的低项已经落地：停相机超时后先弹出「关相机未完成」，点确定之后才恢复自动重连、`timer2` 和关闭按钮。对话框开着时，这两项保持进入方法时关掉的状态，不会在消息泵里开始重连。「打开设备」在中止时仍然不可点。安装包仍收 `bin\Release`，`jitDebugging` 仍是 `false`。失败帧完成位没有动。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

## 已修复 / 仍开放

| 上一轮条目 | 状态 | 这次看到的代码 |
| --- | --- | --- |
| 读对话框时重连定时器可能已经恢复 | 已修 | 超时分支先 `abortTearDown = true`，再 `MessageBox.Show`（约 14360 行），点确定之后才写回 `_cameraReconnectEnabled`、`timer2.Enabled`，并把 `bnClose` 设为 true（约 14362–14364 行），然后 `return`。进入方法时这两项已经被关掉，弹窗期间不会被打开。`finally` 在 `return` 之后才 `Resume`，并因 `abortTearDown` 跳过 `bnOpen.Enabled = true`（约 14455 行） |
| 关相机顺序与超时策略 | 保持 | 停相机第一次 `WaitForIdle(2000)` 超时仍不拆设备。标题栏 `ReleaseAllCameras` 与 `Dispose` 兜底仍是先等回调，再 `ReleaseAllCameras(500, 100)`，超时仍继续销毁 |
| 「打开设备」在中止时不可点 | 保持 | `abortTearDown` 仍只在超时分支置 true。成功拆除后的代码仍把打开设为可点、关闭设为不可点（约 14477–14478 行），中止的 `return` 走不到那里 |
| 安装包 | 保持 `bin\Release` | Setup1 约 7503 行、Setup2 约 10132 行仍是 `bin\Release\WindowsFormsApplication1.exe`，ini 与图标同目录。本提交没改 vdproj |
| `jitDebugging` | 保持已关 | `App.config` 第 13 行仍是 `false`。目标框架仍是 `v4.6` |
| 失败帧完成位 | 仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关 | `IPlcOutputWriter.cs` 不在本提交里 |
| 其余低项 | 未改 | 本提交只调了弹窗和回填的顺序 |

没有回退。成功关闭不会恢复重连和 `timer2`。拆除过程中抛异常时 `abortTearDown` 仍是 false，「打开设备」仍会重新启用。

## 中

无。

点确定之后，关闭可再点，打开保持灰色，重连和 `timer2` 回到点击关闭之前的值。菜单「打开」仍是选择 `.vpp`，不是打开相机。

## 低

### 退出路径、安装包、框架

标题栏在 `StopInspectWorkers` 等满 10 秒仍不空闲时取消关闭。其后 2 秒超时仍继续销毁，`Dispose` 兜底相同。空槽上 `ReleaseAllCameras(500, 100)` 仍会睡眠。停相机在设备已经销毁之后的第二次 `WaitForIdle` 超时仍释放缓冲；第一次已经归零且不再接收新回调。

两份安装包的主程序和 ini 仍指向 `bin\Release`。ini 不在 git 里。`jitDebugging` 仍关。`BuildInfo` 仍指向 `5ad62de`，关于页不会显示 `7cb91d7`。

弹窗期间检测门是关的，新帧不进检测。这是在等操作员点确定，点完 `finally` 才 `Resume`。

### 上一轮其余低项没有变化

切方案置位前的窄窗口、NG 预览空 `catch` 与整文件读入、根目录只进日志、统计目录随区域变化、无人调用的 `PollLoop`、结果队列满丢最旧、CI 无超时、三菱编程口演示窗、OK 帧缺端子发出「无」、仓库根杂物，都没有被这个提交改到。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `RepeatForRegisters` 仍不重复字符串、奇数 `long` / `float` 不写最后一格、字数未配只写第 1 格，各只告警一次。测试工程仍未链接该文件。约定写成具体格子之前不要改。

## 建议下一步

1. **停相机这条不用再改代码。** 上机故意超时一次：弹窗还开着时不要出现重连日志；点确定后关闭可再点，打开设备是灰的，相机没有被拆。
2. **安装包用 Visual Studio 重新生成一次**，确认 MSI 里的 exe 和 ini 来自同一次 `bin\Release`。
3. **完成位继续不动。** 发版时把 `BuildInfo` 补到 `7cb91d7`。其余低项不挡这一轮。
