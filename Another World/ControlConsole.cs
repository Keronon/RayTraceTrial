using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Another_World
{
    internal class ControlConsole
    {

        [LibraryImport("user32.dll")]
        static extern IntPtr GetConsoleWindow();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;

        static void Main()
        {
            CancellationToken token = new CancellationTokenSource().Token;
#if DEBUG
            Task memoryMonitorTask = MemoryUsageMonitor(token);
#endif // DEBUG
            Task.Run(() => ControlMain.Run(token));

            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_MINIMIZE);

            string? input;
            do
            {
                input = Console.ReadLine();
            }
            while (input != "q");
        }

#if DEBUG
#pragma warning disable CA1416
        /// <summary>
        /// Checks memory overflow<br/>
        /// Uses token when memory over 80%<br/>
        /// Kills application when memory over 90%
        /// </summary>
        /// <remarks>only for Windows</remarks>
        /// <param name="token">Token for linked process on memory overcap</param>
        /// <returns></returns>
        static async Task MemoryUsageMonitor(CancellationToken token)
        {
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
        }
#pragma warning restore
#endif // DEBUG
    }
}
