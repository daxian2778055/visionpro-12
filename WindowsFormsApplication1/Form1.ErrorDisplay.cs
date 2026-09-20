using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cognex.VisionPro.PMAlign;
using demo;
using System.Security.Cryptography;
using QRCodeUtil;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using System.Globalization;
using WindowsFormsApplication1.Core.Camera;
using WindowsFormsApplication1.Core.Infrastructure;


namespace WindowsFormsApplication1
{
    // Form1 partial class: camera error message display helpers.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region 显示错误信息
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            if (InvokeRequired)
            {
                _logger.WriteLog(errorMsg);
                return;
            }
            MessageBox.Show(errorMsg, "PROMPT");
        }

        private bool IsCameraGrabbing(int camIndex)
        {
            switch (camIndex)
            {
                case 0: return m_bGrabbing1;
                case 1: return m_bGrabbing2;
                case 2: return m_bGrabbing3;
                case 3: return m_bGrabbing4;
                case 4: return m_bGrabbing5;
                case 5: return m_bGrabbing6;
                case 6: return m_bGrabbing7;
                case 7: return m_bGrabbing8;
                case 8: return m_bGrabbing9;
                case 9: return m_bGrabbing10;
                case 10: return m_bGrabbing11;
                case 11: return m_bGrabbing12;
                default: return false;
            }
        }

        private void SetCameraGrabbing(int camIndex, bool grabbing)
        {
            switch (camIndex)
            {
                case 0: m_bGrabbing1 = grabbing; break;
                case 1: m_bGrabbing2 = grabbing; break;
                case 2: m_bGrabbing3 = grabbing; break;
                case 3: m_bGrabbing4 = grabbing; break;
                case 4: m_bGrabbing5 = grabbing; break;
                case 5: m_bGrabbing6 = grabbing; break;
                case 6: m_bGrabbing7 = grabbing; break;
                case 7: m_bGrabbing8 = grabbing; break;
                case 8: m_bGrabbing9 = grabbing; break;
                case 9: m_bGrabbing10 = grabbing; break;
                case 10: m_bGrabbing11 = grabbing; break;
                case 11: m_bGrabbing12 = grabbing; break;
            }
        }

        private IntPtr GetDriverBuffer(int camIndex)
        {
            switch (camIndex)
            {
                case 0: return m_BufForDriver1;
                case 1: return m_BufForDriver2;
                case 2: return m_BufForDriver3;
                case 3: return m_BufForDriver4;
                case 4: return m_BufForDriver5;
                case 5: return m_BufForDriver6;
                case 6: return m_BufForDriver7;
                case 7: return m_BufForDriver8;
                case 8: return m_BufForDriver9;
                case 9: return m_BufForDriver10;
                case 10: return m_BufForDriver11;
                case 11: return m_BufForDriver12;
                default: return IntPtr.Zero;
            }
        }

        private bool EnsureDriverBuffer(int camIndex, uint payloadSize)
        {
            if (payloadSize > m_nBufSizeForDriver[camIndex] || GetDriverBuffer(camIndex) == IntPtr.Zero)
            {
                IntPtr oldBuf = IntPtr.Zero;
                switch (camIndex)
                {
                    case 0: oldBuf = m_BufForDriver1; break;
                    case 1: oldBuf = m_BufForDriver2; break;
                    case 2: oldBuf = m_BufForDriver3; break;
                    case 3: oldBuf = m_BufForDriver4; break;
                    case 4: oldBuf = m_BufForDriver5; break;
                    case 5: oldBuf = m_BufForDriver6; break;
                    case 6: oldBuf = m_BufForDriver7; break;
                    case 7: oldBuf = m_BufForDriver8; break;
                    case 8: oldBuf = m_BufForDriver9; break;
                    case 9: oldBuf = m_BufForDriver10; break;
                    case 10: oldBuf = m_BufForDriver11; break;
                    case 11: oldBuf = m_BufForDriver12; break;
                }
                if (oldBuf != IntPtr.Zero)
                    Marshal.FreeHGlobal(oldBuf);
                m_nBufSizeForDriver[camIndex] = payloadSize;
                IntPtr newBuf = Marshal.AllocHGlobal((int)m_nBufSizeForDriver[camIndex]);
                switch (camIndex)
                {
                    case 0: m_BufForDriver1 = newBuf; break;
                    case 1: m_BufForDriver2 = newBuf; break;
                    case 2: m_BufForDriver3 = newBuf; break;
                    case 3: m_BufForDriver4 = newBuf; break;
                    case 4: m_BufForDriver5 = newBuf; break;
                    case 5: m_BufForDriver6 = newBuf; break;
                    case 6: m_BufForDriver7 = newBuf; break;
                    case 7: m_BufForDriver8 = newBuf; break;
                    case 8: m_BufForDriver9 = newBuf; break;
                    case 9: m_BufForDriver10 = newBuf; break;
                    case 10: m_BufForDriver11 = newBuf; break;
                    case 11: m_BufForDriver12 = newBuf; break;
                }
            }
            return GetDriverBuffer(camIndex) != IntPtr.Zero;
        }

        /// <summary>
        /// 释放单个相机的驱动缓冲（AllocHGlobal 分配，必须用 FreeHGlobal 释放），释放后置零防止重复释放。
        /// </summary>
        private void FreeDriverBuffer(int i)
        {
            IntPtr buf = IntPtr.Zero;
            switch (i)
            {
                case 0: buf = m_BufForDriver1; m_BufForDriver1 = IntPtr.Zero; break;
                case 1: buf = m_BufForDriver2; m_BufForDriver2 = IntPtr.Zero; break;
                case 2: buf = m_BufForDriver3; m_BufForDriver3 = IntPtr.Zero; break;
                case 3: buf = m_BufForDriver4; m_BufForDriver4 = IntPtr.Zero; break;
                case 4: buf = m_BufForDriver5; m_BufForDriver5 = IntPtr.Zero; break;
                case 5: buf = m_BufForDriver6; m_BufForDriver6 = IntPtr.Zero; break;
                case 6: buf = m_BufForDriver7; m_BufForDriver7 = IntPtr.Zero; break;
                case 7: buf = m_BufForDriver8; m_BufForDriver8 = IntPtr.Zero; break;
                case 8: buf = m_BufForDriver9; m_BufForDriver9 = IntPtr.Zero; break;
                case 9: buf = m_BufForDriver10; m_BufForDriver10 = IntPtr.Zero; break;
                case 10: buf = m_BufForDriver11; m_BufForDriver11 = IntPtr.Zero; break;
                case 11: buf = m_BufForDriver12; m_BufForDriver12 = IntPtr.Zero; break;
            }
            if (buf != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(buf); } catch { }
            }
        }

        /// <summary>
        /// 运行前启动相机取流：检查连接、避免重复取流、分配缓存后开始采集
        /// </summary>
        private static string NormalizeTriggerMode(string mode)
        {
            return (mode ?? "").Replace("\0", "").Trim();
        }

        private bool IsCommTriggerMode(string mode)
        {
            return NormalizeTriggerMode(mode) == "通讯触发";
        }

        private void DisarmCommTrigger()
        {
            _jobs.CommTriggerArmed = false;
            _jobs.ClearAllCommTriggerPending();
        }

        private void ArmCommTriggerAfterRunReady()
        {
            ApplyTriggerModesForOpenedCameras();
            _jobs.ClearAllCommTriggerPending();
            Thread.Sleep(80);
            _jobs.CommTriggerArmed = true;
        }





        /// <summary>软触发并标记通讯触发待处理帧（仅通讯触发模式下回调会跑检测）。</summary>
        private int TriggerSoftwareCamera(int cameraIndex)
        {
            return _jobs.RequestTrigger(cameraIndex,
                default(System.Collections.Generic.KeyValuePair<string, string>), null);
        }

        private int SendSoftwareTrigger(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= 12 || _cameraCtrl.Cameras[cameraIndex] == null)
                return -1;
            // ★ 2026-09-13：触发回帧恢复进行中(含"恢复未完成待重试")禁止该路新触发。
            //   否则新记录会先登记成功、随后被恢复流程的 ClearCommTriggerPending 清掉，再次破坏配对；
            //   也会与"未完成重试"再次停采/清记录相互打架。
            if (_triggerRecovering[cameraIndex] != 0 || _grabRecoveryPending[cameraIndex] != 0)
                return -1;
            // ★ 仅"通讯触发"模式需要 _jobs.CommTriggerArmed 门控；触发拍照模式直接发软触发（修复软触发被静默忽略）
            string trigMode = NormalizeTriggerMode(_jobs.Myjobs[cameraIndex].triggerMode);
            if (IsCommTriggerMode(trigMode))
            {
                if (!_jobs.CommTriggerArmed)
                    return -1;
            }
            // ★ 相机未开始采集时不能发 TriggerSoftware，否则返回 80000106
            if (!IsCameraGrabbing(cameraIndex))
            {
                _logger.WriteLog("相机" + (cameraIndex + 1) + "未采集，跳过软触发");
                return -1;
            }
            return _cameraCtrl.Cameras[cameraIndex].MV_CC_SetCommandValue_NET("TriggerSoftware");
        }

        private bool ShouldProcessImageCallback(int slot)
        {
            if (slot < 0 || slot >= 12) return false;
            string mode = NormalizeTriggerMode(_jobs.Myjobs[slot].triggerMode);
            if (mode == "连续运行" || mode == "触发拍照")
                return true;
            if (mode == "通讯触发")
            {
                // The transaction queue is the single source of truth for outstanding frames.
                if (!_jobs.CommTriggerArmed || !_jobs.HasPendingTrigger(slot))
                    return false;
                return true;
            }
            return false;
        }

        private bool GetSoftTriggerChecked(int slot)
        {
            switch (slot)
            {
                case 0: return cbSoftTrigger1.Checked;
                case 1: return cbSoftTrigger2.Checked;
                case 2: return cbSoftTrigger3.Checked;
                case 3: return cbSoftTrigger4.Checked;
                case 4: return cbSoftTrigger5.Checked;
                case 5: return cbSoftTrigger6.Checked;
                case 6: return cbSoftTrigger7.Checked;
                case 7: return cbSoftTrigger8.Checked;
                case 8: return cbSoftTrigger9.Checked;
                case 9: return cbSoftTrigger10.Checked;
                case 10: return cbSoftTrigger11.Checked;
                case 11: return cbSoftTrigger12.Checked;
                default: return false;
            }
        }

        private void SetSoftTriggerEnabled(int slot, bool enabled)
        {
            switch (slot)
            {
                case 0: cbSoftTrigger1.Enabled = enabled; break;
                case 1: cbSoftTrigger2.Enabled = enabled; break;
                case 2: cbSoftTrigger3.Enabled = enabled; break;
                case 3: cbSoftTrigger4.Enabled = enabled; break;
                case 4: cbSoftTrigger5.Enabled = enabled; break;
                case 5: cbSoftTrigger6.Enabled = enabled; break;
                case 6: cbSoftTrigger7.Enabled = enabled; break;
                case 7: cbSoftTrigger8.Enabled = enabled; break;
                case 8: cbSoftTrigger9.Enabled = enabled; break;
                case 9: cbSoftTrigger10.Enabled = enabled; break;
                case 10: cbSoftTrigger11.Enabled = enabled; break;
                case 11: cbSoftTrigger12.Enabled = enabled; break;
            }
        }

        private void SetTriggerExecEnabled(int slot, bool enabled)
        {
            switch (slot)
            {
                case 0: bnTriggerExec1.Enabled = enabled; break;
                case 1: bnTriggerExec2.Enabled = enabled; break;
                case 2: bnTriggerExec3.Enabled = enabled; break;
                case 3: bnTriggerExec4.Enabled = enabled; break;
                case 4: bnTriggerExec5.Enabled = enabled; break;
                case 5: bnTriggerExec6.Enabled = enabled; break;
                case 6: bnTriggerExec7.Enabled = enabled; break;
                case 7: bnTriggerExec8.Enabled = enabled; break;
                case 8: bnTriggerExec9.Enabled = enabled; break;
                case 9: bnTriggerExec10.Enabled = enabled; break;
                case 10: bnTriggerExec11.Enabled = enabled; break;
                case 11: bnTriggerExec12.Enabled = enabled; break;
            }
        }

        /// <summary>将 UI/ini 中的触发模式写入相机硬件（不依赖 frm5.mark）。</summary>
        private void ApplyTriggerModeToCameraHardware(int slot)
        {
            if (slot < 0 || slot >= 12 || _cameraCtrl.Cameras[slot] == null) return;
            Myjob job = _jobs.Myjobs[slot];
            // 与打开/关闭/重连互斥，防止半开半关时写触发参数
            lock (_cameraLock)
            {
            try
            {
                bool softChecked = GetSoftTriggerChecked(slot);
                if (job.triggerMode == "连续运行")
                {
                    job.trrigerEn = false;
                    _cameraCtrl.Cameras[slot].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                    SetSoftTriggerEnabled(slot, false);
                    SetTriggerExecEnabled(slot, false);
                }
                else if (job.triggerMode == "触发拍照" || IsCommTriggerMode(job.triggerMode))
                {
                    job.trrigerEn = true;
                    _cameraCtrl.Cameras[slot].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                    if (softChecked || IsCommTriggerMode(job.triggerMode))
                    {
                        _cameraCtrl.Cameras[slot].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                        SetTriggerExecEnabled(slot, IsCameraGrabbing(slot));
                    }
                    else
                    {
                        _cameraCtrl.Cameras[slot].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    }
                    SetSoftTriggerEnabled(slot, true);
                    if (IsCommTriggerMode(job.triggerMode))
                        _jobs.ClearCommTriggerPending(slot);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + "触发切换" + (slot + 1));
            }
            }
        }

        private void ApplyTriggerModesForOpenedCameras()
        {
            if (manager1 == null) return;
            int max = Math.Min(12, manager1.JobCount);
            for (int i = 0; i < max; i++)
            {
                if (_cameraCtrl.Cameras[i] != null)
                    ApplyTriggerModeToCameraHardware(i);
            }
        }

        private void ClearCameraSlotOnOpenFailed(string nnn, ref int t1, ref int t2, ref int t3, ref int t4, ref int t5, ref int t6, ref int t7, ref int t8, ref int t9, ref int t10, ref int t11, ref int t12)
        {
            int slot = int.Parse(nnn);
            if (_cameraCtrl.Cameras[slot] != null)
            {
                try { _cameraCtrl.Cameras[slot].MV_CC_StopGrabbing_NET(); } catch { }
                SetCameraGrabbing(slot, false);
                try { _cameraCtrl.Cameras[slot].MV_CC_CloseDevice_NET(); } catch { }
                try { _cameraCtrl.Cameras[slot].MV_CC_DestroyDevice_NET(); } catch { }
                _cameraCtrl.Cameras[slot] = null;
            }
            _jobs.ClearCommTriggerPending(slot);
            switch (nnn)
            {
                case "0": t1 = 0; break;
                case "1": t2 = 0; break;
                case "2": t3 = 0; break;
                case "3": t4 = 0; break;
                case "4": t5 = 0; break;
                case "5": t6 = 0; break;
                case "6": t7 = 0; break;
                case "7": t8 = 0; break;
                case "8": t9 = 0; break;
                case "9": t10 = 0; break;
                case "10": t11 = 0; break;
                case "11": t12 = 0; break;
            }
        }

        private void ApplyGigePacketSizeAfterOpen(int slot, int deviceArrayIndex)
        {
            try
            {
                if (slot < 0 || slot >= 12 || _cameraCtrl.Cameras[slot] == null) return;
                if (deviceArrayIndex < 0 || deviceArrayIndex >= device1.Length) return;
                if (device1[deviceArrayIndex].nTLayerType != MyCamera.MV_GIGE_DEVICE) return;
                int nPacketSize = _cameraCtrl.Cameras[slot].MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                    _cameraCtrl.Cameras[slot].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
            }
            catch { }
        }

        private bool ReconnectCameraSlot(int slot, int deviceArrayIndex, out int nRet)
        {
            nRet = -1;
            if (slot < 0 || slot >= 12 || deviceArrayIndex < 0 || deviceArrayIndex >= device1.Length) return false;
            // ★B1 修复：入口复查中止标志——重连 Task 可能在关闭/切型/停机开始后才执行到这里，
            //   原实现不复查，会拿旧 device1 缓存把刚关掉的槽重新打开（幽灵相机）。
            if (!_cameraReconnectEnabled || _disposingFlag || _switchingScheme || dakaizhong) return false;
            lock (_cameraLock)
            {
                if (_cameraCtrl.Cameras[slot] == null)
                    _cameraCtrl.Cameras[slot] = new MyCamera();
                try { _cameraCtrl.Cameras[slot].MV_CC_StopGrabbing_NET(); } catch { }
                SetCameraGrabbing(slot, false);
                try { _cameraCtrl.Cameras[slot].MV_CC_CloseDevice_NET(); } catch { }
                try { _cameraCtrl.Cameras[slot].MV_CC_DestroyDevice_NET(); } catch { }
                nRet = _cameraCtrl.Cameras[slot].MV_CC_CreateDevice_NET(ref device1[deviceArrayIndex]);
                if (nRet != MyCamera.MV_OK)
                {
                    // ★B4 修复：失败置 null，让主窗"存在 null 槽才重扫"分支接管——
                    //   原实现只销毁句柄不清元素，12 路全非空时名称重扫永不触发，相机换 IP（DHCP）回归绑不回。
                    _cameraCtrl.Cameras[slot] = null;
                    return false;
                }
                nRet = _cameraCtrl.Cameras[slot].MV_CC_OpenDevice_NET();
                if (nRet != MyCamera.MV_OK)
                {
                    // 打开失败时销毁已创建的设备句柄，防止句柄泄漏
                    try { _cameraCtrl.Cameras[slot].MV_CC_DestroyDevice_NET(); } catch { }
                    _cameraCtrl.Cameras[slot] = null;   // ★B4：同上，交给按名重扫接管
                    return false;
                }
                ApplyGigePacketSizeAfterOpen(slot, deviceArrayIndex);
                nRet = _cameraCtrl.Cameras[slot].MV_CC_RegisterImageCallBackEx_NET(cbImage, (IntPtr)slot);
                if (nRet != MyCamera.MV_OK)
                {
                    _cameraCtrl.Cameras[slot] = null;   // ★B4：同上，交给按名重扫接管
                    return false;
                }
                // ★M11 修复：原重连路径只注册图像回调、漏了异常回调——重连后该路 SDK 异常不再上报；
                //   与首次打开路径（Form1.cs:7179/11044）保持一致注册异常回调
                //   （cbException 为常驻字段，已防 GC）。
                try { _cameraCtrl.Cameras[slot].MV_CC_RegisterExceptionCallBack_NET(cbException, (IntPtr)slot); } catch { }
                return true;
            }
        }

        private bool PrepareCameraGrab(int camIndex, out int nRet)
        {
            nRet = -1;
            if (camIndex < 0 || camIndex >= 12 || _cameraCtrl.Cameras[camIndex] == null)
            {
                _logger.WriteLog("相机" + (camIndex + 1) + "未连接，无法启动取流");
                return false;
            }
            lock (_cameraLock)
            {
            try
            {
                if (!_cameraCtrl.Cameras[camIndex].MV_CC_IsDeviceConnected_NET())
                {
                    _logger.WriteLog("相机" + (camIndex + 1) + "设备未连接(IsDeviceConnected=false)");
                    return false;
                }
            }
            catch
            {
                _logger.WriteLog("相机" + (camIndex + 1) + "连接状态检查失败");
                return false;
            }
            if (IsCameraGrabbing(camIndex))
            {
                _cameraCtrl.Cameras[camIndex].MV_CC_StopGrabbing_NET();
                SetCameraGrabbing(camIndex, false);
                Thread.Sleep(50);
            }
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            nRet = _cameraCtrl.Cameras[camIndex].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                _logger.WriteLog("相机" + (camIndex + 1) + " Get PayloadSize failed:" + nRet);
                return false;
            }
            if (!EnsureDriverBuffer(camIndex, stParam.nCurValue))
            {
                _logger.WriteLog("相机" + (camIndex + 1) + " 缓存分配失败");
                return false;
            }
            m_stFrameInfo[camIndex].nFrameLen = 0;
            m_stFrameInfo[camIndex].enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            SetCameraGrabbing(camIndex, true);
            nRet = _cameraCtrl.Cameras[camIndex].MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                SetCameraGrabbing(camIndex, false);
                _logger.WriteLog("相机" + (camIndex + 1) + " Start Grabbing Fail:" + nRet);
                return false;
            }
            if (IsCommTriggerMode(_jobs.Myjobs[camIndex].triggerMode))
                _jobs.ClearCommTriggerPending(camIndex);
            // ★ 2026-09-13：只要采集成功启动（设备重连恢复、或本恢复流程最终成功），就清除"恢复未完成"标志，
            //   避免下一轮定时器再按旧标志停采、清掉刚接收的触发。
            System.Threading.Volatile.Write(ref _grabRecoveryPending[camIndex], 0);
            return true;
            }
        }
        #endregion
    }
}
