using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityShooter.Core
{
    /// <summary>
    /// Generic object pooling system for performance optimization.
    /// Prevents frequent instantiation/destruction during heavy combat scenarios.
    /// </summary>
    /// <typeparam name="T">Type of component to pool (must be a Component).</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _availableObjects;
        private readonly HashSet<T> _activeObjects;
        private readonly int _maxSize;
        private readonly bool _expandable;

        /// <summary>
        /// Creates a new object pool.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="initialSize">Initial number of objects to create.</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited).</param>
        /// <param name="parent">Parent transform for pooled objects.</param>
        /// <param name="expandable">Whether pool can grow beyond initial size.</param>
        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 0, Transform parent = null, bool expandable = true)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _maxSize = maxSize;
            _expandable = expandable;

            _availableObjects = new Queue<T>(initialSize);
            _activeObjects = new HashSet<T>();

            // Pre-warm the pool
            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateObject();
                obj.gameObject.SetActive(false);
                _availableObjects.Enqueue(obj);
            }
        }

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <returns>A pooled object, or null if pool is exhausted and not expandable.</returns>
        public T Get()
        {
            T obj;

            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
            }
            else if (_expandable && (_maxSize == 0 || TotalCount < _maxSize))
            {
                obj = CreateObject();
            }
            else
            {
                return null;
            }

            obj.gameObject.SetActive(true);
            _activeObjects.Add(obj);

            return obj;
        }

        /// <summary>
        /// Gets an object from the pool and positions it.
        /// </summary>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <returns>A pooled object at the specified transform.</returns>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="obj">The object to return.</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            if (!_activeObjects.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] Attempting to return an object that wasn't from this pool: {obj.name}");
                return;
            }

            obj.gameObject.SetActive(false);
            _activeObjects.Remove(obj);
            _availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Returns all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            // Create a copy to avoid modification during iteration
            var activeList = new List<T>(_activeObjects);
            foreach (T obj in activeList)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Creates a new object for the pool.
        /// </summary>
        private T CreateObject()
        {
            T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_Pooled_{TotalCount}";
            return obj;
        }

        /// <summary>
        /// Clears the pool and destroys all objects.
        /// </summary>
        public void Clear()
        {
            foreach (T obj in _availableObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            foreach (T obj in _activeObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            _availableObjects.Clear();
            _activeObjects.Clear();
        }

        /// <summary>
        /// Gets the number of available objects in the pool.
        /// </summary>
        public int AvailableCount => _availableObjects.Count;

        /// <summary>
        /// Gets the number of currently active objects.
        /// </summary>
        public int ActiveCount => _activeObjects.Count;

        /// <summary>
        /// Gets the total number of objects managed by this pool.
        /// </summary>
        public int TotalCount => _availableObjects.Count + _activeObjects.Count;

        /// <summary>
        /// Gets whether the pool can provide more objects.
        /// </summary>
        public bool HasAvailable => _availableObjects.Count > 0 || (_expandable && (_maxSize == 0 || TotalCount < _maxSize));
    }

    /// <summary>
    /// MonoBehaviour wrapper for managing object pools.
    /// Attach to a scene object to persist pools across the scene.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        private static ObjectPoolManager _instance;
        private readonly Dictionary<string, object> _pools = new Dictionary<string, object>();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static ObjectPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectPoolManager");
                    _instance = go.AddComponent<ObjectPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }

        /// <summary>
        /// Creates or retrieves a pool for the specified prefab.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="prefab">The prefab to pool.</param>
        /// <param name="initialSize">Initial pool size.</param>
        /// <param name="maxSize">Maximum pool size.</param>
        /// <returns>The object pool.</returns>
        public ObjectPool<T> GetPool<T>(T prefab, int initialSize = 10, int maxSize = 0) where T : Component
        {
            string key = GetPoolKey(prefab);

            if (_pools.TryGetValue(key, out object existingPool))
            {
                return existingPool as ObjectPool<T>;
            }

            // Create parent container for this pool
            GameObject poolContainer = new GameObject($"Pool_{prefab.name}");
            poolContainer.transform.SetParent(transform);

            var pool = new ObjectPool<T>(prefab, initialSize, maxSize, poolContainer.transform);
            _pools[key] = pool;

            return pool;
        }

        /// <summary>
        /// Removes a pool for the specified prefab.
        /// </summary>
        public void RemovePool<T>(T prefab) where T : Component
        {
            string key = GetPoolKey(prefab);

            if (_pools.TryGetValue(key, out object pool))
            {
                (pool as ObjectPool<T>)?.Clear();
                _pools.Remove(key);
            }
        }

        /// <summary>
        /// Clears all pools managed by this manager.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                // Use reflection to call Clear on the generic pool
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);
            }

            _pools.Clear();
        }

        private string GetPoolKey<T>(T prefab) where T : Component
        {
            return $"{typeof(T).Name}_{prefab.GetInstanceID()}";
        }
    }

    /// <summary>
    /// Helper component for auto-returning pooled objects after a duration.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private float _returnTime;
        private bool _autoReturn;
        private System.Action<PooledObject> _returnCallback;

        /// <summary>
        /// Configures auto-return behavior.
        /// </summary>
        /// <param name="duration">Time before returning to pool.</param>
        /// <param name="returnCallback">Callback to return this object.</param>
        public void SetAutoReturn(float duration, System.Action<PooledObject> returnCallback)
        {
            _returnTime = Time.time + duration;
            _autoReturn = true;
            _returnCallback = returnCallback;
        }

        /// <summary>
        /// Disables auto-return.
        /// </summary>
        public void DisableAutoReturn()
        {
            _autoReturn = false;
            _returnCallback = null;
        }

        private void Update()
        {
            if (_autoReturn && Time.time >= _returnTime)
            {
                _autoReturn = false;
                _returnCallback?.Invoke(this);
            }
        }

        private void OnDisable()
        {
            _autoReturn = false;
        }
    }
}
