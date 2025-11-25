using System.Collections.Generic;
using UnityEngine;

namespace Core.SpatialSystem
{
    // Central manager for the spatial hashing system.
    // Implements a hierarchical grid approach (LOD) to handle objects of varying sizes efficiently.
    public class MM_SpatialTriggerManager : MonoBehaviour
    {
        public static MM_SpatialTriggerManager Instance { get; private set; }

        // (Two grid levels) Small (10m) for details, Large (100m) for broad zones.
        private MM_SpatialHashGrid _smallGrid;
        private MM_SpatialHashGrid _largeGrid;
        
        // Pre-allocated buffer for query results to prevent runtime GC allocation.
        private readonly List<MM_SpatialTriggerVolume> _queryBuffer = new List<MM_SpatialTriggerVolume>(64);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _smallGrid = new MM_SpatialHashGrid(10.0f);
            _largeGrid = new MM_SpatialHashGrid(100.0f);
        }

        public void Register(MM_SpatialTriggerVolume volume)
        {
            float maxSize = Mathf.Max(volume.GetBounds().size.x, volume.GetBounds().size.z);
            
            // Distribute to the appropriate grid level based on size.
            if (maxSize > 50.0f)
            {
                _largeGrid.Add(volume);
            }
            else
            {
                _smallGrid.Add(volume);
            }
        }

        public void Unregister(MM_SpatialTriggerVolume volume)
        {
            // Attempt to remove from both grids as we don't track which one it's in here.
            _smallGrid.Remove(volume);
            _largeGrid.Remove(volume);
        }
        
        // Finds the highest priority trigger volume at the given position.
        public MM_SpatialTriggerVolume FindBestTriggerAt(Vector3 position)
        {
            _queryBuffer.Clear();

            // (Broad-phase) Collect candidates from both grid levels.
            _smallGrid.FindNearby(position, _queryBuffer);
            _largeGrid.FindNearby(position, _queryBuffer);

            MM_SpatialTriggerVolume bestTrigger = null;
            int highestPriority = int.MinValue;

            // (Narrow-phase) Precise AABB check and priority resolution.
            int count = _queryBuffer.Count;
            for (int i = 0; i < count; i++)
            {
                var volume = _queryBuffer[i];
                if (volume.Contains(position))
                {
                    if (volume.Priority > highestPriority)
                    {
                        highestPriority = volume.Priority;
                        bestTrigger = volume;
                    }
                }
            }

            return bestTrigger;
        }
    }
}