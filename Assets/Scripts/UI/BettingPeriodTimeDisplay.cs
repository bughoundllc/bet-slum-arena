using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace bet_slum.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class BettingPeriodTimeDisplay : MonoBehaviour
    {
        private TMP_Text _text;
        private ShowRunner _runner;
        public Image img;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _runner = FindAnyObjectByType<ShowRunner>();
        }

        private void Update()
        {
            if(!_runner.BettingIsEnabled)
            {
                _text.SetText("");
                img.enabled = false;
                return;
            }

            img.enabled = true;
            _text.SetText($"/bet\n{FormatTime(math.max(0f,  _runner.RemainingBetDuration))}");
        }

        string FormatTime(float seconds)
        {
            // Calculate minutes, seconds, and milliseconds
            int totalMilliseconds = (int)(seconds * 1000);
            int minutes = totalMilliseconds / 60000;
            int remainingMilliseconds = totalMilliseconds % 60000;
            int secs = remainingMilliseconds / 1000;
            int millis = remainingMilliseconds % 1000;

            // Format as MM:SS:LL (only showing first two digits of milliseconds)
            return string.Format("{0:00}:{1:00}:{2:00}", minutes, secs, millis / 10);
        }
    }

}
