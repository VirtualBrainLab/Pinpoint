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

    public void UpdateSprite(bool locked)
    {
        _sprite.sprite = locked ? _lockSprite : _unlockSprite;
    }
}
