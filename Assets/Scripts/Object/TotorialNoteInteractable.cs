using Controls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotorialNoteInteractable : Interactable
{
    [SerializeField] private GameObject UInote;
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private AudioSource noteAudioSource;
    [SerializeField] private AudioClip[] crumppleSFXs;

    private void Start()
    {
        UInote.SetActive(false);
    }

    public override void OnFocus()
    {
    }

    public override void OnInteract(RaycastHit hitInfo, FirstPersonController firstPersonControllerRef = null)
    {
        StartCoroutine(enableDisableNote());
    }

    private IEnumerator enableDisableNote()
    {
        //unlock coursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        noteAudioSource.PlayOneShot(crumppleSFXs[UnityEngine.Random.Range(0, crumppleSFXs.Length)]);
        UInote.SetActive(true);

        yield return new WaitForFixedUpdate();
        
        while (true)
        {
            if (Input.GetKeyDown(playerControls.CloseUIPrompt) || Input.GetMouseButtonDown(0))
            {
                UInote.SetActive(false);
                //lock coursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            }

            yield return null;
        }
    }

    public override void OnLoseFocus()
    {
    }
}
