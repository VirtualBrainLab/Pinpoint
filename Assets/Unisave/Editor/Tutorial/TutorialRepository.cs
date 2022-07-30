using System.Collections.Generic;

namespace Unisave.Editor.Tutorial
{
    public static class TutorialRepository
    {
        public static Dictionary<string, Tutorial> tutorials
            = new Dictionary<string, Tutorial> {
                ["Examples/PlayerAuthentication"]
                    = new PlayerAuthenticationTutorial()
            };
    }
}