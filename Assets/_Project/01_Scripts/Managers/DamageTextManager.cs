using DamageNumbersPro;
using UnityEngine;

namespace TFT_Defense.Managers
{
    public class DamageTextManager : MonoSingleton<DamageTextManager>
    {
        [Tooltip("Assign the DamageNumber prefab from the DamageNumbersPro asset.")]
        [SerializeField]
        private DamageNumber damageNumberPrefab;

        protected override void Awake()
        {
            base.Awake();
            if (damageNumberPrefab == null)
            {
                Debug.LogError("DamageNumber prefab is not assigned in the DamageTextManager. Please assign it in the inspector.");
            }
        }

        /// <summary>
        /// Spawns a damage number at the specified world position.
        /// </summary>
        /// <param name="damage">The amount of damage to display.</param>
        /// <param name="position">The world position to spawn the text at.</param>
        public void ShowDamage(float damage, Vector3 position)
        {
            if (damageNumberPrefab == null)
            {
                Debug.LogWarning("Damage number prefab is not set, cannot show damage text.");
                return;
            }

            // DamageNumbersPro usually requires a number and a position.
            // The library handles the rest (animation, pooling, etc.).
            // We'll use the Spawn method, which is common for such assets.
            damageNumberPrefab.Spawn(position, damage);
        }
    }
}
