using UnityEngine;

namespace Core.SpatialSystem
{
    // Component defining a spatial zone.
    // Decoupled from Unity Physics (Collider) for lightweight spatial queries.
    public class MM_SpatialTriggerVolume : MonoBehaviour
    {
        [Header("Volume Settings")]
        [Tooltip("Higher priority volumes override lower ones when overlapping.")]
        public int Priority = 0;
        
        public Vector3 Size = new Vector3(5, 5, 5);
        public Vector3 Center = Vector3.zero;

        [Header("Debug")]
        public Color DebugColor = new Color(0, 1, 0, 0.3f);

        private Bounds _cachedBounds;

        private void OnEnable()
        {
            if (MM_SpatialTriggerManager.Instance != null)
            {
                UpdateBounds();
                MM_SpatialTriggerManager.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (MM_SpatialTriggerManager.Instance != null)
            {
                MM_SpatialTriggerManager.Instance.Unregister(this);
            }
        }

        // Called when dimensions change at runtime to refresh the grid.
        public void UpdateDimensions(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
            UpdateBounds();
            
            if (MM_SpatialTriggerManager.Instance != null)
            {
                // Note: The manager handles re-insertion logic.
                MM_SpatialTriggerManager.Instance.Register(this); 
            }
        }

        private void UpdateBounds()
        {
            // Cache world-space bounds to avoid recalculating every frame.
            _cachedBounds = new Bounds(transform.position + transform.TransformVector(Center), Vector3.Scale(Size, transform.lossyScale));
        }

        public Bounds GetBounds() => _cachedBounds;

        public bool Contains(Vector3 position)
        {
            return _cachedBounds.Contains(position);
        }

#if UNITY_EDITOR
        // Visualization for level design.
        private void OnDrawGizmos()
        {
            Gizmos.color = DebugColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Center, Size);
            Gizmos.DrawWireCube(Center, Size);
        }
#endif
    }
}