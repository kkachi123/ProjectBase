namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>함정에 접촉한 대상에게 진입할 때 한 번 피해를 준다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrapContactDamage : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField] private LayerMask targetLayers;

        private readonly Dictionary<Collider2D, IDamageable> contacts = new();
        private readonly Dictionary<IDamageable, int> targets = new();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger || (targetLayers.value & (1 << other.gameObject.layer)) == 0 || contacts.ContainsKey(other))
                return;

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            contacts.Add(other, target);
            if (targets.TryGetValue(target, out int count))
                targets[target] = count + 1;
            else
            {
                targets.Add(target, 1);
                target.TakeDamage(damage);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!contacts.TryGetValue(other, out IDamageable target)) return;
            contacts.Remove(other);
            int remaining = targets[target] - 1;
            if (remaining == 0) targets.Remove(target);
            else targets[target] = remaining;
        }

        private void OnDisable()
        {
            contacts.Clear();
            targets.Clear();
        }
    }
}
