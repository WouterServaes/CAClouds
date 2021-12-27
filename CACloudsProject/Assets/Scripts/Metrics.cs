using System.Collections.Generic;
using UnityEngine;

public class Metrics : MonoBehaviour
{
    public struct Timer
    {
        public float TimeAtStartCalc; //time from start of play as float at start of the calculation
        public float TimeAtEndCalc; //time from start of play as float after the calculation
        public float CalcDuration => TimeAtEndCalc - TimeAtStartCalc;
    }

    private List<Timer> _SavedTimes = new List<Timer>();
    public List<Timer> SavedTimes => _SavedTimes;
    private Timer _CurrentTime;
    
    public void StartCalcTimer()
    {
        _CurrentTime = new Timer();
        _CurrentTime.TimeAtStartCalc = Time.realtimeSinceStartup;
    }

    public void StopCalcTimer()
    {
        _CurrentTime.TimeAtEndCalc = Time.realtimeSinceStartup;        
        _SavedTimes.Add(_CurrentTime);
    }

    public float GetAverageCalcTime()
    {
        float totalTime = 0f;
        foreach (var timer in _SavedTimes)
            totalTime += timer.CalcDuration;
        return totalTime / _SavedTimes.Count;
    }
    public void PrintMetrics()
    {
        Debug.Log("================ METRICS");
        foreach(var timer in _SavedTimes)
        {
            string logStr = string.Format("start: {0} \nend: {1} \nduration: {2}"
                , timer.TimeAtStartCalc, timer.TimeAtEndCalc, timer.CalcDuration);
            Debug.Log(logStr);
        }

        string averageStr = string.Format("average calculation time: {0}", GetAverageCalcTime());
        Debug.Log(averageStr);
        Debug.Log("================ END METRICS");
    }
}
