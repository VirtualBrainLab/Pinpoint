using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SearchAreaPanel : MonoBehaviour
{
    private CCFTreeNode node;

    public void SetNode(CCFTreeNode node)
    {
        this.node = node;
    }

    public CCFTreeNode GetNode()
    {
        return node;
    }
}
