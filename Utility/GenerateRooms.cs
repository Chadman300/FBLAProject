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

    [Header("Generation Walls")]
    public GameObject doorPrefab;
    public GameObject wallPrefab;
    public float doorPositionOffset = 10;
    public Vector3 doorCenteredOffset = new Vector3(0, 0, -8);

    [Header("Generation Bounds")]
    public Vector3 generationStartPosition = Vector3.zero; // Start point of the level
    public float positionIncrementRange = 30f; // Random offsets

    [HideInInspector] public List<GameObject> generatedRooms = new List<GameObject>();
    [HideInInspector] public List<GameObject> generatedDoors = new List<GameObject>();

    private Vector3 oldDirection = Vector3.zero;
    private RoomController selectedController;

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

            //get rooms controller
            if (!selectedPrefab.TryGetComponent<RoomController>(out selectedController))
                Debug.LogError($"Room Prefab: {selectedPrefab}, does not have a RoomControllerScript!");

            // Randomly increment position
            Vector3 randomDirection = Vector3.zero;
            var random = Random.Range(0, 4);

            if (random == 0)
                randomDirection = transform.forward;
            else if (random == 1)
                randomDirection = -transform.forward;
            else if (random == 2)
                randomDirection = transform.right;
            else if (random == 3)
                randomDirection = -transform.right;

            oldDirection = randomDirection;
            Vector3 randomIncrement = randomDirection * selectedController.roomIncrementSize;

            Vector3 proposedPosition = currentPosition + randomIncrement;

            // Check for collisions
            if (!IsColliding(proposedPosition, selectedPrefab))
            {
                // Instantiate and position the room
                GameObject newRoom = Instantiate(selectedPrefab, proposedPosition, Quaternion.identity);
                newRoom.transform.parent = transform; // Optional: Keep hierarchy clean
                generatedRooms.Add(newRoom);

                //get instanciated rooms controller again
                selectedController = newRoom.GetComponent<RoomController>();

                // Update current position for the next room
                currentPosition = proposedPosition;

                //Add doors and fill walls
                foreach (Transform point in selectedController.doorPoints)
                {
                    GameObject spawnedWall = null;

                    //spawn wall or door
                    if(-randomDirection == point.forward)
                    {
                        spawnedWall = Instantiate(selectedController.doorPrefab);
                    }
                    else
                    {
                        //spawnedWall = Instantiate(selectedController.wallPrefab);
                    }


                    //set vars
                    spawnedWall.transform.position = point.position;
                    spawnedWall.transform.rotation = point.rotation;
                    spawnedWall.transform.parent = newRoom.transform;
                }
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
