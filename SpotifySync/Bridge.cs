using System.Diagnostics;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using SpotifyAPI.Web;

namespace SpotifySync;

public class Bridge
    {
        private static Bridge? _instance;
        private static readonly object Padlock = new object();

        private StreamingHueClient _client;
        
        private IReadOnlyList<Group>? _groups;

        private Show? _show;
        
        private Group _group;
        private StreamingGroup _streamingGroup;
        private EntertainmentLayer _entertainmentLayer;

        private bool _showActive = false;

        private CancellationTokenSource? _currentShowCancellationTokenSource;
        
        
        public delegate void Notify();

        public event Notify? GroupsChanged;
        
        private IReadOnlyList<Group>? Groups
        {
            get { return _groups; }
            set
            {
                _groups = value;
                GroupsChanged?.Invoke();
            }
        }

        public event Notify? GroupSelected;
        private bool _groupSelectedBool;
        private bool GroupSelectedBool
        {
            get { return _groupSelectedBool; }
            set
            {
                _groupSelectedBool = value;
                if (value)
                {
                    GroupSelected?.Invoke();
                }
            }
        }

        public static Bridge Instance
        {
            get
            {
                lock (Padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new Bridge();
                    }

                    return _instance;
                }
            }
        }


        public async void FindBridge()
        {
            _client = new StreamingHueClient("192.168.0.84",
                "dvP5xOkppd7ZmMZfnEl-UK7EFIqHghOeZ31Gctt1",
                "906A7ECCCD738BE185235DAB83B3A05A");
            
            Groups = await _client.LocalHueClient.GetEntertainmentGroups();

        }

        public bool SelectGroup(String? name = "Bedroom")
        {
            if (name is null)
            {
                return false;
            }
            _group = Groups.FirstOrDefault(item => item.Name == name, defaultValue: null);
            if (_group is null)
            {
                return false;
            }
            ConnectToGroup();
            return true;
        }

        public async void ConnectToGroup()
        {
            _streamingGroup = new StreamingGroup(_group.Locations);
            await _client.Connect(_group.Id);
            _client.AutoUpdate(_streamingGroup, CancellationToken.None);
            _entertainmentLayer = _streamingGroup.GetNewLayer(isBaseLayer: true);
            GroupSelectedBool = true;
        }

        public void DefaultFlashing()
        {
            Console.WriteLine("start");
            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(100);
                _entertainmentLayer.SetState(CancellationToken.None, new RGBColor("FFFFFF"), 0, TimeSpan.FromSeconds(0));
                Thread.Sleep(100);
                _entertainmentLayer.SetState(CancellationToken.None, new RGBColor("FFFFFF"), 1, TimeSpan.FromSeconds(0));
            }
            Console.WriteLine("stop");
        }

        public void StartShow(List<TimeInterval> timeIntervals, long startingTime, bool startExecution = true)
        {
            _show = new Show(timeIntervals, startingTime);
            if (startExecution)
            {
                StartExecution();
            }
        }

        private async void StartExecution()
        {
            _currentShowCancellationTokenSource = new CancellationTokenSource();
            while (!_currentShowCancellationTokenSource!.Token.IsCancellationRequested)
            {
                await Task.Run(ExecuteFrame);
            }
        }
        
        public void ResumeExecution(long newTimestamp)
        {
            if (_show is null)
            {
                Console.WriteLine("Resuming attempted before the creation of the show");
                return;
            }
            _show.ShiftStartingTime(newTimestamp);
            StartExecution();   
        }

        public void CancelShow()
        {
            _currentShowCancellationTokenSource?.Cancel();
        }

        public bool NoExistingShow()
        {
            return _show is null;
        }

        private void ExecuteFrame()
        {
            String colour = _show!.Get();
            if (colour == "end")
            {
                return;
            }
            
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            int time = (int)(_show.timestamps[_show.currentPoint] - unixTimeMilliseconds);
            Debug.Print("Time to Sleep: " + time + "   -  " + _show.timestamps[_show.currentPoint] );
            if (time <= 0)
            {
                return;
            }
            Thread.Sleep(time);
            
            _entertainmentLayer.SetState(CancellationToken.None, new RGBColor(colour), 1,
                TimeSpan.FromSeconds(0));
        }
    }