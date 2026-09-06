using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯运行期自动重连：后台线程判定，不阻塞 UI，带冷却与防重入。
    /// </summary>
    public static class CommReconnectHelper
    {
        /// <summary>
        /// 轮询读失败时累加计数；满足条件时返回 true（应触发一次后台重连）。
        /// </summary>
        public static bool ShouldTriggerReconnect(
            ref int failCount,
            bool finsEn,
            int reconnecting,
            ref long lastAttemptUtcTicks,
            int failThreshold = 3,
            int cooldownSeconds = 10)
        {
            failCount++;
            if (!finsEn || reconnecting != 0) return false;
            if (failCount < failThreshold) return false;
            long now = DateTime.UtcNow.Ticks;
            if (lastAttemptUtcTicks > 0 &&
                (now - lastAttemptUtcTicks) < TimeSpan.TicksPerSecond * cooldownSeconds)
                return false;
            lastAttemptUtcTicks = now;
            failCount = 0;
            return true;
        }
    }
}
