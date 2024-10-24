using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TFDJ
{
    internal class ChatPrinter
    {
        private string ExecDirectory { get; set; }
        private string ExecFile { get; set; }
        private ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();
        public ChatPrinter(string execDirectory, string execFile)
        {
            ExecDirectory = execDirectory;
            ExecFile = execFile;

            // Create config file if it doesn't exist
            if (!System.IO.File.Exists(ExecDirectory + ExecFile))
            {
                System.IO.File.Create(ExecDirectory + ExecFile).Close();
                Console.WriteLine($"Created {ExecFile}");
            }

            Console.WriteLine($"Sending chat messages through {ExecDirectory}{ExecFile}");
        }

        public void Start()
        {
            Task.Run(PrintChat);
        }

        public void Print(string message)
        {
            MessageQueue.Enqueue(message);
        }

        private async Task PrintChat()
        {
            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            while (true)
            {
                if (MessageQueue.TryDequeue(out string? message) && message != null)
                {
                    Console.WriteLine($"Printing \"{message}\" to game");
                    // Write message to config file
                    //System.IO.File.WriteAllText(ExecDirectory + ExecFile, $"say_team {message}");
                    System.IO.File.WriteAllText(ExecDirectory + ExecFile, $"say {message}");

                    // Wait 1 s before sending message
                    await Task.Delay(1000);
                    // Press keypad insert for 30 ms to send message
                    keybd_event(0x60, 0x52, 0x0, UIntPtr.Zero);
                    await Task.Delay(30);
                    keybd_event(0x60, 0x52, 0x2, UIntPtr.Zero);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}
