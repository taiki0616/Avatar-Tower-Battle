using UnityEngine;

public class LoseChecker : MonoBehaviour
{
    void Update()
    {
        if (transform.position.y < -5f)
        {
            GameManager.Instance.GameOver(GameManager.Instance.currentPlayer);
            Destroy(this);
        }
    }
}