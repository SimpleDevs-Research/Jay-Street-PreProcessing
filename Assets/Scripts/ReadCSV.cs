using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class ReadCSV : MonoBehaviour
{
    public ParticipantTrial participantTrial;

    /*
    [SerializeField] private TextAsset m_trialCSV;
    [SerializeField] private int m_numTrialCols = 2;
    [Space]
    [SerializeField] private TextAsset m_playerCSV;
    [SerializeField] private int m_numPlayerCols = 28;
    [SerializeField] private int m_playerTimestampCol = 0;
    [SerializeField] private Vector3Int m_playerPosCols = new Vector3Int(1,2,3);
    [SerializeField] private Vector3Int m_playerForwardCols = new Vector3Int(4,5,6);
    [Space]
    [SerializeField] private TextAsset m_pedestrianCSV;
    [SerializeField] private int m_numPedestrianCols = 9;
    [SerializeField] private int m_pedestrianTimestampCol = 0;
    [SerializeField] private int m_pedestrianGuiIDCol= 1;
    [SerializeField] private Vector2Int m_pedestrianPosCols = new Vector2Int(2,3);
    [SerializeField] private Vector3Int m_pedestrianForwardCols = new Vector3Int(4,5,6);
    */
    [Space]

    private List<EntityPosition> m_playerPositions;
    private List<EntityPosition> m_pedestrianPositions;
    private List<EntityPosition> m_entityPositions;
    [SerializeField] private Dictionary<string, Entity> m_entities;
    [SerializeField] private Entity m_playerPrefab;
    [SerializeField] private Entity m_entityPrefab;
    [SerializeField] private int m_replayFPS = 15;
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
    [SerializeField] private CSVWriter m_writer;

    [System.Serializable]
    public class EntityPosition {
        public string id;
        public long timestamp;
        public long rel_timestamp;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 velocity;
        public EntityPosition(string id, long timestamp, long rel_timestamp, Vector3 position, Vector3 forward,  Vector3 velocity) {
            this.id = id;
            this.timestamp = timestamp;
            this.rel_timestamp = rel_timestamp;
            this.position = position;
            this.forward = forward;
            this.velocity = velocity;
        }
        public override string ToString() {
            return $"{timestamp}\t{rel_timestamp}\t{position.ToString()}\t{forward.ToString()}";
        }
    }

    [System.Serializable]
    public class ReplayFrame {
        public int frame_number;
        public float timestamp;
        public Vector3 playerPosition;
        public Dictionary<string, Vector3> pedestrianPositions;
        public ReplayFrame(int frame_number, float timestamp, Vector3 playerPosition, Dictionary<string, Vector3> pedestrianPositions) {
            this.frame_number = frame_number;
            this.timestamp = timestamp;
            this.playerPosition = playerPosition;
            this.pedestrianPositions = pedestrianPositions;
        }
    }

    /* -------------------- */




    // Note: Read a csv file. The outputted string array is the number of cell items, not the number of rows.
    public static string[] ReadCSVFile(TextAsset ta, string colDivider = ",", string rowDivider = "\n") {
        return ta.text.Split(new string[] {colDivider, rowDivider}, StringSplitOptions.None);
    }

    // Returns the number of rows of a table, including the header row
    // Note: `numCols` does NOT include the index column
    public static int GetTableSize(string[] data, int numCols) {
        return data.Length/numCols - 1;
    }

    public static void PrintTable(string[] data, int numRows, int numCols, bool numRowsIncludesHeader=true) {
        int startingRowIndex = numRowsIncludesHeader ? 1 : 0;
        for(int i = startingRowIndex; i < numRows; i++) {
            int rowStartIndex = numCols*i;
            int rowEndIndex = numCols*(i+1);
            string rowString = "";
            for(int j = rowStartIndex; j < rowEndIndex; j++) rowString += data[j]+",";
            Debug.Log(rowString);
        }
    }

    public static void PrintEntityPositions(List<EntityPosition> positions) {
        foreach(EntityPosition pos in positions) Debug.Log(pos.ToString());
    }

    public long GetTrialStart() {
        // Trial data file should be read
        string[] data = ReadCSVFile(participantTrial.trial_file);
        int numRows = GetTableSize(data, participantTrial.trial_num_cols);
        
        // The trial start is expected be in the last row, first column.
        // We get an overflow problem if we parse as int32. Instead, we parse as a long type.
        return long.Parse(data[numRows*participantTrial.trial_num_cols]);
    }

    public List<EntityPosition> GetPlayerPositions(long trialStart) {
        // User data file should be read
        string[] data = ReadCSVFile(participantTrial.user_file);
        int numRows = GetTableSize(data, participantTrial.user_num_cols);
        
        List<EntityPosition> positions = new List<EntityPosition>();
        for(int i = 1; i <= numRows; i++) {
            int rowIndex = i*participantTrial.user_num_cols;
            long timestamp = long.Parse(data[rowIndex+participantTrial.user_timestamp_col]);
            long rel_timestamp = timestamp - trialStart;
            Vector3 position = new Vector3(
                float.Parse(data[rowIndex+participantTrial.user_pos_cols.x]),
                0f,
                float.Parse(data[rowIndex+participantTrial.user_pos_cols.z])
            );
            Vector3 forward = new Vector3(
                float.Parse(data[rowIndex+participantTrial.user_forward_cols.x]),
                float.Parse(data[rowIndex+participantTrial.user_forward_cols.y]),
                float.Parse(data[rowIndex+participantTrial.user_forward_cols.z])
            );
            Vector3 velocity = new Vector3(
                float.Parse(data[rowIndex+participantTrial.user_velocity_cols.x]),
                0f,
                float.Parse(data[rowIndex+participantTrial.user_velocity_cols.z])
            );
            EntityPosition newPos = new EntityPosition("player", timestamp, rel_timestamp, position, forward, velocity);
            positions.Add(newPos);
        }

        return positions;
    }

    public List<EntityPosition> GetPedestrianPositions(long trialStart) {
        // User data file should be read
        string[] data = ReadCSVFile(participantTrial.pedestrian_file);
        int numRows = GetTableSize(data, participantTrial.pedestrian_num_cols);
        
        List<EntityPosition> positions = new List<EntityPosition>();
        for(int i = 1; i <= numRows; i++) {
            int rowIndex = i*participantTrial.pedestrian_num_cols;
            long timestamp = long.Parse(data[rowIndex+participantTrial.pedestrian_timestamp_col]);
            long rel_timestamp = timestamp - trialStart;
            string _id = data[rowIndex+participantTrial.pedestrian_guiID_col];
            Vector3 position = new Vector3(
                float.Parse(data[rowIndex+participantTrial.pedestrian_pos_cols.x]),
                0f,
                float.Parse(data[rowIndex+participantTrial.pedestrian_pos_cols.y])
            );
            Vector3 forward = new Vector3(
                float.Parse(data[rowIndex+participantTrial.pedestrian_forward_cols.x]),
                float.Parse(data[rowIndex+participantTrial.pedestrian_forward_cols.y]),
                float.Parse(data[rowIndex+participantTrial.pedestrian_forward_cols.z])
            );
            Vector3 velocity = new Vector3(
                float.Parse(data[rowIndex+participantTrial.pedestrian_velocity_cols.x]),
                0f,
                float.Parse(data[rowIndex+participantTrial.pedestrian_velocity_cols.y])
            );
            EntityPosition newPos = new EntityPosition(_id, timestamp, rel_timestamp, position, forward, velocity);
            positions.Add(newPos);
        }

        return positions;
    }

    public void InitializeAllEntities() {
        m_entities = new Dictionary<string, Entity>();
        foreach(EntityPosition ep in m_entityPositions) {
            string entityID = ep.id;
            Entity currentEntity;
            if (!m_entities.ContainsKey(entityID)) {
                Entity prefab = (entityID == "player") ? m_playerPrefab : m_entityPrefab;
                currentEntity = Instantiate(prefab, Vector3.zero, Quaternion.identity) as Entity;
                currentEntity.InitializeEntity(entityID);
                m_entities.Add(entityID, currentEntity);
            } else {
                currentEntity = m_entities[entityID];
            }
            currentEntity.AddRawState(ep.rel_timestamp, ep.position, ep.forward, ep.velocity);
        }
    }

    public void AlignPedestriansToPlayer() {
        // Get the player entity
        Entity player = m_entities["player"];
        List<float> processed_timestamps = new List<float>();

        // Loop through this player's raw timestamps
        foreach(Entity.EntityState playerState in player.raw_timestamps) {
            // Get the key and value from kvp
            float timestamp = playerState.timestamp;
            if (processed_timestamps.Contains(timestamp)) Debug.Log($"Already processed timestamp {timestamp}");
            processed_timestamps.Add(timestamp);

            // loop through all other entities
            foreach(KeyValuePair<string, Entity> kvp2 in m_entities) {
                string otherID = kvp2.Key;
                Entity otherEntity = kvp2.Value;
                otherEntity.CreateStateFromTimestamp(timestamp, playerState);
            }
        }
    }



    private void Start() {
        if (m_readOnStart) ReadTrial();
    }

    public string ReadTrial() {
        // Get the trial start
        long trialStart = GetTrialStart();

        // Get the player and pedestrian positions directly from CSV
        m_playerPositions = GetPlayerPositions(trialStart);
        m_pedestrianPositions = GetPedestrianPositions(trialStart);

        // Concatenate the two lists together
        m_entityPositions = new List<EntityPosition>();
        m_entityPositions.AddRange(m_playerPositions);
        m_entityPositions.AddRange(m_pedestrianPositions);

        // Initialize entities and the raw positions of each. Then align all to the player's timestamps
        InitializeAllEntities();
        AlignPedestriansToPlayer();

        // Save player data as CSV files
        if (m_saveAlignedCSVs) {
            m_writer.dirName = participantTrial.output_dir;
            SavePlayerCSV();
            SavePedestriansCSV();
        }

        // Replay the scene
        if (m_replayOn) StartCoroutine(ReplayCoroutine());
        return m_writer.GetLastFilepath();
    }
    
    public void SavePlayerCSV() {
        m_writer.fileName = "user-aligned";
        m_writer.Initialize();

        Entity player = m_entities["player"];
        foreach(Entity.EntityState playerState in player.raw_timestamps) {
            m_writer.AddPayload(playerState.timestamp);
            m_writer.AddPayload(player._id);
            m_writer.AddPayload(playerState.position);
            m_writer.AddPayload(playerState.rel_position);
            m_writer.AddPayload(playerState.forward);
            m_writer.AddPayload(playerState.rel_forward);
            m_writer.AddPayload(playerState.velocity);
            m_writer.AddPayload(playerState.rel_velocity);
            m_writer.AddPayload(playerState.angle_from_participant);
            m_writer.AddPayload(playerState.distance_from_participant);
            m_writer.AddPayload((playerState.isActive) ? 1 : 0);
            m_writer.WriteLine();
        }

        m_writer.Disable();
    }

    public void SavePedestriansCSV() {
        m_writer.fileName = "pedestrians-aligned";
        m_writer.Initialize();

         // loop through all other entities
        foreach(KeyValuePair<string, Entity> kvp2 in m_entities) {
            string otherID = kvp2.Key;
            Entity otherEntity = kvp2.Value;
            if (otherID == "player") continue;
            foreach(Entity.EntityState otherState in otherEntity.timestamps.Values) {
                m_writer.AddPayload(otherState.timestamp);
                m_writer.AddPayload(otherID);
                m_writer.AddPayload(otherState.position);
                m_writer.AddPayload(otherState.rel_position);
                m_writer.AddPayload(otherState.forward);
                m_writer.AddPayload(otherState.rel_forward);
                m_writer.AddPayload(otherState.velocity);
                m_writer.AddPayload(otherState.rel_velocity);
                m_writer.AddPayload(otherState.angle_from_participant);
                m_writer.AddPayload(otherState.distance_from_participant);
                m_writer.AddPayload((otherState.isActive) ? 1 : 0);
                m_writer.WriteLine();
            }
        }

        m_writer.Disable();
    }

    private IEnumerator ReplayCoroutine() {

        // Indicate to the system that we're playing a coroutine
        m_playing = true;

        // Get the player entity
        Entity player = m_entities["player"];

        // Get the time to wait
        WaitForSeconds waitDelay = new WaitForSeconds(1f/(float)m_replayFPS);

        // Calculate the total number of frames, and the current index at 0
        m_replay_total_length = player.raw_timestamps.Count-1;
        m_replay_current_index = 0;
        m_replay_slider = 0f;
        m_prevReplayTimestamp = -1f;

        // Loop through this player's raw timestamps
        while(m_playing) {
            if (m_playReplay) {
                // we increment automatically
                m_replay_current_index += 1;
                if (m_replay_current_index >= m_replay_total_length) m_replay_current_index = 0;
                m_replay_slider = (float)(m_replay_current_index/m_replay_total_length);
            } else {
                // We look to the slider, then apply it to our replay index
                m_replay_current_index = (int)(m_replay_slider*m_replay_total_length);
            }

            // get the current timewstamp from the replay index
            Entity.EntityState playerState = player.raw_timestamps[m_replay_current_index];
            float timestamp = playerState.timestamp;
            if (m_millisecondsTextbox != null) m_millisecondsTextbox.text = timestamp.ToString();
            
            // Draw debug rays to represent visual field
            Vector3 leftVisualEdge = Quaternion.Euler(0, -55, 0) * playerState.forward * m_viewFieldDistance;
            Debug.DrawRay(playerState.position, leftVisualEdge, Color.green);
            Vector3 rightVisualEdge = Quaternion.Euler(0, 55, 0) * playerState.forward * m_viewFieldDistance;
            Debug.DrawRay(playerState.position, rightVisualEdge, Color.green);
            Debug.DrawRay(playerState.position, playerState.forward * m_viewFieldDistance, Color.green);
            
            // Don't change anything if the previous timestamp is the same as the current timestamp.
            if (timestamp == m_prevReplayTimestamp) {
                yield return null;
                continue;
            }

            // loop through all other entities
            foreach(KeyValuePair<string, Entity> kvp2 in m_entities) {
                string otherID = kvp2.Key;
                Entity otherEntity = kvp2.Value;
                otherEntity.RecreateState(timestamp);
            }

            // Prep next frame
            m_prevReplayTimestamp = timestamp;
            yield return waitDelay;
        }
        /*
        foreach(Entity.EntityState playerState in player.raw_timestamps) {
            // Get the key and value from kvp
            float timestamp = playerState.timestamp;
            if (m_millisecondsTextbox != null) m_millisecondsTextbox.text = timestamp.ToString();

            // Draw debug rays to represent visual field
            Vector3 leftVisualEdge = Quaternion.Euler(0, -55, 0) * playerState.forward * m_viewFieldDistance;
            Debug.DrawRay(playerState.position, leftVisualEdge, Color.green);
            Vector3 rightVisualEdge = Quaternion.Euler(0, 55, 0) * playerState.forward * m_viewFieldDistance;
            Debug.DrawRay(playerState.position, rightVisualEdge, Color.green);
            Debug.DrawRay(playerState.position, playerState.forward * m_viewFieldDistance, Color.green);

            // loop through all other entities
            foreach(KeyValuePair<string, Entity> kvp2 in m_entities) {
                string otherID = kvp2.Key;
                Entity otherEntity = kvp2.Value;
                otherEntity.RecreateState(timestamp);
            }
            yield return waitDelay;
        }
        m_playing = false;
        */
    }

    private void DrawFrame() {

    }
}
