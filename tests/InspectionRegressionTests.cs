using System;
using System.Collections.Generic;
using System.Threading;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Core.Threading;

public static class InspectionRegressionTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static CameraTriggerRecord Record(string value, int link)
    {
        return new CameraTriggerRecord(new KeyValuePair<string, string>("fins", value),
            new CommTriggerSource(link, 1, value));
    }
    public static string Run()
    {
        var q = new PendingCameraTriggers();
        var a = Record("A", 1);
        var b = Record("B", 2);
        CameraTriggerRecord received = null;
        Check(q.Execute(a, () => {
            // SDK callback arrives on another thread before the command returns.
            var callback = new Thread(() => received = q.Take()) { IsBackground = true };
            callback.Start();
            Check(callback.Join(2000), "Early callback blocked by SDK submission lock");
            return 0;
        }) == 0, "Early callback command failed");
        Check(ReferenceEquals(received, a) && !q.HasPending, "Early callback lost payload/source");

        q.Execute(a, () => 0);
        q.Execute(b, () => -1);
        Check(ReferenceEquals(q.Take(), a) && q.Take() == null, "Failure removed A instead of B");

        q.Execute(a, () => 0);
        try { q.Execute(b, () => { throw new InvalidOperationException(); }); }
        catch (InvalidOperationException) { }
        Check(ReferenceEquals(q.Take(), a) && !q.HasPending, "SDK exception left a stale trigger");

        var full = new PendingCameraTriggers(1);
        full.Execute(a, () => 0);
        bool commandSent = false;
        Check(full.Execute(b, () => { commandSent = true; return 0; }) != 0,
            "Full trigger queue accepted another command");
        Check(!commandSent && ReferenceEquals(full.Take(), a), "Full queue evicted an in-flight trigger");

        using (var firstEntered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        using (var secondStarted = new ManualResetEventSlim())
        using (var secondSent = new ManualResetEventSlim())
        {
            var t1 = new Thread(() => q.Execute(a, () => {
                firstEntered.Set(); release.Wait(); return 0;
            })) { IsBackground = true };
            var t2 = new Thread(() => {
                secondStarted.Set();
                q.Execute(b, () => { secondSent.Set(); return 0; });
            }) { IsBackground = true };
            t1.Start();
            Check(firstEntered.Wait(2000), "First command did not start");
            t2.Start();
            Check(secondStarted.Wait(2000), "Second caller did not start");
            bool overtook = secondSent.Wait(100);
            release.Set();
            Check(t1.Join(2000) && t2.Join(2000), "Concurrent trigger deadlock");
            Check(!overtook, "Second command overtook first command");
            Check(ReferenceEquals(q.Take(), a) && ReferenceEquals(q.Take(), b), "Concurrent source order changed");
        }
        q.Execute(a, () => 0);
        q.Clear();
        q.Execute(b, () => 0);
        Check(ReferenceEquals(q.Take(), b) && !q.HasPending, "Reset retained old trigger metadata");

        var gate = new InspectionLifecycle();
        Check(gate.TryEnter(), "Initial inspection rejected");
        gate.StopAccepting();
        Check(!gate.TryEnter(), "Inspection admitted while switching");
        Check(!gate.WaitForIdle(20), "Switch unloaded an active transaction");
        gate.Exit();
        Check(gate.WaitForIdle(100), "Completed transaction did not release switch");
        gate.Resume();
        Check(gate.TryEnter(), "Inspection did not resume after switching");
        gate.Exit();

        foreach (var format in new[] { "int", "long", "float" })
            Check(double.Parse(InspectionFailureOutput.ForFormat(format)) == 999,
                "Incorrect numeric PLC fault code: " + format);
        Check(InspectionFailureOutput.ForFormat("string") == "Reject", "String PLC failure changed");
        return "PASS: early callback, failed-command rollback, exception rollback, capacity, concurrent order, reset, lifecycle, PLC fault formats (8 scenarios)";
    }
}
