using UnityEngine;

public class SpawnTestEnemy : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject enemyPrefab;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            var clone = Instantiate(enemyPrefab);
            clone.transform.position = spawnPoint.position;
        }
    }
}
