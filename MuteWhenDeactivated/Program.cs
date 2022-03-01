using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MuteWhenDeactivated
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        static HashSet<int> _rules = new HashSet<int>();
        static int _delay = 10;
        static CancellationTokenSource _source = new CancellationTokenSource();
        static CancellationToken _token = _source.Token;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) => { if (_source != null) _source.Cancel(); };

            Task.Run(() =>
            {
                while (true)
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != null)
                    {
                        uint pid;
                        GetWindowThreadProcessId(hwnd, out pid);

                        foreach (var item in _rules)
                        {
                            if (item == pid)
                                AudioManager.SetApplicationMute(item, false);
                            else
                                AudioManager.SetApplicationMute(item, true);
                        }
                    }

                    if (_delay > 0) Thread.Sleep(_delay);
                }
            }, _token);

            while (true)
            {
                var processes = Process.GetProcesses().OrderBy(process => process.ProcessName);
                foreach (var process in processes)
                {
                    var muteStatus = AudioManager.GetApplicationMute(process.Id);
                    if (muteStatus.HasValue)
                    {
                        if (_rules.Contains(process.Id))
                            Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[{0:D6}] {1} {2}", process.Id, process.ProcessName, (muteStatus.Value) ? "Muted" : "Unmuted");
                        Console.ResetColor();
                    }
                }

                Console.Write("Process Id: ");

                int pid = 0;
                if (int.TryParse(Console.ReadLine(), out pid))
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (_rules.Contains(pid))
                        {
                            _rules.Remove(pid);
                            Console.WriteLine($"Disabled");
                            AudioManager.SetApplicationMute(pid, false);
                        }
                        else
                        {
                            _rules.Add(pid);
                            Console.WriteLine($"Enabled");
                        }
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Please enter a valid process id.");
                    }
                }
                else
                {
                    Console.WriteLine("Please enter a valid number.");
                }

                Thread.Sleep(1500);
                Console.Clear();
            }
        }
    }
}
