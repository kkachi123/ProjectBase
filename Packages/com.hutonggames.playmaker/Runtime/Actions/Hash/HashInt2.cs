using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Generates a hash value from two integers using a simple hash function.")]
    public class HashInt2 : BaseAction
    {
        [Tooltip("The first integer.")]
        [SerializeField]
        private IntegerVar _int1;
        
        [Tooltip("The second integer.")]
        [SerializeField]
        private IntegerVar _int2;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_int1, _int2, _output);

        public override void Execute() => _output.Value = Hash(_int1.Value, _int2.Value);

        public static int Hash(int x, int y)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                return hash;
            }
        }

        public override string GetSummary() => "Hash {_int1}, {_int2} -> {_output}";
    }
}