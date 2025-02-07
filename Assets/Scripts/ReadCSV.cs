using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ReadCSV : MonoBehaviour
{
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
    [SerializeField] private bool m_playing = false;
    [SerializeField] private float m_viewFieldDistance = 10f;
    [SerializeField] private CSVWriter m_writer;

    [System.Serializable]
    public class EntityPosition {
        public string id;
        public long timestamp;
        public long rel_timestamp;
        public Vector3 position;
        public Vector3 forward;
        public EntityPosition(string id, long timestamp, long rel_timestamp, Vector3 position, Vector3 forward) {
            this.id = id;
            this.timestamp = timestamp;
            this.rel_timestamp = rel_timestamp;
            this.position = position;
            this.forward = forward;
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
        string[] data = ReadCSVFile(m_trialCSV);
        int numRows = GetTableSize(data, m_numTrialCols);
        
        // The trial start is expected be in the last row, first column.
        // We get an overflow problem if we parse as int32. Instead, we parse as a long type.
        return long.Parse(data[numRows*m_numTrialCols]);
    }

    public List<EntityPosition> GetPlayerPositions(long trialStart) {
        // User data file should be read
        string[] data = ReadCSVFile(m_playerCSV);
        int numRows = GetTableSize(data, m_numPlayerCols);
        
        List<EntityPosition> positions = new List<EntityPosition>();
        for(int i = 1; i <= numRows; i++) {
            int rowIndex = i*m_numPlayerCols;
            long timestamp = long.Parse(data[rowIndex+m_playerTimestampCol]);
            long rel_timestamp = timestamp - trialStart;
            Vector3 position = new Vector3(
                float.Parse(data[rowIndex+m_playerPosCols.x]),
                0f,
                float.Parse(data[rowIndex+m_playerPosCols.z])
            );
            Vector3 forward = new Vector3(
                float.Parse(data[rowIndex+m_playerForwardCols.x]),
                float.Parse(data[rowIndex+m_playerForwardCols.y]),
                float.Parse(data[rowIndex+m_playerForwardCols.z])
            );
            EntityPosition newPos = new EntityPosition("player", timestamp, rel_timestamp, position, forward);
            positions.Add(newPos);
        }

        return positions;
    }

    public List<EntityPosition> GetPedestrianPositions(long trialStart) {
        // User data file should be read
        string[] data = ReadCSVFile(m_pedestrianCSV);
        int numRows = GetTableSize(data, m_numPedestrianCols);
        
        List<EntityPosition> positions = new List<EntityPosition>();
        for(int i = 1; i <= numRows; i++) {
            int rowIndex = i*m_numPedestrianCols;
            long timestamp = long.Parse(data[rowIndex+m_pedestrianTimestampCol]);
            long rel_timestamp = timestamp - trialStart;
            string _id = data[rowIndex+m_pedestrianGuiIDCol];
            Vector3 position = new Vector3(
                float.Parse(data[rowIndex+m_pedestrianPosCols.x]),
                0f,
                float.Parse(data[rowIndex+m_pedestrianPosCols.y])
            );
            Vector3 forward = new Vector3(
                float.Parse(data[rowIndex+m_pedestrianForwardCols.x]),
                float.Parse(data[rowIndex+m_pedestrianForwardCols.y]),
                float.Parse(data[rowIndex+m_pedestrianForwardCols.z])
            );
            EntityPosition newPos = new EntityPosition(_id, timestamp, rel_timestamp, position, forward);
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
            currentEntity.AddRawState(ep.rel_timestamp, ep.position, ep.forward);
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
        SavePlayerCSV();
        SavePedestriansCSV();

        // Replay the scene
        StartCoroutine(ReplayCoroutine());
    }
    
    public void SavePlayerCSV() {
        m_writer.fileName = "user-aligned.csv";
        m_writer.Initialize();

        Entity player = m_entities["player"];
        foreach(Entity.EntityState playerState in player.raw_timestamps) {
            m_writer.AddPayload(playerState.timestamp);
            m_writer.AddPayload(player._id);
            m_writer.AddPayload(playerState.position);
            m_writer.AddPayload(playerState.rel_position);
            m_writer.AddPayload(playerState.forward);
            m_writer.AddPayload(playerState.rel_forward);
            m_writer.AddPayload(playerState.angle_from_participant);
            m_writer.AddPayload(playerState.distance_from_participant);
            m_writer.AddPayload((playerState.isActive) ? 1 : 0);
            m_writer.WriteLine();
        }

        m_writer.Disable();
    }

    public void SavePedestriansCSV() {
        m_writer.fileName = "pedestrians-aligned.csv";
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

        // Loop through this player's raw timestamps
        foreach(Entity.EntityState playerState in player.raw_timestamps) {
            // Get the key and value from kvp
            float timestamp = playerState.timestamp;

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
    }
}
