using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifySync;

public class Spotify
{
    private const string ClientId = "7c9c2d0b036e4ed5b643d41e4921428a";

    private readonly SpotifyPlayer _spotifyPlayer = new SpotifyPlayer();
    private string? AccessToken { get; set; }
    private SpotifyClient? SpotClient { get; set; }

    public int FetchCurrentPosition()
    {
        return 0;
    }

    public long FetchStartingTimestamp()
    {
        return _spotifyPlayer.GetStartingTimestamp();
    }

    // Fetch AudioAnalysis and pass it to subscribers when complete
    private TrackAudioAnalysis? _audioAnalysis;
    public delegate void AnalysisNotify(TrackAudioAnalysis analysis);
    public event AnalysisNotify? TrackAudioAnalysisChanged;

    private async void GetTrackAudioAnalysis(string id)
    {
        if (SpotClient == null)
        {
            Console.WriteLine("SpotClient is Null - Wait before accessing AudioAnalysis");
            return;
        }

        _audioAnalysis = await SpotClient.Tracks.GetAudioAnalysis(id);
        TrackAudioAnalysisChanged?.Invoke(_audioAnalysis);
    }

    public delegate void Notify();
    public event Notify? MusicPaused;
    // Music played is used for resuming and scrubbing
    public event Notify? MusicPlayed;
    
    private void OnPausedStateChanged(bool paused)
    {
        if (paused)
        {
            MusicPaused?.Invoke();
        }
        else
        {
            MusicPlayed?.Invoke();
        }
    }

    private void OnSongScrubbed()
    {
        MusicPlayed?.Invoke();
    }

    public Spotify()
    {
        _spotifyPlayer.IdChanged += GetTrackAudioAnalysis;
        _spotifyPlayer.PausedStateChanged += OnPausedStateChanged;
        _spotifyPlayer.SongScrubbed += OnSongScrubbed;
    }


    public bool IsPlayerPlaying()
    {
        return _spotifyPlayer.IsPlaying();
    }
    
    public async void OAuth()
    {
        await StartOAuth();
    }
    
    private static EmbedIOAuthServer? _server;

    private async Task StartOAuth()
    {
        // Make sure "http://localhost:5000/callback" is in your spotify application as redirect uri!
        _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
        await _server.Start();

        _server.ImplictGrantReceived += OnImplicitGrantReceived;
        _server.ErrorReceived += OnErrorReceived;

        var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Token)
        {
            Scope = new List<string> { Scopes.UserReadEmail, Scopes.UserReadCurrentlyPlaying, Scopes.AppRemoteControl, Scopes.Streaming }
        };
        BrowserUtil.Open(request.ToUri());
    }

    private async Task OnImplicitGrantReceived(object sender, ImplictGrantResponse response)
    {
        if (_server != null)
        {
            await _server.Stop();
            AccessToken = response.AccessToken;
            SpotClient = new SpotifyClient(AccessToken);
            var thread = new Thread(() => _spotifyPlayer.CreatePlayer(AccessToken));
            thread.Start();
        }
    }

    private async Task OnErrorReceived(object? sender, string? error, string? state)
    {
        Console.WriteLine($"Aborting authorization, error received: {error}");
        if (_server != null)
        {
            await _server.Stop();
        }
    }



}