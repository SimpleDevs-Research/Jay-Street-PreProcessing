using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadMultipleCSVs : MonoBehaviour
{
    public ReadCSV reader;
    public List<ParticipantTrial> trials;
    
    public void Start() {
        if (reader == null) {
            Debug.LogError("Cannot read trials without a CSV reader");
            return;
        }
        if (trials.Count == 0) {
            Debug.LogError("Cannot read an empty list of trials");
            return;
        }

        foreach(ParticipantTrial trial in trials) {
            Debug.Log($"Reading Participant Trial: {trial.trial_name}");
            reader.participantTrial = trial;
            string lastFilepath = reader.ReadTrial();
            Debug.Log($"Finished reading. Last generated filepath: {lastFilepath}");
        }
    }

    private IEnumerator ReadTrials() {
        foreach(ParticipantTrial trial in trials) {
            Debug.Log($"Reading Participant Trial: {trial.trial_name}");
            reader.participantTrial = trial;
            string lastFilepath = reader.ReadTrial();
            Debug.Log($"Finished reading. Last generated filepath: {lastFilepath}");
            yield return null;
        }
    }
}
