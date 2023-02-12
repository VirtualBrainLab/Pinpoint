using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class QuickSettingsMultiDropdown : Dropdown
{
    bool[] toggleIsOn;
    public bool[] IsOnList { get { return toggleIsOn; } set { toggleIsOn = value; } }
    Toggle[] toggleList;
    public Toggle[] ToggleList { get { return toggleList; } private set { } }

    Animator animator;

    public UnityEvent<int, Toggle> toggleEvent;

    public void UpdateToggleList()
    {
        Transform contentTransform = transform.Find("Dropdown List/Viewport/Content");
        if (contentTransform != null)
        {
            toggleList = contentTransform.GetComponentsInChildren<Toggle>(false);

            if (toggleIsOn == null)
            {
                toggleIsOn = new bool[toggleList.Length];
            }

            bool[] prevToggleState = toggleIsOn;
            toggleIsOn = new bool[toggleList.Length];
            for (int i = 0; i < prevToggleState.Length; i++)
                toggleIsOn[i] = prevToggleState[i];

            for (int i = 0; i < toggleList.Length; i++)
            {
                Toggle item = toggleList[i];
                item.onValueChanged.RemoveAllListeners();
                item.isOn = toggleIsOn[i];
                item.onValueChanged.AddListener(x => OnSelectItemCustom(item));
            }
        }
    }

    public void Show()
    {
        base.Show();

        UpdateToggleList();

        if(animator == null)
        {
            Transform listTransform = transform.Find("Dropdown List");
            animator = listTransform.gameObject.GetComponent<Animator>();                
        } 
        PlayAnimation(true);
    }

    public void Hide()
    {
        if(animator == null)
        {
            Transform listTransform = transform.Find("Dropdown List");
            animator = listTransform.gameObject.GetComponent<Animator>();                
        } 
        PlayAnimation(false);
        base.Hide();  
    }

    void PlayAnimation(bool bStart)
    {
        if(animator != null)
        {
            if(animator.enabled == false)
            {
                animator.enabled = true;
            }
            if(bStart)
            {
                animator.Play("In",0,0);  
            }
            else
            {
                animator.Play("Out",0,0);  
            }
        }            
    }     

    public override void OnPointerClick(PointerEventData eventData)
    {
        Show();
    }

    private void OnSelectItemCustom(Toggle toggle)
    {
        int selectedIndex = -1;
        Transform tr = toggle.transform;
        Transform parent = tr.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) == tr)
            {
                selectedIndex = i - 1;
                break;
            }
        }
        if (selectedIndex < 0)
            return;
        toggleIsOn[selectedIndex] = toggle.isOn;
        toggleEvent.Invoke(selectedIndex, toggle);
    }
}