using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour {
  [SerializeField]
  private int xIndex;
  [SerializeField]
  private int yIndex;

  private bool isMoving;

  public enum MoveType {
    Linear,
    EaseOut,
    EaseIn,
    SmoothStep,
    SmootherStep
  }

  public MoveType moveType = MoveType.SmootherStep;

  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {
    if (Input.GetKeyDown(KeyCode.A)) {
      Move((int)transform.position.x - 1, (int)transform.position.y, 0.5f);
    }
    if (Input.GetKeyDown(KeyCode.D)) {
      Move((int)transform.position.x + 1, (int)transform.position.y, 0.5f);
    }
  }

  public void SetCoord(int x, int y) {
    xIndex = x;
    yIndex = y;
  }

  public void Move(int targetX, int targetY, float timeToMove) {
    if (!isMoving) {
      StartCoroutine(MoveRoutine(new Vector3(targetX, targetY, 0), timeToMove));
    }
  }

  IEnumerator MoveRoutine(Vector3 destination, float timeToMove) {
    Vector3 startPosition = transform.position;
    bool reachedDestination = false;
    float elapsedTime = 0.0f;
    isMoving = true;

    while (!reachedDestination) {
      if (Vector3.Distance(transform.position, destination) < 0.01) {
        reachedDestination = true;
        Board.instance.PlaceGamePiece(this, (int)destination.x, (int)destination.y);
        break;
      }

      elapsedTime += Time.deltaTime;
      float t = CalculateStepTime(elapsedTime, timeToMove);

      transform.position = Vector3.Lerp(startPosition, destination, t);
      yield return null;
    }
    isMoving = false;
  }

  float CalculateStepTime(float elapsedTime, float timeToMove) {
    float t = Mathf.Clamp(elapsedTime / timeToMove, 0, 1);
    switch (moveType) {
      case MoveType.Linear:
        return t;
      case MoveType.EaseOut:
        return Mathf.Sin(t * Mathf.PI * 0.5f);
      case MoveType.EaseIn:
        return 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
      case MoveType.SmoothStep:
        return t * t * (3 - 2 * t);
      case MoveType.SmootherStep:
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    return t;
  }
}
