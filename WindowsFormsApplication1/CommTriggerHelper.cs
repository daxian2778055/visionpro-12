using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯页触发值显示与保存（含范围模式 小~大）
    /// </summary>
    public static class CommTriggerHelper
    {
        public static string FormatTrigValDisplay(string mode, string val1, string val2)
        {
            if (mode == "范围")
            {
                if (!string.IsNullOrEmpty(val1) && !string.IsNullOrEmpty(val2) && val1 != val2)
                    return val1 + "~" + val2;
            }
            return val1 ?? "";
        }

        /// <summary>
        /// 解析触发值文本，写入 camera_dic[7]=最小/比较值，[8]=最大（范围模式）
        /// </summary>
        public static void ParseTrigValInput(string mode, string text, string[] cameraRow)
        {
            text = text ?? "";
            if (mode == "范围")
            {
                int sep = text.IndexOf('~');
                if (sep < 0)
                    sep = text.IndexOf('-');
                if (sep > 0)
                {
                    cameraRow[7] = text.Substring(0, sep).Trim();
                    cameraRow[8] = text.Substring(sep + 1).Trim();
                }
                else
                {
                    cameraRow[7] = text.Trim();
                    cameraRow[8] = text.Trim();
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
            mode = camRow != null && camRow.Length > 6 ? (camRow[6] ?? "相等") : "相等";
            val1 = camRow != null && camRow.Length > 7 ? (camRow[7] ?? "") : "";
            val2 = camRow != null && camRow.Length > 8 ? (camRow[8] ?? "") : "";
            if (string.IsNullOrWhiteSpace(mode)) mode = "相等";
        }

        /// <summary>
        /// 判断读到的值是否满足触发条件（相等/包含/范围）
        /// </summary>
        public static bool CheckTriggerCondition(string receivedValue, string mode, string val1, string val2)
        {
            try
            {
                receivedValue = (receivedValue ?? "").Replace("\0", "").Trim();
                val1 = (val1 ?? "").Trim();
                val2 = (val2 ?? "").Trim();
                if (string.IsNullOrEmpty(mode)) mode = "相等";
                switch (mode)
                {
                    case "相等":
                        if (string.IsNullOrEmpty(val1)) return false;
                        if (receivedValue == val1) return true;
                        if (int.TryParse(receivedValue, out int rv) && int.TryParse(val1, out int tv))
                            return rv == tv;
                        return false;
                    case "包含":
                        return !string.IsNullOrEmpty(val1) && receivedValue.Contains(val1);
                    case "范围":
                        if (int.TryParse(receivedValue, out int val) && !string.IsNullOrEmpty(val1))
                        {
                            int minVal = int.Parse(val1);
                            int maxVal = string.IsNullOrEmpty(val2) ? minVal : int.Parse(val2);
                            return val >= minVal && val <= maxVal;
                        }
                        return false;
                    default:
                        return !string.IsNullOrEmpty(val1) && receivedValue == val1;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
