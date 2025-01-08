using Unity.VisualScripting;
using UnityEngine;

public class ObjectAlpha : MonoBehaviour
{
    [Header("Functional Parameters")]
    [SerializeField] private Transform playerTransform = null;
    [SerializeField] private float alphaDis = 5; //distance needed to make object transparent
    [SerializeField] private AnimationCurve alphaCurve;

    [Header("Objects")]
    [SerializeField] private GameObject[] objs = null;

    private Transform objTrans;

    private void Awake()
    {
        objTrans = GetComponent<Transform>();
        playerTransform = GameObject.FindWithTag("hips").transform;
    }

    void Update()
    {
        float distance = getDistance(objTrans, playerTransform);

        for (int i = 0; i < objs.Length; i++)
        {
            float curDis = getDistance(objs[i].transform, playerTransform);
            if (curDis < distance)
                distance = curDis;
        }

        var alphaPrecent = (distance / alphaDis) * 50;
        alphaPrecent = alphaCurve.Evaluate((distance / alphaDis));

        if (distance < alphaDis)
        {
            setMaterials(alphaPrecent);
        }
        else
        {
            setMaterials(100);
        }
    }

    private float getDistance(Transform t1, Transform t2)
    {
        return Mathf.Sqrt(
            ((t2.position.x - t1.position.x) * (t2.position.x - t1.position.x)) +
            ((t2.position.y - t1.position.y) * (t2.position.y - t1.position.y)) +
            ((t2.position.z - t1.position.z) * (t2.position.z - t1.position.z))
            );
    }

    private void setMaterials(float alpha)
    {
        for (int i = 0; i < objs.Length; i++)
        {
            MakeMaterialTransparent(objs[i], alpha);
        }
    }

    // Function to make the material of a GameObject transparent
    public static void MakeMaterialTransparent(GameObject obj, float alpha)
    {
        // Get the Renderer component
        Renderer renderer = obj.GetComponent<Renderer>();

        // Get the material of the object
        Material material = renderer.material;

        // Set the material's rendering mode to Transparent
        material.SetFloat("_Mode", 3); // 3 corresponds to Transparent mode
        material.EnableKeyword("_ALPHABLEND_ON");
        //material.renderQueue = 3000;

        // Adjust the color to make it partially transparent (alpha < 1.0)
        Color color = material.color;
        color.a = alpha / 100; // Set desired transparency level here (0.0 to 1.0)
        material.color = color;
    }
}
