using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuickSettingsLockBehavior : MonoBehaviour
{
    [SerializeField] private Image _sprite;
    [SerializeField] private Sprite _lockSprite;
    [SerializeField] private Sprite _unlockSprite;

    public UnityEvent<bool> LockEvent;
    private bool _locked = false;

    public void InvokeLockEvent()
    {
        _locked = !_locked;
        UpdateSprite();
        LockEvent.Invoke(_locked);
    }

    public void SetLockState(bool state)
    {
        _locked = state;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_locked)
            _sprite.sprite = _lockSprite;
        else
            _sprite.sprite = _unlockSprite;
    }
}
