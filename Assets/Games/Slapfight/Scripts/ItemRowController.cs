using bet_slum.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace bet_slum.Slapfight
{
    public class ItemRowController : MonoBehaviour
    {
        public GameObject iconPrototype;
        private List<GameObject> _items = new();

        // Registry that maps item keys to their resource paths
        private Dictionary<string, string> IconFilePathRegistry = new Dictionary<string, string>();
        private bool _initialized = false;
        
        // Base path for item icons
        private const string IconBasePath = "Icons/Items";

        private void Start()
        {
            iconPrototype.gameObject.SetActive(false);
            
            // Scan and populate the IconFilePathRegistry
            //PopulateIconRegistry();
        }
        
        /// <summary>
        /// Scans the Icons/Items directory and populates the IconFilePathRegistry
        /// Keys and filenames are kept identical, with paths formatted as "Icons/Items/{filename}"
        /// </summary>
        private void PopulateIconRegistry()
        {
            // Clear existing registry
            IconFilePathRegistry.Clear();
            
            // Load all sprites from the Resources folder at the specified path
            Sprite[] icons = Resources.LoadAll<Sprite>(IconBasePath);
            
            foreach (Sprite icon in icons)
            {
                // Get the icon name (filename without extension)
                string iconName = icon.name;
                
                // Add to registry with key and path
                string iconPath = $"{IconBasePath}/{iconName}";
                IconFilePathRegistry[iconName] = iconPath;
                
                Debug.Log($"Registered icon: {iconName} -> {iconPath}");
            }
            
            // If no icons were found, log a warning
            if (IconFilePathRegistry.Count == 0)
            {
                Debug.LogWarning($"No icons found in Resources/{IconBasePath}. IconFilePathRegistry is empty.");
            }
            else
            {
                Debug.Log($"Populated IconFilePathRegistry with {IconFilePathRegistry.Count} icons.");
            }

            _initialized = true;
        }

        public void Refresh(List<InventoryItemDTO> items)
        {
            if (!_initialized)
                PopulateIconRegistry();

            foreach(var item in _items)
                GameObject.Destroy(item);
            _items.Clear();

            foreach(var item in items)
            {
                var go = GameObject.Instantiate(iconPrototype, iconPrototype.transform.parent);
                go.SetActive(true);
                // set image
                if(!string.IsNullOrEmpty(item.definition.icon) && IconFilePathRegistry.TryGetValue(item.definition.icon, out var path))
                {
                    var image = Resources.Load<Sprite>(path);
                    if (image != null)
                    {
                        Debug.Log($"Setting icon to {image.name}");
                        go.GetComponent<Image>().sprite = image;
                    }
                    else
                        Debug.LogError($"Unable to load image for key {item.definition.icon}");
                }
                _items.Add(go);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            gameObject.SetActive(_items.Count > 0);
        }
    }
}
