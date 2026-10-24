using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    public sealed class GridPlacement
    {
        private readonly List<bool[]> takenRows = new();
        private readonly int columns;
        private readonly bool growing;

        public GridPlacement(int rows, int columns, bool growing)
        {
            this.columns = columns;
            this.growing = growing;

            for (int row = 0; row < rows; row++)
            {
                takenRows.Add(new bool[columns]);
            }
        }

        public int Rows => takenRows.Count;

        public bool TryPlace(Vector2Int size, out int row, out int column)
        {
            if (size.x <= columns)
            {
                for (row = 0; growing || row + size.y <= takenRows.Count; row++)
                {
                    while (takenRows.Count < row + size.y)
                    {
                        takenRows.Add(new bool[columns]);
                    }

                    for (column = 0; column + size.x <= columns; column++)
                    {
                        if (!AreCellsFree(row, column, size)) continue;

                        TakeCells(row, column, size);
                        return true;
                    }
                }
            }

            row = 0;
            column = 0;
            return false;
        }

        private bool AreCellsFree(int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++)
                {
                    if (takenRows[row + down][column + right]) return false;
                }
            }

            return true;
        }

        private void TakeCells(int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++)
                {
                    takenRows[row + down][column + right] = true;
                }
            }
        }
    }
}
