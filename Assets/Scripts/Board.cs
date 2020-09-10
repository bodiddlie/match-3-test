using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour {
  public static Board instance;

  public int width;
  public int height;
  public int borderSize;

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
  }

  void SetupTiles() {
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        var position = new Vector3(i, j, 0);
        Debug.Log(position);
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
    gamePieces[x, y] = gamePiece;
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
      Debug.Log($"Clicked tile: {tile.name}");
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
    var clickedPiece = gamePieces[clicked.xIndex, clicked.yIndex];
    var targetPiece = gamePieces[target.xIndex, target.yIndex];

    clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, 0.5f);
    targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, 0.5f);
  }

  bool IsTileAdjacent(Tile start, Tile end) {
    if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex) return true;
    if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex) return true;
    return false;
  }
}
