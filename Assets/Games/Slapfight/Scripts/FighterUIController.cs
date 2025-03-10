using bet_slum;
using bet_slum.Games.Slapfight;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FighterUIController : MonoBehaviour
{
    public GameObject Prefab;
    private ObjectPool<GameObject> _prefabPool;
    public Dictionary<SlapfightAgentController, GameObject> _items = new();

    private SlapfightMatchRunner _matchRunner;
    
    public void Initialize(SlapfightMatchRunner matchRunner)
    {
        _matchRunner = matchRunner;
        if(_prefabPool == null)
        {
            _prefabPool = new(
                () => GameObject.Instantiate(Prefab, Prefab.transform.parent),
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => GameObject.Destroy(view.gameObject)
                );
            Prefab.gameObject.SetActive(false);
        }

        foreach(var item in _items)
            _prefabPool.Release(item.Value);
        _items.Clear();
        
        foreach(var fighter in matchRunner.Fighters)
        {
            var item = _prefabPool.Get();
            _items.Add(fighter, item);
            var fighterUI = item.GetComponent<FighterInfoUI>();
            fighterUI.fighterAgent = fighter;
            fighterUI.NameLabel.SetText(fighter.Competitor.competitor.name);
            fighterUI.HPSlider.value = 1f;
            fighterUI.HPBackgroundSlider.value = 1f;
            fighterUI.itemRow.Refresh(fighter.Competitor.inventory);
            fighterUI.Stat1Label.SetText(fighter.Competitor.stats[0].value.ToString());
            fighterUI.Stat2Label.SetText(fighter.Competitor.stats[1].value.ToString());
            fighterUI.Stat3Label.SetText(fighter.Competitor.stats[2].value.ToString());
        }
    }

    public void Update()
    {
        foreach(var kv in _items)
        {
            var fighterUI = kv.Value.GetComponent<FighterInfoUI>();
            fighterUI.HPSlider.value = kv.Key.HP / kv.Key.MaxHP;
        }
    }
}
