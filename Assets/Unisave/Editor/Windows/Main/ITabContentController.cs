using System;

namespace Unisave.Editor.Windows.Main
{
    public interface ITabContentController
    {
        /// <summary>
        /// Sets the rendered tab taint value
        /// </summary>
        public Action<TabTaint> SetTaint { get; set; }

        /// <summary>
        /// Called after tab controller creation to query all elements
        /// and register needed events. You can also load any additional
        /// UXML assets.
        /// </summary>
        public void OnCreateGUI();
        
        /// <summary>
        /// Called when the contents of the tab should observe the external
        /// reality that the tabs displays and update itself accordingly
        /// </summary>
        public void OnObserveExternalState();

        /// <summary>
        /// Called when the modified content of the tab should be saved,
        /// writing its content into the external reality
        /// </summary>
        public void OnWriteExternalState();
    }
}