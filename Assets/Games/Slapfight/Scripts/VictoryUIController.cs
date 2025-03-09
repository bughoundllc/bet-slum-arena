using bet_slum.Games.Slapfight;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class VictoryUIController: MonoBehaviour
{
    public VictoryCompetitorInfoController _prototype;
    ObjectPool<VictoryCompetitorInfoController> _pool;
    List<VictoryCompetitorInfoController> _items = new();

    public TMP_Text PayoutLabel;

    public void Initialize()
    {
        _pool = new ObjectPool<VictoryCompetitorInfoController>(
            () => GameObject.Instantiate(_prototype, _prototype.transform.parent),
            item => { item.gameObject.SetActive(true); item.transform.SetAsLastSibling(); },
            item => item.gameObject.SetActive(false),
            item => GameObject.Destroy(item.gameObject));
        _prototype.gameObject.SetActive(false);
    }


    // TODO - rank order
    public void SetData(List<SlapfightAgentController> fighters, Dictionary<int, uint> ranks)
    {
        foreach (var item in _items)
            _pool.Release(item);
        _items.Clear();

        // order by rank
        var sorted = new List<(uint, SlapfightAgentController)>();
        for(int i = 0; i < fighters.Count; i++)
        {
            sorted.Add((ranks[i], fighters[i]));
        }
        sorted = sorted.OrderBy(x => x.Item1).ToList();

        PayoutLabel.SetText($"{sorted[0].Item2.Competitor.competitor.name} wins - {9999999999} payout");
        foreach(var fighter in sorted)
        {
            var item = _pool.Get();

            item.SetData(fighter.Item2.Competitor, fighter.Item1, sorted.Count);
            _items.Add(item);
        }
    }
}