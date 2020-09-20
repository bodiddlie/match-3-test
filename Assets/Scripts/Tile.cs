using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType {
  Normal,
  Obstacle,
  Breakable
}

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour {
  public int xIndex;
  public int yIndex;

  public TileType tileType = TileType.Normal;

  private SpriteRenderer spriteRenderer;

  public int breakableValue = 0;
  public Sprite[] breakableSprites;
  public Color normalColor;

  // Start is called before the first frame update
  void Awake() {
    spriteRenderer = GetComponent<SpriteRenderer>();
  }

  public void Init(int x, int y) {
    xIndex = x;
    yIndex = y;

    if (tileType == TileType.Breakable && breakableSprites[breakableValue] != null) {
      spriteRenderer.sprite = breakableSprites[breakableValue];
    }
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

  public void BreakTile() {
    if (tileType != TileType.Breakable) return;

    StartCoroutine(BreakTileRoutine());
  }

  IEnumerator BreakTileRoutine() {
    breakableValue = Mathf.Clamp(--breakableValue, 0, breakableValue);
    yield return new WaitForSeconds(0.25f);

    if (breakableSprites[breakableValue] != null) {
      spriteRenderer.sprite = breakableSprites[breakableValue];
    }

    if (breakableValue == 0) {
      tileType = TileType.Normal;
      spriteRenderer.color = normalColor;
    }
  }
}
