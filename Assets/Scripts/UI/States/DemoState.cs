using Core.Util;
using Pinpoint.Probes;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.States
{
    public enum DemoStage
    {
        Home,
        Bregma,
        Entry,
        Dura,
        Target,
    }
    [CreateAssetMenu]
    public class DemoState : ResettingScriptableObject
    {
        public DemoStage Stage = DemoStage.Home;
    }
}
