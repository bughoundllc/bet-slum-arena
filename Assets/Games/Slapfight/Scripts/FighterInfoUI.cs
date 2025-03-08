using bet_slum.Slapfight;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class FighterInfoUI : MonoBehaviour
{
    public TMP_Text NameLabel;
    public Slider HPSlider;
    public Slider HPBackgroundSlider;
    public ItemRowController itemRow;
    public float bgSliderSpeed;

    private void Update()
    {
        HPBackgroundSlider.value = math.clamp(HPBackgroundSlider.value - bgSliderSpeed * Time.deltaTime, HPSlider.value, 1f);
    }
}
