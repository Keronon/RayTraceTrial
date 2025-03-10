using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Another_World
{
    internal partial class ControlConsole
    {
        static void Main(string[] args)
        {
            CancellationToken token = new CancellationTokenSource().Token;
            Task.Run(() => ControlMain.Run(token));

            Task memoryMonitorTask = Worker.MemoryUsageMonitor(token);
            Worker.MinimizeWindow();

            string? input;
            do
            {
                input = Console.ReadLine();
            }
            while (input != "q");
        }

        /// <summary>
        /// Wrap for uncommon methods
        /// </summary>
        internal static partial class Worker
        {
            /// <summary>
            /// Checks memory overflow<br/>
            /// Uses token when memory over 80%<br/>
            /// Kills application when memory over 90%
            /// </summary>
            /// <remarks>Windows only<br\>
            /// Debug only</remarks>
            /// <param name="token">Token for linked process at memory overcap</param>
            /// <returns></returns>
            public static async Task MemoryUsageMonitor(CancellationToken token)
            {
#if WINDOWS && DEBUG
                PerformanceCounter ramCounter = new("Memory", "Available MBytes");
                const float totalMemory = 1024 * 32;

                while (!token.IsCancellationRequested)
                {
                    float availableMemory = ramCounter.NextValue();
                    float usedMemoryPercentage = ((totalMemory - availableMemory) / totalMemory) * 100;

                    if (usedMemoryPercentage > 80)
                    {
                        Console.WriteLine("Использование памяти превышает 80%. Отмена задачи...");
                        token.ThrowIfCancellationRequested();
                    }
                    if (usedMemoryPercentage > 90)
                    {
                        Environment.Exit(1);
                    }

                    await Task.Delay(5000, CancellationToken.None);
                }
#else
                Console.WriteLine($"{nameof(MemoryUsageMonitor)} - DeBug & Windows only");
#endif // WINDOWS && DEBUG
            }

            /// <summary>
            /// Minimizes current console window
            /// </summary>
            public static void MinimizeWindow()
            {
#if WINDOWS
                [DllImport("kernel32.dll")]
                static extern IntPtr GetConsoleWindow();

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

                const int SW_MINIMIZE = 6;
                IntPtr handle = GetConsoleWindow();
                ShowWindow(handle, SW_MINIMIZE);
#else
                Console.WriteLine("The current OS is not supported yet");
#endif // WINDOWS
            }
        }
    }
}
