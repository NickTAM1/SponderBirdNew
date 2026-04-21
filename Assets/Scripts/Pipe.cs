using UnityEngine;

public class Pipe : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How fast the pipe moves left (units per second)")]
    [SerializeField] private float scrollSpeed = 4f;

    [Header("Gap Settings")]
    [Tooltip("Half the height of the gap between top and bottom pipes")]
    [SerializeField] private float gapHalfHeight = 1.8f;

    [Header("References")]
    [SerializeField] private Transform topPipe;
    [SerializeField] private Transform bottomPipe;

    private bool isScrolling = false;
    private const float destroyX = -12f;

    private void Start()
    {
        float randomOffset = Random.Range(-2.5f, 2.5f);
        SetGapPosition(randomOffset);
    }

    private void SetGapPosition(float centerY)
    {
        if (topPipe != null)
        {
            topPipe.localPosition = new Vector3(0f, centerY + gapHalfHeight + topPipe.localScale.y * 0.5f, 0f);
        }
        if (bottomPipe != null)
        {
            bottomPipe.localPosition = new Vector3(0f, centerY - gapHalfHeight - bottomPipe.localScale.y * 0.5f, 0f);
        }
    }

    public void SetScrolling(bool scroll)
    {
        isScrolling = scroll;
    }

    private void Update()
    {
        if (!isScrolling) return;

        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}
