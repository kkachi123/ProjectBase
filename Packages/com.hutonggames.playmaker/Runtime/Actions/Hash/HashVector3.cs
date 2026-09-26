using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Hashes a Vector3 by converting its components to integers using the specified rounding mode, then combining them into a single hash value.")]
    public class HashVector3 : BaseAction
    {
        [Tooltip("The Vector3 to hash.")]
        [SerializeField]
        private Vector3Var _vector3;

        [Tooltip("How to convert float components to integers.")]
        [SerializeField]
        private RoundingMode _roundingMode = RoundingMode.Round;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_vector3, _output);

        public override void Execute()
        {
            var vector = _vector3.Value;
            
            int x = ConvertToInt(vector.x);
            int y = ConvertToInt(vector.y);
            int z = ConvertToInt(vector.z);
            
            _output.Value = HashVector3Int(x, y, z);
        }

        private int ConvertToInt(float value)
        {
            return _roundingMode switch
            {
                RoundingMode.Floor => Mathf.FloorToInt(value),
                RoundingMode.Round => Mathf.RoundToInt(value),
                RoundingMode.Ceiling => Mathf.CeilToInt(value),
                _ => Mathf.RoundToInt(value)
            };
        }

        public static int HashVector3Int(int x, int y, int z)
        {
            unchecked
            {
                // Combine three integers using a well-distributed hash function
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                
                // Additional mixing for better distribution
                hash ^= hash >> 16;
                hash *= -2048144709; // 0x85ebca6b as int
                hash ^= hash >> 13;
                hash *= -1028477387; // 0xc2b2ae35 as int
                hash ^= hash >> 16;
                
                return hash;
            }
        }

        public override string GetSummary() => "Hash {_vector3} ({_roundingMode}) -> {_output}";
    }
}
