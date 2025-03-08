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
    public TMP_Text Stat1Label;
    public TMP_Text Stat2Label;
    public TMP_Text Stat3Label;

    private void Update()
    {
        HPBackgroundSlider.value = math.clamp(HPBackgroundSlider.value - bgSliderSpeed * Time.deltaTime, HPSlider.value, 1f);
    }
}
