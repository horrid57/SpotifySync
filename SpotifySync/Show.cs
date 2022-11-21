using System.Diagnostics;
using SpotifyAPI.Web;

namespace SpotifySync;

public class Show
{
    public long[] timestamps;
    public String[] colours;
    public int currentPoint = 0;
    public long startTime = 0;
    private long _averageBeatTime;

    public Show(List<TimeInterval> timeIntervals, long startingTime)
    {
        startTime = startingTime;
        timestamps = new long[timeIntervals.Count];
        colours = new string[timeIntervals.Count];
            
        DateTimeOffset now = DateTimeOffset.UtcNow;
        long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

        Debug.Print(startingTime + " then " + unixTimeMilliseconds);

        for (int i = 0; i < timeIntervals.Count; i++)
        {
            timestamps[i] = (long)(timeIntervals[i].Start * 1000) + startingTime;
            Debug.Print(timeIntervals[i].Start + " : " + timestamps[i] + ":    " + unixTimeMilliseconds);
            if (timestamps[i] < unixTimeMilliseconds)
            {
                currentPoint = i + 1;
            }
            if (i % 2 == 0)
            {
                colours[i] = "FF0000";
            }
            else
            {
                colours[i] = "FFFFFF";
            }
        }
        _averageBeatTime = (timestamps[^1] - timestamps[0]) / timestamps.Length;
    }


    public String Get()
    {
        currentPoint += 1;
        Debug.Print(timestamps.Length + "  " + currentPoint);
        if (currentPoint >= timestamps.Length)
        {
            return "end";
        }

        // difference is how far ahead it is
        var difference = timestamps[currentPoint] - DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (Math.Abs(difference) > 1.5 * _averageBeatTime)
        {
            Debug.Print(difference + " Moving to current point");
            MoveToCurrentPoint();
        } 

        var time = timestamps[currentPoint] - startTime;
        Debug.Print((int)(time / 60000) + ":" + time % 60000);
        return colours[currentPoint];
    }

    private void MoveToCurrentPoint()
    {
        var timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        currentPoint = 0;
        while (timestamps[currentPoint] < timeNow && currentPoint < timestamps.Length - 1)
        {
            currentPoint += 1;
        }
    }

    public void ShiftStartingTime(long newTimestamp)
    {
        long difference = newTimestamp - startTime;
        startTime = newTimestamp;
        long[] newTimestampsArray = new long[timestamps.Length];
        for (int i = 0; i < timestamps.Length; i++)
        {
            newTimestampsArray[i] = timestamps[i] + difference;
        }
        timestamps = newTimestampsArray;
    }
}