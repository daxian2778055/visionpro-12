using System;

// ★测试替身：InspectionFailureOutput.SafeLog 通过 AppHost.Services.Resolve<LoggingService>() 落日志。
// 生产文件 IPlcOutputWriter.cs 被活链接进测试工程，但 AppHost 依赖整个 DI 组合根（含 Cognex），
// 无法链接。这里提供一个最小 stub 让 SafeLog 的 try/catch 有类型可解析——告警落日志本身
// 不是被测行为（被测的是 ForFormat/RepeatForRegisters 的故障值口径），stub 只保证不抛异常。
namespace WindowsFormsApplication1.Core.Infrastructure
{
    internal static class AppHost
    {
        internal static readonly StubServices Services = new StubServices();

        internal sealed class StubServices
        {
            internal T Resolve<T>() where T : class, new() { return new T(); }
        }
    }
}
