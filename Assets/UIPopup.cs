using TMPro;
using UnityEngine;

public class UIPopup : MonoBehaviour
{
    [SerializeField] private float AutoHideDuration = 2f;
    [SerializeField] private TMP_Text Text;

    private float _lastTimeTriggered = 0f;

    public void Show(string displayText = null)
    {
        _lastTimeTriggered = Time.realtimeSinceStartup;

        gameObject.SetActive(true);
        Text.SetText(displayText);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        Hide();
    }

    private void Update()
    {
        if(Time.realtimeSinceStartup - _lastTimeTriggered >= AutoHideDuration)
        {
            Hide();
        }
    }
}
