using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static HashSet<TMP_InputField> FocusableInputs = new();
    public static HashSet<GameObject> FocusableGOs = new();

    [SerializeField] private List<TMP_InputField> EditorFocusableInputs;
    [SerializeField] private List<GameObject> EditorFocusableGOs;

    private void Awake()
    {
        FocusableInputs.UnionWith(EditorFocusableInputs);
        FocusableGOs.UnionWith(EditorFocusableGOs);
    }

    public static bool InputsFocused
    {
        get
        {
            return FocusableInputs.Any(x => x.isFocused) || FocusableGOs.Any(x => x.activeSelf);
        }
    }
}
