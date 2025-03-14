using bet_slum.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
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

        public CompetitorData Competitor => _competitorData;
        private CompetitorData _competitorData;

        public bool IsDead => HP <= 0f;
        public float HP;
        public float MaxHP;
        private float _hpPerLevel = 100f;

        public int CurrentAbilityIndex = -1;
        public int CurrentTargetIndex = -1;
        private Vector3 _spawnPosition;
        private NavMeshAgent _agent;
        private int _fighterIndex;
        public Transform AttackerPosition;
        private bool _initialized = false;

        public void Initialize(
            int fighterIndex, 
            SlapfightMatchRunner matchRunner,
            CompetitorData competitorData,
            RuntimeAnimatorController animatorController,
            Vector3 spawnPosition)
        {
            _fighterIndex = fighterIndex;
            _agent = GetComponent<NavMeshAgent>();
            _spawnPosition = spawnPosition;
            _nameLabel.SetText($"{competitorData.competitor.displayName}");
            _matchRunner = matchRunner;
            _competitorData = competitorData;
            MaxHP = competitorData.stats[0].value * _hpPerLevel;

            // spawn avatar - simpler to spawn different for now, until all possible options are cached 
            //Debug.Log("Initializing agent");
            //Debug.Log(animatorController);
            if (_avatarModel != null)
                GameObject.Destroy(_avatarModel);
            _avatarModel = GameObject.Instantiate(_matchRunner.FighterModels[0], transform);

            _avatarModel.transform.SetAsFirstSibling();
            // make sure this is first animator we get

            Animator = GetComponentInChildren<Animator>(includeInactive: true);
            
            Animator.runtimeAnimatorController = animatorController;
            Animator.applyRootMotion = false;
            
            // anim event broadcasting
            _animEventBroadcaster = Animator.transform.gameObject.AddComponent<SlapfightAgentAnimationEventBroadcaster>();
            _animEventBroadcaster.OnAttackEndEvent.AddListener(OnAttackEnd);

            HP = MaxHP;
            _initialized = true;
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
            
            CurrentAbilityIndex =
                 // get most damaging
                 //_competitorData.availableAbilities.IndexOf(_competitorData.availableAbilities.OrderByDescending(a => a.damage).First());
                 // get random
                 UnityEngine.Random.Range(0, _competitorData.availableAbilities.Count);

            var target = _matchRunner.Fighters[CurrentTargetIndex];
            var ability = _competitorData.availableAbilities[CurrentAbilityIndex];

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
            // TODO - animate turn
            transform.rotation = Quaternion.LookRotation((target.transform.position - transform.position).normalized, Vector3.up);

            _agent.isStopped = true;

            Debug.Log($"setting index to {CurrentAbilityIndex}");
            Animator.SetInteger("AbilityIndex", CurrentAbilityIndex);
            Debug.Log($"gonna popup {ability.ability.name}");
            
            //Debug.Log($"{_matchRunner._popup}");
            _matchRunner._popup.Show($"{ability.ability.name} <color=red>({ability.ability.damage})</color>", ability.statRequirements?.Select(r => (r.statDefinition.name, r.value)).ToList(), ability.itemRequirements?.Select(r => r.item.icon).ToList());


            Debug.Log($"firing use ability");
            Animator.SetTrigger("UseAbility");
            _attacking = true;
            AudioManager.Instance.PlayOneshot("hit");
            while (_attacking)
            {
                await Task.Delay(1);
            }

            // Switch to reaction camera
            SoloCamera.gameObject.SetActive(false);
            target.SoloCamera.gameObject.SetActive(true);

            // probably shouldnt be handling this here uwu
            target.TakeDamage(ability.ability.damage);
            if (target.IsDead)
            {
                AudioManager.Instance.PlayOneshot("jesse");
                target.Animator.SetTrigger("Killed");
                _matchRunner._deadFighters.Add(target);
            }
            else
                target.Animator.SetTrigger("Damaged");

            // Return to spawn
            _agent.isStopped = false;
            targetPosition = _spawnPosition;
            dirTo = targetPosition - transform.position;
            transform.rotation = Quaternion.LookRotation(dirTo.normalized, Vector3.up);
            // ignore y
            while (Vector3.Distance(new Vector3(targetPosition.x, transform.position.y, targetPosition.z), transform.position) > stoppingDistance)
            {
                //Debug.Log(Vector3.Distance(targetPosition, transform.position));
                _agent.SetDestination(targetPosition);
                await Task.Delay(1);
            }
            //Debug.Log("Player returned to spawn");
            // TODO - animate turn toward center
            transform.rotation = Quaternion.LookRotation((new Vector3(0, _spawnPosition.y, 0) - _spawnPosition).normalized, Vector3.up);
            

            await _matchRunner.EndTurn();
        }

        private bool _attacking = false;

        public void OnAttackEnd()
        {
            Debug.Log($"Agent Ending attack");
            _attacking = false;
        }

        public void TakeDamage(float damage)
        {
            HP = math.min(HP - damage, MaxHP);
        }

        private void Update()
        {
            if (!_initialized) return;

            var totalSpeed = _agent.velocity.magnitude / _agent.speed;
            Animator.SetFloat("MoveSpeed", totalSpeed);
        }
    }
}
