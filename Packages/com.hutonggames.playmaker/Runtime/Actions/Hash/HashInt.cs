using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Generates a hash value from a single integer using a hash function.")]
    public class HashInt : BaseAction
    {
        [Tooltip("The integer to hash.")]
        [SerializeField]
        private IntegerVar _integer;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_integer, _output);

        public override void Execute() => _output.Value = Hash(_integer.Value);

        public static int Hash(int value)
        {
            unchecked
            {
                // Simple hash mixing for a single integer
                value ^= value >> 16;
                value *= -2048144709; // 0x85ebca6b as int
                value ^= value >> 13;
                value *= -1028477387; // 0xc2b2ae35 as int
                value ^= value >> 16;
                return value;
            }
        }

        public override string GetSummary() => "Hash {_integer} -> {_output}";
    }
}
