using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int currentPlayer = 1;
    public bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void NextTurn()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        Debug.Log("Player " + currentPlayer + "'s turn");
    }

    public void GameOver(int loser)
    {
        isGameOver = true;
        Debug.Log("Player " + loser + " loses!");
    }
}