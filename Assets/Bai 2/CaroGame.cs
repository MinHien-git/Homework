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
    private string xAlgorithm = "minmax"; // Thuật toán cho X
    private string oAlgorithm = "priority"; // Thuật toán cho O
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
        oAlgorithm = algorithm; // Chỉ áp dụng cho O trong humanVsMachine
        statusText.text = $"AI Algorithm for O: {algorithm}";
    }

    void StartGame(string mode)
    {
        gameMode = mode;
        if (gameMode == "machineVsMachine")
        {
            xAlgorithm = "minmax";
            oAlgorithm = "priority";
            statusText.text = "Machine X (MinMax) vs Machine O (Priority)";
        }
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
                    : "Machine X (MinMax) is thinking...";

        if (gameMode == "machineVsMachine")
        {
            // Đặt nước đi đầu tiên ở trung tâm nếu ô trống
            if (boardState[5, 5] == null)
            {
                MakeMove(5, 5, "X");
                currentPlayer = "O";
                statusText.text = "Machine O (Priority) is thinking...";
            }
            StartCoroutine(MachineMove(currentPlayer));
        }
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
                if (gameMode == "humanVsHuman")
                {
                    statusText.text = currentPlayer == "X" ? "Player X's turn" : "Player O's turn";
                }
                else if (gameMode == "humanVsMachine" && currentPlayer == "O")
                {
                    statusText.text = "AI is thinking...";
                    StartCoroutine(MachineMove("O"));
                }
                else
                {
                    statusText.text = "Your turn (X)";
                }
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
            if (gameMode == "humanVsHuman")
            {
                statusText.text = (player == "X" ? "Player X" : "Player O") + " wins!";
            }
            else if (gameMode == "machineVsMachine")
            {
                statusText.text = (player == "X" ? "Machine X (MinMax)" : "Machine O (Priority)") + " wins!";
            }
            else
            {
                statusText.text = (player == "X" ? "Player X" : "AI (O)") + " wins!";
            }
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

    Vector2Int? move = null;
    if (player == "X")
    {
        yield return StartCoroutine(GetBestMove_MinMax_Coroutine(player, m => move = m));
    }
    else
    {
        yield return StartCoroutine(GetBestMove_Priority_Coroutine(player, m => move = m));
    }

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
    else
    {
        // Nếu không tìm thấy nước đi, kết thúc trò chơi (trường hợp hiếm)
        statusText.text = "No valid moves left! Game Over.";
        gameOver = true;
    }
}

   IEnumerator GetBestMove_MinMax_Coroutine(string player, System.Action<Vector2Int?> callback)
{
    string enemy = player == "X" ? "O" : "X";

    // BƯỚC 1: Nếu đối thủ sắp thắng (dãy 4), chặn ngay
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
                    callback(new Vector2Int(i, j)); // Chặn lại
                    yield break;
                }
                boardState[i, j] = null;
            }
            yield return null;
        }
    }
    // BƯỚC 1.5: Nếu đối thủ sắp tạo threat mạnh (3 hoặc 4 không bị chặn) thì chặn luôn
    var (threatMove, threatLevel) = FindThreat(enemy, true);
    if (threatMove.HasValue && threatLevel >= 1)
    {
        callback(threatMove);
        yield break;
    }
    // BƯỚC 2: Lọc các ô gần X
    int bestScore = int.MinValue;
    Vector2Int? bestMove = null;

    List<Vector2Int> candidateMoves = new List<Vector2Int>();
    for (int i = 0; i < 10; i++)
    {
        for (int j = 0; j < 10; j++)
        {
            if (boardState[i, j] == player)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ni = i + dx, nj = j + dy;
                        if (InBounds(ni, nj) && boardState[ni, nj] == null)
                        {
                            Vector2Int pos = new Vector2Int(ni, nj);
                            if (!candidateMoves.Contains(pos))
                                candidateMoves.Add(pos);
                        }
                    }
                }
            }
        }
    }

    if (candidateMoves.Count == 0)
    {
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                if (boardState[i, j] == null)
                    candidateMoves.Add(new Vector2Int(i, j));
    }

    // BƯỚC 3: Chạy Minimax trên các ô khả thi
    foreach (var move in candidateMoves)
    {
        int i = move.x;
        int j = move.y;

        boardState[i, j] = player;
        int score = Minimax(0, false, int.MinValue, int.MaxValue, player == "O" ? "X" : "O");
        boardState[i, j] = null;

        if (score > bestScore)
        {
            bestScore = score;
            bestMove = move;
        }

        yield return null;
    }

    callback(bestMove);
}


    int Minimax(int depth, bool isMax, int alpha, int beta, string player)
{
    if (CheckWin("O", out _))
        return 10 - depth;
    if (CheckWin("X", out _))
        return depth - 10;
    if (IsBoardFull())
        return 0;
    if (depth >= 2) // Giảm độ sâu về 2
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

  IEnumerator GetBestMove_Priority_Coroutine(string player, System.Action<Vector2Int?> callback)
{
    string enemy = player == "O" ? "X" : "O";

    // 1. Kiểm tra nước đi thắng ngay lập tức cho đối thủ (để chặn)
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
                    callback(new Vector2Int(i, j));
                    yield break;
                }
                boardState[i, j] = null;
            }
            yield return null;
        }
    }

    // 2. Kiểm tra nước đi thắng ngay lập tức cho bản thân
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
                    callback(new Vector2Int(i, j));
                    yield break;
                }
                boardState[i, j] = null;
            }
            yield return null;
        }
    }

    // 3. Kiểm tra mối đe dọa từ đối thủ (dãy 4, dãy 3 không bị chặn)
    var (blockMove, blockThreat) = FindThreat(player, false);
    if (blockMove.HasValue && blockThreat > 0)
    {
        callback(blockMove);
        yield break;
    }

    // 4. Kiểm tra cơ hội tạo dãy 4 hoặc 3 không bị chặn cho bản thân
    var (winMove, winThreat) = FindThreat(player, true);
    if (winMove.HasValue && winThreat > 0)
    {
        callback(winMove);
        yield break;
    }

    // 5. Ưu tiên ô trung tâm
    if (boardState[5, 5] == null)
    {
        callback(new Vector2Int(5, 5));
        yield break;
    }

    // 6. Ưu tiên ô gần nước đi hiện tại của bản thân
    List<Vector2Int> emptyNearOwn = new List<Vector2Int>();
    for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
            if (boardState[i, j] == player)
                for (int di = -1; di <= 1; di++)
                    for (int dj = -1; dj <= 1; dj++)
                        if (InBounds(i + di, j + dj) && boardState[i + di, j + dj] == null)
                            emptyNearOwn.Add(new Vector2Int(i + di, j + dj));

    if (emptyNearOwn.Count > 0)
    {
        callback(emptyNearOwn[Random.Range(0, emptyNearOwn.Count)]);
        yield break;
    }

    // 7. Nếu không, chọn ngẫu nhiên từ tất cả ô trống
    List<Vector2Int> empty = new List<Vector2Int>();
    for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
            if (boardState[i, j] == null)
                empty.Add(new Vector2Int(i, j));

    callback(empty.Count > 0 ? empty[Random.Range(0, empty.Count)] : null);
}

    private (Vector2Int? move, int threatLevel) FindThreat(string player, bool forWin)
    {
        string target = forWin ? player : (player == "O" ? "X" : "O");
        int[] dx = { 1, 0, 1, 1 };
        int[] dy = { 0, 1, 1, -1 };
        Vector2Int? bestMove = null;
        int highestThreat = 0;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (boardState[i, j] != null) continue;

                boardState[i, j] = target;
                for (int dir = 0; dir < 4; dir++)
                {
                    int count = 1, block = 0;
                    int nx = i + dx[dir], ny = j + dy[dir];
                    while (InBounds(nx, ny) && boardState[nx, ny] == target)
                    {
                        count++;
                        nx += dx[dir];
                        ny += dy[dir];
                    }
                    if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                    nx = i - dx[dir];
                    ny = j - dy[dir];
                    while (InBounds(nx, ny) && boardState[nx, ny] == target)
                    {
                        count++;
                        nx -= dx[dir];
                        ny -= dy[dir];
                    }
                    if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                    int threatLevel = 0;
                    if (count == 4 && block == 0) threatLevel = 3; // Dãy 4 không bị chặn
                    else if (count == 4 && block == 1) threatLevel = 2; // Dãy 4 bị chặn 1 đầu
                    else if (count == 3 && block == 0) threatLevel = 1; // Dãy 3 không bị chặn

                    if (threatLevel > highestThreat)
                    {
                        highestThreat = threatLevel;
                        bestMove = new Vector2Int(i, j);
                    }
                }
                boardState[i, j] = null;
            }
        }
        return (bestMove, highestThreat);
    }
    int EvaluateBoard()
    {
        int scoreO = EvaluateFor("O");
        int scoreX = EvaluateFor("X");

        // Nếu X (đối thủ) có dãy 4 không bị chặn → cực kỳ nguy hiểm → giảm điểm
        int dangerX = DetectThreats("X");
        int dangerO = DetectThreats("O");

        return (scoreO - scoreX) - dangerX * 10 + dangerO * 5;
    }

    int DetectThreats(string player)
{
    int threatScore = 0;
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
                int count = 1, block = 0;
                int nx = x + dx[dir], ny = y + dy[dir];
                while (InBounds(nx, ny) && boardState[nx, ny] == player)
                {
                    count++;
                    nx += dx[dir];
                    ny += dy[dir];
                }
                if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                nx = x - dx[dir];
                ny = y - dy[dir];
                while (InBounds(nx, ny) && boardState[nx, ny] == player)
                {
                    count++;
                    nx -= dx[dir];
                    ny -= dy[dir];
                }
                if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                if (count == 4 && block == 1)
                    threatScore += 3; // Dãy 4 bị chặn 1 đầu
                else if (count == 3 && block == 0)
                    threatScore += 2; // Dãy 3 không bị chặn
                else if (count == 2 && block == 0)
                    threatScore += 1; // Dãy 2 không bị chặn
            }
        }
    }
    return threatScore;
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
                int count = 1, block = 0;
                int nx = x + dx[dir], ny = y + dy[dir];
                while (InBounds(nx, ny) && boardState[nx, ny] == player)
                {
                    count++;
                    nx += dx[dir];
                    ny += dy[dir];
                }
                if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                nx = x - dx[dir];
                ny = y - dy[dir];
                while (InBounds(nx, ny) && boardState[nx, ny] == player)
                {
                    count++;
                    nx -= dx[dir];
                    ny -= dy[dir];
                }
                if (!InBounds(nx, ny) || boardState[nx, ny] != null) block++;

                if (count >= 5)
                    score += 100000;
                else if (count == 4 && block == 0)
                    score += 10000;
                else if (count == 3 && block == 0) // Tăng điểm cho dãy 3 không bị chặn
                    score += 5000;
                else if (count == 3 && block == 1)
                    score += 1000;
                else if (count == 2 && block <= 1)
                    score += 100;
            }
        }
    }
    // Thêm điểm thưởng cho ô trung tâm
    for (int i = 0; i < 10; i++)
        for (int j = 0; j < 10; j++)
            if (boardState[i, j] == player)
                score += 10 - Mathf.Min(Mathf.Abs(i - 5), 5) - Mathf.Min(Mathf.Abs(j - 5), 5);
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
