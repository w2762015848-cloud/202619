using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    public class StatusIconManager : MonoBehaviour
    {
        [Header("UI引用")]
        public RectTransform statusIconContainer;
        public GameObject statusIconPrefab;
        public GameObject statusTooltipPrefab;

        [Header("图标设置")]
        public Sprite burnIcon;
        public Sprite freezeIcon;
        public Sprite paralyzeIcon;
        public Sprite poisonIcon;
        public Sprite blindIcon;
        public Sprite confusionIcon;
        public Sprite parasiticIcon;
        public Sprite stunIcon;
        public Sprite defaultIcon;

        [Header("布局设置")]
        public float iconSpacing = 5f;
        public int maxIconsPerRow = 5;
        public float iconSize = 40f;

        [Header("位置设置 - 重要!")]
        [Tooltip("我方状态图标相对于血条的偏移")]
        public Vector2 allyIconOffset = new Vector2(-46f, 590f);

        [Tooltip("敌方状态图标相对于血条的偏移")]
        public Vector2 enemyIconOffset = new Vector2(-6f, -240f);

        [Tooltip("是否为每个宠物手动指定阵营? 如果为false，将尝试自动判断")]
        public bool manuallyAssignFaction = false;

        [Header("阵营判断设置（当manuallyAssignFaction为false时使用）")]
        [Tooltip("根据名称包含的关键词判断敌方阵营")]
        public string[] enemyNameKeywords = new string[] { "Enemy", "敌人", "Boss" };

        [Tooltip("根据名称包含的关键词判断友方阵营")]
        public string[] allyNameKeywords = new string[] { "Player", "Ally", "友方" };

        private Dictionary<PetEntity, RectTransform> _petIconContainers = new Dictionary<PetEntity, RectTransform>();
        private Dictionary<PetEntity, List<StatusIconUI>> _petStatusIcons = new Dictionary<PetEntity, List<StatusIconUI>>();
        private Dictionary<StatusCondition, Sprite> _statusSprites = new Dictionary<StatusCondition, Sprite>();

        private Dictionary<PetEntity, bool> _petIsAlly = new Dictionary<PetEntity, bool>();

        void Awake()
        {
            InitializeStatusSprites();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void SetPetFaction(PetEntity pet, bool isAlly)
        {
            if (pet != null)
            {
                _petIsAlly[pet] = isAlly;
                Debug.Log($"设置阵营: {pet.petName} 为 {(isAlly ? "我方" : "敌方")}");

                if (_petIconContainers.ContainsKey(pet))
                {
                    UpdateIconContainerPosition(pet);
                    RearrangeIcons(pet);
                }
            }
        }

        private bool IsPetAlly(PetEntity pet)
        {
            if (_petIsAlly.ContainsKey(pet))
            {
                return _petIsAlly[pet];
            }

            if (manuallyAssignFaction)
            {
                Debug.LogWarning($"宠物 {pet.petName} 未手动指定阵营，请调用SetPetFaction方法或关闭manuallyAssignFaction选项");
                return true;
            }

            string petName = pet.petName;

            foreach (string keyword in enemyNameKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) && petName.Contains(keyword))
                {
                    return false;
                }
            }

            foreach (string keyword in allyNameKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) && petName.Contains(keyword))
                {
                    return true;
                }
            }

            Debug.Log($"宠物 {petName} 无法通过名称判断阵营，默认视为友方");
            return true;
        }

        private void InitializeStatusSprites()
        {
            _statusSprites[StatusCondition.Burn] = burnIcon;
            _statusSprites[StatusCondition.Freeze] = freezeIcon;
            _statusSprites[StatusCondition.Paralyze] = paralyzeIcon;
            _statusSprites[StatusCondition.Poison] = poisonIcon;
            _statusSprites[StatusCondition.Blind] = blindIcon;
            _statusSprites[StatusCondition.Confusion] = confusionIcon;
            _statusSprites[StatusCondition.Parasitic] = parasiticIcon;
            _statusSprites[StatusCondition.Stun] = stunIcon;
        }

        private void SubscribeToEvents()
        {
            BattleEvents.OnStatusEffectApplied += OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved += OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated += OnStatusEffectUpdated;
            BattleEvents.OnPetDeath += OnPetDeath;
        }

        private void UnsubscribeFromEvents()
        {
            BattleEvents.OnStatusEffectApplied -= OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved -= OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated -= OnStatusEffectUpdated;
            BattleEvents.OnPetDeath -= OnPetDeath;
        }

        private void OnStatusEffectApplied(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null) return;

            if (!_petIsAlly.ContainsKey(pet))
            {
                _petIsAlly[pet] = IsPetAlly(pet);
            }

            AddOrUpdateStatusIcon(pet, effect);
        }

        private void OnStatusEffectRemoved(PetEntity pet, StatusCondition condition)
        {
            if (pet == null) return;
            RemoveStatusIcon(pet, condition);
        }

        private void OnStatusEffectUpdated(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null) return;
            UpdateStatusIcon(pet, effect);
        }

        private void OnPetDeath(PetEntity pet)
        {
            if (pet == null) return;
            ClearPetStatusIcons(pet);
            if (_petIsAlly.ContainsKey(pet))
            {
                _petIsAlly.Remove(pet);
            }
        }

        private void AddOrUpdateStatusIcon(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null)
            {
                Debug.LogWarning("添加状态图标失败: 宠物或状态效果为空");
                return;
            }

            if (!_petIconContainers.ContainsKey(pet))
            {
                CreatePetIconContainer(pet);
            }

            if (!_petStatusIcons.ContainsKey(pet))
            {
                _petStatusIcons[pet] = new List<StatusIconUI>();
            }

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            StatusIconUI existingIcon = petIcons.Find(icon => icon != null && icon.condition == effect.condition);

            if (existingIcon != null)
            {
                existingIcon.UpdateStatus(effect.remainingTurns, effect.stackCount);
            }
            else
            {
                RectTransform container = _petIconContainers[pet];
                if (container == null)
                {
                    CreatePetIconContainer(pet);
                    container = _petIconContainers[pet];
                }

                if (statusIconPrefab == null)
                {
                    Debug.LogError("状态图标预制体未分配!");
                    return;
                }

                GameObject iconObj = Instantiate(statusIconPrefab, container);
                if (iconObj == null)
                {
                    Debug.LogError("实例化状态图标失败!");
                    return;
                }

                StatusIconUI newIcon = iconObj.GetComponent<StatusIconUI>();
                if (newIcon == null)
                {
                    Debug.LogError("状态图标预制体缺少 StatusIconUI 组件!");
                    Destroy(iconObj);
                    return;
                }

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                }

                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null)
                {
                    Sprite statusSprite = null;
                    if (_statusSprites.ContainsKey(effect.condition))
                    {
                        statusSprite = _statusSprites[effect.condition];
                    }

                    if (statusSprite == null)
                    {
                        statusSprite = GetSpriteFromInspector(effect.condition);
                    }

                    if (statusSprite == null && defaultIcon != null)
                    {
                        statusSprite = defaultIcon;
                        Debug.LogWarning($"状态 {effect.condition} 的图标未配置，使用默认图标");
                    }

                    if (statusSprite != null)
                    {
                        iconImage.sprite = statusSprite;
                        iconImage.color = Color.white;
                    }
                    else
                    {
                        Debug.LogError($"状态 {effect.condition} 的图标未配置，且无默认图标");
                        iconImage.color = Color.gray;
                    }
                }
                else
                {
                    Debug.LogWarning("状态图标预制体缺少 Image 组件!");
                }

                newIcon.tooltipPrefab = statusTooltipPrefab;
                newIcon.Initialize(effect.condition, effect.remainingTurns, effect.stackCount, pet);

                petIcons.Add(newIcon);
            }

            RearrangeIcons(pet);
        }

        private Sprite GetSpriteFromInspector(StatusCondition condition)
        {
            return condition switch
            {
                StatusCondition.Burn => burnIcon,
                StatusCondition.Freeze => freezeIcon,
                StatusCondition.Paralyze => paralyzeIcon,
                StatusCondition.Poison => poisonIcon,
                StatusCondition.Blind => blindIcon,
                StatusCondition.Confusion => confusionIcon,
                StatusCondition.Parasitic => parasiticIcon,
                StatusCondition.Stun => stunIcon,
                _ => defaultIcon
            };
        }

        private void CreatePetIconContainer(PetEntity pet)
        {
            if (pet == null || statusIconContainer == null)
            {
                Debug.LogWarning("创建图标容器失败: 宠物或容器为空");
                return;
            }

            // 寻找宠物的血条UI
            Transform petHealthBar = FindPetHealthBar(pet);

            if (petHealthBar == null)
            {
                Debug.LogWarning($"找不到宠物 {pet.petName} 的血条UI，将使用全局容器");
                petHealthBar = statusIconContainer;
            }

            GameObject containerObj = new GameObject($"{pet.petName}_StatusIcons");
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();

            // 设置为宠物血条的子物体
            containerRect.SetParent(petHealthBar, false);
            containerRect.localScale = Vector3.one;

            // 根据阵营设置不同的锚点
            bool isAlly = IsPetAlly(pet);

            if (isAlly)
            {
                // 我方：血条上方，居中对齐
                containerRect.anchorMin = new Vector2(0.5f, 1f);
                containerRect.anchorMax = new Vector2(0.5f, 1f);
                containerRect.pivot = new Vector2(0.5f, 1f);
                containerRect.anchoredPosition = allyIconOffset;
            }
            else
            {
                // 敌方：血条下方，居中对齐
                containerRect.anchorMin = new Vector2(0.5f, 0f);
                containerRect.anchorMax = new Vector2(0.5f, 0f);
                containerRect.pivot = new Vector2(0.5f, 0f);
                containerRect.anchoredPosition = enemyIconOffset;
            }

            _petIconContainers[pet] = containerRect;

            Debug.Log($"为宠物 {pet.petName} 创建图标容器，父对象: {petHealthBar.name}，阵营: {(isAlly ? "我方" : "敌方")}，位置: {containerRect.anchoredPosition}");
        }

        private Transform FindPetHealthBar(PetEntity pet)
        {
            if (pet == null) return null;

            // 方法1：通过PetUI组件查找
            PetUI petUI = pet.GetComponent<PetUI>();
            if (petUI != null && petUI.hpSlider != null)
            {
                return petUI.hpSlider.transform.parent ?? petUI.hpSlider.transform;
            }

            // 方法2：在宠物对象下查找常见的血条名称
            string[] possibleNames = { "HealthBar", "HPBar", "Health", "HP", "BloodBar", "血条", "血量" };
            foreach (string name in possibleNames)
            {
                Transform healthBar = pet.transform.Find(name);
                if (healthBar != null) return healthBar;

                // 在子对象中递归查找
                healthBar = FindChildRecursive(pet.transform, name);
                if (healthBar != null) return healthBar;
            }

            // 方法3：查找包含"health"或"hp"的对象
            foreach (Transform child in pet.transform)
            {
                if (child.name.ToLower().Contains("health") ||
                    child.name.ToLower().Contains("hp") ||
                    child.name.ToLower().Contains("血"))
                {
                    return child;
                }
            }

            // 方法4：查找Slider组件
            Slider slider = pet.GetComponentInChildren<Slider>();
            if (slider != null) return slider.transform;

            Debug.LogWarning($"找不到宠物 {pet.petName} 的血条UI");
            return null;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;

                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void UpdateIconContainerPosition(PetEntity pet)
        {
            if (!_petIconContainers.ContainsKey(pet)) return;

            RectTransform containerRect = _petIconContainers[pet];
            bool isAlly = IsPetAlly(pet);

            // 根据阵营更新位置
            containerRect.anchoredPosition = isAlly ? allyIconOffset : enemyIconOffset;
        }

        private void UpdateStatusIcon(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            StatusIconUI existingIcon = petIcons.Find(icon => icon != null && icon.condition == effect.condition);

            if (existingIcon != null)
            {
                existingIcon.UpdateStatus(effect.remainingTurns, effect.stackCount);
            }
        }

        private void RemoveStatusIcon(PetEntity pet, StatusCondition condition)
        {
            if (pet == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            StatusIconUI iconToRemove = petIcons.Find(icon => icon != null && icon.condition == condition);

            if (iconToRemove != null)
            {
                petIcons.Remove(iconToRemove);
                if (iconToRemove.gameObject != null)
                {
                    Destroy(iconToRemove.gameObject);
                }
            }

            RearrangeIcons(pet);

            if (petIcons.Count == 0 && _petIconContainers.ContainsKey(pet))
            {
                if (_petIconContainers[pet] != null && _petIconContainers[pet].gameObject != null)
                {
                    Destroy(_petIconContainers[pet].gameObject);
                }
                _petIconContainers.Remove(pet);
                _petStatusIcons.Remove(pet);
                if (_petIsAlly.ContainsKey(pet))
                {
                    _petIsAlly.Remove(pet);
                }
            }
        }

        private void ClearPetStatusIcons(PetEntity pet)
        {
            if (pet == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            foreach (var icon in petIcons)
            {
                if (icon != null && icon.gameObject != null)
                {
                    Destroy(icon.gameObject);
                }
            }

            if (_petIconContainers.ContainsKey(pet) && _petIconContainers[pet] != null)
            {
                Destroy(_petIconContainers[pet].gameObject);
                _petIconContainers.Remove(pet);
            }

            _petStatusIcons.Remove(pet);
            if (_petIsAlly.ContainsKey(pet))
            {
                _petIsAlly.Remove(pet);
            }
        }

        private void RearrangeIcons(PetEntity pet)
        {
            if (!_petIconContainers.ContainsKey(pet) || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            RectTransform containerRect = _petIconContainers[pet];

            if (containerRect == null) return;

            for (int i = 0; i < petIcons.Count; i++)
            {
                if (petIcons[i] == null) continue;

                RectTransform iconRect = petIcons[i].GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    int row = i / maxIconsPerRow;
                    int col = i % maxIconsPerRow;

                    float xPos = col * (iconSize + iconSpacing);
                    float yPos = -row * (iconSize + iconSpacing);

                    iconRect.anchoredPosition = new Vector2(xPos, yPos);
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                }
            }
        }

        void Update()
        {
            // 移除频繁的位置更新，因为位置已经固定
            // 如果血条会移动，可以在这里添加位置更新逻辑
            // 但目前我们先假设血条位置是固定的
        }

        public bool HasStatusIcon(StatusCondition condition)
        {
            return _statusSprites.ContainsKey(condition) && _statusSprites[condition] != null;
        }

        public void RefreshAllIconPositions()
        {
            foreach (var pet in _petIconContainers.Keys)
            {
                if (pet != null)
                {
                    UpdateIconContainerPosition(pet);
                    RearrangeIcons(pet);
                }
            }
        }
    }
}