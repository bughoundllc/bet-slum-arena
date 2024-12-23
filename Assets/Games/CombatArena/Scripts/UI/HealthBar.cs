using bet_slum.CombatArena;
using bet_slum.CombatArena.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _nameText;

    public void UpdateAgentHP(ArenaAgent agent)
    {
        _nameText.SetText(agent.Name);
        _slider.SetValueWithoutNotify(agent.CurrentHP / agent.MaxHP);
    }
}
