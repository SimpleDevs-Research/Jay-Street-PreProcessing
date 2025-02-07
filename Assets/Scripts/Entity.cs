using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [System.Serializable]
    public class EntityState {
        public float timestamp;
        public Vector3 position;
        public Vector3 rel_position;
        public Vector3 forward;
        public Vector3 rel_forward;
        public Vector3 velocity;
        public Vector3 rel_velocity;
        public float angle_from_participant;
        public float distance_from_participant;
        public bool isActive;
        public EntityState(float timestamp, Vector3 position, Vector3 rel_position, Vector3 forward, Vector3 rel_forward, Vector3 velocity, Vector3 rel_velocity, float angle_from_participant, float distance_from_participant, bool isActive) {
            this.timestamp = timestamp;
            this.position = position;
            this.rel_position = rel_position;
            this.forward = forward;
            this.rel_forward = rel_forward;
            this.velocity = velocity;
            this.rel_velocity = rel_velocity;
            this.angle_from_participant = angle_from_participant;
            this.distance_from_participant = distance_from_participant;
            this.isActive = isActive;
        }
        public EntityState(float timestamp, Vector3 position, Vector3 forward, Vector3 velocity, bool isActive) {
            this.timestamp = timestamp;
            this.position = position;
            this.rel_position = Vector3.zero;
            this.forward = forward;
            this.rel_forward = Vector3.zero;
            this.velocity = velocity;
            this.rel_velocity = Vector3.zero;
            this.angle_from_participant = 0f;
            this.distance_from_participant = 0f;
            this.isActive = isActive;
        }
        public EntityState(float timestamp, EntityState copyState, bool isActive) {
            this.timestamp = timestamp;
            this.position = copyState.position;
            this.forward = copyState.forward;
            this.velocity = copyState.velocity;
            this.isActive = isActive;
        }
    }

    [Header("=== GameObject Settings ===")]
    [SerializeField] private Renderer[] m_renderers;
    
    [Header("=== Timestamp Settings ===")]
    [SerializeField] private string m_id;
    public string _id => m_id;
    [SerializeField] private List<EntityState> m_raw_timestamps;
    public List<EntityState> raw_timestamps => m_raw_timestamps;
    [SerializeField] private List<float> m_raw_timestamps_check;
    [SerializeField] private Dictionary<float, EntityState> m_timestamps;
    public Dictionary<float, EntityState> timestamps => m_timestamps;
    [SerializeField] private List<float> m_timestamps_check;

    public void InitializeEntity(string newID) {
        m_id = newID;
        gameObject.name = m_id;
        m_raw_timestamps = new List<EntityState>();
        m_timestamps = new Dictionary<float, EntityState>();
        m_raw_timestamps_check = new List<float>();
        m_timestamps_check = new List<float>();
    }

    public void AddRawState(float timestamp, Vector3 position, Vector3 forward, Vector3 velocity) {
        if (m_raw_timestamps_check.Contains(timestamp)) return;
        m_raw_timestamps.Add(new EntityState(timestamp, position, forward, velocity, true));
        m_raw_timestamps_check.Add(timestamp);
    }

    public void CreateStateFromTimestamp(float timestamp, EntityState refState = null) {
        // First, check if the timeestamp is less than the first raw timestamp or is bigger than the last raw timestamp.
        if (timestamp < m_raw_timestamps[0].timestamp) {
            m_timestamps.Add(timestamp, new EntityState(timestamp, m_raw_timestamps[0], false));
            m_timestamps_check.Add(timestamp);
            return;
        }
        if (timestamp > m_raw_timestamps[m_raw_timestamps.Count-1].timestamp) {
            m_timestamps.Add(timestamp, new EntityState(timestamp, m_raw_timestamps[m_raw_timestamps.Count-1], false));
            m_timestamps_check.Add(timestamp);
            return;
        }

        // Since we've guaranteed that the provided timestamp is within the range of raw timestamps, let's check where this timestamp resides
        for(int i = 0; i < m_raw_timestamps.Count-1; i++) {
            float start_timestamp = m_raw_timestamps[i].timestamp;
            float end_timestamp = m_raw_timestamps[i+1].timestamp;
            float time_range = end_timestamp - start_timestamp;
            if (start_timestamp <= timestamp && timestamp < end_timestamp) {
                float timestamp_diff = timestamp - start_timestamp;
                float lerpFactor = timestamp_diff/time_range;
                Vector3 position = Vector3.Lerp(m_raw_timestamps[i].position, m_raw_timestamps[i+1].position, lerpFactor);
                Vector3 rel_position = (refState != null) ? position - refState.position : Vector3.zero;
                Vector3 forward = Vector3.Lerp(m_raw_timestamps[i].forward, m_raw_timestamps[i+1].forward, lerpFactor);
                Vector3 rel_forward = (refState != null) ? forward - refState.forward : Vector3.zero;
                Vector3 velocity = Vector3.Lerp(m_raw_timestamps[i].velocity, m_raw_timestamps[i+1].velocity, lerpFactor);
                Vector3 rel_velocity = (refState != null) ? velocity - refState.velocity : Vector3.zero;
                float angle_from_participant = 0f;
                float distance_from_participant = 0f;
                if (refState != null) {
                    distance_from_participant = rel_position.magnitude;
                    if (distance_from_participant != 0f) angle_from_participant = Vector3.SignedAngle(refState.forward, rel_position.normalized, Vector3.up);
                }
                m_timestamps.Add(timestamp, new EntityState(timestamp, position, rel_position, forward, rel_forward, velocity, rel_velocity, angle_from_participant, distance_from_participant, true));
                m_timestamps_check.Add(timestamp);
                break;
            }
        }
    }

    public void RecreateState(float timestamp) {
        if (!m_timestamps.ContainsKey(timestamp)) {
            foreach(Renderer r in m_renderers) r.enabled = false;
            return;
        }

        EntityState currentState = m_timestamps[timestamp];
        transform.position = currentState.position;
        transform.forward = currentState.forward;
        foreach(Renderer r in m_renderers) r.enabled = currentState.isActive;
    }
}
