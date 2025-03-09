using bet_slum.Games.Slapfight;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VictoryUIController: MonoBehaviour
{
    public VictoryCompetitorInfoController _prototype;
    ObjectPool<VictoryCompetitorInfoController> _pool;
    List<VictoryCompetitorInfoController> _items = new();

    public void Initialize()
    {
        _pool = new ObjectPool<VictoryCompetitorInfoController>(
            () => GameObject.Instantiate(_prototype, _prototype.transform.parent),
            item => item.gameObject.SetActive(true),
            item => item.gameObject.SetActive(false),
            item => GameObject.Destroy(item.gameObject));
        _prototype.gameObject.SetActive(false);
    }


    // TODO - rank order
    public void SetData(List<SlapfightAgentController> fighters)
    {
        foreach (var item in _items)
            _pool.Release(item);
        _items.Clear();

        foreach(var fighter in fighters)
        {
            var item = _pool.Get();
            item.SetData(fighter.Competitor);
            _items.Add(item);
        }
    }
}