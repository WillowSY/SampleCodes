using System.Collections.Generic;
using UnityEngine;

namespace Core.SpatialSystem
{
    /// Spatial partitioning system using a sparse hash grid.
    /// Designed for zero-allocation queries to support high-frequency updates.
    public class MM_SpatialHashGrid
    {
        // Implements IEquatable to prevent boxing when used as a Dictionary key.
        private readonly struct CellKey : System.IEquatable<CellKey>
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public CellKey(int x, int y, int z)
            {
                X = x; Y = y; Z = z;
            }

            public bool Equals(CellKey other) => X == other.X && Y == other.Y && Z == other.Z;
            public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
        }

        private class Cell
        {
            // Initial capacity set to 16 to reduce resizing overhead.
            public readonly List<MM_SpatialTriggerVolume> Items = new List<MM_SpatialTriggerVolume>(16);
        }

        private readonly float _cellSize;
        private readonly Dictionary<CellKey, Cell> _cells = new Dictionary<CellKey, Cell>();
        private readonly HashSet<MM_SpatialTriggerVolume> _globalItems = new HashSet<MM_SpatialTriggerVolume>();

        public MM_SpatialHashGrid(float cellSize)
        {
            _cellSize = cellSize;
        }
        
        public void Add(MM_SpatialTriggerVolume item)
        {
            if (_globalItems.Contains(item)) return;

            _globalItems.Add(item);
            InsertToCells(item);
        }
        public void Remove(MM_SpatialTriggerVolume item)
        {
            if (!_globalItems.Contains(item)) return;

            RemoveFromCells(item);
            _globalItems.Remove(item);
        }
        
        // Updates the item's position in the grid.
        // Essential for dynamic objects that move or change size.
        public void UpdateItem(MM_SpatialTriggerVolume item)
        {
            RemoveFromCells(item);
            InsertToCells(item);
        }

        // Populates the provided results list with items in the relevant cells.
        // Uses an external buffer to ensure zero allocation (GC-free query).
        public void FindNearby(Vector3 position, List<MM_SpatialTriggerVolume> results)
        {
            CellKey key = GetCellKey(position);

            if (_cells.TryGetValue(key, out Cell cell))
            {
                for (int i = 0; i < cell.Items.Count; i++)
                {
                    results.Add(cell.Items[i]);
                }
            }
        }

        private void InsertToCells(MM_SpatialTriggerVolume item)
        {
            Bounds bounds = item.GetBounds();
            Vector3Int min = GetCellCoords(bounds.min);
            Vector3Int max = GetCellCoords(bounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        CellKey key = new CellKey(x, y, z);
                        
                        // (Lazy initialization) Create cell only when needed.
                        if (!_cells.TryGetValue(key, out Cell cell))
                        {
                            cell = new Cell();
                            _cells[key] = cell;
                        }
                        cell.Items.Add(item);
                    }
                }
            }
        }

        private void RemoveFromCells(MM_SpatialTriggerVolume item)
        {
            Bounds bounds = item.GetBounds();
            Vector3Int min = GetCellCoords(bounds.min);
            Vector3Int max = GetCellCoords(bounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        CellKey key = new CellKey(x, y, z);
                        if (_cells.TryGetValue(key, out Cell cell))
                        {
                            // Note: List.Remove is O(N), but cell density is expected to be low.
                            cell.Items.Remove(item);
                            
                            // Note: We keep empty cells to prevent re-allocation overhead 
                            // if an item moves back into this cell later.
                        }
                    }
                }
            }
        }

        private CellKey GetCellKey(Vector3 position)
        {
            return new CellKey(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize)
            );
        }

        private Vector3Int GetCellCoords(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize)
            );
        }
    }
}