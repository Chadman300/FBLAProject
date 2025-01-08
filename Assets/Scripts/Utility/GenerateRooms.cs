using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class GenerateRooms : MonoBehaviour
{
    [Header("Generation Settings")]
    public GameObject[] roomPrefabs; // Array of room prefabs to choose from
    public int numberOfRooms = 10;   // Total number of rooms to spawn
    public float roomSpacing = 10f; // Minimum spacing between room centers
    public LayerMask roomLayer;     // Layer for room collision checking

    [Header("Generation Bounds")]
    public Vector3 generationStartPosition = Vector3.zero; // Start point of the level
    public float positionIncrementRange = 30f; // Random offsets

    [HideInInspector] public List<GameObject> generatedRooms = new List<GameObject>();

    void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        Vector3 currentPosition = generationStartPosition;

        for (int i = 0; i < numberOfRooms; i++)
        {
            // Randomly choose a room prefab
            GameObject selectedPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

            // Randomly increment position
            Vector3 randomIncrement = Vector3.zero;
            var random = Random.Range(0, 4);

            if (random == 0)
                randomIncrement = new Vector3(positionIncrementRange, 0, 0);
            else if (random == 1)
                randomIncrement = new Vector3(-positionIncrementRange, 0, 0);
            else if (random == 2)
                randomIncrement = new Vector3(0, 0, positionIncrementRange);
            else if (random == 3)
                randomIncrement = new Vector3(0, 0, -positionIncrementRange);

            Vector3 proposedPosition = currentPosition + randomIncrement;

            // Check for collisions
            if (!IsColliding(proposedPosition, selectedPrefab))
            {
                // Instantiate and position the room
                GameObject newRoom = Instantiate(selectedPrefab, proposedPosition, Quaternion.identity);
                newRoom.transform.parent = transform; // Optional: Keep hierarchy clean
                generatedRooms.Add(newRoom);

                // Update current position for the next room
                currentPosition = proposedPosition;
            }
            else
            {
                // Skip this iteration if the room collides
                Debug.Log($"Room {i} skipped due to collision.");
                i--; // Retry the same room count
            }
        }

        Debug.Log($"Generated {generatedRooms.Count} rooms successfully.");
    }

    bool IsColliding(Vector3 position, GameObject prefab)
    {
        // Get the bounds of the prefab
        BoxCollider prefabCollider = prefab.GetComponent<BoxCollider>();
        if (!prefabCollider)
        {
            Debug.LogError($"{prefab.name} does not have a BoxCollider!");
            return true; // Avoid placing rooms without proper collision data
        }

        Vector3 halfExtents = prefabCollider.size / 2;

        // Perform an OverlapBox check
        Collider[] collisions = Physics.OverlapBox(position + prefabCollider.center, halfExtents, Quaternion.identity, roomLayer);

        return collisions.Length > 0;
    }
}
