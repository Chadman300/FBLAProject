using UnityEngine;

public class ProceduralTarget : MonoBehaviour
{
    [Header("Ray")]
    [SerializeField] private Transform origin;
    [SerializeField] private float maxRayDis = 10.0f;
    [SerializeField] private LayerMask inclusionLayers;

    void Update()
    {
        Vector3 rayDirection = Vector3.down;
        RaycastHit hit;

        // Send the ray
        if (Physics.Raycast(origin.position, rayDirection, out hit, maxRayDis, inclusionLayers))
        {
            //gameObject.transform.position = new Vector3(gameObject.transform.position.x, hit.point.y, gameObject.transform.position.z);
            gameObject.transform.position = hit.point;
        }
        else
        {
            //didnt hit so just set it to where it ended
            //gameObject.transform.position = new Vector3(gameObject.transform.position.x, origin.position.y + (Vector3.down.y * maxRayDis), gameObject.transform.position.z); 
        }
    }
}
