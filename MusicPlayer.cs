using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace TFDJ
{
    internal class MusicPlayer
    {
        public struct Song
        {
            public string URL;
            public string Title;
            public string Requester;
        }

        private static YoutubeClient youtubeClient = new YoutubeClient();
        private static ConcurrentQueue<Song> SongQueue = new ConcurrentQueue<Song>();
        private static ChatPrinter? printer = null;
        private static Song? CurrentSong = null;


        // Audio out instance
        private static WaveOutEvent? outputDevice = null;

        public MusicPlayer(ChatPrinter chatPrinter)
        {
            printer = chatPrinter;
        }

        public void QueueSong(string url, string title, string requester)
        {
            SongQueue.Enqueue(new Song { URL = url, Title = title, Requester = requester });
        }

        public void Start()
        {
            // Find output device
            int id = -1;
            for (int i = -1; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                if (capabilities.ProductName.Contains("VB-Audio"))
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                Console.WriteLine($"Playing audio through {WaveOut.GetCapabilities(id).ProductName}");
            }
            else
            {
                Console.WriteLine($"Failed to find a valid output device");
            }

            outputDevice = new WaveOutEvent() { DeviceNumber = id, DesiredLatency = 250 };

            Task.Run(PlayMusic);
        }

        public void PrintQueue()
        {
            if (SongQueue.Count == 0 && printer != null)
            {
                printer.Print("Queue is empty");
            }
            else if (printer != null)
            {
                printer.Print($"Songs queued: {SongQueue.Count}");
                foreach (Song song in SongQueue)
                {
                    printer.Print($"{song.Title} requested by {song.Requester}");
                }
            }
        }

        public void ClearQueue()
        {
            if (SongQueue.Count > 0)
            {
                SongQueue = new ConcurrentQueue<Song>();
            }
        }

        public void SkipSong()
        {
            if (CurrentSong == null && printer != null)
            {
                Console.WriteLine($"Queue is empty");
                printer.Print($"Nothing to skip, queue is empty");
            }
            if (outputDevice != null)
            {
                outputDevice.Stop();
                if (CurrentSong != null && printer != null)
                {
                    Console.WriteLine($"Skipped {CurrentSong?.Title}");
                    printer.Print($"Skipped {CurrentSong?.Title}");
                }
            }
        }

        private static async Task PlayMusic()
        {
            while (true)
            {
                if (SongQueue.TryDequeue(out Song song))
                {
                    if (outputDevice == null)
                    {
                        Console.WriteLine("No output device found");
                        continue;
                    }
                    try
                    {
                        var streamManifest = youtubeClient.Videos.Streams.GetManifestAsync(song.URL).Result;
                        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                        var stream = youtubeClient.Videos.Streams.GetAsync(audioStreamInfo).Result;

                        CurrentSong = song;
                        Console.WriteLine($"Now playing {song.Title} from {song.Requester}");
                        if (printer != null)
                        {
                            printer.Print($"Now playing {song.Title} from {song.Requester}");
                        }

                        string tempFile = Path.GetTempFileName();
                        using (var fileStream = File.Create(tempFile))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        // Use MediaFoundationReader to decode the audio
                        using (var reader = new MediaFoundationReader(tempFile))
                        {
                            outputDevice.Init(reader);
                            outputDevice.Play();

                            // Wait for playback to finish
                            while (outputDevice.PlaybackState == PlaybackState.Playing)
                            {
                                await Task.Delay(1000);
                            }
                        }

                        // Clean up the temporary file
                        File.Delete(tempFile);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to play song: {e.Message}");
                        if (printer != null)
                        {
                            printer.Print($"Failed to play song: {e.Message}");
                        }
                    }
                }
                else
                {
                    CurrentSong = null;
                    await Task.Delay(10);
                }
            }
        }
    }
}
