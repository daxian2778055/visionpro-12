using MvCamCtrl.NET;

namespace WindowsFormsApplication1.Core.Camera
{
    /// <summary>
    /// 相机设备信息。把海康 SDK 的结构体（含定长 char[]、联合体等 P/Invoke 细节）
    /// 转成干净的托管模型，上层不再需要 Marshal.PtrToStructure / ByteToStruct。
    /// </summary>
    public sealed class CameraDeviceInfo
    {
        /// <summary>传输层类型：MyCamera.MV_GIGE_DEVICE / MV_USB_DEVICE。</summary>
        public uint TransportLayerType { get; private set; }

        /// <summary>用户自定义名称（相机里设置的 DeviceUserID），用于绑定工位槽位。</summary>
        public string UserDefinedName { get; private set; }

        public string SerialNumber { get; private set; }
        public string ManufacturerName { get; private set; }
        public string ModelName { get; private set; }

        /// <summary>列表展示名，形如 "GEV: 相机1 (SN123)"。</summary>
        public string DisplayName { get; private set; }

        /// <summary>是否为 GigE 设备。</summary>
        public bool IsGigE
        {
            get { return TransportLayerType == (uint)MyCamera.MV_GIGE_DEVICE; }
        }

        /// <summary>打开设备所需的原生信息（由枚举流程 FromNative 填充，供底层 SDK 打开调用使用）。</summary>
        internal MyCamera.MV_CC_DEVICE_INFO NativeInfo { get; private set; }

        private CameraDeviceInfo()
        {
        }

        /// <summary>从 SDK 原生结构构造，并清理 char[] 残留的 '\0'。</summary>
        internal static CameraDeviceInfo FromNative(MyCamera.MV_CC_DEVICE_INFO native)
        {
            CameraDeviceInfo info = new CameraDeviceInfo();
            info.NativeInfo = native;
            info.TransportLayerType = native.nTLayerType;

            if (native.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                MyCamera.MV_GIGE_DEVICE_INFO gige =
                    (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(
                        native.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                info.UserDefinedName = Clean(gige.chUserDefinedName);
                info.SerialNumber = Clean(gige.chSerialNumber);
                info.ManufacturerName = Clean(gige.chManufacturerName);
                info.ModelName = Clean(gige.chModelName);
            }
            else if (native.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                MyCamera.MV_USB3_DEVICE_INFO usb =
                    (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(
                        native.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));

                info.UserDefinedName = Clean(usb.chUserDefinedName);
                info.SerialNumber = Clean(usb.chSerialNumber);
                info.ManufacturerName = Clean(usb.chManufacturerName);
                info.ModelName = Clean(usb.chModelName);
            }

            string prefix = info.IsGigE ? "GEV" : "U3V";
            string name = string.IsNullOrEmpty(info.UserDefinedName)
                ? (info.ManufacturerName + " " + info.ModelName).Trim()
                : info.UserDefinedName;
            info.DisplayName = prefix + ": " + name + " (" + info.SerialNumber + ")";

            return info;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>SDK 定长 char[] 转字符串后会带尾部 '\0' 与空白，需清理后再比较。</summary>
        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\0", string.Empty).Trim();
        }
    }
}
