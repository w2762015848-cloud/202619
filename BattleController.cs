using System.Collections;
using UnityEngine;

namespace BattleSystem
{
    public class BattleController : MonoBehaviour
    {
        [Header("战斗设置")]
        public bool isPlayerTurn = true;
        public float enemyTurnDelay = 1.5f;

        [Header("引用")]
        public PetEntity playerPet;
        public PetEntity enemyPet;
        public SkillExecutor skillExecutor;
        public BattleUIController uiController;

        public static BattleController Instance { get; private set; }

        private bool _isBattleActive = false;
        private StatusEffectManager _statusManager;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 自动获取组件
            if (skillExecutor == null) skillExecutor = GetComponent<SkillExecutor>();
            if (uiController == null) uiController = GetComponent<BattleUIController>();

            // 确保状态管理器存在
            _statusManager = FindObjectOfType<StatusEffectManager>();
            if (_statusManager == null)
            {
                GameObject go = new GameObject("StatusEffectManager");
                _statusManager = go.AddComponent<StatusEffectManager>();
            }
        }

        void Start()
        {
            Debug.Log("=== BattleController Start ===");
            Debug.Log($"PlayerPet: {playerPet?.petName ?? "空"}");
            Debug.Log($"EnemyPet: {enemyPet?.petName ?? "空"}");
            Debug.Log($"SkillExecutor: {skillExecutor != null}");
            Debug.Log($"UIController: {uiController != null}");
            Debug.Log($"StatusManager: {_statusManager != null}");

            if (playerPet != null && playerPet.skills != null)
            {
                Debug.Log($"玩家技能数量: {playerPet.skills.Count}");
                for (int i = 0; i < playerPet.skills.Count; i++)
                {
                    var skill = playerPet.skills[i];
                    Debug.Log($"技能{i}: {skill?.skillName ?? "空"} (PP: {skill?.currentPP}/{skill?.maxPP})");
                }
            }
            else
            {
                Debug.LogWarning("玩家宠物或技能列表为空");
            }

            InitializeBattle();
        }

        // 初始化战斗
        public void InitializeBattle()
        {
            Debug.Log("=== 战斗开始初始化 ===");

            // 重置宠物状态
            if (playerPet != null)
            {
                playerPet.ResetState();
                Debug.Log($"重置玩家宠物: {playerPet.petName}");
            }

            if (enemyPet != null)
            {
                enemyPet.ResetState();
                Debug.Log($"重置敌人宠物: {enemyPet.petName}");
            }

            // 重置技能PP
            ResetAllSkillPP();

            // 清除所有状态效果
            if (_statusManager != null)
            {
                if (playerPet != null) _statusManager.ClearAllStatusEffects(playerPet);
                if (enemyPet != null) _statusManager.ClearAllStatusEffects(enemyPet);
            }

            // 初始化UI
            if (uiController != null)
            {
                uiController.InitializeUI(playerPet);
            }
            else
            {
                Debug.LogWarning("UIController为空，无法初始化UI");
            }

            _isBattleActive = true;
            isPlayerTurn = true;

            BattleEvents.TriggerBattleStart();
            Debug.Log("战斗初始化完成");
        }

        // 重置所有技能PP
        private void ResetAllSkillPP()
        {
            if (playerPet != null && playerPet.skills != null)
            {
                foreach (var skill in playerPet.skills)
                {
                    if (skill != null)
                    {
                        skill.ResetPP();
                        Debug.Log($"重置玩家技能PP: {skill.skillName} -> {skill.currentPP}/{skill.maxPP}");
                    }
                }
            }

            if (enemyPet != null && enemyPet.skills != null)
            {
                foreach (var skill in enemyPet.skills)
                {
                    if (skill != null)
                    {
                        skill.ResetPP();
                        Debug.Log($"重置敌人技能PP: {skill.skillName} -> {skill.currentPP}/{skill.maxPP}");
                    }
                }
            }
        }

        // 玩家使用技能
        public void OnPlayerUseSkill(int skillIndex)
        {
            Debug.Log($"=== 尝试使用技能: 索引{skillIndex} ===");

            if (!_isBattleActive)
            {
                Debug.LogWarning($"无法使用技能: 战斗未激活");
                return;
            }

            if (!isPlayerTurn)
            {
                Debug.LogWarning($"无法使用技能: 不是玩家回合 (当前回合: {(isPlayerTurn ? "玩家" : "敌人")})");
                return;
            }

            if (playerPet == null)
            {
                Debug.LogWarning($"无法使用技能: 玩家宠物为空");
                return;
            }

            if (playerPet.isDead)
            {
                Debug.LogWarning($"无法使用技能: 玩家宠物已死亡");
                return;
            }

            // 检查宠物是否能行动（状态影响）
            if (_statusManager != null && !_statusManager.CanPetTakeAction(playerPet))
            {
                Debug.LogWarning($"无法使用技能: 玩家宠物因异常状态无法行动");
                return;
            }

            // 检查技能列表是否存在
            if (playerPet.skills == null)
            {
                Debug.LogWarning($"技能列表为空");
                return;
            }

            if (skillIndex < 0 || skillIndex >= playerPet.skills.Count)
            {
                Debug.LogWarning($"技能索引无效: {skillIndex} (技能总数: {playerPet.skills.Count})");
                return;
            }

            SkillData skill = playerPet.skills[skillIndex];

            if (skill == null)
            {
                Debug.LogWarning($"技能{skillIndex}为空");
                return;
            }

            if (skill.currentPP <= 0)
            {
                Debug.LogWarning($"技能 {skill.skillName} PP不足 (当前: {skill.currentPP}/{skill.maxPP})");
                return;
            }

            Debug.Log($"✓ 成功使用技能: {skill.skillName} (索引: {skillIndex}, PP: {skill.currentPP}/{skill.maxPP})");

            // 消耗PP
            skill.currentPP--;

            // 执行技能
            if (skillExecutor != null)
            {
                skillExecutor.ExecuteSkill(playerPet, enemyPet, skill);
            }
            else
            {
                Debug.LogError("SkillExecutor为空，无法执行技能");
                return;
            }

            // 禁用UI
            if (uiController != null)
            {
                uiController.SetSkillButtonsInteractable(false);
            }
            else
            {
                Debug.LogWarning("UIController为空");
            }

            isPlayerTurn = false;

            BattleEvents.TriggerSkillUsed(playerPet, skill);

            // 立即更新UI显示剩余PP
            if (uiController != null)
            {
                uiController.UpdateSkillButtons(playerPet);
            }
        }

        // 结束回合
        public void EndTurn(PetEntity attacker)
        {
            if (!_isBattleActive)
            {
                Debug.Log("EndTurn: 战斗未激活");
                return;
            }

            Debug.Log($"结束回合，攻击者: {attacker?.petName ?? "空"}");

            // 处理状态效果（持续伤害等）
            if (_statusManager != null)
            {
                _statusManager.ProcessTurnEnd(attacker);
            }

            // 检查战斗是否结束
            if (CheckBattleEnd())
            {
                EndBattle();
                return;
            }

            // 切换回合
            if (attacker == playerPet && enemyPet != null && !enemyPet.isDead)
            {
                Debug.Log("切换到敌人回合");
                StartCoroutine(EnemyTurnCoroutine());
            }
            else if (attacker == enemyPet && playerPet != null && !playerPet.isDead)
            {
                Debug.Log("切换到玩家回合");
                isPlayerTurn = true;
                if (uiController != null)
                {
                    uiController.SetSkillButtonsInteractable(true);
                }
            }
        }

        // 敌人回合
        private IEnumerator EnemyTurnCoroutine()
        {
            yield return new WaitForSeconds(enemyTurnDelay);

            if (!_isBattleActive || enemyPet == null || enemyPet.isDead)
            {
                Debug.Log("敌人回合被取消");
                yield break;
            }

            Debug.Log("敌人回合开始");

            // 处理状态效果（检查是否能行动）
            if (_statusManager != null)
            {
                _statusManager.ProcessTurnStart(enemyPet);

                // 检查敌人是否能行动
                if (!_statusManager.CanPetTakeAction(enemyPet))
                {
                    // 敌人因状态无法行动
                    Debug.Log($"{enemyPet.petName} 因异常状态无法行动");
                    EndTurn(enemyPet);
                    yield break;
                }

                // 检查是否会攻击自己（混乱状态）
                if (_statusManager.ShouldAttackSelf(enemyPet))
                {
                    // 敌人攻击自己
                    Debug.Log($"{enemyPet.petName} 因混乱状态攻击自己");
                    // 使用一个默认的自我攻击技能
                    SkillData selfAttackSkill = GetDefaultSelfAttackSkill();
                    if (selfAttackSkill != null)
                    {
                        enemyPet.TakeDamage(20); // 固定伤害
                        Debug.Log($"{enemyPet.petName} 因混乱受到20点伤害");
                    }
                    EndTurn(enemyPet);
                    yield break;
                }
            }

            // 选择随机技能
            SkillData enemySkill = GetRandomEnemySkill();
            if (enemySkill != null)
            {
                // 消耗PP
                enemySkill.currentPP--;

                Debug.Log($"敌人使用技能: {enemySkill.skillName}");

                // 执行技能
                skillExecutor.ExecuteSkill(enemyPet, playerPet, enemySkill);

                BattleEvents.TriggerSkillUsed(enemyPet, enemySkill);
            }
            else
            {
                Debug.LogWarning("敌人没有可用技能");
                EndTurn(enemyPet);
            }
        }

        // 获取默认的自我攻击技能（用于混乱状态）
        private SkillData GetDefaultSelfAttackSkill()
        {
            // 创建一个临时的自我攻击技能
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillName = "自我攻击";
            skill.power = 20;
            skill.effectType = SkillEffectType.FixedDamage;
            return skill;
        }

        // 获取敌人随机技能
        private SkillData GetRandomEnemySkill()
        {
            if (enemyPet == null || enemyPet.skills == null || enemyPet.skills.Count == 0)
            {
                Debug.LogWarning("敌人宠物或技能列表为空");
                return null;
            }

            // 收集有PP的技能
            System.Collections.Generic.List<SkillData> availableSkills = new System.Collections.Generic.List<SkillData>();
            foreach (var skill in enemyPet.skills)
            {
                if (skill != null && skill.currentPP > 0)
                {
                    availableSkills.Add(skill);
                }
            }

            if (availableSkills.Count == 0)
            {
                Debug.LogWarning("敌人没有可用PP的技能");
                return enemyPet.skills[0]; // 返回第一个技能（即使PP为0）
            }

            return availableSkills[Random.Range(0, availableSkills.Count)];
        }

        // 检查战斗是否结束
        private bool CheckBattleEnd()
        {
            if (playerPet != null && playerPet.isDead)
            {
                Debug.Log("玩家宠物被击败！");
                return true;
            }

            if (enemyPet != null && enemyPet.isDead)
            {
                Debug.Log("敌人宠物被击败！");
                return true;
            }

            return false;
        }

        // 结束战斗
        private void EndBattle()
        {
            Debug.Log("=== 战斗结束 ===");
            _isBattleActive = false;
            isPlayerTurn = false;

            if (uiController != null)
            {
                uiController.SetSkillButtonsInteractable(false);
            }

            BattleEvents.TriggerBattleEnd();
        }

        // 重置战斗
        public void ResetBattle()
        {
            Debug.Log("=== 重置战斗 ===");
            InitializeBattle();
        }

        // 设置宠物（用于外部设置）
        public void SetPets(PetEntity player, PetEntity enemy)
        {
            playerPet = player;
            enemyPet = enemy;

            if (_isBattleActive)
            {
                InitializeBattle();
            }
        }
    }
}