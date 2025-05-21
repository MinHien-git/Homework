using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public GameObject walkablePrefab; // Prefab cho ô đi được
    public GameObject obstaclePrefab; // Prefab cho chướng ngại vật
    public GameObject npcPrefab; // Prefab cho NPC
    public GameObject enemyPrefab; // Prefab cho Enemy
    public float maxObstaclePercentage = 0.2f; // Tối đa 20% ô là obstacle
    private Node[,,] grid;
    private GameObject npc; // Tham chiếu đến NPC
    private GameObject enemy; // Tham chiếu đến Enemy

    public class Node
    {
        public Vector3 position;
        public bool isWalkable;
        public Node parent;
        public float gCost, hCost, fCost;

        public Node(Vector3 pos, bool walkable)
        {
            position = pos;
            isWalkable = walkable;
        }
    }

    void Start()
    {
        CreateGrid();
        SpawnNPCAndEnemy();
    }

    void CreateGrid()
    {
        grid = new Node[gridWidth, gridHeight, 1]; // Grid 2D trên mặt phẳng Z=0
        float offsetX = gridWidth * cellSize / 2f; // Offset để căn giữa
        float offsetZ = gridHeight * cellSize / 2f;
        int totalCells = gridWidth * gridHeight;
        int maxObstacles = Mathf.FloorToInt(totalCells * maxObstaclePercentage); // Số obstacle tối đa
        int obstacleCount = 0;

        // Danh sách các vị trí để phân bố obstacle ngẫu nhiên
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                availableCells.Add(new Vector2Int(x, y));
            }
        }

        // Khởi tạo grid với tất cả ô là walkable
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = new Vector3(
                    (x - gridWidth / 2f + 0.5f) * cellSize,
                    0.1f,
                    (y - gridHeight / 2f + 0.5f) * cellSize
                );
                grid[x, y, 0] = new Node(worldPos, true); // Ban đầu tất cả là walkable
            }
        }

        // Đặt ngẫu nhiên obstacle trong giới hạn maxObstacles
        while (obstacleCount < maxObstacles && availableCells.Count > 2) // Đảm bảo còn ít nhất 2 ô walkable
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int cell = availableCells[randomIndex];
            grid[cell.x, cell.y, 0].isWalkable = false;
            obstacleCount++;
            availableCells.RemoveAt(randomIndex);
        }

        // Tạo GameObject trực quan
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject prefab = grid[x, y, 0].isWalkable ? walkablePrefab : obstaclePrefab;
                Instantiate(prefab, grid[x, y, 0].position, Quaternion.identity);
            }
        }
    }

    void SpawnNPCAndEnemy()
    {
        // Tìm tất cả các ô walkable
        List<Node> walkableNodes = new List<Node>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y, 0].isWalkable)
                    walkableNodes.Add(grid[x, y, 0]);
            }
        }

        if (walkableNodes.Count < 2)
        {
            Debug.LogError("Không đủ ô walkable để spawn NPC và Enemy!");
            return;
        }

        // Spawn NPC
        int npcIndex = Random.Range(0, walkableNodes.Count);
        Node npcNode = walkableNodes[npcIndex];
        Vector3 npcPos = npcNode.position + new Vector3(0, 0.5f, 0);
        npc = Instantiate(npcPrefab, npcPos, Quaternion.identity);
        NPCController npcController = npc.AddComponent<NPCController>();
        npcController.gridManager = this;

        // Xóa ô đã chọn để tránh spawn Enemy trùng
        walkableNodes.RemoveAt(npcIndex);

        // Spawn Enemy
        int enemyIndex = Random.Range(0, walkableNodes.Count);
        Node enemyNode = walkableNodes[enemyIndex];
        Vector3 enemyPos = enemyNode.position + new Vector3(0, 0.5f, 0);
        enemy = Instantiate(enemyPrefab, enemyPos, Quaternion.identity);
        EnemyController enemyController = enemy.AddComponent<EnemyController>();
        enemyController.gridManager = this;
        enemyController.npc = npc.transform;
    }

    public Node GetNodeAtPosition(Vector3 pos)
    {
        float offsetX = gridWidth * cellSize / 2f;
        float offsetZ = gridHeight * cellSize / 2f;
        int x = Mathf.FloorToInt((pos.x + offsetX) / cellSize);
        int y = Mathf.FloorToInt((pos.z + offsetZ) / cellSize);
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return grid[x, y, 0];
        return null;
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        float offsetX = gridWidth * cellSize / 2f;
        float offsetZ = gridHeight * cellSize / 2f;
        int x = Mathf.FloorToInt((node.position.x + offsetX) / cellSize);
        int y = Mathf.FloorToInt((node.position.z + offsetZ) / cellSize);

        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        for (int i = 0; i < 4; i++)
        {
            int newX = x + directions[i, 0];
            int newY = y + directions[i, 1];
            if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
            {
                Node neighbor = grid[newX, newY, 0];
                if (neighbor.isWalkable)
                    neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    public void UpdateGrid()
    {
        int totalCells = gridWidth * gridHeight;
        int maxObstacles = Mathf.FloorToInt(totalCells * maxObstaclePercentage);
        int obstacleCount = 0;

        // Đếm số obstacle hiện tại
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!grid[x, y, 0].isWalkable)
                    obstacleCount++;
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 nodePos = grid[x, y, 0].position;
                if (Vector3.Distance(nodePos, npc.transform.position) < 0.5f ||
                    Vector3.Distance(nodePos, enemy.transform.position) < 0.5f)
                    continue;

                if (Random.value < 0.05f)
                {
                    bool wasWalkable = grid[x, y, 0].isWalkable;
                    grid[x, y, 0].isWalkable = !grid[x, y, 0].isWalkable;

                    if (!wasWalkable && grid[x, y, 0].isWalkable)
                        obstacleCount--; // Giảm obstacle
                    else if (wasWalkable && !grid[x, y, 0].isWalkable && obstacleCount < maxObstacles)
                        obstacleCount++; // Tăng obstacle nếu trong giới hạn
                    else
                        grid[x, y, 0].isWalkable = wasWalkable; // Hoàn tác nếu vượt giới hạn

                    GameObject[] objects = GameObject.FindGameObjectsWithTag(grid[x, y, 0].isWalkable ? "Obstacle" : "Walkable");
                    foreach (GameObject obj in objects)
                    {
                        if (Vector3.Distance(obj.transform.position, grid[x, y, 0].position) < 0.1f)
                            Destroy(obj);
                    }
                    Instantiate(grid[x, y, 0].isWalkable ? walkablePrefab : obstaclePrefab, grid[x, y, 0].position, Quaternion.identity);
                }
            }
        }
    }
}