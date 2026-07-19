using UnityEngine;

namespace VRacer.Car
{
    /// <summary>
    /// Handles visual damage representation on the car.
    /// Swaps meshes, applies vertex deformation, or changes materials 
    /// to show dents and scrapes like the original Model 1 game.
    /// </summary>
    public class CarDamage : MonoBehaviour
    {
        [Header("Damage Meshes")]
        [SerializeField] private MeshRenderer[] bodyPanels;
        [SerializeField] private Material undamagedMaterial;
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private Material heavilyDamagedMaterial;

        [Header("Damage Visuals")]
        [SerializeField] private Color dentColor = Color.gray;
        [SerializeField] private Color scrapeColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Detachable Parts")]
        [SerializeField] private GameObject frontWingLeft;
        [SerializeField] private GameObject frontWingRight;
        [SerializeField] private GameObject rearWing;
        [SerializeField] private float detachThreshold = 0.6f;

        private MaterialPropertyBlock propBlock;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();

            if (bodyPanels == null || bodyPanels.Length == 0)
            {
                bodyPanels = GetComponentsInChildren<MeshRenderer>();
            }
        }

        public void ApplyDamage(float damagePercent)
        {
            // Phase 1: Color change for light damage
            if (damagePercent > 0.1f && damagePercent <= 0.4f)
            {
                foreach (var panel in bodyPanels)
                {
                    if (panel != null && damagedMaterial != null)
                    {
                        panel.material = damagedMaterial;
                    }
                }
            }
            // Phase 2: Heavier damage
            else if (damagePercent > 0.4f)
            {
                foreach (var panel in bodyPanels)
                {
                    if (panel != null && heavilyDamagedMaterial != null)
                    {
                        panel.material = heavilyDamagedMaterial;
                    }
                }

                // Detach parts at high damage
                if (damagePercent > detachThreshold)
                {
                    DetachWing(frontWingLeft);
                    DetachWing(frontWingRight);
                    DetachWing(rearWing);
                }
            }
            else
            {
                foreach (var panel in bodyPanels)
                {
                    if (panel != null && undamagedMaterial != null)
                    {
                        panel.material = undamagedMaterial;
                    }
                }
            }
        }

        private void DetachWing(GameObject wing)
        {
            if (wing == null) return;

            wing.transform.SetParent(null);
            Rigidbody wingRb = wing.AddComponent<Rigidbody>();
            wingRb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            wingRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            Destroy(wing, 3f); // Clean up after 3 seconds
        }

        public void ResetDamage()
        {
            foreach (var panel in bodyPanels)
            {
                if (panel != null && undamagedMaterial != null)
                {
                    panel.material = undamagedMaterial;
                }
            }
        }
    }
}
