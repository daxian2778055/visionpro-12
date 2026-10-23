# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `8b47a87`（完整 `8b47a87c456830dee47cfb0ec829c394c47ab8fd`，2026-10-23）。版本戳 `BuildInfo.Version = 2026.10.23+2e167f0`，`BuildTime = 2026-10-23 11:13:39`。上一轮基线是 `947f6b7`（`947f6b7b91c44251861fecfc10820f8a190dcc9f`，版本戳 `2026.10.23+500bf99`）。本次只更新本报告，不改应用源码。提交说明里的 Release / Debug 编译和 178 个单测不是这次重跑的结果（本环境没有 Cognex VisionPro）。

**没有严重项，也没有高项，也没有新的中项。**

相对 `947f6b7` 有两个提交：`2e167f0`（回归复审修复）和 `8b47a87`（版本戳）。4 个文件，约 +107 / −45 行：

| 文件 | 主题 |
| --- | --- |
| `Form1.cs` | 关相机 Close/Destroy 失败记入 `_cameraCloseHadFailure`；切方案在槽位已空但关闭失败时同样回滚。无协议结果改走 `changeokTcp` / `changeokSerial` |
| `Form1.ErrorDisplay.cs` | `PrepareCameraGrab` 已在采集时，停流后的 50ms 移出 `_cameraLock`，醒来后再复查槽位 |
| `Form3.cs` | `changeok` 拆成按介质过滤的 `changeokCore`；原 `changeok` 仍是 TCP 与串口都允许 |
| `BuildInfo.cs` | 版本戳 |

`IPlcOutputWriter.cs`、安装工程、`App.config` 不在本批 diff 里。失败帧完成位没有改。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

报告对应的拉取请求：https://github.com/daxian2778055/visionpro-12/pull/1

## 相对上一轮

### 已修

| 上一轮低项 | 这轮看到的代码 |
| --- | --- |
| 关相机成功路径在 Close/Destroy 非 `MV_OK` 时仍把槽位置空，切方案只看「槽位非空」，会继续卸载 | `bnClose_Click` 每次进入先把 `_cameraCloseHadFailure` 清掉（约 14514 行）。Close 或 Destroy 不是 `MV_OK`，以及该路拆除抛异常，都把标志置上（约 14614–14626 行），然后仍然把槽位置空。切方案在调用前再清一次标志（约 12392 行），返回后 `cameraStillOpen \|\| _cameraCloseHadFailure` 为真就 `RollbackSchemeSwitch` 并留下原方案（约 12410–12421 行）。超时中止时槽位还在，仍走原来的「槽位非空」分支 |
| 无协议两个使能都开时，tcp 值和 serial 值都会出现在每个已勾选通道上 | 结果输出改为 `changeokTcp` / `changeokSerial`（`Form1.cs` 约 8792、8797 行）。`changeokCore` 用 `allowTcp` / `allowSerial` 过滤客户机、服务器、串口（`Form3.cs` 约 2020–2045 行）。tcp 输出只进已勾选的 TCP 通道，serial 输出只进已勾选的串口。`changeok` 仍调用 `changeokCore(aa, true, true)`，全库结果路径已经不再用它 |
| 启动取流时若该路已在采集，仍在 `_cameraLock` 内 `Sleep(50)` | `PrepareCameraGrab` 在第一段锁里停流并记下 `needSettle`，出锁后再睡 50ms，第二段锁里复查（`Form1.ErrorDisplay.cs` 约 655–725 行）。槽位已空，或这 50ms 里已经又在取流，就放弃本次启动，不去 `StartGrabbing`。清通讯待处理记录仍在第二段锁成功之后、锁外执行 |

关相机失败时软件侧槽位已经空了，方案文件留下。操作员要再检，需要重新打开相机。状态栏两句原因共用「切方案中止：关相机未完成，已继续使用原方案」；日志里才分开写「仍有槽位未释放」和「Close/Destroy 返回异常」。超时中止那条仍会先弹出原来的对话框。

### 仍稳

| 条目 | 这轮看到的代码 |
| --- | --- |
| 关窗先停连接 1 的三路轮询，再泵消息 | `StopAllCommPolling()` 仍在 `DoEvents` 之前（约 8522 行）。连接 2～4 仍只置停止标志 |
| 切方案关闭闸门 | `xinghao_qiehuan` 仍在切换锁之前看 `_disposingFlag`（约 12307 行） |
| 接收计数 | `counted = true` 仍在确定收帧之后（约 14301 行） |
| 空路径 | `Form3.qiehuan` 仍 `return 2`（约 920 行） |
| `timer10` | 关窗泵消息前仍禁用（约 8509 行） |
| 软触发快照、清待处理记录出锁、三协议切型一次发布 | 本批没有改这些段落。快照仍在 `Form1.ErrorDisplay.cs` 约 247 行；`ApplyTriggerModeToCameraHardware` 仍在锁外清理（约 425 行）。欧姆龙 / Modbus TCP / RTU 的 `_schemeSnapshot` 行号未动 |
| 关相机按钮超时恢复、安装包、`jitDebugging`、配置窗 | `Setup2` 仍指向 `bin\Release\WindowsFormsApplication1.exe`。`App.config` 里 `jitDebugging="false"`。重连路径锁外 30ms 之后仍做对象引用和取流状态复查（约 588–594 行） |
| 连续运行只留最新帧、令牌最多等 2 秒、结果按已配置连接写出 | 本批没有改队列和 `recordBusy`。无协议仍按拍照触发字符判断该不该回传，只是发送通道按介质分开 |

没有看到上一轮已收口的项被改回去。

### 仍开放

- **失败帧完成位：仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `InspectionFailureOutput.RepeatForRegisters` 不在本批 diff 里。数值故障值仍是 `999`。奇数 `long` / `float` 仍不写最后一格，字数未配仍只写第 1 格，各只告警一次。字符串仍不重复。该文件仍未链进测试工程。
- **无协议串口 / TCP 接收线程仍不在 `StopAllCommPolling` 里。** 连接 2～4 的 FINS / Modbus 仍是只置标志、当前这一圈跑完。
- **连续运行挤帧和令牌等待。** 深度 1 时被挤掉的旧帧仍记 `QueueEvicted`，进归因菜单的内部漏帧。`recordBusy` 仍最多等 2 秒。本批没改。
- **切方案中止仍不弹第二层模态框。** 超时中止有 `bnClose` 自己的对话框。Close/Destroy 失败只有日志和 `label133`。
- **`Global\` 互斥失败仍回退会话内名字。** 只记日志。
- **打开失败拆除仍在调用方已持有的 `_cameraLock` 里睡 30ms。** `ClearCameraSlotOnOpenFailed` 约 519 行。这和本批挪走的取流 50ms 不是同一处。
- **关窗兜底在空槽上仍睡约 600ms。**
- **根目录失焦写失败仍只记日志。** 映射盘、大小写比较、运行中不额外锁脉冲框、启动时把 `timespace` 写成 100，都还在。
- **上一轮其余低项，这批 diff 没碰到：** 切方案置位前的窄窗口、NG 预览、根目录不可达只进启动日志、统计目录名随区域变化、`PollLoop`、结果队列容量 64 且满时仍丢最旧、三菱编程口演示窗、OK 帧缺端子写「无」、CI 无超时、`verify.ps1` 的 `D:\` 路径、仓库根杂物。

### 新发现

没有新的中项。取流等待出锁后的复查比重点，见下面「低」。

## 中

无。

## 低

### 取流等待期间换成另一台相机

`PrepareCameraGrab` 出锁后只看槽位是否为 null、是否已经在取流（约 692–700 行）。重连那条路径在同样的等待之后还会比对相机对象是不是原来那一个（约 594 行）。这里没有比对。50ms 内若槽位换成另一台尚未取流的相机，本次 `StartGrabbing` 会作用在新对象上。重连自己在锁里完成创建后，调用方通常还会再进一次 `PrepareCameraGrab`，最多多一次停流再开。槽位已空或已经在取流时会放弃，不会对半拆的设备再 `Start`。

### 关相机失败后的界面口径

Close/Destroy 失败时方案留下，12 个槽已经置空。状态栏不区分「超时，相机还开着」和「返回码异常，软件侧已经拆掉句柄」。日志有返回码。

### 无协议、单实例、打开失败拆除、空槽睡眠、配置窗

无协议接收线程仍不随三协议轮询一起停。`Global\` 创建失败只记日志。打开失败拆除仍按住全局相机锁。退出兜底对已经拆空的槽仍睡约 600ms。根目录文本框失焦写失败没有对话框。

### 连续运行挤帧与令牌等待

连续运行忙时被挤掉的旧帧进归因菜单的内部漏帧。主界面那四行仍显示原因。通讯触发和触发拍照仍是深度 3。令牌被占时这一路检测线程最多等 2 秒；切方案、停检或关窗会提前结束等待并丢帧。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** 奇数 `long` / `float` 仍不写最后一格，字数未配仍只写第 1 格，各只告警一次。

## 建议下一步

1. **上机看关相机失败后再切方案。** Close/Destroy 返回异常时，应留在原方案，槽位已经空了，需要重新打开相机才能再检。日志里应有关闭返回码。超时中止那条应仍弹出对话框，并且相机保持打开。
2. **上机看无协议两个使能同时开。** TCP 通道只应出现 tcp 输出值，串口只应出现 serial 输出值，同一通道不要再先后收到两串。只开其中一个使能时，另一个介质的勾选通道不应收到这一串。
3. **取流重启时看其它相机。** 某一路已在采集、再次启动取流的那 50ms，其它路的打开/关闭不应再被这把全局相机锁按住。
4. **完成位继续不动**，直到格子约定写下来。安装包按现有 `bin\Release` 重新生成一次。`jitDebugging` 保持 `false`。
5. 连续运行挤帧、令牌等待、无协议接收线程，以及其余低项，不挡这一轮。
