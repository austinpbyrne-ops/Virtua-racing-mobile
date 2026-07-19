using UnityEngine;
using VRacer.Car;

namespace VRacer.Track
{
    /// <summary>
    /// Checkpoint trigger placed along the track.
    /// Part of the sector-based checkpoint timer system — the core arcade mechanic.
    /// </summary>
    public class CheckpointTrigger : MonoBehaviour
    {
        [Header("Checkpoint Configuration")]
        [SerializeField] private int checkpointIndex = 0;
        [SerializeField] private int sectorIndex = 0;
        [SerializeField] private bool isStartFinish = false;
        [SerializeField] private float checkpointWidth = 15f;

        public int CheckpointIndex => checkpointIndex;
        public int SectorIndex => sectorIndex;
        public bool IsStartFinish => isStartFinish;

        private void OnTriggerEnter(Collider other)
        {
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null) return;

            // Notify race manager (player car only)
            if (car.IsPlayer && RaceManager.Instance != null)
            {
                RaceManager.Instance.OnCarPassedCheckpoint(checkpointIndex);
            }

            // Track per-car checkpoint
            car.OnCheckpointPassed(checkpointIndex);

            // Visual flash
            ActivateCheckpointEffect();
        }

        private void ActivateCheckpointEffect()
        {
            // Brief flash or color change on the checkpoint marker
            // In the original, this was a simple "CHECKPOINT!" flash
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isStartFinish ? Color.green : Color.yellow;
            Vector3 size = new Vector3(checkpointWidth, 3f, 2f);
            Gizmos.DrawWireCube(transform.position, size);

            // Draw checkpoint number
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                isStartFinish ? $"START/FINISH ({checkpointIndex})" : $"CP {checkpointIndex}"
            );
#endif
        }
    }
}
