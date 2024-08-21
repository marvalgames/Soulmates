using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PowerUpTest : MonoBehaviour
{
    public Animator anim;
    public VisualEffect levelUp;
    public AudioSource audioSource;
    public AudioClip chargingSFX;
    public AudioClip buffSFX;
    public float delay = 0.5f;

    private bool levelingUp;

    void Update()
    {
        if(anim != null)
        {
            if (Input.GetButtonDown("Fire1") && !levelingUp)
            {
                anim.SetTrigger("PowerUp");

                if(chargingSFX != null)
                    audioSource.PlayOneShot(chargingSFX);

                if (levelUp != null)
                    levelUp.Play();

                levelingUp = true;
                StartCoroutine (ResetBool(levelingUp, delay));
            }
        }
    }

    IEnumerator ResetBool (bool boolToReset, float delay = 0.1f)
    {
        yield return new WaitForSeconds(delay);

        if(buffSFX != null)
            audioSource.PlayOneShot(buffSFX);

        levelingUp = !levelingUp;
    }
}
