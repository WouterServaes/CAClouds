using System;
using UnityEngine;
using UnityEngine.Events;

public class CellularAutomaton : MonoBehaviour
{
    [SerializeField] private CASettings _CASettings = null;
    private bool _IsPaused = true;

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused

    public UnityAction ResetAction; //resets ca

    //Update timer
    private float _ElapsedSec = 0f;

    //Metric saver
    private Metrics _Metrics = null;

    private void Start()
    {
        _Metrics = GetComponent<Metrics>();
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
    }

    private void PauseContinue(bool isPaused)
    {
        _IsPaused = isPaused;

        if (_IsPaused) Debug.Log("Paused");
        else Debug.Log("Continuing");
    }

    private void ResetCA()
    {
        Debug.Log("Reset CA");
        throw new NotImplementedException();
    }

    private void Update()
    {
        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
                UpdateCA();
            }
        }
    }

    private void UpdateCA()
    {
        _Metrics.StartCalcTimer();

        System.Threading.Thread.Sleep(1500);
        Debug.Log("Updated CA");

        _Metrics.StopCalcTimer();
    }
}