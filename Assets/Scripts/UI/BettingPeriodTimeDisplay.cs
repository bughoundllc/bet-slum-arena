using TMPro;
using UnityEngine;

namespace bet_slum.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class BettingPeriodTimeDisplay : MonoBehaviour
    {
        private TMP_Text _text;
        private ShowRunner _runner;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _runner = FindAnyObjectByType<ShowRunner>();
        }

        private void Update()
        {
            _text.SetText($"{_runner.RemainingBetDuration:F2}");
        }
    }

}
