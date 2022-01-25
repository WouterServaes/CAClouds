using System.Collections.Generic;
using UnityEngine;

public class Metrics : MonoBehaviour
{
    public struct Timer
    {
        public Timer(float timeAtStart, int generation = 0)
        {
            TimeAtStartCalc = timeAtStart;
            TimeAtEndCalc = 0f;
            Generation = generation;
        }
        public float TimeAtStartCalc; //time from start of play as float at start of the calculation
        public float TimeAtEndCalc; //time from start of play as float after the calculation
        public float CalcDuration => TimeAtEndCalc - TimeAtStartCalc;
        public int Generation;
    }

    private List<Timer> _SavedTimes = new List<Timer>();
    public List<Timer> SavedTimes => _SavedTimes;
    private Timer _CurrentTime;
    
    public void StartCalcTimer(int generation = 0)
    {
        _CurrentTime = new Timer(Time.realtimeSinceStartup, generation);
    }

    public void StopCalcTimer(int generation = 0)
    {
        _CurrentTime.TimeAtEndCalc = Time.realtimeSinceStartup;  
        _CurrentTime.Generation = generation;
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
            string logStr = string.Format("gen: {0} \nstart: {1} \nend: {2} \nduration (ms): {3}"
                ,timer.Generation, timer.TimeAtStartCalc, timer.TimeAtEndCalc, timer.CalcDuration * 1000f);
            Debug.Log(logStr);
        }

        string averageStr = string.Format("average calculation time (ms): {0}", GetAverageCalcTime() * 1000f);
        Debug.Log(averageStr);
        Debug.Log("================ END METRICS");
    }

    public void ClearMetrics()
    {
        _SavedTimes.Clear();
    }
}
