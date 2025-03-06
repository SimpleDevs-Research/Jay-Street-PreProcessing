using UnityEngine;
using System;

[CreateAssetMenu(fileName = "PedestrianTrial", menuName = "ScriptableObjects/PedestrianTrial", order = 2)]
public class PedestrianTrial : ScriptableObject
{
    [Header("=== Details ===")]
    public string trial_name;
    public TextAsset pedestrian_file;

    [Space]
    [Header("=== Pedestrians ===")]    
    public int num_cols = 8;
    public int timestamp_col = 0;
    public int frame_col = 1;
    public int guid_col = 2;
    public Vector2Int position_cols = new Vector2Int(3,4);
    public Vector2Int forward_cols = new Vector2Int(5,6);
    public int active_col = 7;

    [Space]
    [Header("=== Outputs ===")]
    [Tooltip("The directory where the file should be saved. Should auto-create any subdirectories automatically")]
    public string output_dir;
    [Tooltip("The output filename, sans extension")] 
    public string output_filename = "pedestrians_aligned";

}