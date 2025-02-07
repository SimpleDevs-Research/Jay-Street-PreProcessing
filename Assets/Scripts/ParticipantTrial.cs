using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ParticipantTrial", menuName = "ScriptableObjects/ParticipantTrial", order = 1)]
public class ParticipantTrial : ScriptableObject
{
    public string trial_name;
    public string output_dir;
    [Space]
    public TextAsset trial_file;
    public int trial_num_cols;
    [Space]
    public TextAsset user_file;
    public int user_num_cols;
    public int user_timestamp_col = 0;
    public Vector3Int user_pos_cols = new Vector3Int(1,2,3);
    public Vector3Int user_forward_cols = new Vector3Int(4,5,6);
    public Vector3Int user_velocity_cols = new Vector3Int(7,8,9);
    [Space]
    public TextAsset pedestrian_file;
    public int pedestrian_num_cols;
    public int pedestrian_timestamp_col = 0;
    public int pedestrian_guiID_col = 1;
    public Vector2Int pedestrian_pos_cols = new Vector2Int(2,3);
    public Vector3Int pedestrian_forward_cols = new Vector3Int(4,5,6);
    public Vector2Int pedestrian_velocity_cols = new Vector2Int(7,8);
}