//
// Special command line tool for Ryochan7/DS4Windows app to query the status of the app and profile via WinOS command line.
//
// DS4WindowsCmd app does nothing without the DS4Windows host app (DS4Windows host app https://github.com/Ryochan7/DS4Windows).
//
// This cmdline console tool just sends commands to the background host app. The host app itself supports the same command options,
// but especially the use of "-command Query.Device#.PropertyName" command is a bit difficult to use in batch scripts with the host app.
// The host app is WPF GUI app, so Windows batch script would not wait for a result and console output is not shown by default.
// This DS4WindowsCmd app solves this problem by sending the command to the host app as a "real" Windows console app. Console apps
// are easier to integrated with Windows batch scripts.
//
// License of this DS4WindowsCmd command line tool: "Do What the f**k You Want To" license. (http://www.wtfpl.net/) 
//   USE AT YOUR OWN RISK.
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//   IN THE SOFTWARE.
//
using System;
using System.Text;

using System.Runtime.InteropServices;   // DllImport
using System.IO.MemoryMappedFiles;      // MemoryMappedFiles
using System.Threading;                 // Mutex, Event

namespace DS4WindowsCmd
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    class Program
    {
        public const int WM_COPYDATA = 0x004A;

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string sClass, string sWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        static MemoryMappedFile ipcResultDataMMF = null; // MemoryMappedFile for inter-process communication used to exchange string result data between cmdline client process and the background running DS4Windows app
        static MemoryMappedViewAccessor ipcResultDataMMA = null;

        static private void CreateIPCResultDataMMF()
        {
            // Cmdline client process calls this to create the MMF file used in inter-process-communications. The background DS4Windows process 
            // uses WriteIPCResultDataMMF method to write a command result and the client process reads the result from the same MMF file.
            if (ipcResultDataMMA != null) return; // Already holding a handle to MMF file. No need to re-write the data

            try
            {
                ipcResultDataMMF = MemoryMappedFile.CreateNew("DS4Windows_IPCResultData.dat", 256);
                ipcResultDataMMA = ipcResultDataMMF.CreateViewAccessor(0, 256);
                // The MMF file is alive as long this process holds the file handle open
            }
            catch (Exception)
            {
                /* Eat all exceptions because errors here are not fatal for DS4Win */
            }
        }

        static private string WaitAndReadIPCResultDataMMF(EventWaitHandle ipcNotifyEvent)
        {
            if (ipcResultDataMMA != null)
            {
                // Wait until the inter-process-communication (IPC) result data is available and read the result
                try
                {
                    // Wait max 10 secs and if the result is still not available then timeout and return "empty" result
                    if (ipcNotifyEvent == null || ipcNotifyEvent.WaitOne(10000))
                    {
                        int strNullCharIdx;
                        byte[] buffer = new byte[256];
                        ipcResultDataMMA.ReadArray(0, buffer, 0, buffer.Length);
                        strNullCharIdx = Array.FindIndex(buffer, byteVal => byteVal == 0);
                        return ASCIIEncoding.ASCII.GetString(buffer, 0, (strNullCharIdx <= 1 ? 1 : strNullCharIdx));
                    }
                }
                catch (Exception)
                {
                    /* Eat all exceptions because errors here are not fatal for DS4Win */
                }
            }

            return String.Empty;
        }

        static private string ReadIPCClassNameMMF()
        {
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mma = null;

            try
            {
                byte[] buffer = new byte[128];
                mmf = MemoryMappedFile.OpenExisting("DS4Windows_IPCClassName.dat");
                mma = mmf.CreateViewAccessor(0, 128);
                mma.ReadArray(0, buffer, 0, buffer.Length);
                return ASCIIEncoding.ASCII.GetString(buffer);
            }
            catch (Exception)
            {
                // Eat all exceptions
            }
            finally
            {
                if (mma != null) mma.Dispose();
                if (mmf != null) mmf.Dispose();
            }

            return null;
        }


        static void Main(string[] args)
        {
            string strResult = String.Empty;
            bool   bWaitResultData = false;
            bool   bDoSendMsg = true;
            IntPtr hWndDS4WindowsForm = IntPtr.Zero;

            if (args.Length < 2 || (args[0].ToLower() != "-command" && args[0].ToLower() != "command"))
            {
                bDoSendMsg = false;
                Console.WriteLine("");
                Console.WriteLine("DS4WindowsCmd.exe app is a command line interface to Ryochan7/DS4Windows host app.");
                Console.WriteLine("This command line tool does nothing without the host DS4Windows application running in background.");
                Console.WriteLine("The host DS4Windows.exe app supports the same command line options, but because it is Windows GUI application");
                Console.WriteLine("it has few limitations when integrated with Windows batch scripts (especially with the Query command).");
                Console.WriteLine("This DS4WindowsCmd.exe app was created just to send commands to the background host app and make it easier");
                Console.WriteLine("to integrate with batch scripts.");
                Console.WriteLine("");
                Console.WriteLine("RETURN VALUE: Commands return results in errorlevel variable and as a console text output.");
                Console.WriteLine("RETURN VALUE: If the text output is True or False boolean string then errorlevel is also set to 1 or 0.");
                Console.WriteLine("RETURN VALUE: Query.Battery option outputs the battery level as text and sets errorlevel to 0-100.");
                Console.WriteLine("RETURN VALUE: If there was an error then output is empty and errorlevel is set to 1000.");
                Console.WriteLine("");
                Console.WriteLine("USAGE: ");
                Console.WriteLine("DS4WindowsCmd.exe -command Start | Stop | Shutdown  (start/stop controllers or shutdown the background app)");
                Console.WriteLine("");
                Console.WriteLine("DS4WindowsCmd.exe -command LoadProfile.device#.ProfileName  (load and set a new default profile)");
                Console.WriteLine("DS4WindowsCmd.exe -command LoadTempProfile.device#.ProfileName  (load a temporary runtime profile)");
                Console.WriteLine("   device#=1..8 as controller slot index");
                Console.WriteLine("   ProfileName=Name of the existing DS4Windows profile");
                Console.WriteLine("   Example: -command LoadProfile.1.SnakeGame");
                Console.WriteLine("");
                Console.WriteLine("DS4WindowsCmd.exe -command Query.device#.PropertyName  (query the value of a property)");
                Console.WriteLine("   device#=1..8 as controller slot index");
                Console.WriteLine("   PropertyName=ProfileName | OutContType | ActiveOutDevType | UseDInputOnly | DeviceVIDPID | DevicePath | MacAddress | DisplayName | ConnType | ExclusiveStatus | Battery | Charging | AppRunning");
                Console.WriteLine("   Example: -command Query.1.Battery");
                Console.WriteLine("   Example: -command Query.1.ProfileName");
                Console.WriteLine("");
                Console.WriteLine("ERROR. Invalid cmd line. See https://github.com/Ryochan7/DS4Windows/wiki/Command-line-options for more info.");

            }
            else
                hWndDS4WindowsForm = FindWindow(ReadIPCClassNameMMF(), "DS4Windows");

            if (hWndDS4WindowsForm != IntPtr.Zero)
            {
                bool bOwnsMutex = false;
                Mutex ipcSingleTaskMutex = null;
                EventWaitHandle ipcNotifyEvent = null;

                COPYDATASTRUCT cds;
                cds.lpData = IntPtr.Zero;

                try
                {
                    if (bDoSendMsg && args[1].ToLower().StartsWith("query."))
                    {
                        // Query.device# (1..4) command returns a string result via memory mapped file. The cmd is sent to the background DS4Windows 
                        // process (via WM_COPYDATA wnd msg), then this client process waits for the availability of the result and prints it to console output pipe.
                        // Use mutex obj to make sure that concurrent client calls won't try to write and read the same MMF result file at the same time.
                        ipcSingleTaskMutex = new Mutex(false, "DS4Windows_IPCResultData_SingleTaskMtx");
                        try
                        {
                            bOwnsMutex = ipcSingleTaskMutex.WaitOne(10000);
                        }
                        catch (AbandonedMutexException)
                        {
                            bOwnsMutex = true;
                        }

                        if (bOwnsMutex)
                        {
                            // This process owns the inter-process sync mutex obj. Let's proceed with creating the output MMF file and waiting for a result.
                            bWaitResultData = true;
                            CreateIPCResultDataMMF();
                            ipcNotifyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DS4Windows_IPCResultData_ReadyEvent");
                        }
                        else
                            // If the mtx failed then something must be seriously wrong. Cannot do anything in that case because MMF file may be modified by concurrent processes.
                            bDoSendMsg = false;
                    }

                    if (bDoSendMsg)
                    {
                        cds.dwData = IntPtr.Zero;
                        cds.cbData = args[1].Length;
                        cds.lpData = Marshal.StringToHGlobalAnsi(args[1]);
                        SendMessage(hWndDS4WindowsForm, WM_COPYDATA, IntPtr.Zero, ref cds);

                        if (bWaitResultData)
                            strResult = WaitAndReadIPCResultDataMMF(ipcNotifyEvent);
                    }
                }
                finally
                {
                    // Release the result MMF file in the client process before releasing the mtx and letting other client process to proceed with the same MMF file
                    if (ipcResultDataMMA != null) ipcResultDataMMA.Dispose();
                    if (ipcResultDataMMF != null) ipcResultDataMMF.Dispose();
                    ipcResultDataMMA = null;
                    ipcResultDataMMF = null;

                    // If this was "Query.xxx" cmdline client call then release the inter-process mutex and let other concurrent clients to proceed (if there are anyone waiting for the MMF result file)
                    if (bOwnsMutex && ipcSingleTaskMutex != null)
                        ipcSingleTaskMutex.ReleaseMutex();

                    if (cds.lpData != IntPtr.Zero)
                        Marshal.FreeHGlobal(cds.lpData);
                }
            }

            if (!bWaitResultData && args[1].ToLower().StartsWith("query.") && args[1].ToLower().EndsWith(".apprunning"))
                bWaitResultData = true;

            // The cmd was "Query.xx". Let's dump the result string to console
            if (bWaitResultData)
            {
                strResult = strResult.Trim();

                if (String.IsNullOrEmpty(strResult) && args[1].ToLower().StartsWith("query.") && args[1].ToLower().EndsWith(".apprunning"))
                    strResult = "False";

                Console.WriteLine(strResult);
            }

            if (bDoSendMsg == false)
                Environment.ExitCode = 1000; // Something went wrong with Query.xxx cmd. The cmd was not sent, so return error status 1000
            else if (args[1].ToLower().StartsWith("query.") && args[1].ToLower().EndsWith(".battery"))
            {
                int iBatteryLevel = 1000;
                if(!int.TryParse(strResult, out iBatteryLevel))
                    iBatteryLevel = 1000;
                Environment.ExitCode = iBatteryLevel;
            }
            else if (strResult.ToLower() == "true")
                Environment.ExitCode = 1;
            else
                Environment.ExitCode = 0;
        }
    }
}
