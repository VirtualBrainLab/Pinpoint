using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace UI.States
{
    public class LeftSidePanelState : IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        #region State

        private bool _isPanelVisible = true;

        #endregion

        #region Properties

        [CreateProperty]
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                if (_isPanelVisible == value) return;
                _isPanelVisible = value;
                NotifyPropertyChanged();
            }
        }
        
        #endregion

        #region Management

        private long _viewVersion;
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        #endregion

        #region Management Functions

        public void Publish()
        {
            ++_viewVersion;
        }

        public long GetViewHashCode()
        {
            return _viewVersion;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}