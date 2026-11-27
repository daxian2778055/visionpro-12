using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯页触发值显示与保存（含范围模式 小~大）
    /// </summary>
    public static class CommTriggerHelper
    {
        /// <summary>
        /// ★R11（第25轮）：读结果文本是否为“读取失败”文案（DemoUtils.ReadResultRender1 失败时输出
        /// “Read Failed”/“not corrent”）。失败文案不能当作 PLC 值参与触发/切型判定，
        /// 否则网络抖动会造成同值重复触发 + 每圈向 PLC 反复回写反馈。
        /// </summary>
        public static bool IsReadFailureText(string resultText)
        {
            if (string.IsNullOrEmpty(resultText)) return false;
            return resultText.Contains("Failed") || resultText.Contains("corrent");
        }

        public static string FormatTrigValDisplay(string mode, string val1, string val2)
        {
            val1 = Clean(val1);
            val2 = Clean(val2);
            if (Clean(mode) == "范围")
            {
                if (!string.IsNullOrEmpty(val1) && !string.IsNullOrEmpty(val2) && val1 != val2)
                    return val1 + "~" + val2;
            }
            return val1;
        }

        /// <summary>
        /// 解析触发值文本，写入 camera_dic[7]=最小/比较值，[8]=最大（范围模式）
        /// </summary>
        public static void ParseTrigValInput(string mode, string text, string[] cameraRow)
        {
            text = Clean(text);
            if (Clean(mode) == "范围")
            {
                int sep = FindRangeSeparator(text);
                if (sep > 0)
                {
                    cameraRow[7] = text.Substring(0, sep).Trim();
                    cameraRow[8] = text.Substring(sep + 1).Trim();
                }
                else
                {
                    cameraRow[7] = text;
                    cameraRow[8] = text;
                }
            }
            else
            {
                cameraRow[7] = text;
                cameraRow[8] = "";
            }
        }

        public static void GetCamTrigParams(string[] camRow, out string mode, out string val1, out string val2)
        {
            mode = camRow != null && camRow.Length > 6 ? Clean(camRow[6]) : "相等";
            val1 = camRow != null && camRow.Length > 7 ? Clean(camRow[7]) : "";
            val2 = camRow != null && camRow.Length > 8 ? Clean(camRow[8]) : "";
            if (string.IsNullOrEmpty(mode)) mode = "相等";
        }

        /// <summary>
        /// 判断读到的值是否满足触发条件（相等/包含/范围）
        /// </summary>
        public static bool CheckTriggerCondition(string receivedValue, string mode, string val1, string val2)
        {
            receivedValue = Clean(receivedValue);
            mode = Clean(mode);
            val1 = Clean(val1);
            val2 = Clean(val2);
            if (string.IsNullOrEmpty(mode)) mode = "相等";
            switch (mode)
            {
                case "相等":
                    if (string.IsNullOrEmpty(val1)) return false;
                    if (receivedValue == val1) return true;
                    if (int.TryParse(receivedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rv)
                        && int.TryParse(val1, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tv))
                        return rv == tv;
                    return false;
                case "包含":
                    return !string.IsNullOrEmpty(val1) && receivedValue.Contains(val1);
                case "范围":
                    if (!int.TryParse(receivedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val)) return false;
                    if (!TryParseRange(val1, val2, out int minVal, out int maxVal)) return false;
                    return val >= minVal && val <= maxVal;
                default:
                    return !string.IsNullOrEmpty(val1) && receivedValue == val1;
            }
        }

        private static string Clean(string value)
        {
            return (value ?? "").Replace("\0", "").Trim();
        }

        private static int FindRangeSeparator(string text)
        {
            int found = -1;
            ConsiderRangeSeparator(ref found, text.IndexOf('~'));
            ConsiderRangeSeparator(ref found, text.IndexOf('～'));
            ConsiderRangeSeparator(ref found, text.Length > 1 ? text.IndexOf('-', 1) : -1);
            ConsiderRangeSeparator(ref found, text.IndexOf('–'));
            ConsiderRangeSeparator(ref found, text.IndexOf('—'));
            return found;
        }

        private static void ConsiderRangeSeparator(ref int found, int candidate)
        {
            if (candidate > 0 && (found < 0 || candidate < found)) found = candidate;
        }

        private static bool TryParseRange(string val1, string val2, out int minVal, out int maxVal)
        {
            minVal = 0;
            maxVal = 0;
            if (string.IsNullOrEmpty(val1)) return false;

            int sep = FindRangeSeparator(val1);
            if (sep > 0)
            {
                string left = val1.Substring(0, sep).Trim();
                string right = val1.Substring(sep + 1).Trim();
                return int.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out minVal)
                    && int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxVal);
            }

            if (!int.TryParse(val1, NumberStyles.Integer, CultureInfo.InvariantCulture, out minVal)) return false;
            if (string.IsNullOrEmpty(val2))
            {
                maxVal = minVal;
                return true;
            }
            return int.TryParse(val2, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxVal);
        }
    }
}
