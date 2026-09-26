using System;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [Serializable]
    [ActionCategory(Category.Hash)]
    [ActionDescription("Hashes a string into an integer value. Internally uses Unity’s Animator.StringToHash.")]
    public class HashString : BaseAction
    {
        [Tooltip("The string to hash.")]
        [SerializeField]
        private StringVar _string;

        [Tooltip("The hashed value.")]
        [SerializeField, WriteOnly]
        private IntegerRef _output;

        public override bool CanExecute() => CheckParameters(_string, _output);

        public override void Execute() => _output.Value = Animator.StringToHash(_string.Value);

        public override string GetSummary() => "Hash {_string} -> {_output}";
    }
}