using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    public class BattleOptimizer : MonoBehaviour
    {
        [Header("优化设置")]
        public int initialDamageTextPoolSize = 20;
        public bool enableObjectPooling = true;
        public float garbageCollectionInterval = 30f;

        private float _lastGCTime;
        private Dictionary<string, Queue<GameObject>> _poolDictionary;

        void Start()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _lastGCTime = Time.time;

            if (enableObjectPooling)
            {
                PrewarmPools();
            }
        }

        void Update()
        {
            // 定期触发垃圾回收（避免频繁GC卡顿）
            if (Time.time - _lastGCTime > garbageCollectionInterval)
            {
                if (System.GC.GetTotalMemory(false) > 100 * 1024 * 1024) // 超过100MB
                {
                    System.GC.Collect();
                    Resources.UnloadUnusedAssets();
                }
                _lastGCTime = Time.time;
            }
        }

        private void PrewarmPools()
        {
            // 预加载常用对象
            // 可以在需要时扩展
        }

        public GameObject GetFromPool(string poolKey, GameObject prefab, Transform parent)
        {
            if (!enableObjectPooling || !_poolDictionary.ContainsKey(poolKey))
            {
                return Instantiate(prefab, parent);
            }

            var pool = _poolDictionary[poolKey];
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return Instantiate(prefab, parent);
        }

        public void ReturnToPool(string poolKey, GameObject obj)
        {
            if (!enableObjectPooling) return;

            if (!_poolDictionary.ContainsKey(poolKey))
            {
                _poolDictionary[poolKey] = new Queue<GameObject>();
            }

            obj.SetActive(false);
            _poolDictionary[poolKey].Enqueue(obj);
        }
    }
}