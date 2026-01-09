using UnityEngine;

namespace BattleSystem
{
    public class BattleDebugger : MonoBehaviour
    {
        [Header("调试设置")]
        public bool enableDebugging = true;

        private BattleController _battleController;
        private BattleUIController _uiController;

        void Awake()
        {
            _battleController = BattleController.Instance;
            _uiController = GetComponent<BattleUIController>();
        }

        void Update()
        {
            if (!enableDebugging) return;

            HandleDebugInput();
        }

        private void HandleDebugInput()
        {
            // F1: 显示战斗状态
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("=== 战斗系统调试 ===");

                if (_battleController != null)
                {
                    Debug.Log($"玩家回合: {_battleController.isPlayerTurn}");

                    if (_battleController.playerPet != null)
                    {
                        var pet = _battleController.playerPet;
                        Debug.Log($"玩家宠物: {pet.petName}");
                        Debug.Log($"  HP: {pet.CurrentHP}/{pet.MaxHP} (死亡: {pet.isDead})");
                        Debug.Log($"  能力等级: ATK+{pet.attackLevel} MATK+{pet.magicAttackLevel} DEF+{pet.defenseLevel}");
                        Debug.Log($"  技能数量: {pet.skills?.Count}");
                    }
                }
            }

            // F2: 重置战斗
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (_battleController != null)
                {
                    Debug.Log("重置战斗...");
                    _battleController.ResetBattle();
                }
            }

            // F3: 调试UI
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("=== UI调试 ===");

                if (_uiController != null)
                {
                    Debug.Log($"技能按钮数量: {_uiController.skillButtons?.Length}");
                }
            }

            // F4: 强制更新UI
            if (Input.GetKeyDown(KeyCode.F4))
            {
                Debug.Log("强制更新UI...");

                BattleEvents.TriggerSkillButtonsUpdateNeeded(_battleController?.playerPet);

                var pets = FindObjectsOfType<PetEntity>();
                foreach (var pet in pets)
                {
                    BattleEvents.TriggerPetUIUpdateNeeded(pet);
                }
            }

            // F5: 显示技能信息
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (_battleController != null && _battleController.playerPet != null)
                {
                    var playerPet = _battleController.playerPet;
                    Debug.Log($"=== {playerPet.petName} 技能信息 ===");

                    for (int i = 0; i < playerPet.skills.Count; i++)
                    {
                        var skill = playerPet.skills[i];
                        Debug.Log($"技能{i}: {skill.skillName}");
                        Debug.Log($"  分类: {skill.category}, 目标: {(skill.applyToTarget ? "目标" : "自身")}");
                        Debug.Log($"  是否为属性技能: {skill.IsStatusSkill()}");
                        Debug.Log($"  PP: {skill.currentPP}/{skill.maxPP}");
                    }
                }
            }
        }
    }
}