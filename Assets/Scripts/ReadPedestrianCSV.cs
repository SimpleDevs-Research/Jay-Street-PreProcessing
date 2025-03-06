using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ReadPedestrianCSV : MonoBehaviour
{
    [System.Serializable]
    public class PedestrianPosition {
        public string _id;
        public float timestamp;
        public Vector2 position;
        public Vector2 forward;
        public int is_active;
        public PedestrianPosition(string _id, float timestamp, Vector2 position, Vector2 forward, int is_active) {
            this._id = _id;
            this.timestamp = timestamp;
            this.position = position;
            this.forward = forward;
            this.is_active = is_active;
        }
        public PedestrianPosition(float timestamp, PedestrianPosition toCopy, int is_active) {
            this.timestamp = timestamp;
            this.position = toCopy.position;
            this.forward = toCopy.forward;
            this.is_active = is_active;
        }
        public override string ToString() {
            return $"{timestamp}\t{position.ToString()}\t{forward.ToString()}";
        }
    }

    [System.Serializable]
    public class Pedestrian {
        public string _id;
        public List<PedestrianPosition> rawPositions;
        public List<float> rawTimestamps;
        public Dictionary<float, PedestrianPosition> positions;
        public Pedestrian(string _id, float raw_timestamp, PedestrianPosition rawPosition) {
            this._id = _id;
            this.rawPositions = new List<PedestrianPosition>();
            this.rawPositions.Add(rawPosition);
            this.rawTimestamps = new List<float>();
            this.rawTimestamps.Add(raw_timestamp);
        }
        public void AddRawPosition(float raw_timestamp, PedestrianPosition rawPosition) {
            if (this.rawTimestamps.Contains(raw_timestamp)) return; // prevent duplicates
            this.rawTimestamps.Add(raw_timestamp);
            this.rawPositions.Add(rawPosition);
        }
        public void CreateStateFromTimestamp(float timestamp) {
            // First, check if the timeestamp is less than the first raw timestamp or is bigger than the last raw timestamp.
            if (timestamp < rawPositions[0].timestamp) {
                PedestrianPosition earlyPosition = new PedestrianPosition(timestamp, rawPositions[0], 0);
                positions.Add(timestamp, earlyPosition);
                return;
            }
            if (timestamp > rawPositions[rawTimestamps.Count-1].timestamp) {
                PedestrianPosition latePosition = new PedestrianPosition(timestamp, rawPositions[rawPositions.Count-1], 0);
                positions.Add(timestamp, latePosition);
                return;
            }

            // Since we've guaranteed that the provided timestamp is within the range of raw timestamps, let's check where this timestamp resides
            for(int i = 0; i < rawPositions.Count-1; i++) {
                float start_timestamp = rawPositions[i].timestamp;
                float end_timestamp = rawPositions[i+1].timestamp;
                float time_range = end_timestamp - start_timestamp;
                if (start_timestamp <= timestamp && timestamp < end_timestamp) {
                    // The provided timestep is between this row and the future row. We can lerp from there.
                    float timestamp_diff = timestamp - start_timestamp;
                    float lerpFactor = timestamp_diff/time_range;

                    // Lerp positions and forward, ignoring delta time between these rows
                    Vector3 position = Vector3.Lerp(rawPositions[i].position, rawPositions[i+1].position, lerpFactor);
                    Vector3 forward = Vector3.Lerp(rawPositions[i].forward, rawPositions[i+1].forward, lerpFactor);

                    //  Contribute lerped position
                    PedestrianPosition newPosition = new PedestrianPosition(_id, timestamp, position, forward, 1);
                    positions.Add(timestamp, newPosition);
                    break;
                }
            }
        }
    }

    public PedestrianTrial pedestrianTrial;

    private List<PedestrianPosition> m_pedestrianPositions;
    private float m_latest_timestamp = 0f; // == duration;
    private Dictionary<string, Pedestrian> m_pedestrians;

    [SerializeField] private int m_outputFPS = 30;
    [SerializeField] private CSVWriter m_writer;

    /*
    [SerializeField] private Entity m_playerPrefab;
    [SerializeField] private Entity m_entityPrefab;
   
    [Space]

    [Header("Settings")]
    [SerializeField] private bool m_readOnStart = true;
    [SerializeField] private bool m_saveAlignedCSVs = true;
    [SerializeField] private bool m_replayOn = true;
    [SerializeField] private bool m_playReplay = true;
    [SerializeField] private bool m_playing = false;
    [SerializeField] private int m_replay_total_length;
    [SerializeField] private int m_replay_current_index;
    [SerializeField, Range(0f,1f)] private float m_replay_slider;
    [SerializeField] private float m_prevReplayTimestamp;
    [SerializeField] private float m_viewFieldDistance = 10f;
    [SerializeField] private TextMeshProUGUI m_millisecondsTextbox;
    */

    public static string[] ReadCSVFile(TextAsset ta, string colDivider = ",", string rowDivider = "\n") {
        return ta.text.Split(new string[] {colDivider, rowDivider}, StringSplitOptions.None);
    }

    public static int GetTableSize(string[] data, int numCols) {
        return data.Length/numCols - 1;
    }

    public void GetPedestrianPositions() {
        // User data file should be read
        string[] data = ReadCSVFile(pedestrianTrial.pedestrian_file);
        int numRows = GetTableSize(data, pedestrianTrial.num_cols);
        
        // Initialize our pedestrian positions raw data, and our list of pedestrians
        m_pedestrianPositions = new List<PedestrianPosition>();
        m_pedestrians = new Dictionary<string, Pedestrian>();

        // Iterate through the csv rows.
        for(int i = 1; i <= numRows; i++) {
            int rowIndex = i*pedestrianTrial.num_cols;
            float timestamp = float.Parse(data[rowIndex+pedestrianTrial.timestamp_col]);
            string _id = data[rowIndex+pedestrianTrial.guid_col];
            Vector2 position = new Vector2(
                float.Parse(data[rowIndex+pedestrianTrial.position_cols.x]),
                float.Parse(data[rowIndex+pedestrianTrial.position_cols.y])
            );
            Vector2 forward = new Vector2(
                float.Parse(data[rowIndex+pedestrianTrial.forward_cols.x]),
                float.Parse(data[rowIndex+pedestrianTrial.forward_cols.y])
            );

            // A new position is formed from this row. Assumes activeness
            PedestrianPosition newPos = new PedestrianPosition(_id, timestamp, position, forward, 1);
            // add to raw position count.
            m_pedestrianPositions.Add(newPos);

            // Add specifically to our pedestrian.
            if (!m_pedestrians.ContainsKey(_id)) {
                Pedestrian newPed = new Pedestrian(_id, timestamp, newPos);
                m_pedestrians.Add(_id, newPed);
            } else {
                m_pedestrians[_id].AddRawPosition(timestamp, newPos);
            }

            // Update duration
            if (m_latest_timestamp == 0 || m_latest_timestamp < timestamp) m_latest_timestamp = timestamp;
        }

        // By the end, we should ahve raw positions and pedestrian list each with raw positions in of themselves
        Debug.Log($"Detected Duration: {m_latest_timestamp}");
    }

    private void AlignPedestriansToFPS() {
        // Calculate the space discretization, given the FPS
        float dt = 1f/(float)m_outputFPS;
        int numFrames = Mathf.FloorToInt(m_latest_timestamp / dt);

        // We iterate until we reach the last frame
        for(int i = 0; i < numFrames; i++) {
            float timestamp = i * dt;
            foreach(KeyValuePair<string, Pedestrian> kvp in m_pedestrians) {
                Pedestrian p = kvp.Value;
                p.CreateStateFromTimestamp(timestamp);
            }
        }  

        // We're all good!
    }

    public void SaveCSV() {
        // Set dirname and filename
        m_writer.dirName = pedestrianTrial.output_dir;
        m_writer.fileName = "user-aligned";

        // Set column names
        m_writer.columns = new List<string>();
        m_writer.columns.Add("id");
        m_writer.columns.Add("timestamp");
        m_writer.columns.Add("pos_x");
        m_writer.columns.Add("pos_y");
        m_writer.columns.Add("for_x");
        m_writer.columns.Add("for_y");
        m_writer.columns.Add("active");
        
        // Initialize the writer
        m_writer.Initialize();

        // loop through all other entities
        foreach(KeyValuePair<string, Pedestrian> kvp in m_pedestrians) {
            Pedestrian p = kvp.Value;
            foreach(PedestrianPosition pos in p.positions.Values) {
                m_writer.AddPayload(pos._id);
                m_writer.AddPayload(pos.timestamp);
                
                m_writer.AddPayload(pos.position);
                m_writer.AddPayload(pos.forward);
                m_writer.AddPayload(pos.is_active);
                m_writer.WriteLine();
            }
        }

        m_writer.Disable();
    }

    public void ReadTrial() {
        // Assumption: Trial start is 0. We can treat timestamps as they are
        GetPedestrianPositions();

        // Refactor agents based on chosen FPS. Requires lerping
        AlignPedestriansToFPS();

        // Save outputs to file
        SaveCSV();
    }
}
