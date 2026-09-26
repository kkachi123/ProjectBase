using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Hashes a Vector2 by converting its components to integers using the specified rounding mode, then combining them into a single hash value.")]
    public class HashVector2 : BaseAction
    {
        [Tooltip("The Vector2 to hash.")]
        [SerializeField]
        private Vector2Var _vector2;

        [Tooltip("How to convert float components to integers.")]
        [SerializeField, DefaultValue(RoundingMode.Round)]
        private RoundingMode _roundingMode;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_vector2, _output);

        public override void Execute()
        {
            var vector = _vector2.Value;
            
            var x = ConvertToInt(vector.x);
            var y = ConvertToInt(vector.y);
            
            _output.Value = HashVector2Int(x, y);
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

        public static int HashVector2Int(int x, int y)
        {
            unchecked
            {
                // Combine two integers using a well-distributed hash function
                var hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                
                // Additional mixing for better distribution
                hash ^= hash >> 16;
                hash *= -2048144709; // 0x85ebca6b as int
                hash ^= hash >> 13;
                hash *= -1028477387; // 0xc2b2ae35 as int
                hash ^= hash >> 16;
                
                return hash;
            }
        }

        public override string GetSummary() => "Hash {_vector2} ({_roundingMode}) -> {_output}";
    }
}
