using bet_slum.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryCompetitorInfoController : MonoBehaviour
{
    public TMP_Text RankLabel;
    public TMP_Text NameLabel;
    public TMP_Text AuxLabel;

    public TMP_Text Stat1Label;
    public TMP_Text Stat2Label;
    public TMP_Text Stat3Label;

    public void SetData(CompetitorData data, uint rank, int totalTeams)
    {
        NameLabel.SetText(data.competitor.name);
        RankLabel.SetText((rank == 0 ? 1 : rank).ToString());

        // TODO - DUPLICATED FROM BACKEND
        var baseExperienceBonus = 5;
        var experienceAmount = (uint)baseExperienceBonus * (uint)Math.Pow(2, rank - 1);

        var baseBountyBonus = 10;
        var bonusMultiplier = (uint)(totalTeams - rank + 1);
        var bountyAmount = baseBountyBonus * bonusMultiplier;
        // TODO - show who will level up
        // --

        AuxLabel.SetText($"+ {experienceAmount}xp, +{bountyAmount}g");

        Stat1Label.SetText(data.stats[0].value.ToString());
        Stat2Label.SetText(data.stats[1].value.ToString());
        Stat3Label.SetText(data.stats[2].value.ToString());
    }
}
