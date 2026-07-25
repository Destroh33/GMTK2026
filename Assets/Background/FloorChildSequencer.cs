using UnityEngine;

public class FloorChildSequencer : MonoBehaviour
{
    [SerializeField] private GameObject[] floorChildren;

    void Awake()
    {
        if (floorChildren == null || floorChildren.Length == 0)
        {
            floorChildren = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                floorChildren[i] = transform.GetChild(i).gameObject;
        }

        SetActiveChild(-1);
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted += HandleFloorStarted;
            GameManager.Instance.OnRunReset += HandleRunReset;

            if (GameManager.Instance.CurrentFloorIndex >= 0)
                SetActiveChild(GameManager.Instance.CurrentFloorIndex);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted -= HandleFloorStarted;
            GameManager.Instance.OnRunReset -= HandleRunReset;
        }
    }

    void HandleFloorStarted(int floorIndex) => SetActiveChild(floorIndex);

    void HandleRunReset() => SetActiveChild(-1);

    void SetActiveChild(int index)
    {
        // Default to showing the first child whenever there's no active floor index yet.
        int activeIndex = index >= 0 ? index : 0;

        for (int i = 0; i < floorChildren.Length; i++)
        {
            if (floorChildren[i] != null)
                floorChildren[i].SetActive(i == activeIndex);
        }
    }
}
