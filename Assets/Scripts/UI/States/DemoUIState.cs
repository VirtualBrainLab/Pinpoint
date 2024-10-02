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
        ReferenceCoordinate,
        Entry,
        Dura,
        Target,
    }
    [CreateAssetMenu]
    public class DemoUIState : ResettingScriptableObject
    {
        public DemoStage Stage = DemoStage.Home;
        
        [CreateProperty]
        public bool IsHomeEnabled => Stage == DemoStage.Home;
        [CreateProperty]
        public bool IsReferenceCoordinateEnabled => Stage == DemoStage.ReferenceCoordinate;
        [CreateProperty]
        public bool IsEntryEnabled => Stage == DemoStage.Entry;
        [CreateProperty]
        public bool IsDuraEnabled => Stage == DemoStage.Dura;
        [CreateProperty]
        public bool IsTargetEnabled => Stage == DemoStage.Target;
    }
}
