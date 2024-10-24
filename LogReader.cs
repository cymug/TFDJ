using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TFDJ
{
    public enum CommandType
    {
        Request,
        Skip,
        Clear,
        Ban,
        Help,
        Queue
    }

    public struct Command
    {
        public string Player;
        public CommandType Type;
        public List<string> Arguments;
    }
    internal class LogReader
    {
        private string LogDirectory { get; set; }
        private string LogFile { get; set; }
        private ConcurrentQueue<Command> CommandQueue { get; set; }

        public LogReader(string logDirectory, string logFile, ConcurrentQueue<Command> commandQueue)
        {
            LogDirectory = logDirectory;
            LogFile = logFile;
            CommandQueue = commandQueue;
        }

        public void Start()
        {
            Task.Run(ParseLog);
        }

        private async Task ParseLog()
        {
            Console.WriteLine($"Monitoring {LogDirectory}{LogFile}");
            var fs = new FileStream(LogDirectory + LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(fs.Length, SeekOrigin.Begin); // Skip to end of file
            using (var sr = new StreamReader(fs))
            {
                var s = "";
                while (true)
                {
                    s = sr.ReadLine();
                    if (s != null)
                    {
                        //Console.WriteLine(s);

                        Match match = Regex.Match(s, @"^(.+?) : (.+)$");
                        if (match.Success)
                        {
                            string player = match.Groups[1].Value.Trim();
                            string message = match.Groups[2].Value.Trim();
                            // Break up message into command and arguments
                            string[] split = message.Split(' ');
                            string command = split[0];
                            List<string> arguments = new List<string>();
                            for (int i = 1; i < split.Length; i++)
                            {
                                arguments.Add(split[i]);
                            }

                            // Check if command is a request
                            if (command == "!request")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Request;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                            else if (command == "!skip")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Skip;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                            else if (command == "!clear")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Clear;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                            else if (command == "!ban")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Ban;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                            else if (command == "!help")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Help;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                            else if (command == "!queue")
                            {
                                Command chatCommand = new Command();
                                chatCommand.Type = CommandType.Queue;
                                chatCommand.Player = player;
                                chatCommand.Arguments = arguments;
                                CommandQueue.Enqueue(chatCommand);
                            }
                        }
                        else
                        {
                            await Task.Delay(1);
                        }
                    }
                }
            }
        }
    }
}
