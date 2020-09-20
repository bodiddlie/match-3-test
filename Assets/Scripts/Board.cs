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

  public GameObject tileNormalPrefab;
  public GameObject tileObstaclePrefab;
  public GameObject[] gamePiecePrefabs;

  private Tile[,] tiles;
  private GamePiece[,] gamePieces;

  private Tile clickedTile;
  private Tile targetTile;

  private bool inputEnabled = true;

  public StartingTile[] startingTiles;

  [System.Serializable]
  public class StartingTile {
    public GameObject tilePrefab;
    public int x;
    public int y;
    public int z;
  }

  // Start is called before the first frame update
  void Start() {
    instance = GetComponent<Board>();
    tiles = new Tile[width, height];
    gamePieces = new GamePiece[width, height];
    SetupTiles();
    SetupCamera();
    FillBoard(10, 0.5f);
  }

  void MakeTile(GameObject prefab, int x, int y, int z = 0) {
    if (prefab == null) return;

    var position = new Vector3(x, y, z);
    var tile = Instantiate(prefab, position, Quaternion.identity);
    tile.name = $"Tile ({x},{y})";
    tiles[x, y] = tile.GetComponent<Tile>();
    tile.transform.parent = transform;
    tiles[x, y].Init(x, y);
  }

  void SetupTiles() {
    foreach (var tile in startingTiles) {
      if (tile != null) {
        MakeTile(tile.tilePrefab, tile.x, tile.y, tile.z);
      }
    }

    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        if (tiles[i, j] == null) {
          MakeTile(tileNormalPrefab, i, j);
        }
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

  void FillBoard(int falseYOffset = 0, float moveTime = 0.1f) {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        if (gamePieces[i, j] == null && tiles[i, j].tileType != TileType.Obstacle) {
          var piece = FillRandomAt(i, j, falseYOffset, moveTime);
          while (HasMatchOnFill(i, j)) {
            ClearPieceAt(i, j);
            piece = FillRandomAt(i, j, falseYOffset, moveTime);
          }
        }
      }
    }
  }


  GamePiece FillRandomAt(int x, int y, int falseYOffset = 0, float moveTime = 0.1f) {
    var randomPiece = Instantiate(GetRandomPiece(), Vector3.zero, Quaternion.identity);
    if (randomPiece != null) {
      randomPiece.transform.parent = transform;
      var gamePiece = randomPiece.GetComponent<GamePiece>();
      PlaceGamePiece(gamePiece, x, y);
      if (falseYOffset != 0) {
        randomPiece.transform.position = new Vector3(x, y + falseYOffset, 0);
        randomPiece.GetComponent<GamePiece>().Move(x, y, moveTime);
      }
      return gamePiece;
    }
    return null;
  }

  bool HasMatchOnFill(int x, int y, int minLength = 3) {
    var leftMatches = FindMatches(x, y, Vector2.left, minLength);
    var downwardMatches = FindMatches(x, y, Vector2.down, minLength);

    return leftMatches.Count + downwardMatches.Count > 0;
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
    if (clickedTile != null && targetTile != null && clickedTile.tileType != TileType.Obstacle && targetTile.tileType != TileType.Obstacle) {
      SwitchTiles(clickedTile, targetTile);
    }

    clickedTile = null;
    targetTile = null;
  }

  void SwitchTiles(Tile clicked, Tile target) {
    StartCoroutine(SwitchTileRoutine(clicked, target));
  }

  IEnumerator SwitchTileRoutine(Tile clicked, Tile target) {
    if (inputEnabled) {
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
      } else {
        yield return new WaitForSeconds(swapTime);
        var allMathces = clickedMatches.Union(targetMatches).ToList();
        ClearAndRefillBoard(allMathces);
      }
    }
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
      return new List<GamePiece>();
    }

    int nextX;
    int nextY;
    int maxValue = Mathf.Max(width, height);

    for (int i = 1; i < maxValue - 1; i++) {
      nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
      nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

      if (!IsWithinBounds(nextX, nextY)) break;

      var nextPiece = gamePieces[nextX, nextY];

      if (nextPiece == null) {
        break;
      }

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
    if (rightMatches == null) rightMatches = new List<GamePiece>();
    if (leftMatches == null) leftMatches = new List<GamePiece>();

    var combined = rightMatches.Union(leftMatches).ToList();

    return combined.Count >= minLength ? combined : new List<GamePiece>();
  }

  List<GamePiece> FindAllMatches() {
    var combined = new List<GamePiece>();

    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        var matches = FindMatchesAt(i, j);
        combined = combined.Union(matches).ToList();
      }
    }

    return combined;
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

  List<GamePiece> FindMatchesAt(List<GamePiece> pieces, int minLength = 3) {
    var matches = new List<GamePiece>();

    foreach (var piece in pieces) {
      matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();
    }

    return matches;
  }

  void HightlightTileOff(int x, int y) {
    var spriteRenderer = tiles[x, y].GetComponent<SpriteRenderer>();
    spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.0f);
  }

  void HighlightTileOn(int x, int y, Color color) {
    var spriteRenderer = tiles[x, y].GetComponent<SpriteRenderer>();
    spriteRenderer.color = color;
  }

  void HighlightPieces(List<GamePiece> pieces) {
    foreach (var piece in pieces) {
      if (piece != null) {
        HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
      }
    }
  }

  void ClearPieceAt(int x, int y) {
    GamePiece pieceToClear = gamePieces[x, y];

    if (pieceToClear != null) {
      gamePieces[x, y] = null;
      Destroy(pieceToClear.gameObject);
    }

    HightlightTileOff(x, y);
  }

  void ClearBoard() {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        ClearPieceAt(i, j);
      }
    }
  }

  void ClearPieces(List<GamePiece> gamePieces) {
    foreach (var piece in gamePieces) {
      if (piece != null) {
        ClearPieceAt(piece.xIndex, piece.yIndex);
      }
    }
  }

  List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f) {
    var movingPieces = new List<GamePiece>();

    for (int i = 0; i < height - 1; i++) {
      if (gamePieces[column, i] == null && tiles[column, i].tileType != TileType.Obstacle) {
        for (int j = i + 1; j < height; j++) {
          if (gamePieces[column, j] != null) {
            gamePieces[column, j].Move(column, i, collapseTime * (j - i));
            gamePieces[column, i] = gamePieces[column, j];
            gamePieces[column, i].SetCoord(column, i);

            if (!movingPieces.Contains(gamePieces[column, i])) {
              movingPieces.Add(gamePieces[column, i]);
            }

            gamePieces[column, j] = null;
            break;
          }
        }
      }
    }

    return movingPieces;
  }

  List<GamePiece> CollapseColumn(List<GamePiece> pieces) {
    var movingPieces = new List<GamePiece>();
    var columnsToCollapse = GetColumns(pieces);

    foreach (int column in columnsToCollapse) {
      movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
    }

    return movingPieces;
  }

  List<int> GetColumns(List<GamePiece> pieces) {
    var columns = new List<int>();

    foreach (var piece in pieces) {
      if (!columns.Contains(piece.xIndex)) {
        columns.Add(piece.xIndex);
      }
    }

    return columns;
  }

  void ClearAndRefillBoard(List<GamePiece> pieces) {
    StartCoroutine(ClearAndRefillRoutine(pieces));
  }

  IEnumerator ClearAndRefillRoutine(List<GamePiece> pieces) {
    inputEnabled = false;

    var matches = pieces.ToList();

    do {
      // clear and collapse
      yield return StartCoroutine(ClearAndCollapseRoutine(pieces));
      yield return null;

      // refill
      yield return StartCoroutine(RefillRoutine());
      matches = FindAllMatches();

      inputEnabled = true;
    } while (matches.Count != 0);
  }

  IEnumerator ClearAndCollapseRoutine(List<GamePiece> pieces) {
    var movingPieces = new List<GamePiece>();
    var matches = new List<GamePiece>();

    HighlightPieces(pieces);

    yield return new WaitForSeconds(0.2f);

    bool isFinished = false;

    while (!isFinished) {
      ClearPieces(pieces);
      yield return new WaitForSeconds(0.2f);
      movingPieces = CollapseColumn(pieces);
      yield return new WaitForSeconds(0.1f);
      matches = FindMatchesAt(movingPieces);
      if (matches.Count == 0) {
        isFinished = true;
      } else {
        yield return StartCoroutine(ClearAndCollapseRoutine(matches));
      }
    }
    yield return null;
  }

  IEnumerator RefillRoutine() {
    FillBoard(10, 0.5f);
    yield return null;
  }

  bool isCollapsed(List<GamePiece> pieces) {
    foreach (var piece in pieces) {
      if (piece != null) {
        if (piece.transform.position.y - (float)piece.yIndex > Mathf.Epsilon) {
          return false;
        }
      }
    }
    return true;
  }
}
