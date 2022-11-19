using System;
using System.Linq;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = System.Random;

[Serializable]
public struct TetrominoData
{
    public GameObject prefab;
    public Vector2 spawnOffset;
}

public class GameStateManager : MonoBehaviour
{
    /// An array containing prefabs of tetrominoes
    public TetrominoData[] tetrominoes;

    public float FallTimeout { get; private set; } = FallSpeedPerLevels[0];

    [SerializeField]
    private float instantFallTimeout = 40;
    public float InstantFallTimeout => instantFallTimeout;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI linesText;
    public TextMeshProUGUI scoreText;
    public GameObject nextBlockPreview;

    /// <summary>
    /// Determines how many lines should user clear before going to the next level
    /// <see cref="GetLinesToClearForLevel"/>
    /// </summary>
    public int linesPerLevelMultiplier = 10;
    
    private int _score;
    private int _level;
    private int _linesToClear;
    private int _totalLinesCleared;
    private int _linesCleared;
    private static readonly int[] ClearedLinesScoring = { 40, 100, 300, 1200 };
    private BlockController _controller;
    private TetrominoData? _nextTetromino;
    
    private static readonly float[] FallSpeedPerLevels =
    {
        0.88f,
        0.82f,
        0.75f,
        0.68f,
        0.62f,
        0.55f,
        0.47f,
        0.37f,
        0.28f,
    };

    // ? Use if needed fixed sequence of tetrominoes instead of full randomness each turn
    // private TetrominoData[] _gameSequence;
    // private TetrominoData[] GenerateGameSequence()
    // {
    //     var rnd = new Random();
    //     return tetrominoes.OrderBy(x => rnd.Next()).ToArray();    
    // }

    public TetrominoData Next()
    {
        _nextTetromino ??= GetRandomTetromino();

        var current = _nextTetromino;
        
        _nextTetromino = GetRandomTetromino();
        SpawnPreview();

        return (TetrominoData)current;
    }
    
    private TetrominoData GetRandomTetromino()
    {
        return tetrominoes[new Random().Next(0, tetrominoes.Length)];
    }

    private void SpawnPreview()
    {
        var preview = Instantiate(_nextTetromino?.prefab);
        
        foreach (Transform child in nextBlockPreview.transform)
        {
            Destroy(child.gameObject);
        }
        
        preview.transform.parent = nextBlockPreview.transform;
        preview.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        preview.transform.localPosition = Vector3.zero;
    }
    
    private void Awake() => UpdateText();

    private void UpdateText()
    {
        levelText.text = $"Level {_level + 1}";
        scoreText.text = $"{_score}";
        linesText.text = $"{_totalLinesCleared}";
    }
    

    private void Start()
    {
        _controller = GetComponent<BlockController>();
        _linesToClear = GetLinesToClearForLevel(_level);
    }

    public void OnGameOver()
    {
        _controller.enabled = false;
    }

    public void OnLineClear(int linesCleared)
    {
        AddScore(linesCleared);
        
        _linesCleared += linesCleared;
        _totalLinesCleared += linesCleared;
        
        if (_linesCleared >= _linesToClear)
        {
            _linesCleared -= _linesToClear;
            LevelUp();
        };

        UpdateText();
    }

    private int GetLinesToClearForLevel(int level)
    {
        return (level) *  linesPerLevelMultiplier + linesPerLevelMultiplier;
    }

    private void LevelUp()
    {
        _linesToClear = GetLinesToClearForLevel(_level + 1);
        _level += 1;
        FallTimeout = _level >= FallSpeedPerLevels.Length ? FallSpeedPerLevels.Last() : FallSpeedPerLevels[_level];
    }

    private void AddScore(int linesCleared)
    {
        _score += ClearedLinesScoring[linesCleared - 1] * (_level + 1);
    }
}