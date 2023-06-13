#define EXECUTE_WITH_CANCELLATION_TOKEN

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;


namespace FFBitrateViewer
{
    public class ExecStatus
    {
        public int    Code   = 0;
        public string StdErr = "";
        public string StdOut = "";
    }


    class Execute
    {
        private readonly static int TimeoutDefault = 5000; // milliseconds
        private readonly static int TimeoutStep    = 200;  // milliseconds


        public static ExecStatus Exec(string executable, string args, int? timeout = null, CancellationToken? cancellationToken = null, Action<string>? stdoutAction = null, Action<string>? stderrAction = null)
        {
            string func = "Execute.Exec";

            Log.WriteCommand(executable, args);

            Log.Write(LogLevel.DEBUG, func + ": Started", executable, args);
            var result = new ExecStatus();
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            timeout    ??= TimeoutDefault;

            using (var stdoutWaitHandle = new AutoResetEvent(false))
            using (var stderrWaitHandle = new AutoResetEvent(false))
            {
                using (Process process = new())
                {
                    process.StartInfo.UseShellExecute  = false;
                    process.StartInfo.CreateNoWindow   = true;
                    process.EnableRaisingEvents        = false;
                    process.StartInfo.FileName         = executable;
                    process.StartInfo.Arguments        = args;
                    process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // To prevent deadlock, at least one stream (stdout or stderr) must be redirected (read async, I'm redirecting both):
                    // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=netframework-4.7.2

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError  = true;

                    try
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (string.IsNullOrEmpty(e.Data)) stdoutWaitHandle.Set();
                            else
                            {
#if DEBUG
//                                Debug.WriteLine("StdOut: " + e.Data); // very slow
#endif
                                if (stdoutAction == null) stdout.AppendLine(e.Data);
                                else stdoutAction(e.Data);
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (string.IsNullOrEmpty(e.Data)) stderrWaitHandle.Set();
                            else
                            {
#if DEBUG
//                                Debug.WriteLine("StdErr: " + e.Data); // very slow
#endif
                                if (stderrAction == null) stderr.AppendLine(e.Data);
                                else stderrAction(e.Data);
                            }
                        };

                        process.Start();
                        //process.Refresh();
                        process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

#if EXECUTE_WITH_CANCELLATION_TOKEN
                        bool cancelled = false;
                        int time = 0;
                        do
                        {
                            if (cancellationToken != null && ((CancellationToken)cancellationToken).IsCancellationRequested) // todo@ cancellationToken.ThrowIfCancellationRequested()
                            {
                                Log.Write(LogLevel.DEBUG, func + ": Cancellation request received");
                                cancelled = true;
                                process.CancelOutputRead();
                                process.CancelErrorRead();
                                stdoutWaitHandle.Set();
                                stderrWaitHandle.Set();
                                break;
                            }
                            time += TimeoutStep;
                            if (time > timeout) break;
                        } while (!process.WaitForExit(TimeoutStep));

                        if (cancelled)
                        {
                            Log.Write(LogLevel.DEBUG, func + ": Cancellation request received, trying to close external program...");
                            if (process.CloseMainWindow())
                            {
                                Log.Write(LogLevel.DEBUG, func + ": External program closed successfully");
                            }
                            else
                            {
                                Log.Write(LogLevel.DEBUG, func + ": External program closing failed. Killing it");
                                process.Kill();
                            }
                            process.WaitForExit();
                        }
                        else
                        {
                            process.WaitForExit(); // double checking
                            process.Refresh();

                            result.Code = process.HasExited ? process.ExitCode : -3;
                            Log.Write(LogLevel.DEBUG, func + ": Exited (" + result.Code + ")");

                            // Sometimes ExitCode = -1073741819 (caused by LAVSplitter -- check windows Application Log)
                            //if (result.Code != 0) throw new InvalidOperationException();

                            result.StdOut = stdout.ToString();
                            result.StdErr = stderr.ToString();
                            Log.Write(LogLevel.DEBUG, func + ": StdOut=" + result.StdOut);
                            Log.Write(LogLevel.DEBUG, func + ": StdErr=" + result.StdErr);
                            if (result.Code != 0 && string.IsNullOrEmpty(result.StdErr)) result.StdErr = "Could not get any output";
                        }
#else
                        if (process.WaitForExit((int)timeout))
                        {
                            process.WaitForExit(); // checking
                            process.Refresh();     // checking
                            result.Code = process.HasExited ? process.ExitCode : -3;

                            result.StdOut = stdout.ToString();
                            result.StdErr = stderr.ToString();
                            Log.Write(LogLevel.DEBUG, func + ": StdOut=" + result.StdOut);
                            Log.Write(LogLevel.DEBUG, func + ": StdErr=" + result.StdErr);
                            if (result.Code != 0 && string.IsNullOrEmpty(result.StdErr)) result.StdErr = "Could not get any output";
                        }
                        else
                        {
                            // Timed out
                            result.Code   = -2;
                            result.StdErr = "Timed out";
                        }
#endif
                        Debug.WriteLine("Finished");
                    }
                    catch (Exception e)
                    {
                        Log.Write(LogLevel.ERROR, func + ": exception", e.Message);
                        result.Code = -1;
                        result.StdErr = e.Message;
                        stdoutWaitHandle.Set();
                        stderrWaitHandle.Set();
                    }
                    finally
                    {
                        stdoutWaitHandle.WaitOne((int)timeout);
                        stderrWaitHandle.WaitOne((int)timeout);
                    }
                }
            }

            Log.Write(LogLevel.DEBUG, func + ": Finished. stdout=" + result.StdOut + ", stderr=" + result.StdErr + "(" + result.Code + ")");
            return result;
        }


    }
}
