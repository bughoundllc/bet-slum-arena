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
        private Transform _spawnPosition;
        private NavMeshAgent _agent;
        private int _fighterIndex;
        public Transform AttackerPosition;

        public void Initialize(
            int fighterIndex, 
            SlapfightMatchRunner matchRunner, 
            Competitor competitorData, 
            RuntimeAnimatorController animatorController, 
            List<FighterAbility> abilities,
            Transform spawnPoint)
        {
            _fighterIndex = fighterIndex;
            _agent = GetComponent<NavMeshAgent>();
            _spawnPosition = spawnPoint;
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
            _animEventBroadcaster.OnAttackEndEvent.AddListener(OnAttackEnd);

            HP = MaxHP;
        }

        public async Awaitable RunTurn()
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
            var target = _matchRunner.Fighters[CurrentTargetIndex];
            var ability = _abilities[CurrentAbilityIndex];

            // go to target attack pt
            var targetPosition = target.AttackerPosition.position;
            var dirTo = targetPosition - transform.position;
            transform.rotation = Quaternion.LookRotation(dirTo.normalized, Vector3.up);
            var stoppingDistance = 0.25f;
            // ignore y
            while (Vector3.Distance(new Vector3(targetPosition.x, transform.position.y, targetPosition.z), transform.position) > stoppingDistance)
            {
                _agent.SetDestination(targetPosition);
                await Task.Delay(1);
            }
            _agent.isStopped = true;
            
            Animator.SetInteger("AbilityIndex", CurrentAbilityIndex);
            _matchRunner._popup.Show($"{ability.Name}");
            
            //await Task.Delay(1000);
            
            Animator.SetTrigger("UseAbility");
            _attacking = true;
            while (_attacking)
            {
                await Task.Delay(1);
            }

            // Switch to reaction camera
            SoloCamera.gameObject.SetActive(false);
            target.SoloCamera.gameObject.SetActive(true);

            // probably shouldnt be handling this here uwu
            target.TakeDamage(ability.Damage);
            if (target.IsDead)
                target.Animator.SetTrigger("Killed");
            else
                target.Animator.SetTrigger("Damaged");


            // Return to spawn
            _agent.isStopped = false;
            targetPosition = _spawnPosition.position;
            dirTo = targetPosition - transform.position;
            transform.rotation = Quaternion.LookRotation(dirTo.normalized, Vector3.up);
            // ignore y
            while (Vector3.Distance(new Vector3(targetPosition.x, transform.position.y, targetPosition.z), transform.position) > stoppingDistance)
            {
                Debug.Log(Vector3.Distance(targetPosition, transform.position));
                _agent.SetDestination(targetPosition);
                await Task.Delay(1);
            }
            Debug.Log("Player returned to spawn");
            // TODO - animate turn toward center
            transform.rotation = Quaternion.LookRotation((new Vector3(0, _spawnPosition.position.y, 0) - _spawnPosition.position).normalized, Vector3.up);
            
            await _matchRunner.EndTurn();
        }

        private bool _attacking = false;

        public void OnAttackEnd()
        {
            Debug.Log($"Agent Ending turn");
            _attacking = false;
            //_camera.gameObject.SetActive(false);

            // we're fine with the fire & forget re: await here
            //_matchRunner.EndTurn();
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
