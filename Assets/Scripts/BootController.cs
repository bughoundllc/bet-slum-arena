using bet_slum.showRunner;
using UnityEngine;

namespace bet_slum
{
    public class BootController: MonoBehaviour
    {
        public bool GoLive = false;

        private void Awake()
        {
            if (GoLive)
                gameObject.AddComponent<LiveShowRunner>();
            else
                gameObject.AddComponent<MockShowRunner>();

            Destroy(this);
        }
    }
}