using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    public GridManager gridManager;
    public float moveSpeed = 5f;
    private List<GridManager.Node> path = new List<GridManager.Node>();
    private Vector3 targetPosition;
    private bool isInitialized = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        if (!isInitialized)
        {
            if (gridManager != null)
            {
                isInitialized = true;
                Debug.Log("NPCController initialized with GridManager");
            }
            else
            {
                Debug.LogWarning("Waiting for GridManager to be assigned...");
                return;
            }
        }

        // Xử lý click chuột
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GridManager.Node targetNode = gridManager.GetNodeAtPosition(hit.point);
                if (targetNode != null && targetNode.isWalkable)
                {
                    targetPosition = targetNode.position;
                    FindPath(transform.position, targetPosition);
                }
            }
        }

        FollowPath();
    }

    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        GridManager.Node startNode = gridManager.GetNodeAtPosition(startPos);
        GridManager.Node targetNode = gridManager.GetNodeAtPosition(targetPos);

        if (startNode == null || targetNode == null || !targetNode.isWalkable) return;

        List<GridManager.Node> openSet = new List<GridManager.Node> { startNode };
        HashSet<GridManager.Node> closedSet = new HashSet<GridManager.Node>();
        startNode.gCost = 0;
        startNode.hCost = Vector3.Distance(startNode.position, targetNode.position);
        startNode.fCost = startNode.hCost;

        while (openSet.Count > 0)
        {
            GridManager.Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (GridManager.Node neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor)) continue;

                float newGCost = currentNode.gCost + Vector3.Distance(currentNode.position, neighbor.position);
                if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = Vector3.Distance(neighbor.position, targetNode.position);
                    neighbor.fCost = neighbor.gCost + neighbor.hCost;
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
    }

    void RetracePath(GridManager.Node startNode, GridManager.Node endNode)
    {
        path.Clear();
        GridManager.Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
    }

    void FollowPath()
    {
        if (path.Count > 0)
        {
            Vector3 targetPos = path[0].position + new Vector3(0, 0.5f, 0); // Điều chỉnh độ cao
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                path.RemoveAt(0);
        }
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i].position + new Vector3(0, 0.5f, 0), path[i + 1].position + new Vector3(0, 0.5f, 0));
            }
        }
    }
}