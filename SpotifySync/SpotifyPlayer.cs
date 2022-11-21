// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)

using System.Diagnostics;
using System.Text;
using System.Net;

namespace SpotifySync;

public class SpotifyPlayer
{
    private static HttpListener? _listener;
    private const string Url = "http://localhost:8000/";

    private string? _pageData;

    private struct PositionData
    {
        public long Timestamp;
        public int Position;
        public bool Paused;
        public string Id;
    }

    private PositionData _positionData = new()
    {
        Timestamp = 0, Position = 0, Paused = false, Id = ""
    };

    public string GetSongId()
    {
        return _positionData.Id; 
    }

    public bool IsPlaying()
    {
        return !_positionData.Paused;
    }

    public long GetStartingTimestamp()
    {
        return _positionData.Timestamp - _positionData.Position;
    }


    public delegate void PausedNotify(bool paused);
    public event PausedNotify? PausedStateChanged;

    public delegate void IdNotify(string id);
    public event IdNotify? IdChanged;

    public delegate void ScrubbedNotify();
    public event ScrubbedNotify? SongScrubbed;


    private async Task HandleIncomingConnections()
    {
        while (true)
        {
            // Will wait here until we hear from a connection
            var ctx = await _listener!.GetContextAsync();

            // Peel out the requests and response objects
            var req = ctx.Request;
            var resp = ctx.Response;

            if (req.Url == null)
            {
                continue;
            }

            // Extract the player data from the URL
            var parts = req.Url.ToString()[22..].Split("/");
            if (parts.Length == 4 && parts[0] != "")
            {
                var newTimestamp = long.Parse(parts[0]);
                var newPosition = int.Parse(parts[1]);
                var newPaused = bool.Parse(parts[2]);
                var difference = newPosition - _positionData.Position - (newTimestamp - _positionData.Timestamp);
                if ((difference > 10 || difference < -10) && !newPaused)
                {
                    SongScrubbed?.Invoke();   
                }

                _positionData.Timestamp = newTimestamp;
                _positionData.Position = newPosition;
                // If the new paused value is different, notify the subscribers
                if (newPaused != _positionData.Paused)
                {
                    _positionData.Paused = newPaused;
                    PausedStateChanged?.Invoke(newPaused);
                }
                // If the id has changed, notify the subscribers
                Console.WriteLine(parts[3] + " " + _positionData.Id);
                if (parts[3] != _positionData.Id)
                {
                    _positionData.Id = parts[3];
                    IdChanged?.Invoke(parts[3]);
                }

                Console.WriteLine(_positionData.Timestamp + " " + _positionData.Position + " " + _positionData.Paused
                                  + " " + _positionData.Id);
                resp.Close();
            }
            else
            {
                var data = Encoding.UTF8.GetBytes(_pageData!);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data);
                resp.Close();
            }
        }
    }


    public async void CreatePlayer(string? accessToken)
    {
        _pageData = (await File.ReadAllTextAsync(@"C:\Users\alex\RiderProjects\SpotifySync\SpotifySync\Player.html")).Replace("[My access token]", accessToken);
        
        // Create a Http server and start listening for incoming connections
        _listener = new HttpListener();
        _listener.Prefixes.Add(Url);
        _listener.Start();
        Console.WriteLine("Listening for connections on {0}", Url);

        Process.Start("explorer", Url);

        // Handle requests
        Task listenTask = HandleIncomingConnections(); 
        listenTask.GetAwaiter().GetResult();
        
        
        // Close the listener
        _listener.Close();
    }
    
    
}