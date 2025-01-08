using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerateRooms))]
public class GenerateRoomsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector

        GenerateRooms levelGenerator = (GenerateRooms)target;

        // Add a button to the inspector
        if (Application.isPlaying ) 
        {
            if (GUILayout.Button("Generate Level"))
            {
                //remove all rooms
                foreach (GameObject g in levelGenerator.generatedRooms)
                {
                    Destroy(g);
                }
                levelGenerator.generatedRooms = new List<GameObject>();

                levelGenerator.GenerateLevel();
            }
        } 
    }
}

