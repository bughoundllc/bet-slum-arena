using UnityEngine;
using UnityEngine.Events;

namespace bet_slum.Games.Slapfight
{
    public class SlapfightAgentAnimationEventBroadcaster : MonoBehaviour
    {
        public UnityEvent OnAttackEndEvent = new();
        public UnityEvent OnAttackSoundEvent = new();

        public void OnAttackEnd()
        {
            Debug.Log("Received attack end anim signal");
            OnAttackEndEvent?.Invoke();
        }

        public void OnAttackSound()
        {
            Debug.Log("Received attack sound anim signal");
            OnAttackSoundEvent?.Invoke();
        }

        private void OnDestroy()
        {
            OnAttackEndEvent?.RemoveAllListeners();
        }
    }
}

