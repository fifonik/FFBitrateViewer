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

    public enum ExecStop
    {
        None    = 0,
        Timeout = 1,
        Cancel  = 2
    }


    class Execute
    {
        private readonly static int TimeoutDefault  = 5000;   // milliseconds
        private readonly static int TimeoutNoOutput = 15_000; // milliseconds
        private readonly static int TimeoutStep     = 200;    // milliseconds


        public static ExecStatus Exec(string executable, string args, int? timeout = null, CancellationToken? cancellationToken = null, Action<string>? stdoutAction = null, Action<string>? stderrAction = null)
        {
            string func = "Execute.Exec";

            Log.WriteCommand(executable, args);

            Log.Write(LogLevel.DEBUG, func + ": Started", executable, args);
            var result    = new ExecStatus();
            var stdout    = new StringBuilder();
            var stderr    = new StringBuilder();
            int time1     = 0;
            int time2     = 0;
            int timeout1  = timeout ?? TimeoutDefault;
            int timeout2  = TimeoutNoOutput;

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
                            time2 = 0;
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
                            time2 = 0;
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

                        ExecStop stopped = ExecStop.None;

                        do
                        {
                            time1 += TimeoutStep;
                            time2 += TimeoutStep;
                            if (time1 > timeout1 || time2 > timeout2)
                            {
                                Log.Write(LogLevel.DEBUG, func + ": Timed out");
                                stopped = ExecStop.Timeout;
                                result.Code = -2;
                                result.StdErr = "Timed out";
                                break;
                            }
                            if (cancellationToken != null && ((CancellationToken)cancellationToken).IsCancellationRequested) // todo@ cancellationToken.ThrowIfCancellationRequested()
                            {
                                Log.Write(LogLevel.DEBUG, func + ": Cancellation request received");
                                stopped = ExecStop.Cancel;
                                result.Code = -4;
                                result.StdErr = "Cancelled";
                                break;
                            }
                        } while (!process.WaitForExit(TimeoutStep));

                        if (stopped == ExecStop.None)
                        {
                            process.WaitForExit(); // double checking
                            process.Refresh();

                            result.Code = process.HasExited ? process.ExitCode : -3;
                            Log.Write(LogLevel.DEBUG, func + ": Exited (" + result.Code + ")");

                            // Sometimes ExitCode = -1073741819 (caused by LAVSplitter -- check windows Application Log)
                            //if (result.Code != 0) throw new InvalidOperationException();

                            result.StdOut = stdout.ToString();
                            Log.Write(LogLevel.DEBUG, func + ": StdOut=" + result.StdOut);
                            result.StdErr = stderr.ToString();
                            Log.Write(LogLevel.DEBUG, func + ": StdErr=" + result.StdErr);
                            if (result.Code != 0 && string.IsNullOrEmpty(result.StdErr)) result.StdErr = "Could not get any output";
                        }
                        else
                        {
                            process.CancelOutputRead();
                            process.CancelErrorRead();
                            stdoutWaitHandle.Set();
                            stderrWaitHandle.Set();
                            Log.Write(LogLevel.DEBUG, func + ": Closing external program");
                            if (process.CloseMainWindow())
                            {
                                Log.Write(LogLevel.DEBUG, func + ": External program closed successfully");
                            }
                            else
                            {
                                Log.Write(LogLevel.DEBUG, func + ": Unable to close external program. Killing it");
                                process.Kill();
                            }
                            process.WaitForExit();
                            process.Refresh();
                        }
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
                        stdoutWaitHandle.WaitOne(timeout1);
                        stderrWaitHandle.WaitOne(timeout1);
                    }
                }
            }

            Log.Write(LogLevel.DEBUG, func + ": Finished. stdout=" + result.StdOut + ", stderr=" + result.StdErr + " (" + result.Code + ")");
            return result;
        }


    }
}
