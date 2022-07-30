using System;
using System.Collections.Generic;

namespace Unisave.Editor.Tutorial
{
    public class Tutorial
    {
        public readonly List<Action> slides = new List<Action>();

        public Tutorial AddSlide(Action slide)
        {
            slides.Add(slide);
            return this;
        }

        public virtual bool ShouldCloseImmediately()
        {
            return false;
        }
    }
}