using bet_slum.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryCompetitorInfoController : MonoBehaviour
{
    public TMP_Text NameLabel;
    public TMP_Text AuxLabel;

    public TMP_Text Stat1Label;
    public TMP_Text Stat2Label;
    public TMP_Text Stat3Label;

    public void SetData(CompetitorData data)
    {
        NameLabel.SetText(data.competitor.name);

        // TODO - match backend 
        AuxLabel.SetText($"+ {10}xp, +{10}g");

        Stat1Label.SetText(data.stats[0].value.ToString());
        Stat2Label.SetText(data.stats[1].value.ToString());
        Stat3Label.SetText(data.stats[2].value.ToString());
    }
}
