using bet_slum.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static bet_slum.Games.Slapfight.SlapfightMatchRunner;

namespace bet_slum.Games.Slapfight
{
    public class SlapfightAgentController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [HideInInspector] public Animator Animator;

        public CinemachineCamera SoloCamera;
        private GameObject _avatarModel;

        private SlapfightMatchRunner _matchRunner;
        private SlapfightAgentAnimationEventBroadcaster _animEventBroadcaster;

        public Competitor Competitor => _competitorData;
        private Competitor _competitorData;

        public bool IsDead => HP <= 0f;
        public float HP;
        public float MaxHP = 100f;

        public List<FighterAbility> Abilities => _abilities;
        private List<FighterAbility> _abilities = new();
        public int CurrentAbilityIndex = -1;
        public int CurrentTargetIndex = -1;
        private Vector3 _spawnPosition;
        private NavMeshAgent _agent;
        private int _fighterIndex;

        public void Initialize(int fighterIndex, SlapfightMatchRunner matchRunner, Competitor competitorData, RuntimeAnimatorController animatorController, List<FighterAbility> abilities)
        {
            _fighterIndex = fighterIndex;
            _agent = GetComponent<NavMeshAgent>();
            _spawnPosition = transform.position;
            _nameLabel.SetText($"{competitorData.name}");
            _matchRunner = matchRunner;
            _competitorData = competitorData;
            _abilities = abilities;

            // spawn avatar - simpler to spawn different for now, until all possible options are cached 
            Debug.Log("Initializing agent");
            Debug.Log(animatorController);
            if (_avatarModel != null)
                GameObject.Destroy(_avatarModel);
            _avatarModel = GameObject.Instantiate(_matchRunner.FighterModels[UnityEngine.Random.Range(0, _matchRunner.FighterModels.Count)], transform);

            _avatarModel.transform.SetAsFirstSibling();
            // make sure this is first animator we get

            Animator = GetComponentInChildren<Animator>(includeInactive: true);
            
            Animator.runtimeAnimatorController = animatorController;
            Animator.applyRootMotion = false;
            
            // anim event broadcasting
            _animEventBroadcaster = Animator.transform.gameObject.AddComponent<SlapfightAgentAnimationEventBroadcaster>();
            _animEventBroadcaster.OnAttackEndEvent.AddListener(OnTurnEnd);

            HP = MaxHP;
        }

        public async Awaitable BeginTurn()
        {
            // just play animation
            Debug.Log($"Agent Beginning turn");
            SoloCamera.gameObject.SetActive(true);
            // TODO - WAIT ON MOVE TO DESTINATION
            // need to get target now instead of later
            var validTargetIndices = new List<int>();
            for (int i = 0; i < _matchRunner.Fighters.Count; i++)
            {
                if (i != _fighterIndex
                    && !_matchRunner.Fighters[i].IsDead)
                    validTargetIndices.Add(i);
            }
            if (validTargetIndices.Count <= 0)
            {
                Debug.LogError($"No valid targets found for fighter {_fighterIndex}");
                return;
            }

            // lock in target/ability
            CurrentTargetIndex = validTargetIndices[UnityEngine.Random.Range(0, validTargetIndices.Count)];
            CurrentAbilityIndex = UnityEngine.Random.Range(0, _abilities.Count);

            var targetPosition = _matchRunner.Fighters[CurrentTargetIndex].transform.position;
            var dirTo = targetPosition - transform.position;
            transform.rotation = Quaternion.LookRotation(dirTo.normalized, Vector3.up);
            var stoppingDistance = (dirTo).normalized * 2.5f;
            if (Vector3.Distance(targetPosition, transform.position) > stoppingDistance.magnitude)
            {
                _agent.SetDestination(targetPosition - stoppingDistance);
                await Task.Delay(6000);
            }
            
            Animator.SetInteger("AbilityIndex", CurrentAbilityIndex);
            _matchRunner._popup.Show($"{_abilities[CurrentAbilityIndex].Name}");
            
            await Task.Delay(1000);
            
            Animator.SetTrigger("UseAbility");
        }

        public void OnTurnEnd()
        {
            Debug.Log($"Agent Ending turn");
            //_camera.gameObject.SetActive(false);

            // we're fine with the fire & forget re: await here
            _matchRunner.EndTurn();
        }

        public void TakeDamage(float damage)
        {
            HP = math.min(HP - damage, MaxHP);
        }

        private void Update()
        {
            var totalSpeed = _agent.velocity.magnitude / _agent.speed;
            Animator.SetFloat("MoveSpeed", totalSpeed);
        }
    }
}
