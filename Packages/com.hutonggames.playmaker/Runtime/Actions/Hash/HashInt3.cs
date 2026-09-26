using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Generates a hash value from three integers using a hash function.")]
    public class HashInt3 : BaseAction
    {
        [Tooltip("The first integer.")]
        [SerializeField]
        private IntegerVar _int1;
        
        [Tooltip("The second integer.")]
        [SerializeField]
        private IntegerVar _int2;
        
        [Tooltip("The third integer.")]
        [SerializeField]
        private IntegerVar _int3;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_int1, _int2, _int3, _output);

        public override void Execute() => _output.Value = Hash(_int1.Value, _int2.Value, _int3.Value);

        public static int Hash(int x, int y, int z)
        {
            unchecked
            {
                return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
            }
        }

        public override string GetSummary() => "Hash {_int1}, {_int2}, {_int3} -> {_output}";
    }
}
