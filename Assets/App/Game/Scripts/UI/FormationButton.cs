using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationButton : MonoBehaviour
{
    [SerializeField] FormationType type;
    [SerializeField] MovementManager movementManager;
    public void Set()
    {
        movementManager.SetFormationType(type);
    }
}
