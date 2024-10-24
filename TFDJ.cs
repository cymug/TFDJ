using AngleSharp.Io;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Search;

namespace TFDJ
{
    internal class TFDJ
    {
        static void Main()
        {
            string TF_DIRECTORY_WIN = "C:/Program Files (x86)/Steam/steamapps/common/Team Fortress 2/";
            string LOG_FILE = "console.log";
            string TF_PATH_REL = "tf/";
            string CFG_PATH_REL = "cfg/";
            string AUTOEXEC_FILE = "autoexec.cfg";
            string TFDJ_EXEC_FILE = "tfdj.cfg";
            string TFDJ_CFG_FILE = "config.txt";
            string ADMIN_NAME = "cymug";
            int MAX_SONG_LENGTH = 5 * 60; // Seconds

            List<string> BannedPlayers = new List<string>();
            ConcurrentQueue<Command> CommandQueue = new ConcurrentQueue<Command>();
            YoutubeClient youtubeClient = new YoutubeClient();

            // Check if config file exists
            if (System.IO.File.Exists(TFDJ_CFG_FILE))
            {
                // Parse config file and extract game location, admin name, max song length, and banned players
                string[] lines = System.IO.File.ReadAllLines(TFDJ_CFG_FILE);
                try
                {
                    TF_DIRECTORY_WIN = lines[0];
                    ADMIN_NAME = lines[1];
                    MAX_SONG_LENGTH = int.Parse(lines[2]);
                    BannedPlayers = lines.Skip(3).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing config file. Please restart the program to create a new file.");
                    Console.WriteLine(e.Message);
                    // Delete config file
                    System.IO.File.Delete(TFDJ_CFG_FILE);
                    return;
                }

                Console.WriteLine("Config file loaded");
                Console.WriteLine($"Game location: {TF_DIRECTORY_WIN}");
                Console.WriteLine($"Admin name: {ADMIN_NAME}");
                Console.WriteLine($"Max song length: {MAX_SONG_LENGTH} seconds");
                Console.WriteLine($"{BannedPlayers.Count} banned players");
            }
            else // Guide user through creating config file
            {
                // Create config file
                Console.WriteLine("Config file not found. Creating config file...");
                System.IO.File.Create(TFDJ_CFG_FILE).Close();


                string? response = null;
                // Check if default game directory exists
                if (System.IO.Directory.Exists(TF_DIRECTORY_WIN))
                {
                    Console.WriteLine("Found TF2 installation");
                }
                else
                {
                    response = null;
                    while (response == null)
                    {
                        Console.Write("Enter the path to your Team Fortress 2 directory: ");
                        response = Console.ReadLine();
                    }
                    TF_DIRECTORY_WIN = response;
                }
                while (response == null)
                {
                    Console.Write("Enter your in-game username: ");
                    response = Console.ReadLine();
                }
                ADMIN_NAME = response;

                response = null;
                while (response == null)
                {
                    Console.Write("Enter the maximum song length in seconds: ");
                    response = Console.ReadLine();
                }
                MAX_SONG_LENGTH = int.Parse(response);

                // Save config file
                System.IO.File.WriteAllText(TFDJ_CFG_FILE, $"{TF_DIRECTORY_WIN}\n{ADMIN_NAME}\n{MAX_SONG_LENGTH}\n");
            }

            // Patch autoexec.cfg to log to console.log and execute tfdj.cfg on insert key press
            // TODO

            LogReader reader = new LogReader(TF_DIRECTORY_WIN + TF_PATH_REL, LOG_FILE, CommandQueue);
            ChatPrinter printer = new ChatPrinter(TF_DIRECTORY_WIN + TF_PATH_REL + CFG_PATH_REL, TFDJ_EXEC_FILE);
            MusicPlayer player = new MusicPlayer(printer);
            reader.Start();
            printer.Start();
            player.Start();

            while (true)
            {
                // Pull from command queue
                if (CommandQueue.TryDequeue(out Command command))
                {

                    // Handle dead or team players
                    string commandPlayer = command.Player;
                    // Check if player starts with *DEAD*, *SPEC*, or (TEAM)
                    if (commandPlayer.StartsWith("*DEAD*") || commandPlayer.StartsWith("*SPEC*") || commandPlayer.StartsWith("(TEAM)"))
                    {
                        // Remove prefix
                        commandPlayer = commandPlayer.Substring(commandPlayer.IndexOf(" ") + 1);
                    }

                    switch (command.Type)
                    {
                        case CommandType.Request:
                            if (command.Arguments.Count > 0)
                            {
                                // Check if player is banned
                                if (BannedPlayers.Contains(commandPlayer))
                                {
                                    printer.Print($"{commandPlayer}, you aren't allowed to make requests");
                                    continue;
                                }

                                // Check if video is a youtube link using regex
                                if (Regex.IsMatch(command.Arguments[0], @"^(http(s)?://)?((w){3}.)?youtu(be|.be)?(\.com)?/.+"))
                                {
                                    // Check if video is valid
                                    try
                                    {
                                        var metadata = youtubeClient.Videos.GetAsync(command.Arguments[0]).Result;
                                        if (metadata.Duration?.TotalSeconds > MAX_SONG_LENGTH)
                                        {
                                            printer.Print($"{metadata.Title} is too long!");
                                            continue;
                                        }
                                        var duration = metadata.Duration ?? TimeSpan.Zero;
                                        var minutes = metadata.Duration?.Minutes ?? 0;
                                        var seconds = metadata.Duration?.Seconds;
                                        printer.Print($"{metadata.Title} ({minutes}:{seconds}) requested by {commandPlayer}");
                                        player.QueueSong(command.Arguments[0], metadata.Title, commandPlayer);

                                    }
                                    catch (Exception e) // System.ArgumentException indicates nonexistent video
                                    {
                                        // Check if exception is due to invalid video
                                        if (e.GetType() == typeof(System.ArgumentException))
                                        {
                                            printer.Print($"{command.Arguments[0]} is not a valid Youtube link!");
                                            continue;
                                        }
                                    }
                                }
                                else // Not a youtube link, search for video
                                {
                                    var searchStr = "";
                                    for (int i = 0; i < command.Arguments.Count; i++)
                                    {
                                        searchStr += command.Arguments[i];
                                        if (i < command.Arguments.Count - 1)
                                        {
                                            searchStr += " ";
                                        }
                                    }
                                    printer.Print($"Searching for {searchStr}");
                                    var results = youtubeClient.Search.GetVideosAsync(searchStr).GetAwaiter().GetResult();
                                    var found = false;
                                    // Find first video with duration less than max song length
                                    foreach (var result in results)
                                    {
                                        var metadata = youtubeClient.Videos.GetAsync(result.Id).Result;
                                        if (metadata.Duration?.TotalSeconds > MAX_SONG_LENGTH)
                                        {
                                            continue;
                                        }
                                        var duration = metadata.Duration ?? TimeSpan.Zero;
                                        var minutes = metadata.Duration?.Minutes ?? 0;
                                        var seconds = metadata.Duration?.Seconds;
                                        printer.Print($"{metadata.Title} ({minutes}:{seconds}) requested by {commandPlayer}");
                                        player.QueueSong(result.Id, metadata.Title, commandPlayer);
                                        found = true;
                                        break;
                                    }

                                    if (!found)
                                    {
                                        // Only reached if no valid video found
                                        printer.Print($"No results found for {searchStr}");
                                    }
                                }
                            }
                            break;
                        case CommandType.Skip:
                            if (commandPlayer == ADMIN_NAME)
                            {
                                //printer.Print($"{commandPlayer} skipped current song");
                                player.SkipSong();
                            }
                            else
                            {
                                printer.Print($"{commandPlayer}, you don't have permission to use that command");
                            }
                            break;
                        case CommandType.Clear:
                            if (commandPlayer == ADMIN_NAME)
                            {
                                printer.Print($"{commandPlayer} cleared the queue");
                                player.ClearQueue();
                            }
                            else
                            {
                                printer.Print($"{commandPlayer}, you don't have permission to use that command");
                            }
                            break;
                        case CommandType.Queue:
                            player.PrintQueue();
                            break;
                        case CommandType.Ban:
                            if (commandPlayer == ADMIN_NAME)
                            {
                                // Combine all arguments into one string to handle names with spaces
                                string banPlayer = "";
                                for (int i = 0; i < command.Arguments.Count; i++)
                                {
                                    banPlayer += command.Arguments[i];
                                    if (i < command.Arguments.Count - 1)
                                    {
                                        banPlayer += " ";
                                    }
                                }

                                printer.Print($"Banned {banPlayer}");
                                BannedPlayers.Add(banPlayer);
                                // Save banned players to config file
                                System.IO.File.AppendAllText(TFDJ_CFG_FILE, $"{banPlayer}\n");
                            }
                            break;
                        case CommandType.Help:
                            printer.Print("TFDJ commands: !request <Youtube link>, !skip, !clear, !queue");
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Task.Delay(10).Wait();
                }
            }
        }
    }
}