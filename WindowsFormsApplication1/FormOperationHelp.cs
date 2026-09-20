using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class FormOperationHelp : Form
    {
        private static FormOperationHelp _instance;
        private string _protocolName;

        /// <summary>
        /// 非模态打开操作说明，可同时操作配置界面
        /// </summary>
        public static void ShowHelp(IWin32Window owner, string protocolName)
        {
            if (_instance == null || _instance.IsDisposed)
                _instance = new FormOperationHelp(protocolName);
            else
            {
                _instance.LoadContentFor(protocolName);
                _instance.Text = protocolName + " - 操作说明";
            }
            if (owner != null)
                _instance.Show(owner);
            else
                _instance.Show();
            _instance.BringToFront();
        }

        public FormOperationHelp(string protocolName)
        {
            _protocolName = protocolName;
            InitializeComponent();
            this.Text = protocolName + " - 操作说明";
            LoadContent();
        }

        private void LoadContentFor(string protocolName)
        {
            _protocolName = protocolName;
            flpContent.Controls.Clear();
            LoadContent();
        }

        private void LoadContent()
        {
            string conn, dataCfg, bind, trigger, dataFlow, scheme, faq;

            string triggerDetail =
                "每行相机右侧有「触发模式」和「触发值」两列，配合左侧「触发」下拉框选定的数据源使用。\n" +
                "触发条件全部在通讯页配置即可。\n\n" +
                "【相等】\n" +
                "  · 读到的值必须与「触发值」完全一致才触发。\n" +
                "  · 示例：触发值填 1，PLC 读到 1 时触发拍照。\n\n" +
                "【包含】\n" +
                "  · 读到的字符串包含「触发值」中的内容即触发。\n" +
                "  · 示例：触发值填 ABC，读到 XABC123 也会触发。\n\n" +
                "【范围】（仅数值）\n" +
                "  · 读到的值必须能转成整数，且在区间内才触发。\n" +
                "  · 触发值填写格式：最小值~最大值（也可用减号 -）\n" +
                "  · 示例1：填 10~20 → 读到 10、15、20 都触发，9 和 21 不触发。\n" +
                "  · 示例2：只填 5 → 等价于 5~5，只有读到 5 才触发。\n\n" +
                "说明：同一触发源可同时让多个相机拍照，每个相机可设不同触发规则。\n\n" +
                "防二次触发（返回值 + 勾选框）：\n" +
                "  · 勾选后，轮询发现触发寄存器值 ≠「返回值」时，先将触发寄存器写为「返回值」（如 0），再触发拍照。\n" +
                "  · PLC 若保持非零不复位，软件会自动清零，避免同一信号重复触发。";

            if (_protocolName.Contains("欧姆龙") || _protocolName.Contains("Omron"))
            {
                conn =
                    "1. 打开欧姆龙通讯页，在窗口顶部（非标签页内）填写 PLC 的 IP 地址和端口号。\n" +
                    "2. 默认端口一般为 9600，IP 按现场 PLC 实际地址填写。\n" +
                    "3. 点击「连接」按钮；连接成功后按钮状态变化，通讯开始轮询。\n" +
                    "4. 需要断开时点击「断开连接」。";

                dataCfg =
                    "1. 切换到「数据配置」标签页。\n" +
                    "2. 在上方表格中点击要选择的数据区起始格子（绿色表示已被占用）。\n" +
                    "3. 在页面底部填写：\n" +
                    "   · 起始地址（如 D100、W100 等，参考顶部地址示例）\n" +
                    "   · 长度（连续读取的字数）\n" +
                    "   · 数据格式：string / int / float / long\n" +
                    "   · 类型：触发 / 反馈 / 心跳（★心跳类型当前为预留；相机13 心跳行请绑「反馈」类数据源）\n" +
                    "   · 名称：自定义名称，供数据绑定页下拉选择\n" +
                    "4. 点击「确认」保存；表格对应区域变绿表示配置成功。\n" +
                    "5. 清空全部：点一次「清除」自动清空名称栏，再点一次「清除」并确认即可清空全部配置。";

                bind =
                    "1. 切换到「数据绑定」标签页。\n" +
                    "2. 每一行对应一个相机（相机1~相机12），配置如下：\n" +
                    "   · 「触发」下拉：选择「数据配置」里类型为「触发」的数据名称\n" +
                    "     → 该地址数值变化时，系统会判断是否触发对应相机拍照\n" +
                    "   · 「反馈」下拉：选择类型为「反馈」的数据名称（检测完成后写检测结果）\n" +
                    "   · 「返回值」：触发寄存器的空闲/复位值（常用 0）\n" +
                    "   · 勾选框：启用防二次触发——当触发寄存器≠返回值时，先写回返回值再拍照\n" +
                    "3. 底部「心跳」行：反馈下拉绑「反馈」类数据源；心跳勾 + 反馈 + 返回值三者齐全后，每 1 秒把返回值写入该反馈通道。\n" +
                    "4. 配置会自动保存，无需单独点保存按钮。";

                dataFlow =
                    "触发拍照时，PLC 读到的数据会传入对应相机的 VisionPro 流程，整体流向如下：\n\n" +
                    "  PLC 写入触发地址\n" +
                    "       ↓\n" +
                    "  软件轮询读取（数据配置页定义的触发类数据）\n" +
                    "       ↓\n" +
                    "  写入该相机流程的「fins」输入端（值为 PLC 读到的内容）\n" +
                    "       ↓\n" +
                    "  按本页「触发模式 + 触发值」判断是否软触发拍照\n" +
                    "       ↓\n" +
                    "  相机取图 → 流程 Run() 执行检测（fins 值仍保留在输入端）\n" +
                    "       ↓\n" +
                    "  检测结果按「反馈」配置写回 PLC\n\n" +
                    "流程侧说明：\n" +
                    "  · 请在 VPP 方案的 ToolBlock 中定义名为「fins」的输入端，流程内引用该输入即可获取触发数据。\n" +
                    "  · 触发判断已在通讯页完成，流程里无需再配置 triggerZifu。\n" +
                    "  · 同一触发源可绑定多台相机，每台相机的 fins 输入都会收到相同的 PLC 值，但可按不同触发规则分别决定是否拍照。";

                scheme =
                    "1. 仍在「数据绑定」页，找到顶部「切换：」一行。\n" +
                    "2. 「切换」下拉：绑定一个「触发」类数据源（PLC 写方案号到此地址）。\n" +
                    "3. 下方多组文本框：左侧填方案编号，右侧填对应 VPP 方案文件路径。\n" +
                    "4. 当 PLC 写入的方案号与左侧编号匹配时，软件自动加载右侧路径的方案。\n" +
                    "5. 方案切换后，主界面相机数量和布局会按新方案的流程数自动调整。";
            }
            else if (_protocolName.Contains("Modbus TCP"))
            {
                conn =
                    "1. 打开 Modbus TCP 通讯页，在窗口顶部填写设备 IP 和端口（默认 502）。\n" +
                    "2. 按需设置字节序（ABCD/BADC 等）和用户名、密码（若设备需要）。\n" +
                    "3. 可勾选「字符串颠倒」调整多字节数据的字节顺序。\n" +
                    "4. 点击「连接」建立通讯；断开时点击对应断开按钮。";

                dataCfg =
                    "1. 切换到「数据配置」标签页。\n" +
                    "2. 在表格中点选 Modbus 寄存器起始位置。\n" +
                    "3. 底部填写起始地址、长度、数据格式、类型（触发/反馈/心跳）、名称。\n" +
                    "4. 点击「确认」保存配置。\n" +
                    "5. 绿色区域表示该段寄存器已被占用。";

                bind =
                    "1. 切换到「数据绑定」标签页。\n" +
                    "2. 相机1~12 每行配置：\n" +
                    "   · 「触发」：选择触发类寄存器数据名 → 数值变化时判断拍照\n" +
                    "   · 「反馈」：选择反馈类寄存器（检测完成后写结果）\n" +
                    "   · 「返回值」：触发寄存器空闲值（常用 0）\n" +
                    "   · 勾选：启用防二次触发——触发值≠返回值时先写回再拍照\n" +
                    "3. 通讯轮询到触发数据变化后，满足条件的相机会软触发拍照。";

                dataFlow =
                    "触发拍照时，设备读到的数据会传入对应相机的 VisionPro 流程，整体流向如下：\n\n" +
                    "  设备写入触发寄存器\n" +
                    "       ↓\n" +
                    "  软件轮询读取（数据配置页定义的触发类数据）\n" +
                    "       ↓\n" +
                    "  写入该相机流程的「modbustcp」输入端（值为读到的内容）\n" +
                    "       ↓\n" +
                    "  按本页「触发模式 + 触发值」判断是否软触发拍照\n" +
                    "       ↓\n" +
                    "  相机取图 → 流程 Run() 执行检测（modbustcp 值仍保留在输入端）\n" +
                    "       ↓\n" +
                    "  检测结果按「反馈」配置写回 Modbus 寄存器\n\n" +
                    "流程侧说明：\n" +
                    "  · 请在 VPP 方案的 ToolBlock 中定义名为「modbustcp」的输入端，流程内引用该输入即可获取触发数据。\n" +
                    "  · 触发判断已在通讯页完成，流程里无需再配置 triggerZifu。\n" +
                    "  · 同一触发源可绑定多台相机，每台相机的 modbustcp 输入都会收到相同的值，但可按不同触发规则分别决定是否拍照。";

                scheme =
                    "1. 「数据绑定」页顶部「方案：」下拉，绑定一个触发类数据源。\n" +
                    "2. 配置方案编号与 VPP 文件路径的对应关系（编号文本框 + 路径文本框）。\n" +
                    "3. 设备写入对应方案号时，软件自动切换 VPP 方案并更新主界面布局。";
            }
            else
            {
                conn =
                    "1. 打开 Modbus RTU 通讯页，在顶部选择串口号（COM 口）。\n" +
                    "2. 设置波特率、数据位、停止位、校验位（与设备一致，常用 9600-8-N-1）。\n" +
                    "3. 填写站号（从站地址，默认 1）。\n" +
                    "4. 点击「打开串口」建立通讯；关闭时点「关闭串口」。";

                dataCfg =
                    "1. 「数据配置」页：在表格点选寄存器区域。\n" +
                    "2. 底部填写起始地址、长度、格式、类型、名称，点「确认」。\n" +
                    "3. 与 Modbus TCP 配置方式相同，只是通讯介质为串口。";

                bind =
                    "1. 「数据绑定」页：每行相机绑定触发/反馈数据源。\n" +
                    "2. 「触发」下拉选触发类数据 → 串口读到变化后判断拍照。\n" +
                    "3. 「返回值」+勾选：防二次触发，先把触发寄存器写回空闲值。\n" +
                    "4. 「反馈」下拉：检测完成后把结果写到反馈寄存器。";

                dataFlow =
                    "触发拍照时，设备读到的数据会传入对应相机的 VisionPro 流程，整体流向如下：\n\n" +
                    "  设备写入触发寄存器\n" +
                    "       ↓\n" +
                    "  软件串口轮询读取（数据配置页定义的触发类数据）\n" +
                    "       ↓\n" +
                    "  写入该相机流程的「modbusrtu」输入端（值为读到的内容）\n" +
                    "       ↓\n" +
                    "  按本页「触发模式 + 触发值」判断是否软触发拍照\n" +
                    "       ↓\n" +
                    "  相机取图 → 流程 Run() 执行检测（modbusrtu 值仍保留在输入端）\n" +
                    "       ↓\n" +
                    "  检测结果按「反馈」配置写回设备寄存器\n\n" +
                    "流程侧说明：\n" +
                    "  · 请在 VPP 方案的 ToolBlock 中定义名为「modbusrtu」的输入端，流程内引用该输入即可获取触发数据。\n" +
                    "  · 触发判断已在通讯页完成，流程里无需再配置 triggerZifu。\n" +
                    "  · 同一触发源可绑定多台相机，每台相机的 modbusrtu 输入都会收到相同的值，但可按不同触发规则分别决定是否拍照。";

                scheme =
                    "1. 「方案：」下拉绑定触发数据源。\n" +
                    "2. 配置方案号与 VPP 路径映射，设备写号后自动切方案。";
            }

            faq =
                "Q: 连接/打开串口失败？\n" +
                "A: 检查 IP/端口/COM 口/波特率/站号是否正确，确认设备在线、线缆正常。\n\n" +
                "Q: 数据绑定里「触发」下拉是空的？\n" +
                "A: 请先在「数据配置」页添加类型为「触发」的数据块并点确认。\n\n" +
                "Q: 范围模式不触发？\n" +
                "A: 确认触发值格式为 最小~最大（如 10~20）；读到的值必须是整数。\n\n" +
                "Q: 相等/包含模式不触发？\n" +
                "A: 检查「触发值」是否填写正确；相等模式必须完全一致，包含模式检查子串是否正确。\n\n" +
                "Q: 方案切换无效？\n" +
                "A: 确认「切换/方案」绑定的数据源正确，方案号与路径映射是否填写完整。";

            AddSection("一、建立通讯连接", conn);
            AddSection("二、配置通讯数据（数据配置页）", dataCfg);
            AddSection("三、绑定相机（数据绑定页）", bind);
            AddSection("四、配置触发规则", triggerDetail);
            AddSection("五、触发数据流向（通讯 → 流程）", dataFlow);
            AddSection("六、切换方案", scheme);
            AddSection("七、常见问题", faq);
        }

        private void AddSection(string title, string description)
        {
            const int contentWidth = 660;
            Font descFont = new Font("微软雅黑", 9.5F);

            Label lblDesc = new Label();
            lblDesc.Text = description;
            lblDesc.Font = descFont;
            lblDesc.ForeColor = Color.FromArgb(50, 50, 50);
            lblDesc.MaximumSize = new Size(contentWidth, 0);
            lblDesc.AutoSize = true;
            lblDesc.Location = new Point(15, 28);

            int lblHeight = lblDesc.PreferredSize.Height + 12;
            int gbHeight = lblHeight + 48;

            GroupBox gb = new GroupBox();
            gb.Text = title;
            gb.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            gb.ForeColor = Color.FromArgb(0, 102, 204);
            gb.Size = new Size(contentWidth + 30, gbHeight);
            gb.Margin = new Padding(10, 8, 10, 12);
            gb.Padding = new Padding(12, 22, 12, 12);

            gb.Controls.Add(lblDesc);
            flpContent.Controls.Add(gb);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
