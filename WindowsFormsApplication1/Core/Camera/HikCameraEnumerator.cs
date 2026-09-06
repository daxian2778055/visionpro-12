using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MvCamCtrl.NET;

namespace WindowsFormsApplication1.Core.Camera
{
    /// <summary>
    /// 海康相机枚举与设备工厂。
    /// <para>
    /// 取代 Form1 中 DeviceListAcq（Marshal.PtrToStructure + 逐类型分支拼 DisplayName）
    /// 以及 bnOpen_Click 里用 12 个 temp 变量 + 12 个 "if (chUserDefinedName == camera_name[N])"
    /// 复制块来做的「设备名 → 槽位」映射 —— 后者用 <see cref="MapToSlots"/> 一次字典匹配即可完成。
    /// </para>
    /// </summary>
    public static class HikCameraEnumerator
    {
        /// <summary>枚举所有 GigE 与 USB3 设备。</summary>
        public static List<CameraDeviceInfo> Enumerate()
        {
            return Enumerate((uint)(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE));
        }

        /// <summary>枚举指定传输层类型的设备。</summary>
        public static List<CameraDeviceInfo> Enumerate(uint transportLayerTypes)
        {
            List<CameraDeviceInfo> result = new List<CameraDeviceInfo>();

            MyCamera.MV_CC_DEVICE_INFO_LIST stDevList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int nRet = MyCamera.MV_CC_EnumDevices_NET(transportLayerTypes, ref stDevList);
            if (nRet != MyCamera.MV_OK) return result;

            for (uint i = 0; i < stDevList.nDeviceNum; i++)
            {
                try
                {
                    MyCamera.MV_CC_DEVICE_INFO native =
                        (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                            stDevList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                    result.Add(CameraDeviceInfo.FromNative(native));
                }
                catch
                {
                    // 单个设备信息解析失败不应中断整次枚举
                }
            }

            return result;
        }

        /// <summary>
        /// 按「槽位名 → 设备 UserDefinedName」做大小写不敏感匹配，返回 槽位索引 → 设备 的映射。
        /// <para>
        /// 例：slotNames = ["相机1","相机2",...]，会把 UserDefinedName 为 "相机1" 的设备映射到槽位 0，
        /// 取代 Form1.bnOpen_Click 中 12 段复制的 if 块。
        /// </para>
        /// </summary>
        /// <param name="devices">枚举到的设备列表。</param>
        /// <param name="slotNames">按槽位顺序的用户自定义名（相机里设置的 DeviceUserID）。</param>
        public static Dictionary<int, CameraDeviceInfo> MapToSlots(
            IList<CameraDeviceInfo> devices, IList<string> slotNames)
        {
            Dictionary<int, CameraDeviceInfo> map = new Dictionary<int, CameraDeviceInfo>();
            if (devices == null || slotNames == null) return map;

            for (int slot = 0; slot < slotNames.Count; slot++)
            {
                string want = Normalize(slotNames[slot]);
                if (want.Length == 0) continue;

                foreach (CameraDeviceInfo dev in devices)
                {
                    if (string.Equals(Normalize(dev.UserDefinedName), want, StringComparison.OrdinalIgnoreCase))
                    {
                        map[slot] = dev;
                        break;
                    }
                }
            }

            return map;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\0", string.Empty).Trim();
        }
    }
}
