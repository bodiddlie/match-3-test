using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {
  public int xIndex;
  public int yIndex;

  // Start is called before the first frame update
  void Start() {

  }

  public void Init(int x, int y) {
    xIndex = x;
    yIndex = y;
  }

  void OnMouseDown() {
    Board.instance.ClickTile(this);
  }

  void OnMouseEnter() {
    Board.instance.DragToTile(this);
  }

  void OnMouseUp() {
    Board.instance.ReleaseTile();
  }
}
