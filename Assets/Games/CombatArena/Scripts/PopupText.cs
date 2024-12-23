using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TMP_Text _textObject;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _duration;
    private float _startTime;

    public void Initialize(string displayStr)
    {
        _textObject.SetText(displayStr);
        _startTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - _startTime >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        _textObject.transform.position += _moveSpeed * Vector3.up * Time.deltaTime;
    }
}
