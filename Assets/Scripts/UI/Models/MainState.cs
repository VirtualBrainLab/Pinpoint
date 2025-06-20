using System;
using UnityEngine;

namespace UI.Models
{
    [Serializable]
    public record MainState
    {
        [SerializeField] public MainModes Mode;
    }
}