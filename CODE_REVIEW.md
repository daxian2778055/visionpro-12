# visionpro-12 代码审查报告

## 总评

审查基线：`origin/master` 为 `947f6b7`（完整 `947f6b7b91c44251861fecfc10820f8a190dcc9f`，2026-10-23）。版本戳 `BuildInfo.Version = 2026.10.23+500bf99`，`BuildTime = 2026-10-23 10:50:58`。上一轮基线是 `656c02d`（`656c02d1a3930d6a73666f5ce1eac86255a2f273`，版本戳 `2026.10.21+170a23e`）。本次只更新本报告，不改应用源码。提交说明里的 Release / Debug 编译和 178 个单测不是这次重跑的结果（本环境没有 Cognex VisionPro）。

**没有严重项，也没有高项，也没有新的中项。**

相对 `656c02d` 有两个提交：`500bf99`（回归分析修复）和 `947f6b7`（版本戳）。13 个文件，约 +399 / −181 行：

| 文件 | 主题 |
| --- | --- |
| `Form1.cs` | 切方案发现关相机中止后回滚；检测队列按模式分深度；`recordBusy` 最多等 2 秒；结果按已配置反馈的连接写出；关窗对连接 2～4 只置停止标志 |
| `Form1.ErrorDisplay.cs` | 软触发勾选快照 + 控件写入改投递；清通讯待处理记录挪出 `_cameraLock` |
| `Form3.cs` | 8 组切型串与 8 条路径一次发布；拍照触发字符另有一份快照，供「配置了触发即回传」判断 |
| `FormOmron.cs` / `FormModbus.cs` / `FormModbusRtu.cs` | 各加 `RequestStopPolling`（只置标志）。单份 `Tuple` 仍是上一轮 `170a23e` 的代码 |
| `Services/FinsRuntime.cs` / `ModbusTcpRuntime.cs` / `ModbusRtuRuntime.cs` | `RequestStop`：只置停止标志，不 `Join` |
| `Services/FormCommManager.cs` | `RequestStopAllLinkPolling` 覆盖连接 2～4 的三协议整窗；无协议接收线程明确不在这里停 |
| `SubSet.cs` | `JobCount >= 8` 时使能分组 1～8 |
| `Form1.Designer.cs` | `label148` 文案 `YA1021` → `YA1023`，只改显示 |
| `BuildInfo.cs` | 版本戳 |

`IPlcOutputWriter.cs`、`App.config`、两个安装工程、`Program.cs` 不在本批 diff 里。失败帧完成位没有改。

按所有者决定忽略，不列入开放问题：`Form1` 体量 / 12 路复制 / 空 `catch`；`GrantIniAccess`；授权失败只打日志；`Form5` 口令；`FormMES` / `FormMySQL`。

报告对应的拉取请求：https://github.com/daxian2778055/visionpro-12/pull/1

## 相对上一轮

### 已落地

| 本批行为 | 这轮看到的代码 |
| --- | --- |
| 切方案在关相机超时中止后留下原方案 | `xinghao_qiehuan` 在 `Invoke(bnClose_Click)` 返回后扫 12 个槽（`Form1.cs` 约 12398–12412 行）。任一 `Cameras[slot] != null` 就 `RollbackSchemeSwitch`（约 12228 行，清 `_switchingScheme`、`Resume`、`qiehuanzhong = 0`、运行按钮重新可见），`Frm2.start = 1` 关掉进度窗，`label133` 写明仍用原方案，然后 `return`。超时中止路径在置空槽位之前就 `return`（约 14555–14563 行），所以这次检查能分清「中止」和「已经拆完」 |
| 启动布防不再跨线程碰软触发勾选框 | UI 线程在 `button1_Click` 进入 `Task.Run` 之前发布 `bool[12]`（约 3921 行；勾选变化在约 14751 行再发布）。后台 `GetSoftTriggerChecked` 只读这份 `volatile` 快照（`Form1.ErrorDisplay.cs` 约 263–287 行）。`Enabled` 写入在非 UI 线程走 `TryBeginInvoke` |
| 清通讯待处理记录不再持着相机锁去拿发送锁 | `ApplyTriggerModeToCameraHardware` 与 `PrepareCameraGrab` 在锁内只记 `clearCommPending`，出锁后再 `ClearCommTriggerPending`（约 422–429 行、653–704 行）。失败提前返回时标志仍是 false，不会误清 |
| 连接 2～4 的 FINS / Modbus 在关窗泵消息前收到停止标志 | `StopAllCommPolling` 仍先 `Stop` 连接 1 的三个单例（含最多 1 秒 `Join`），再 `RequestStopAllLinkPolling`（约 8519、8846–8854 行）。运行时 `RequestStop` 只把 `StopRequested` 或 `_stopPolling` 置位（`FinsRuntime.cs` 约 86–89 行，TCP / RTU 同形）。管理器注释写明无协议不在此停（`FormCommManager.cs` 约 1250–1259 行） |
| 连续运行待检队列只留最新一帧；通讯触发 / 触发拍照仍按序 | `EnqueueInspectFrame`：模式为「连续运行」时深度 1，否则 `InspectQueueDepth`（3）（约 4745、4854–4876 行）。挤掉的帧记 `QueueEvicted` |
| 这一路检测线程在令牌被占时先等再丢 | 占 `recordBusy` 失败时，每 20ms 重试，上限 2000ms；`_inspectStop`、`_disposingFlag`、`_switchingScheme` 任一成立就结束等待（约 4780–4803 行）。仍拿不到才记 `WorkerBusy` 并丢帧 |
| 结果写给配置了该流程反馈的连接 | `WriteResultToProtocolOwners` 对三种协议各扫连接 1～4，`TryGetLinkCamera` 命中才写；连接 1 走 `WriteCameraOutput`，2～4 走 `WriteCameraOutputForLink`（约 8804–8840 行）。无协议走 `WriteNoProtoResultToConfiguredLinks`：该连接 `HasPhotoTriggerFor` 为真才 `changeok`（约 8766–8796 行）。调用点在 `getrecord`（约 5926–5952、5987–5997 行），仍进每路结果队列。同协议一台相机仍只归一条连接 |
| 无协议切型串和路径一次发布，接收线程不再同步读界面 | `Form3` 一个 `volatile Tuple<string[], string[]>`（约 71–91 行）。载入文本之后 `SyncPhotoTriggerCache` 再 `SyncSchemeCache`（约 189–194 行），16 个框的 `TextChanged` 再整体发布。`qiehuan` 只取一次 `snap`（约 897–909 行）。框位顺序仍是切型 `textBox18,19,21,20,23,22,25,24`，路径 `textBox10`～`17` |
| 方案数达到 8 路及以上时，设置窗前 8 个分组保持可操作 | `SubSet.cs` 约 287 行 `JobCount >= 8`。设计器只有分组 1～8，9～12 路没有对应分组 |

三个协议窗体的切型 `Tuple` 一次发布、映射顺序，本批没有改回去。`Form3` 空路径仍 `return 2`（约 912–920 行），不清 `zifu` / `lujing`。

### 仍稳

| 条目 | 这轮看到的代码 |
| --- | --- |
| 关窗先停连接 1 的三路轮询，再泵消息 | `StopAllCommPolling()` 仍在 `DoEvents` 之前（约 8519 行）。`qiehuan` 仍不 `Invoke` 回界面 |
| 切方案关闭闸门 | `xinghao_qiehuan` 仍在切换锁之前看 `_disposingFlag`（约 12301–12304 行），日志「切方案请求被忽略：软件正在关闭」 |
| 接收计数 | `counted = true` 仍在 `TakeTrigger` 且确定收帧之后（约 14282–14292 行）。没有在途触发的帧在计数之前返回 |
| `timer10` 与关闭按钮 | 关窗泵消息前仍禁用（约 8505–8506 行） |
| 关相机按钮超时 | 仍先 `MessageBox`，再恢复进入时的自动重连、`timer2`、`bnClose`；`abortTearDown` 时 `finally` 不恢复 `bnOpen`（约 14555–14563、14661 行）。`Sleep(30)` 仍在相机锁外面 |
| 关相机 / 安装包 / `jitDebugging` / 配置窗 | 本批没有这些文件。`Setup1` / `Setup2` 的可执行文件仍指向 `bin\Release\WindowsFormsApplication1.exe`。`App.config` 里 `jitDebugging="false"`。根目录没改就不写 ini、UNC 不探测、两个输出时间框同一把锁，都还在 |

### 仍开放

- **失败帧完成位：仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** `InspectionFailureOutput.RepeatForRegisters` 不在本批 diff 里。数值故障值仍是 `999`。奇数 `long` / `float` 仍不写最后一格，字数未配仍只写第 1 格，各只告警一次。字符串仍不重复。该文件仍未链进测试工程。
- **无协议串口 / TCP 接收线程仍不在 `StopAllCommPolling` 里。** 管理器注释把这一点写明了：切方案仍靠关闭闸门。关窗泵消息的 2 秒内，无协议仍可能收到一帧并排切换任务，闸门会丢掉它。连接 1 的 `timer10` 已禁用。
- **连接 2～4 的停止是标志，当前这一圈轮询会跑完。** 已经进了 PLC 读或切型判断的那一圈不会被 `Join` 截住。切型判断本身不再同步 `Invoke`，关窗不会因此空等满 1 秒。
- **`Global\` 互斥失败仍回退会话内名字。** 只记日志。
- **打开失败拆除仍在调用方已持有的 `_cameraLock` 里睡 30ms。**
- **关窗兜底在空槽上仍睡约 600ms。**
- **根目录失焦写失败仍只记日志。** 映射盘、大小写比较、运行中不额外锁脉冲框、启动时把 `timespace` 写成 100，都还在。
- **上一轮其余低项，这批 diff 没碰到：** 切方案置位前的窄窗口、NG 预览空 `catch` 与无大小上限的读文件、根目录不可达只进启动日志、统计目录名随区域变化、`PollLoop` 未接到现场连接 1、结果队列容量 64 且满时仍丢最旧（注释写「不丢结果」，`CameraWorkQueue.Enqueue` 满时 `TryTake` 再入队）、三菱编程口演示窗、OK 帧缺端子写「无」、CI 无超时、`verify.ps1` 的 `D:\` 路径、仓库根杂物。

### 新发现

没有新的中项。下面几条是本批行为带出来的残留，都放在「低」。

## 中

无。

## 低

### 连续运行挤帧、令牌等待、关相机槽位已清空

连续运行深度为 1。检测忙时被挤掉的旧帧记 `QueueEvicted`，进「帧计数归因」的内部漏帧。主界面连续运行那四行仍按原设计显示原因，不显示计件数字。通讯触发和触发拍照仍是深度 3 的先入先出，只有队列满才挤最旧。

`recordBusy` 等待发生在这一路检测线程上，上限 2 秒，步进 20ms。手动回图通常更短。等待期间切方案、停检或关窗会放弃并按 `WorkerBusy` 丢帧。PLC 侧超时若短于这次等待，回执仍会晚到。

关相机的成功路径在关闭返回码非 `MV_OK` 时仍把槽位置空，只记日志（约 14603–14611 行）。切方案只把「槽位仍非空」当成未关完。SDK 关闭失败但槽位已经清空时，切换会继续卸载旧方案。这是原有拆除口径，本批的新检查专门拦住超时中止那一条。

切方案因关相机中止时，进度窗关掉，说明写在 `label133`，没有模态框。

### 无协议按连接回传时，一次 `changeok` 会走该窗全部已勾选通道

`changeok` 按该窗自己的 TCP 客户机、TCP 服务器、串口勾选发送传入的那一串（约 2020 行起）。主界面 TCP 使能与串口使能都开时，同一连接会调用两次：一次传 tcp 输出值，一次传 serial 输出值。每个已勾选通道都会先后看到这两串。以前单目标就是这样发；现在每个「该相机拍照触发字符非空」的无协议连接各做一遍。未配置拍照触发的连接不会发。

### 启动取流仍在相机锁内等 50ms

`PrepareCameraGrab` 发现该路已在采集时，仍在 `_cameraLock` 内 `Sleep(50)`（约 670–674 行）。清待处理记录已经挪到锁外，这 50ms 还在锁里。

### 无协议、单实例、打开失败拆除、空槽睡眠、配置窗

无协议接收线程仍不随三协议轮询一起停。`Global\` 创建失败只记日志。打开失败拆除仍按住全局相机锁。退出兜底对已经拆空的槽仍睡约 600ms。根目录文本框失焦写失败没有对话框。

### 失败帧完成位

状态：**仍开放 / 所有者澄清中：客户端自定写寄存器，与客户免编程无关。** 奇数 `long` / `float` 仍不写最后一格，字数未配仍只写第 1 格，各只告警一次。

## 建议下一步

1. **上机看切方案碰上关相机超时。** 应留在原方案，相机保持打开，进度窗关掉，运行按钮回来，状态栏出现「切方案中止：关相机未完成」。不要出现「相机仍在推流、方案已经卸掉」。
2. **上机看通讯触发布防。** 勾上软触发再点运行，该路应能布防并回写。连接 2～4 若配了同一流程的反馈，结果应写到那些连接；没配拍照触发的无协议连接不应多收到一串。
3. **连续运行看画面。** 检测忙时画面应跟着最新帧，不再固定落后约 3 帧。归因菜单里可能出现「丢弃最旧帧」，主界面计件行不必为此改数字。
4. **完成位继续不动**，直到格子约定写下来。安装包按现有 `bin\Release` 重新生成一次。`jitDebugging` 保持 `false`。
5. 连接 2～4 的停止标志、无协议接收线程，以及其余低项，不挡这一轮。
