using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SearchAreaPanel : MonoBehaviour
{
    public CCFTreeNode Node { get; private set; }

    public void SetNode(CCFTreeNode node)
    {
        Node = node;
    }
}
