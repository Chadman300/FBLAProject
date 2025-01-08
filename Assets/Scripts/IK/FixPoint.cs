using UnityEngine;

public class FixPoint : MonoBehaviour
{
    [SerializeField] private Transform point;
    // Update is called once per frame
    void Update()
    {
        transform.position = point.position;
    }
}
