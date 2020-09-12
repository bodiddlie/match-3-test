using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour {
  public static Board instance;

  public int width;
  public int height;
  public int borderSize;
  public float swapTime;

  public GameObject tilePrefab;
  public GameObject[] gamePiecePrefabs;

  private Tile[,] tiles;
  private GamePiece[,] gamePieces;

  private Tile clickedTile;
  private Tile targetTile;

  // Start is called before the first frame update
  void Start() {
    instance = GetComponent<Board>();
    tiles = new Tile[width, height];
    gamePieces = new GamePiece[width, height];
    SetupTiles();
    SetupCamera();
    FillRandom();
    //HighlightMatches();
  }

  void SetupTiles() {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        var position = new Vector3(i, j, 0);
        var tile = Instantiate(tilePrefab, position, Quaternion.identity);
        tile.name = $"Tile ({i},{j})";
        tiles[i, j] = tile.GetComponent<Tile>();
        tile.transform.parent = transform;
        tiles[i, j].Init(i, j);
      }
    }
  }

  void SetupCamera() {
    Camera.main.transform.position = new Vector3((float)(width - 1) / 2f, (float)(height - 1) / 2f, -10f);
    float aspectRatio = (float)Screen.width / (float)Screen.height;
    float verticalSize = (float)height / 2f + (float)borderSize;
    float horizontalSize = ((float)width / 2f + (float)borderSize) / aspectRatio;
    Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
  }

  GameObject GetRandomPiece() {
    int randomIndex = Random.Range(0, gamePiecePrefabs.Length);

    var randomPiece = gamePiecePrefabs[randomIndex];
    if (randomPiece == null) {
      Debug.LogWarning($"BOARD: {randomIndex} does not contain a valid GamePiece prefab.");
    }

    return randomPiece;
  }

  public void PlaceGamePiece(GamePiece gamePiece, int x, int y) {
    if (gamePiece == null) {
      Debug.LogWarning("BOARD: Invalid game piece");
      return;
    }

    gamePiece.transform.position = new Vector3(x, y, 0);
    gamePiece.transform.rotation = Quaternion.identity;
    gamePiece.SetCoord(x, y);
    if (IsWithinBounds(x, y)) {
      gamePieces[x, y] = gamePiece;
    }
  }

  void FillRandom() {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        var randomPiece = Instantiate(GetRandomPiece(), Vector3.zero, Quaternion.identity);
        if (randomPiece != null) {
          randomPiece.transform.parent = transform;
          var gamePiece = randomPiece.GetComponent<GamePiece>();
          PlaceGamePiece(gamePiece, i, j);
        }
      }
    }
  }

  public void ClickTile(Tile tile) {
    if (clickedTile == null) {
      clickedTile = tile;
    }
  }

  public void DragToTile(Tile tile) {
    if (clickedTile != null && IsTileAdjacent(tile, clickedTile)) {
      targetTile = tile;
    }
  }

  public void ReleaseTile() {
    if (clickedTile != null && targetTile != null) {
      SwitchTiles(clickedTile, targetTile);
    }

    clickedTile = null;
    targetTile = null;
  }

  void SwitchTiles(Tile clicked, Tile target) {
    StartCoroutine(SwitchTileRoutine(clicked, target));
  }

  IEnumerator SwitchTileRoutine(Tile clicked, Tile target) {
    var clickedPiece = gamePieces[clicked.xIndex, clicked.yIndex];
    var targetPiece = gamePieces[target.xIndex, target.yIndex];

    clickedPiece.Move(target.xIndex, target.yIndex, swapTime);
    targetPiece.Move(clicked.xIndex, clicked.yIndex, swapTime);

    yield return new WaitForSeconds(swapTime);

    var clickedMatches = FindMatchesAt(clicked.xIndex, clicked.yIndex, 3);
    var targetMatches = FindMatchesAt(target.xIndex, target.yIndex, 3);

    if (clickedMatches.Count + targetMatches.Count == 0) {
      clickedPiece.Move(clicked.xIndex, clicked.yIndex, swapTime);
      targetPiece.Move(target.xIndex, target.yIndex, swapTime);
    }

    HighlightMatchesAt(clicked.xIndex, clicked.yIndex);
    HighlightMatchesAt(target.xIndex, target.yIndex);
  }

  bool IsTileAdjacent(Tile start, Tile end) {
    if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex) return true;
    if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex) return true;
    return false;
  }

  bool IsWithinBounds(int x, int y) {
    return (x >= 0 && x < width && y >= 0 && y < height);
  }

  List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3) {
    var matches = new List<GamePiece>();
    GamePiece startPiece = null;

    if (IsWithinBounds(startX, startY)) {
      startPiece = gamePieces[startX, startY];
    }

    if (startPiece != null) {
      matches.Add(startPiece);
    } else {
      return null;
    }

    int nextX;
    int nextY;
    int maxValue = Mathf.Max(width, height);

    for (int i = 1; i < maxValue - 1; i++) {
      nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
      nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

      if (!IsWithinBounds(nextX, nextY)) break;

      var nextPiece = gamePieces[nextX, nextY];

      if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece)) {
        matches.Add(nextPiece);
      } else {
        break;
      }
    }

    if (matches.Count >= minLength) {
      return matches;
    }

    return new List<GamePiece>();
  }

  List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3) {
    var upwardMatches = FindMatches(startX, startY, Vector2.up, 2);
    var downwardMatches = FindMatches(startX, startY, Vector2.down, 2);

    var combined = upwardMatches.Union(downwardMatches).ToList();

    return combined.Count >= minLength ? combined : new List<GamePiece>();
  }

  List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3) {
    var rightMatches = FindMatches(startX, startY, Vector2.right, 2);
    var leftMatches = FindMatches(startX, startY, Vector2.left, 2);

    var combined = rightMatches.Union(leftMatches).ToList();

    return combined.Count >= minLength ? combined : new List<GamePiece>();
  }

  void HighlightMatchesAt(int x, int y) {
    HightlightTileOff(x, y);

    var combined = FindMatchesAt(x, y);
    if (combined.Count > 0) {
      foreach (var piece in combined) {
        HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
      }
    }
  }

  void HighlightMatches() {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        HighlightMatchesAt(i, j);
      }
    }
  }

  List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3) {
    var horizontalMatches = FindHorizontalMatches(x, y);
    var verticalMatches = FindVerticalMatches(x, y);
    return horizontalMatches.Union(verticalMatches).ToList();
  }

  void HightlightTileOff(int x, int y) {
    var spriteRenderer = tiles[x, y].GetComponent<SpriteRenderer>();
    spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.0f);
  }

  void HighlightTileOn(int x, int y, Color color) {
    var spriteRenderer = tiles[x, y].GetComponent<SpriteRenderer>();
    spriteRenderer.color = color;
  }
}
