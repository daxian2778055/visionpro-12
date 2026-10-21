# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `9bc2b20`（完整 `9bc2b20a24595109d89851ab137d7ce8979bb169`，2026-10-21）。版本戳 `BuildInfo.Version = 2026.10.21+d35e990`，`BuildTime = 2026-10-21 21:59:29`。上一轮基线是 `285e6cc`。本次只更新本报告，不改应用源码。提交说明里的 Release 编译和 178 个单测不是这次重跑的结果；本环境无法编译主工程（Cognex）。

**没有严重项，也没有高项，也没有新的中项。**

相对 `285e6cc` 的实质改动在 `d35e990`（全代码复审修复）。`989ff45` 只动配置窗设计器坐标，`912ec30` 的版本戳已被 `9bc2b20` 覆盖。12 个文件，约 +335 / −91 行：

| 文件 | 主题 |
| --- | --- |
| `Form1.cs` | 槽位拆除标志、`FormClosed` 先收口再 `Exit`、关闭时等在途重连、`timer10` 先取走路径再切型、关相机记录返回码并清通讯触发 |
| `Form1.ErrorDisplay.cs` | 打开失败拆除、自动重连：置拆除标志后再停流/销毁 |
| `Form1.DisplaySettings.cs` | 根目录写失败不再提示「已保存」 |
| `Form1.Designer.cs` | 配置窗控件位置 |
| `Form3.cs` | 连接成功后复查停止；切型路径为空改为普通触发并限流记日志 |
| `FormModbus.cs` / `FormModbusRtu.cs` / `FormOmron.cs` | 触发回执地址、长度、数值改为 `TryParse`，非法则跳过这一次写 |
| `RunLog.cs` + `RunLogWriteDateTests.cs` | 满 35 行的当天第一条改为滚动写入；切方案追加的表头不再把序号打回 1 |
| `Program.cs` | 单实例互斥改为固定产品名，并优先用 `Global\` |
| `BuildInfo.cs` | 版本戳 |

关相机两条主路径、安装包 `bin\Release`、`jitDebugging="false"`、上一轮配置窗三条低项，在这批提交里都还在。失败帧完成位没有改。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

报告对应的拉取请求：https://github.com/daxian2778055/visionpro-12/pull/1

## 相对上一轮

### 上一轮已修，这轮仍在

| 条目 | 这轮看到的代码 |
| --- | --- |
| 关相机：停流 → 等回调静止 → 再销毁；超时中止并先弹窗再恢复 | `bnClose_Click`（约 14417–14570 行）。进入时记下重连开关和 `timer2`，等待在途重连前关掉打开/关闭/方案菜单。`WaitForIdle(2000)` 超时：先 `MessageBox`，再恢复重连、`timer2`、关闭按钮，并 `return`。`abortTearDown` 为真时 `finally` 不重新打开「打开设备」（约 14545 行）。成功路径在 `finally` 之后把「打开设备」打开、关闭按钮关掉，并关掉重连（约 14569–14602 行）。`Thread.Sleep(30)` 仍在 `_cameraLock` 外面。这轮在确定要拆之后增加了 `ClearAllCommTriggerPending`（约 14451 行），`Close` / `Destroy` 非 `MV_OK` 会写日志（约 14487 行），拆除异常改为记日志 |
| 标题栏关闭：检测未停则中止关闭、不拆相机；退出路径超时仍销毁 | `StopInspectWorkers` 仍等 10 秒，未静止则回滚 `_disposingFlag` 并取消关闭。确定关闭后 `ReleaseAllCameras`（约 8571 行）先 `StopAccepting` + `WaitForIdle(2000)`，超时记日志并继续 `_cameraCtrl.ReleaseAllCameras(500, 100)`。注释仍写明这是退出路径的有意选择（GigE 未 `Destroy` 时下次开机可能 `0x80000007`）。外层 `catch` 仍把 `e.Cancel` 置 `false` |
| 安装包指向 Release | `Setup1.vdproj` 约 7503 行、`Setup2.vdproj` 约 10132 行仍是 `bin\Release\WindowsFormsApplication1.exe`。ini / 图标同样在 `bin\Release`。本批提交没改 vdproj，ini 仍不在 git 里 |
| `jitDebugging` 已关 | `App.config` 第 13 行仍是 `false`。两个工程目标框架仍是 `v4.6` |
| 根目录文本没变时不写 ini | 启动把规范化后的 `pathhead_root` 记入 `_rootDirPersisted`（`Form1.cs` 约 3782 行）。`PersistRootDir` 在 `norm` 相同处直接 `return true`（`Form1.DisplaySettings.cs` 约 1742 行） |
| UNC 双击不探测 | `IsLocalDrivePath`（约 1758 行）拒绝 `\\` 开头、也拒绝根不是「字母:」的路径 |
| 两个输出时间框同一把锁 | 设计器里 `nudOutTime`、`nudContOut` 默认 `Enabled = false`。菜单「输出时间」、未登录（约 6828 行）、注销（约 11661 行）对两个框一起开、一起关。按下运行（`yunxing`）仍只关打开/保存/另存为 |

按钮关相机和标题栏关闭没有使用新的 `_slotClosing`。这两条路径先 `StopAccepting` 再 `WaitForIdle`，排空之后才拆设备。新标志用在自动重连和打开失败拆除上，那两条路径不排空全机。

### 本轮新落地（相对 285e6cc）

- **重连 / 打开失败与在途像素转换。** `_slotClosing[12]`（`Form1.cs` 约 116 行）。`ProcessImageCallback` 在取通讯触发之前先看标志（约 14137 行），拿转换锁之后再看一次（约 14183 行）。`ClearCameraSlotOnOpenFailed`（`Form1.ErrorDisplay.cs` 约 464 行）和 `ReconnectCameraSlot`（约 531 行）先置标志、停流、睡 30ms，再在 `m_BufForSaveImageLock` 里 `Close` + `Destroy`，`finally` 清标志。
- **关窗收口被 `Environment.Exit` 跳过。** `Form1_FormClosed`（约 8436 行）在 `Exit(0)` 之前调用 `SafeCleanupBeforeDispose`。该方法开头用 `_cleanupDone` 保证只做一次（约 8314 行）。标题栏路径仍先在 `FormClosing` 里拆相机；这里补上的是队列、存图定时器、VisionPro COM 和缓冲。
- **无协议切型路径在切换过程中被清空。** `timer10_Tick`（约 13548 行）用 `Interlocked.Exchange` 先取走 `frm3.lujing` 再调用 `xinghao_qiehuan`。切换耗时期间新写入的路径留到下一拍。
- **无协议 TCP 连接成功后窗已关闭。** `Form3` 约 377 行：`ConnectWithTimeout` 成功后再看 `stop`，为真则关掉 socket 并返回。
- **切型字符串命中但方案路径为空。** `qiehuan`（约 902 行）限流记日志并 `return 0`。`DispatchReceivedFrame`（约 1696 行）把返回 0 的帧交给相机触发。
- **Modbus TCP / RTU、FINS 触发回执。** `WriteTriggerFanhuizhi`：地址或长度 `TryParse` 失败、或 int/long/float 的值解析失败时，限流记日志并跳过这一次写（`FormModbus.cs` 约 1735 行，`FormOmron.cs` 约 1608 行，`FormModbusRtu.cs` 约 3072 行）。同一连接本轮后面的数据块不再被这次异常带走。
- **生产统计满 35 行。** `RunLog.WriteDate`（约 226 行）在非今日且非空行已满时保留第 0 行、丢掉最旧一条数据、把当天第一行写到末尾。测试 `功能8_超过35行且非今日_滚掉最旧行并写入当日首条` 锁住「仍是 35 行、末行是今日、第 2 行从 2日 起」。
- **每日统计序号。** `WriteDate1`（约 301 行）若最后一行是「次序」或「序号」，再向上找一条数据行。只有表头时序号从 1 起。
- **单实例。** `Program.Main`（约 49 行）互斥名为 `Global\VisionPro12-Lianxu-Haikang`，构造失败则用会话内同名。不再把程序集版本编进互斥名。
- **根目录双击。** `PersistRootDir` 改为返回 `bool`。写失败时双击路径弹出「保存失败」，成功才提示「已保存」（约 1701 行）。

### 仍开放

- **失败帧完成位：仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `InspectionFailureOutput.RepeatForRegisters`（`Services/IPlcOutputWriter.cs` 约 28 行）未改。数值故障值仍是 `999`。`long` / `float` 且寄存器数为奇数时仍不写最后一格，并只告警一次。`block[2]` 未配时仍只写第 1 格，并只告警一次。字符串格式仍不重复。该文件仍未链进测试工程。约定写成具体格子之前不要改。
- **提交说明里明确留下的项**（本轮不升格）：结果队列满仍丢最旧；`PollLoop` 与未接线的 Modbus `LinkContext`；三菱 Fx 编程口演示窗不在检测回写路径上；CI 无超时；连续运行下通讯触发的返回值语义；日志目录不可写时的回退；界面未处理异常的退出策略。
- **上一轮其余低项，这批 diff 没碰到：** 两条无协议连接在 `_switchingScheme` 置位前的窄窗口及不限流的「已有切换在途」；`sourceLink` 越界直接返回；NG 预览空 `catch` 与整文件读入；根目录不可达只进启动日志，未创建目录 `Directory.Exists` 为假，死掉的 UNC 仍可能拖住 `InitializeJobManager` 后台线程；统计目录名随区域格式 `ToString("Y")` / `ToString("m")`；OK 帧缺 VisionPro 端子时 TCP/串口/FINS/Modbus 字符串写「无」；仓库根杂物（根目录另一份 `Form6.cs`、`新建文本文档.txt`、`Menu.ini`、`reasonix.toml`）；`verify.ps1` 里写死的 `D:\` MSBuild 路径；检验回归脚本不在 CI 里。

没有看到上一轮已修项被改回去。

### 新发现

下面几条是这轮改动带出来的残留，都按低项记。详见下一节。

1. 标题栏关闭等待在途重连时泵了 `DoEvents`，没有像按钮关相机那样先禁用打开和方案菜单。`bnOpen_Click` 不看 `_disposingFlag`。
2. 自动重连、以及从 `bnOpenCore` 调用的打开失败拆除，把 `Sleep(30)` 放在已持有的 `_cameraLock` 里面。
3. 转换锁内的第二道拆除检查发生在已经取走通讯触发、并且接收计数已经加一之后。
4. `FormClosed` 的兜底释放在相机槽已空时仍会睡约 600ms。
5. 配了切型字符但路径为空时，这一帧会去触发相机。
6. 根目录文本框失焦写失败只记日志。
7. `Global\` 互斥构造失败后，各远程会话仍可能各开一份。
8. 每日统计末尾若连续两行都是表头，序号仍会从 1 再计。

## 中

无。

## 低

### 标题栏关闭的消息泵没有关入口

`Form1_FormClosing` 在用户确认关闭、`_disposingFlag` 已置位、三个定时器已停之后，把 `_cameraReconnectEnabled` 置假，再用最多 2 秒的 `DoEvents` 等 `_reconnectSettled`（约 8528–8534 行）。按钮关相机在同样的等待前会关掉打开、关闭和方案菜单。标题栏这条没有关。`bnOpen_Click`（约 13599 行）入口不判断 `_disposingFlag` 或 `_isFormClosing`。

窗口已经点了「关闭」。这 2 秒里若再点「打开设备」或仍在跑的 `timer10` 推进切型，会和随后的 `ReleaseAllCameras` 叠在一起。超时后关闭仍继续拆相机并 `Application.Exit`。产线影响局限在关软件的这一小段，最坏是这一次释放不干净，下次开机出现相机占用。

### 重连与打开失败拆除仍在相机锁里睡眠

`ReconnectCameraSlot` 整段在 `lock (_cameraLock)` 内：置 `_slotClosing`、停流、`Thread.Sleep(30)`、再拿转换锁销毁，然后在锁内 `Create` / `Open`（约 525–576 行）。`bnOpenCore` 已持有 `_cameraLock`（约 13652 行）时调用 `ClearCameraSlotOnOpenFailed`，后者再睡 30ms（约 470 行）。

按钮关相机已经把这 30ms 移出 `_cameraLock`。重连和打开失败没有。一路 GigE 重连会按住全局相机锁，其它要这把锁的开/关操作排队。30ms 本身很短，真正的时间在锁内的 `Create` / `Open`，这一点重连路径原来就有；这轮新增的是锁内多出来的 30ms，以及销毁改到转换锁里。回调线程只拿转换锁、不再拿 `_cameraLock`，这两把锁的顺序是先相机锁再转换锁，当前回调路径看不到反向加锁。

### 已进门的一帧会在拆除标志下丢掉触发

入口检查（约 14137 行）在 `TakeTrigger` 之前，正在拆除的槽不会吃掉通讯触发。第二道检查在接收计数和 `TakeTrigger` 之后、转换锁之内（约 14158–14183 行）。标志若卡在这两道检查中间置上，这一帧的通讯触发已经被取走，计数加一，图像不再入队。重连当时 PLC 这一拍按无图/超时处理。这是避开「用已销毁句柄」的一侧，窗口只有正在进回调的那一帧。

### 退出兜底在空槽上仍睡眠

`CameraController.ReleaseAllCameras` 只要 `stopSleepMs` / `closeSleepMs` 大于 0 就 `Sleep`，不看是否真有相机（约 74、83 行）。标题栏关闭已经拆完相机后，`FormClosed` 再进 `SafeCleanupBeforeDispose`，会再执行 `ReleaseAllCameras(500, 100)`。正常关窗多约 600ms。收口本身补上了以前被 `Exit` 跳过的队列和 COM 释放。`CameraController.Dispose` 用 `(0, 0)`，仍没有调用方。

### 空的切型路径会变成一次拍照

`qiehuan` 命中切型字符串且对应文本框为空白时返回 0，并每 5 秒记一行「已按普通触发帧处理」。调用方因此走 `getData` 或 `RaiseCameraTrigger`。以前返回 1，整帧被丢掉，既不切型也不拍照，也没有日志。现在配了切型字符、路径忘了填时，PLC 每发一次这个字符就会拍一张。日志能对上。补全路径后恢复切型。

### 根目录失焦写失败没有对话框

双击选目录：写入失败会弹错误框，成功才说「已保存」。失焦仍调用 `PersistRootDir`（约 1722 行），失败只进日志，返回值被丢掉。操作员改了字再点到别处、ini 又写不进去时，界面没有失败提示，重启后仍是旧根。没改字时的「不覆盖 `statistics_root`」守卫还在。真正改了值时两个键仍写成同一个规范化路径。比较仍区分大小写。映射盘（如 `Z:`）仍走 `Directory.Exists`，断开的映射盘双击时仍可能拖住界面。

### 全局互斥失败时回到会话内名字

`Global\VisionPro12-Lianxu-Haikang` 能建起来时，远程桌面和控制台不能再各开一份，升级换版本号也不会因为互斥名变化而双开。构造抛错（没有全局命名空间权限，或上次进程异常退出留下放弃状态）时，`catch` 改用会话内同名互斥并继续启动。那种机器上两个远程会话仍可能各跑一份，同时写统计 CSV、抢相机。

### 连续两行表头时每日序号仍从 1 起

`WriteDate1` 只在最后一行是表头时再向上看一行。再上一行仍是「次序」或「序号」时，把序号基数当成 0。当天连续两次切方案、中间没有数据行时，下一条明细序号从 1 再计。单行表头的情况已经按数据行续号。

### 配置窗与启动写 100 的旧残留

`_jobs.yunxing == true` 不额外禁用两个脉冲框。已经在「设置 → 输出时间」里解锁之后，运行中两个框都能改。下一帧用新值。未登录和注销会把两个框一起关掉。

`InitializeJobManager` 后台线程仍把各路 `timespace` 写成 100（`Form1.cs` 约 638 行），不写 `timespaceContinuous`。界面稍后用 ini 填数字框。常见顺序是后台先写 100、界面再覆盖。反过来时，已保存的触发输出时长可能被写回 100，框上仍显示 ini 里的数。

`IOOutputMs`（`Form1.ErrorDisplay.cs` 约 139 行）未改：连续运行用 `timespaceContinuous`，否则用 `timespace`，都兜底 100。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** 奇数 `long` / `float` 仍不写最后一格，字数未配仍只写第 1 格，各只告警一次。测试工程仍未链接该文件。

## 建议下一步

1. **上机看这轮新路径。** 拔掉一路相机让它重连，确认其它路仍在检、日志没有「用已销毁句柄」一类崩溃。关软件时看进程能退出，并且退出前日志里有收口而不是只剩「软件关闭」。生产统计 CSV 凑满约 35 天后，新的一天第一条 OK/NG 应写在文件末尾，最旧的一天被滚掉。把 Modbus/FINS 回执块的地址改成非数字，该连接应只限流告警，其它块照常轮询。无协议切型字符对应的路径留空时，应看到限流日志，并且相机会被触发一次。
2. **完成位继续不动**，直到格子约定写下来。
3. **发版。** `BuildInfo` 已经是 `2026.10.21+d35e990`。安装包用 Visual Studio 按现有 `bin\Release` 工程重新生成一次。`jitDebugging` 保持 `false`，目标框架保持 `v4.6`。
4. 标题栏关闭的 2 秒消息泵、重连锁内的 30ms、空路径改触发，留到下一次改相机或通讯时再收。其余低项不挡这一轮。
