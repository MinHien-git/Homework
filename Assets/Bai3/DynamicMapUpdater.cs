using UnityEngine;

public class DynamicMapUpdater : MonoBehaviour
{
    public GridManager gridManager;
    public float updateInterval = 5f; // Cập nhật mỗi 5 giây
    private float timer = 0;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            gridManager.UpdateGrid();
            timer = 0;
        }
    }
}