using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopup : MonoBehaviour
{
    [SerializeField] private float AutoHideDuration = 2f;
    [SerializeField] private TMP_Text Text;

    [SerializeField] private RectTransform _statPanelPrototype;
    private List<RectTransform> _statsPanels = new();
    [SerializeField] private RectTransform _itemPanelPrototype;
    private List<RectTransform> _itemPanels = new();

    private float _lastTimeTriggered = 0f;

    private Dictionary<string, Color> _statColorLookup = new Dictionary<string, Color>
    {
        { "Cohesion", new Color32(200, 90, 200, 255) },
        { "Directive", new Color32(255, 160, 80, 255)  },
        { "Signal", new Color32(50, 220, 200, 255)  }
    };

    public void Show(string displayText = null, List<(string statName, uint statValue)> statReqs = null, List<string> itemReqs = null)
    {
        Debug.Log("Showing popup");
        _lastTimeTriggered = Time.realtimeSinceStartup;

        gameObject.SetActive(true);
        Text.SetText(displayText);

        foreach(var panel in _statsPanels)
        {
            GameObject.Destroy(panel.gameObject);
        }
        _statsPanels.Clear();

        foreach (var panel in _itemPanels)
        {
            GameObject.Destroy(panel.gameObject);
        }
        _itemPanels.Clear();
        Debug.Log("Cleared");

        if (statReqs != null)
        {
            foreach (var req in statReqs)
            {
                var go = GameObject.Instantiate(_statPanelPrototype, _statPanelPrototype.transform.parent);
                go.gameObject.SetActive(true);

                // set color and text value
                if (_statColorLookup.TryGetValue(req.statName, out var color))
                    go.GetComponent<Image>().color = color;
                go.GetComponentInChildren<TMP_Text>().SetText(req.statValue.ToString());

                _statsPanels.Add(go);
            }
        }
        

        if(_iconFilePathRegistry.Count == 0)
            PopulateIconRegistry();

        if(itemReqs != null)
        {
            foreach (var req in itemReqs)
            {
                var go = GameObject.Instantiate(_itemPanelPrototype, _itemPanelPrototype.transform.parent);
                go.gameObject.SetActive(true);
                // set sprite
                if (_iconFilePathRegistry.TryGetValue(req, out var path))
                {
                    var sprite = Resources.Load<Sprite>(path);
                    go.GetComponent<Image>().sprite = sprite;
                }

                _itemPanels.Add(go);
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        _statPanelPrototype.gameObject.SetActive(false);
        _itemPanelPrototype.gameObject.SetActive(false);
        Hide();
    }

    private void Update()
    {
        if(Time.realtimeSinceStartup - _lastTimeTriggered >= AutoHideDuration)
        {
            Hide();
        }
    }

    // shameless duplication, delete me
    private const string IconBasePath = "Icons/Items";
    private Dictionary<string, string> _iconFilePathRegistry = new();
    private void PopulateIconRegistry()
    {
        // Clear existing registry
        _iconFilePathRegistry.Clear();

        // Load all sprites from the Resources folder at the specified path
        Sprite[] icons = Resources.LoadAll<Sprite>(IconBasePath);

        foreach (Sprite icon in icons)
        {
            // Get the icon name (filename without extension)
            string iconName = icon.name;

            // Add to registry with key and path
            string iconPath = $"{IconBasePath}/{iconName}";
            _iconFilePathRegistry[iconName] = iconPath;

            Debug.Log($"Registered icon: {iconName} -> {iconPath}");
        }

        // If no icons were found, log a warning
        if (_iconFilePathRegistry.Count == 0)
        {
            Debug.LogWarning($"No icons found in Resources/{IconBasePath}. IconFilePathRegistry is empty.");
        }
        else
        {
            Debug.Log($"Populated IconFilePathRegistry with {_iconFilePathRegistry.Count} icons.");
        }

    }
}
