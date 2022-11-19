using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int rightBound = 9;
    public int topBound = 19;
    public int maxTetrominoHeight = 4;

    private GameObject[][] _gameGrid;
    private GameStateManager _state;

    // Start is called before the first frame update
    private void Awake()
    {
        _state = GetComponent<GameStateManager>();
        _gameGrid = new GameObject[topBound + 1][];
        for (var i = 0; i <= topBound; i++)
        {
            _gameGrid[i] = new GameObject[rightBound + 1];
        }
    }


    public bool IsOccupied(int x, int y) => !ReferenceEquals(_gameGrid[y][x], null);

    public void SaveBlock(GameObject block)
    {
        var lowestCoord = topBound + 1;
        foreach (Transform child in block.transform)
        {
            var pos = child.position;
            var x = Mathf.RoundToInt(pos.x);
            var y = Mathf.RoundToInt(pos.y);
            
            if (y < lowestCoord) lowestCoord = y;
            
            _gameGrid[y][x] = child.gameObject;
        }
        
        block.transform.DetachChildren();
        Destroy(block);
        
        CheckForCompleteLines(lowestCoord);
    }

    /// <summary>
    /// This function checks all lines from <paramref name="lowest"/> to <paramref name="lowest"/>+<see cref="maxTetrominoHeight"/>,
    /// deletes complete lines and then moves all lines above complete line 1 cell down
    /// </summary>
    /// <param name="lowest">Lowest line to check</param>
    private void CheckForCompleteLines(int lowest)
    {
        // How many rows we've checked
        var rowsChecked = 0;
        // Starting with the lowest row
        var currentRow = lowest;
        var rowsCleared = 0;
        
        // Check untill exceeded max height of placed tetromino
        while (rowsChecked < maxTetrominoHeight)
        {
            if (currentRow > topBound) break;
            
            // Check if all cells in row is not empty
            if (_gameGrid[currentRow].All(v => !ReferenceEquals(v, null)))
            {
                // Removing the actual cell game object from scene
                foreach (var cell in _gameGrid[currentRow])
                {
                    Destroy(cell); 
                }

                // Removing line from the game grid and appending it with new empty line to compensate
                _gameGrid = _gameGrid
                    .Where((_, index) => index != currentRow)
                    .Append(new GameObject[rightBound + 1])
                    .ToArray();

                MoveRowsAboveDown(currentRow);
                
                // Decrement row to compensate that we had removed current row, so we need to check it again since
                // it's updated
                currentRow--;

                rowsCleared++;
            }

            currentRow++;
            rowsChecked++;
        }

        if (rowsCleared != 0)
        {
            _state.OnLineClear(rowsCleared);
        }
    }

    /// <summary>
    /// Moves every row equals or above <paramref name="startingPoint"/> 1 cell down
    /// </summary>
    /// <param name="startingPoint">From what point to move move rows down</param>
    private void MoveRowsAboveDown(int startingPoint)
    {
        for (var i = startingPoint; i < _gameGrid.Length; i++)
        {
            foreach (var cell in _gameGrid[i])
            {
                if (ReferenceEquals(cell, null)) continue;
                cell.transform.position += Vector3.down;
            }
        }
    }
}
