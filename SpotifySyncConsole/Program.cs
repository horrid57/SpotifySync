using SpotifyAPI.Web;
using SpotifySync;

Bridge bridge = Bridge.Instance;

// Create the Spotify Instance and local spotify player - start receiving playtime info
Spotify spotify = new Spotify();
spotify.OAuth();

// Connect to the bridge and start an action automatically
void SelectGroup()
{
    // Spotify listeners are added only after the group has been selected to prevent errors
    bridge.GroupSelected += AddSpotifyListeners;
    Console.WriteLine("Enter Zone Name: ");
    do
    {
        Console.WriteLine("Enter Zone Name: ");
    } while (!bridge.SelectGroup(Console.ReadLine()));
}
bridge.GroupsChanged += SelectGroup;
bridge.FindBridge();

void AddSpotifyListeners()
{
    spotify.TrackAudioAnalysisChanged += OnAnalysisFetched;
    spotify.MusicPaused += bridge.CancelShow;
    spotify.MusicPlayed += OnMusicResumed;
}
void OnAnalysisFetched(TrackAudioAnalysis audioAnalysis)
{
    Console.WriteLine("Audio Analysis Fetched");
    bridge.CancelShow();
    bridge.StartShow(audioAnalysis.Beats, spotify.FetchStartingTimestamp(), spotify.IsPlayerPlaying());
}
void OnMusicResumed()
{
    bridge.CancelShow();
    bridge.ResumeExecution(spotify.FetchStartingTimestamp());
}

while (true)
{
    String? text = Console.ReadLine();
    if (text == ":q")
    {
        break;
    }
}
// todo Create custom light shows 
    // todo Create a visualisation of the spotify analysis over time
    // todo Save and load shows from a text file
    // todo Edit shows and check if they work correctly (somehow) 