using bet_slum.Data;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace bet_slum.Slapfight
{
    public class ItemRowController : MonoBehaviour
    {
        public GameObject iconPrototype;
        private List<GameObject> _items = new();

        // TODO - move
        private Dictionary<string, string> IconFilePathRegistry = new Dictionary<string, string> 
        {
            {"TestIcon", "Icons/Items/TestIcon" }
        };

        private void Start()
        {
            iconPrototype.gameObject.SetActive(false);
        }

        public void Refresh(List<InventoryItemDTO> items)
        {
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
