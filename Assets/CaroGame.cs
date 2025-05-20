using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaroGame : MonoBehaviour
{
    public GameObject buttonPrefab;
    private Button[,] board = new Button[10, 10];
    private string[,] boardState = new string[10, 10];
    private string currentPlayer = "X";
    private string gameMode = "";
    private string aiAlgorithm = "priority";
    private bool gameOver = false;

    public TextMeshProUGUI statusText;
    public Button humanVsHumanButton,
        humanVsMachineButton,
        machineVsMachineButton,
        minmaxButton,
        priorityButton,
        restartButton;
    public Canvas canvas;
    private float cellSize = 50f;

    void Start()
    {
        if (
            !buttonPrefab
            || !canvas
            || !statusText
            || !humanVsHumanButton
            || !humanVsMachineButton
            || !machineVsMachineButton
            || !minmaxButton
            || !priorityButton
            || !restartButton
        )
        {
            Debug.LogError("Inspector missing assignments");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                GameObject buttonObj = Instantiate(buttonPrefab, canvas.transform);
                Button button = buttonObj.GetComponent<Button>();
                button.transform.localPosition = new Vector3(
                    j * cellSize - 225,
                    -i * cellSize + 225,
                    0
                );
                button.name = $"Cell_{i}_{j}";
                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.text = "";
                int row = i,
                    col = j;
                button.onClick.AddListener(() => OnCellClick(row, col));
                board[i, j] = button;
                boardState[i, j] = null;
            }
        }

        humanVsHumanButton.onClick.AddListener(() => StartGame("humanVsHuman"));
        humanVsMachineButton.onClick.AddListener(() => StartGame("humanVsMachine"));
        machineVsMachineButton.onClick.AddListener(() => StartGame("machineVsMachine"));
        minmaxButton.onClick.AddListener(() => SetAlgorithm("minmax"));
        priorityButton.onClick.AddListener(() => SetAlgorithm("priority"));
        restartButton.onClick.AddListener(RestartGame);

        statusText.text = "Chọn chế độ chơi";
    }

    void SetAlgorithm(string algorithm)
    {
        aiAlgorithm = algorithm;
        statusText.text = $"Algorithm: {algorithm}";
    }

    void StartGame(string mode)
    {
        gameMode = mode;
        RestartGame();
    }

    void RestartGame()
    {
        for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
        {
            boardState[i, j] = null;
            var text = board[i, j].GetComponentInChildren<TextMeshProUGUI>();
            text.text = "";
            text.color = Color.black;
        }

        currentPlayer = "X";
        gameOver = false;
        statusText.text =
            gameMode == "humanVsHuman"
                ? "Player X's turn"
                : gameMode == "humanVsMachine"
                    ? "Your turn (X)"
                    : "Machine vs Machine";

        if (gameMode == "machineVsMachine")
            StartCoroutine(MachineMove("X"));
    }

    void OnCellClick(int i, int j)
    {
        if (gameOver || boardState[i, j] != null)
            return;

        if (gameMode == "humanVsHuman" || (gameMode == "humanVsMachine" && currentPlayer == "X"))
        {
            MakeMove(i, j, currentPlayer);
            if (!gameOver)
            {
                currentPlayer = currentPlayer == "X" ? "O" : "X";
                statusText.text = currentPlayer == "X" ? "Player X's turn" : "AI is thinking...";
                if (gameMode == "humanVsMachine" && currentPlayer == "O")
                    StartCoroutine(MachineMove("O"));
            }
        }
    }

    void MakeMove(int i, int j, string player)
    {
        boardState[i, j] = player;
        var text = board[i, j].GetComponentInChildren<TextMeshProUGUI>();
        text.text = player;
        text.color = Color.black;

        if (CheckWin(player, out var winCells))
        {
            foreach (var cell in winCells)
                board[cell.x, cell.y].GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
            statusText.text = (player == "X" ? "Player X" : "AI (O)") + " wins!";
            gameOver = true;
        }
        else if (IsBoardFull())
        {
            statusText.text = "Draw!";
            gameOver = true;
        }
    }

    IEnumerator MachineMove(string player)
    {
        yield return new WaitForSeconds(0.5f);
        if (gameOver)
            yield break;

        Vector2Int? move =
            aiAlgorithm == "minmax" ? GetBestMove_MinMax(player) : GetBestMove_Priority(player);
        if (move.HasValue)
        {
            MakeMove(move.Value.x, move.Value.y, player);
            if (!gameOver)
            {
                currentPlayer = player == "X" ? "O" : "X";
                statusText.text =
                    gameMode == "machineVsMachine"
                        ? $"Machine {currentPlayer} is thinking..."
                        : currentPlayer == "X"
                            ? "Your turn (X)"
                            : "AI is thinking...";

                if (gameMode == "machineVsMachine")
                    StartCoroutine(MachineMove(currentPlayer));
            }
        }
    }

    Vector2Int? GetBestMove_MinMax(string player)
    {
        int bestScore = int.MinValue;
        Vector2Int? bestMove = null;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (boardState[i, j] == null)
                {
                    boardState[i, j] = player;
                    int score = Minimax(
                        0,
                        false,
                        int.MinValue,
                        int.MaxValue,
                        player == "O" ? "X" : "O"
                    );
                    boardState[i, j] = null;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = new Vector2Int(i, j);
                    }
                }
            }
        }
        return bestMove;
    }

    int Minimax(int depth, bool isMax, int alpha, int beta, string player)
    {
        if (CheckWin("O", out _))
            return 10 - depth;
        if (CheckWin("X", out _))
            return depth - 10;
        if (IsBoardFull())
            return 0;
        if (depth >= 2)
            return EvaluateBoard();

        int best = isMax ? int.MinValue : int.MaxValue;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (boardState[i, j] == null)
                {
                    boardState[i, j] = player;
                    int score = Minimax(depth + 1, !isMax, alpha, beta, player == "O" ? "X" : "O");
                    boardState[i, j] = null;

                    if (isMax)
                    {
                        best = Mathf.Max(best, score);
                        alpha = Mathf.Max(alpha, best);
                    }
                    else
                    {
                        best = Mathf.Min(best, score);
                        beta = Mathf.Min(beta, best);
                    }

                    if (beta <= alpha)
                        break;
                }
            }
        }
        return best;
    }

    Vector2Int? GetBestMove_Priority(string player)
    {
        string enemy = player == "O" ? "X" : "O";

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (boardState[i, j] == null)
                {
                    boardState[i, j] = enemy;
                    if (CheckWin(enemy, out _))
                    {
                        boardState[i, j] = null;
                        return new Vector2Int(i, j);
                    }
                    boardState[i, j] = null;
                }
            }
        }

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (boardState[i, j] == null)
                {
                    boardState[i, j] = player;
                    if (CheckWin(player, out _))
                    {
                        boardState[i, j] = null;
                        return new Vector2Int(i, j);
                    }
                    boardState[i, j] = null;
                }
            }
        }

        if (boardState[5, 5] == null)
            return new Vector2Int(5, 5);

        List<Vector2Int> empty = new List<Vector2Int>();
        for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
            if (boardState[i, j] == null)
                empty.Add(new Vector2Int(i, j));

        return empty.Count > 0 ? empty[Random.Range(0, empty.Count)] : null;
    }

    int EvaluateBoard()
    {
        return EvaluateFor("O") - EvaluateFor("X");
    }

    int EvaluateFor(string player)
    {
        int score = 0;
        int[] dx = { 1, 0, 1, 1 };
        int[] dy = { 0, 1, 1, -1 };

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (boardState[x, y] != player)
                    continue;

                for (int dir = 0; dir < 4; dir++)
                {
                    int count = 1,
                        block = 0;
                    int nx = x + dx[dir],
                        ny = y + dy[dir];

                    while (InBounds(nx, ny) && boardState[nx, ny] == player)
                    {
                        count++;
                        nx += dx[dir];
                        ny += dy[dir];
                    }
                    if (!InBounds(nx, ny) || boardState[nx, ny] != null)
                        block++;

                    nx = x - dx[dir];
                    ny = y - dy[dir];
                    while (InBounds(nx, ny) && boardState[nx, ny] == player)
                    {
                        count++;
                        nx -= dx[dir];
                        ny -= dy[dir];
                    }
                    if (!InBounds(nx, ny) || boardState[nx, ny] != null)
                        block++;

                    if (count >= 5)
                        score += 100000;
                    else if (count == 4 && block == 0)
                        score += 10000;
                    else if (count == 3 && block <= 1)
                        score += 1000;
                    else if (count == 2 && block <= 1)
                        score += 100;
                }
            }
        }
        return score;
    }

    bool InBounds(int i, int j) => i >= 0 && i < 10 && j >= 0 && j < 10;

    bool IsBoardFull()
    {
        for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
            if (boardState[i, j] == null)
                return false;
        return true;
    }

    bool CheckWin(string player, out List<Vector2Int> winningCells)
    {
        winningCells = new List<Vector2Int>();
        int[] dx = { 1, 0, 1, 1 };
        int[] dy = { 0, 1, 1, -1 };

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                foreach (var dir in System.Linq.Enumerable.Range(0, 4))
                {
                    List<Vector2Int> line = new List<Vector2Int>();
                    for (int k = 0; k < 5; k++)
                    {
                        int ni = i + dx[dir] * k;
                        int nj = j + dy[dir] * k;
                        if (InBounds(ni, nj) && boardState[ni, nj] == player)
                            line.Add(new Vector2Int(ni, nj));
                        else
                            break;
                    }
                    if (line.Count == 5)
                    {
                        winningCells = line;
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
