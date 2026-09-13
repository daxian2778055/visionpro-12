using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MvCamCtrl.NET;

namespace WindowsFormsApplication1.Core.Camera
{
    /// <summary>
    /// 项目自有的像素格式，用于隔离具体相机厂商 SDK。
    /// 上层（检测流水线、VisionPro 适配层）只依赖本枚举，不感知海康类型。
    /// </summary>
    public enum CameraPixelFormat
    {
        Unknown = 0,

        /// <summary>8 位灰度，每像素 1 字节。</summary>
        Mono8 = 1,

        /// <summary>24 位 RGB，内存序 R,G,B，每像素 3 字节。</summary>
        Rgb8 = 2,

        /// <summary>24 位 BGR，内存序 B,G,R，每像素 3 字节。</summary>
        Bgr8 = 3
    }

    /// <summary>
    /// 像素格式工具。其中的 Mono/Color 判定与 Form1 原有的 IsMonoData/IsColorData 行为保持一致，
    /// 保证抽取后相机行为不变。
    /// </summary>
    public static class CameraPixelFormatHelper
    {
        public static int GetBytesPerPixel(CameraPixelFormat format)
        {
            switch (format)
            {
                case CameraPixelFormat.Mono8:
                    return 1;
                case CameraPixelFormat.Rgb8:
                case CameraPixelFormat.Bgr8:
                    return 3;
                default:
                    return 0;
            }
        }

        public static bool IsMono(CameraPixelFormat format)
        {
            return format == CameraPixelFormat.Mono8;
        }

        /// <summary>把海康像素格式映射到项目枚举。非目标格式返回 Unknown，交由 SDK 转换。</summary>
        public static CameraPixelFormat FromHik(MyCamera.MvGvspPixelType type)
        {
            switch (type)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                    return CameraPixelFormat.Mono8;
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                    return CameraPixelFormat.Rgb8;
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                    return CameraPixelFormat.Bgr8;
                default:
                    return CameraPixelFormat.Unknown;
            }
        }

        public static MyCamera.MvGvspPixelType ToHik(CameraPixelFormat format)
        {
            switch (format)
            {
                case CameraPixelFormat.Mono8:
                    return MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                case CameraPixelFormat.Rgb8:
                    return MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                case CameraPixelFormat.Bgr8:
                    return MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
                default:
                    return MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            }
        }

        /// <summary>海康格式是否为单色数据（可转为 Mono8）。与 Form1.IsMonoData 等价。</summary>
        public static bool IsHikMonoData(MyCamera.MvGvspPixelType type)
        {
            switch (type)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>海康格式是否为彩色数据（可转为 RGB8）。与 Form1.IsColorData 等价。</summary>
        public static bool IsHikColorData(MyCamera.MvGvspPixelType type)
        {
            switch (type)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YCBCR411_8_CBYYCRYY:
                    return true;
                default:
                    return false;
            }
        }

        // ---- 以下像素转换与位图拷贝原实现位于 Form1，现下沉以隔离海康 SDK ----

        /// <summary>深拷贝一份独立拥有的 Bitmap（避免跨线程共享 SDK 缓冲导致崩溃）。</summary>
        public static Bitmap CloneOwnedBitmap(Bitmap src)
        {
            if (src == null) return null;
            return src.Clone(new Rectangle(0, 0, src.Width, src.Height), src.PixelFormat);
        }

        /// <summary>
        /// 从原始像素缓冲直接构造“独立拥有”的 Bitmap（逐行复制）。
        /// 一次性解决原实现的三个问题：
        /// ① stride 未做 4 字节对齐（GDI+ 强制要求），宽度非 4 倍数时会取图失败/花屏；
        /// ② 彩色把 SDK 的 RGB 序数据直接按 Format24bppRgb（内存实为 B,G,R）解释，导致红蓝颠倒；
        /// ③ 不再用 pData 零拷贝包装出中间 Bitmap，省掉每帧一次 Bitmap 分配。
        /// 返回 null 表示失败（调用方应丢弃该帧）。
        /// </summary>
        public static Bitmap BuildOwnedBitmap(IntPtr pData, int width, int height, CameraPixelFormat srcFormat, ColorPalette grayPalette)
        {
            if (pData == IntPtr.Zero || width <= 0 || height <= 0) return null;

            bool isMono = (srcFormat == CameraPixelFormat.Mono8);
            int bpp = isMono ? 1 : 3;
            PixelFormat pf = isMono ? PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb;

            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(width, height, pf);
                if (isMono && grayPalette != null)
                    bmp.Palette = grayPalette;

                BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pf);
                try
                {
                    int srcStride = width * bpp;            // 源行字节（SDK 缓冲，紧密排列）
                    int dstStride = Math.Abs(data.Stride);  // 目标行字节（GDI+ 已按 4 字节对齐）
                    bool swapRb = !isMono && (srcFormat == CameraPixelFormat.Rgb8); // 源 R,G,B -> 目标 B,G,R

                    byte[] row = new byte[Math.Max(srcStride, dstStride)];
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(new IntPtr(pData.ToInt64() + (long)y * srcStride), row, 0, srcStride);
                        if (swapRb)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                byte t = row[x * 3];
                                row[x * 3] = row[x * 3 + 2];
                                row[x * 3 + 2] = t;
                            }
                        }
                        // 目标行尾部 padding（dstStride-srcStride）保持 0
                        Marshal.Copy(row, 0, new IntPtr(data.Scan0.ToInt64() + (long)y * dstStride), srcStride);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                return bmp;
            }
            catch
            {
                if (bmp != null) { try { bmp.Dispose(); } catch { } }
                return null;
            }
        }

        /// <summary>其他黑白格式转为 Mono8。与 Form1.ConvertToMono8 等价。</summary>
        public static Int32 ConvertToMono8(MyCamera device, IntPtr pInData, IntPtr pOutData, ushort nHeight, ushort nWidth, MyCamera.MvGvspPixelType nPixelType)
        {
            if (IntPtr.Zero == pInData || IntPtr.Zero == pOutData)
            {
                return MyCamera.MV_E_PARAMETER;
            }

            int nRet = MyCamera.MV_OK;
            MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();

            stPixelConvertParam.pSrcData = pInData;//源数据
            if (IntPtr.Zero == stPixelConvertParam.pSrcData)
            {
                return -1;
            }

            stPixelConvertParam.nWidth = nWidth;//图像宽度
            stPixelConvertParam.nHeight = nHeight;//图像高度
            stPixelConvertParam.enSrcPixelType = nPixelType;//源数据的格式
            stPixelConvertParam.nSrcDataLen = (uint)(nWidth * nHeight * ((((uint)nPixelType) >> 16) & 0x00ff) >> 3);

            stPixelConvertParam.nDstBufferSize = (uint)(nWidth * nHeight * ((((uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed) >> 16) & 0x00ff) >> 3);
            stPixelConvertParam.pDstBuffer = pOutData;//转换后的数据
            stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
            stPixelConvertParam.nDstBufferSize = (uint)(nWidth * nHeight * 3);

            nRet = device.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);//格式转换
            if (MyCamera.MV_OK != nRet)
            {
                return -1;
            }

            return nRet;
        }

        /// <summary>其他彩色格式转为 RGB8。与 Form1.ConvertToRGB 等价。</summary>
        public static Int32 ConvertToRGB(MyCamera device, IntPtr pSrc, ushort nHeight, ushort nWidth, MyCamera.MvGvspPixelType nPixelType, IntPtr pDst)
        {
            if (IntPtr.Zero == pSrc || IntPtr.Zero == pDst)
            {
                return MyCamera.MV_E_PARAMETER;
            }

            int nRet = MyCamera.MV_OK;
            MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();

            stPixelConvertParam.pSrcData = pSrc;//源数据
            if (IntPtr.Zero == stPixelConvertParam.pSrcData)
            {
                return -1;
            }

            stPixelConvertParam.nWidth = nWidth;//图像宽度
            stPixelConvertParam.nHeight = nHeight;//图像高度
            stPixelConvertParam.enSrcPixelType = nPixelType;//源数据的格式
            stPixelConvertParam.nSrcDataLen = (uint)(nWidth * nHeight * ((((uint)nPixelType) >> 16) & 0x00ff) >> 3);

            stPixelConvertParam.nDstBufferSize = (uint)(nWidth * nHeight * ((((uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed) >> 16) & 0x00ff) >> 3);
            stPixelConvertParam.pDstBuffer = pDst;//转换后的数据
            stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
            stPixelConvertParam.nDstBufferSize = (uint)nWidth * nHeight * 3;

            nRet = device.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);//格式转换
            if (MyCamera.MV_OK != nRet)
            {
                return -1;
            }

            return MyCamera.MV_OK;
        }
    }
}
