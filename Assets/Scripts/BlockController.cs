using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    public float horizontalMoveTimeout;
    public Vector2 spawnPoint;    
    
    private GameStateManager _state;
    private BoardManager _board;
    
    private TetrominoData _tetrominoData;
    private GameObject _tetromino;
    private GameObject _ghost;
    /// Amount of time ticks (tick == fallTimeout) block wasn't moving because of obstacles
    private int _ticksNotMovingDown;
    /// Is current block falling to snap to place
    private bool _isInstantFall;
    /// Time when block should go down 1 line
    private float _nextFallTick;
    /// Time when block can go right or left
    private float _nextHorizontalMoveTick;
    
    private void Start() 
    {
        _state = GetComponent<GameStateManager>();
        _board = GetComponent<BoardManager>();
        
        _tetrominoData = _state.Next();
        SpawnTetromino();
        SpawnGhost();
    }

    private void OnDisable()
    {
        if (_tetromino)
        {
            // Make tetromino be on top of everything
            foreach (Transform child in _tetromino.transform)
            {
                child.GetComponent<SpriteRenderer>().sortingOrder = 10;
            }
        }
        if (_ghost) Destroy(_ghost);
    }

    /// <summary>
    /// Tries to spawn a new tetromino.
    /// </summary>
    /// <returns>true if spawned tetromino overlaps other tetromino</returns>
    private bool SpawnTetromino()
    {
        _ticksNotMovingDown = 0;
        _isInstantFall = false;

        _tetrominoData = _state.Next();
        
        var spawnX = Mathf.RoundToInt(spawnPoint.x);
        var spawnY = Mathf.RoundToInt(spawnPoint.y);
        
        var pos = new Vector3(spawnX, spawnY, 0) + (Vector3)_tetrominoData.spawnOffset;
        _tetromino = Instantiate(_tetrominoData.prefab, pos, _tetrominoData.prefab.transform.rotation);

        return OverlapsAnother(_tetromino);
    }
    
    
    // ReSharper disable Unity.PerformanceAnalysis
    // It's called only when block is placed, so it's not that expensive for performance
    private void SpawnGhost()
    {
        // Destroy previous ghost before creating new one
        Destroy(_ghost);
        _ghost = Instantiate(_tetromino);

        foreach (Transform child in _ghost.transform)
        {
            var c = Color.gray;
            c.a = 0.5f;
            
            var sr = child.GetComponent<SpriteRenderer>();
            sr.material.color = c;
            // sr.sortingOrder = -1;
        }
    }


    private void UpdateGhost()
    {
        _ghost.transform.position = _tetromino.transform.position;
        _ghost.transform.rotation = _tetromino.transform.rotation;

        while (CanMoveDown(_ghost))
        {
            _ghost.transform.position += Vector3.down;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("s"))
        {
            _isInstantFall = true;
            _nextFallTick = 0;
        }
        
        var shouldSpawnNew = MoveDown();
        if (shouldSpawnNew)
        {
            _board.SaveBlock(_tetromino);
            var gameOver = SpawnTetromino();
            
            if (gameOver) _state.OnGameOver();
            else SpawnGhost();
            
            return;
        }
        
        if (Input.GetKeyDown("space"))
        {
            RotateIfPossible();
        }
        
        MoveSideways();
        
        UpdateGhost();
    }

    #region Movement

    // TODO: Move block from top bound too
    private void NormalizePosition()
    {
        var moveX = 0f;
        var moveY = 0f;
        foreach (Transform child in _tetromino.transform)
        {
            var pos = child.position;
            var offsetX = OutOfBoundOffset(pos.x, _board.rightBound);
            var offsetY = OutOfBoundOffset(pos.y, _board.topBound);
            moveX = Math.Abs(offsetX) > Math.Abs(moveX) ? -offsetX : moveX;
            moveY = Math.Abs(offsetY) > Math.Abs(moveY) ? -offsetY : moveY;
        }
        
        _tetromino.transform.position += new Vector3(moveX, moveY, 0);
    }

    /// <summary>
    ///
    /// * First bound counted as 0, so function need only one bound 
    /// </summary>
    /// <returns>How many whole numbers between one of the bounds and given position if the position is out of bounds</returns>
    /// <returns>0 if n</returns>
    private float OutOfBoundOffset(float pos, float bound)
    {
        if (pos >= 0 && pos <= bound) return 0;
        var offset = pos > bound ? (pos - bound) : pos;

        return offset;

    }
    
    private void RotateIfPossible()
    {
        if (_isInstantFall) return;
        
        RotateTetromino();
        NormalizePosition();
        
        if (OverlapsAnother(_tetromino))
        {
            RotateTetromino(true);
        }
    }
    private void RotateTetromino(bool opposite = false)
    {
        var angle = opposite ? 90 : -90;
        _tetromino.transform.RotateAround( _tetromino.transform.position, new Vector3(0, 0, 1), angle);
    }
    
    private bool MoveDown()
    {
        if (Time.time < _nextFallTick) return false;
        
        var multiplier = _isInstantFall ? _state.InstantFallTimeout : 1;
        _nextFallTick = Time.time + _state.FallTimeout / multiplier;
        
        if (CanMoveDown(_tetromino))
        {
            var pos = _tetromino.transform.position;
            pos.y -= 1;
            _tetromino.transform.position = pos;
        }
        else
        {
            if (_ticksNotMovingDown == 1)
            {
                // Normalize falling time in case of instant fall
                _nextFallTick = Time.time + _state.FallTimeout;
                return true;
            }
                
            _ticksNotMovingDown++;
        }
        
        return false;
    }
    
    private void MoveSideways()
    {
        if (!(Time.time > _nextHorizontalMoveTick) || _isInstantFall) return;
        
        var moveLeft = Input.GetKey("a");
        var moveRight = Input.GetKey("d");
        var pos = _tetromino.transform.position;
        
        // Do not do anything when requested both directions
        if (moveLeft && moveRight) return;
        var moved = moveLeft || moveRight;
        
        if (moveLeft && CanMoveLeft()) pos.x += -1;
        else if (moveRight && CanMoveRight()) pos.x += 1;
        
        if (moved) _nextHorizontalMoveTick = Time.time + horizontalMoveTimeout;
        
        _tetromino.transform.position = pos;
    }
    #endregion


    private bool OverlapsAnother(GameObject block, Vector2Int offset = default)
    {
        return block.transform.Cast<Transform>().Any(child => {
            var pos = child.position;
            var y = Mathf.RoundToInt(pos.y + offset.y);
            var x = Mathf.RoundToInt(pos.x + offset.x);

            return _board.IsOccupied(x, y);
        });
    }
    
    private bool CanMoveRight() => ValidateChildren(_tetromino, v => v.position.x < _board.rightBound) && !OverlapsAnother(_tetromino, Vector2Int.right);
    private bool CanMoveLeft() => ValidateChildren(_tetromino, v => Mathf.RoundToInt(v.position.x) > 0) && !OverlapsAnother(_tetromino, Vector2Int.left);
    
    private bool CanMoveDown(GameObject o) => ValidateChildren(o, v => Mathf.RoundToInt(v.position.y) > 0) && !OverlapsAnother(o, Vector2Int.down);
    /// <summary>
    /// Validates that each child tile of parent matches the check 
    /// </summary>
    private static bool ValidateChildren(GameObject parent, [NotNull, InstantHandle] Func<Transform, bool> check)
    {
        return parent.transform.Cast<Transform>().All(check);
    }
}