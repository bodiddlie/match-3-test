using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour {

  public int width;
  public int height;
  public int borderSize;

  public GameObject tilePrefab;

  private Tile[,] tiles;

  // Start is called before the first frame update
  void Start() {
    tiles = new Tile[width, height];
    SetupTiles();
    SetupCamera();
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
}
