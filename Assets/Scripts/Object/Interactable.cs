using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public virtual void Awake()
    {
        gameObject.layer = 6; // this will be the layer of which the interactables are on
    }

    public abstract void OnInteract(RaycastHit hitInfo, FirstPersonController firstPersonControllerRef = null);

    public abstract void OnFocus();

    public abstract void OnLoseFocus();
}
