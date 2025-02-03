using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SyncedAnimation : MonoBehaviour
{
    //The animator controller attached to this GameObject
    private Animator animator;
    //Array containing all of my animations
    private AnimationClip[] clips;

    //Records the animation state or animation that the Animator is currently in
    private AnimatorStateInfo animatorStateInfo;

    //Used to address the current state within the Animator using the Play() function
    private int currentState;

    public bool onEnable = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        clips = animator.runtimeAnimatorController.animationClips;
    }

    void Start()
    {
        //Get the info about the current animator state
        animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        //Convert the current state name to an integer hash for identification
        currentState = animatorStateInfo.fullPathHash;

        //Start playing the current animation from wherever the current conductor loop is
        animator.Play(currentState, -1, RythmManager.instance.animationOffset);

        //Set the animation be to the bpm of the rythm manager
        animator.SetFloat("animSpeed", RythmManager.instance.songBpm/120 * 2);
        
        //Beat event
        RythmManager.instance.BeatEvent += OnBeatEvent;
    }

    private void OnDisable() 
    {
        onEnable = true;
    }

    private void OnEnable()
    {
        if(onEnable)
        {
            //Get the info about the current animator state
            animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            //Convert the current state name to an integer hash for identification
            currentState = animatorStateInfo.fullPathHash;

            //Start playing the current animation from wherever the current conductor loop is
            animator.Play(currentState, -1, RythmManager.instance.animationOffset);

            //Set the animation be to the bpm of the rythm manager
            animator.SetFloat("animSpeed", RythmManager.instance.songBpm/120 * 2);
        }
    }

    void Update()
    {
    }

    void OnBPMChange()
    {
        animator.SetFloat("animSpeed", RythmManager.instance.songBpm/120);
    }
}
