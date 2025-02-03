using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;
using System.Linq;

public class MusicalAttack : MonoBehaviour
{
    public delegate void musicalTimerDelegate();
    public event musicalTimerDelegate musicalEnded;

    private Collider _colMusical;
    private bool _musicalZoneApplied;
    private List<EnemyActions> _enemyInZone;
    private List<Mannequin> _dummyInZone;
    private int _musicalAttackDamage = 25;
    private int[] _musicalAttackDamageArray;
    private int _nbZonesColliding = 0;
    private int _currentBeat = 0;
    private int _musicalZoneEffectDuration = 4;

    [SerializeField] public PlayerActions actions;
    public MMF_Player startATKPreview, stopATKPreview, successATKPreview, startATK, bumpATK, stopATK, musicalCollide;

    void Awake()
    {
        _colMusical = GetComponent<Collider>();
    }

    void Start()
    {
        _colMusical.enabled = false;
        _musicalZoneApplied = false;
        _currentBeat = 0;
        _enemyInZone = new List<EnemyActions>();
        _dummyInZone = new List<Mannequin>();
        

        startATKPreview.PlayFeedbacks();
    }

    public void SetPlayerActions(PlayerActions _actions)
    {
        actions = _actions;
        _musicalZoneEffectDuration = actions.data.musicalZoneEffectDuration;
        _musicalAttackDamageArray = actions.data.musicalAttackDamageArray;
        _musicalAttackDamage = _musicalAttackDamageArray[0];
    }

    public void FailMusicalAttack()
    {
        RythmManager.instance.BeatEvent -= OnBeatEvent;
        stopATKPreview.PlayFeedbacks();
    }

    public void ApplyMusicalAttack()
    {
        _musicalZoneApplied = true;

        //Play/Change VFX of the zone appearing
        successATKPreview.PlayFeedbacks();
        startATK.PlayFeedbacks();
        
        _colMusical.enabled = true;
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Enemy"))
        {
            EnemyActions enemy = col.GetComponent<EnemyActions>();
            _enemyInZone.Add(enemy);
        }

        if (col.CompareTag("Musical"))
        {
            MusicalZonesCollide(col);
        }

        if(col.CompareTag("Mannequin"))
        {
            Mannequin maq = col.GetComponent<Mannequin>();
            _dummyInZone.Add(maq);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag("Enemy"))
        {
            EnemyActions enemy = col.GetComponent<EnemyActions>();
            if (_enemyInZone.Contains(enemy))
            {
                _enemyInZone.Remove(enemy);
            }
        }

        if (col.CompareTag("Musical"))
        {
            _nbZonesColliding -= 1;
            _musicalAttackDamage = _musicalAttackDamageArray[_nbZonesColliding];
        }

        if(col.CompareTag("Mannequin"))
        {
            Mannequin maq = col.GetComponent<Mannequin>();
            if (_dummyInZone.Contains(maq))
            {
                _dummyInZone.Remove(maq);
            }
        }
    }

    private void MusicalZonesCollide(Collider col)
     {
        _nbZonesColliding += 1;
        _musicalAttackDamage = _musicalAttackDamageArray[_nbZonesColliding];
        musicalCollide.PlayFeedbacks();
     }

    private Vector3 FindLineIntersection(Vector3 start1, Vector3 dir1, Vector3 start2, Vector3 dir2)
    {
        Vector3 intersection = Vector3.zero;

        Vector3 p1 = start1;
        Vector3 p2 = start2;
        Vector3 d1 = dir1;
        Vector3 d2 = dir2;

        Vector3 diff = p2 - p1;
        Vector3 crossD1D2 = Vector3.Cross(d1, d2);
        Vector3 crossDiffD2 = Vector3.Cross(diff, d2);

        float denominator = crossD1D2.magnitude;
        if (denominator < Mathf.Epsilon)
        {
            // Lines are parallel or coincident
            return Vector3.zero;
        }

        float t = crossDiffD2.magnitude / denominator;
        intersection = p1 + t * d1;

        return intersection;
    }

    public void OnBeatEvent()
    {
        //Calls the start zone VFX
        if (_musicalZoneApplied == false)
        {
            startATKPreview.PlayFeedbacks();
        }
        //Increment the zone applied and hit all enemies
        else
        {
            bumpATK.PlayFeedbacks();
            _currentBeat += 1;

            for (int i = _enemyInZone.Count - 1; i >= 0; i --)
            {
                if (_enemyInZone[i] != null) _enemyInZone[i].TakeDamage(_musicalAttackDamage);
                else _enemyInZone.Remove(_enemyInZone[i]);
            }

            for (int i = _dummyInZone.Count - 1; i >= 0; i --)
            {
                if (_dummyInZone[i] != null) _dummyInZone[i]._animationEvents.OnHit();
                else _dummyInZone.Remove(_dummyInZone[i]);
            }

            if (_currentBeat == _musicalZoneEffectDuration)
            {
                musicalEnded();
                _colMusical.enabled = false;
                RythmManager.instance.BeatEvent -= OnBeatEvent;
                stopATK.PlayFeedbacks();
            }
        }
    }
}
