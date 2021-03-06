﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Common;
using MarsPC;

public class CharacterTriggerBehaviour : MonoBehaviour
{
    public UnityAction<Collider2D> OnEnterCall2D;
    public UnityAction<Collider2D> OnStayCall2D;
    public UnityAction<Collider2D> OnExitCall2D;

    public UnityAction<Collider> OnEnterCall;
    public UnityAction<Collider> OnStayCall;
    public UnityAction<Collider> OnExitCall;

    public Dictionary<TriggerColliderBase, List<TriggerBehaviourBase>> triggerDic = new Dictionary<TriggerColliderBase, List<TriggerBehaviourBase>>();

    private void OnTriggerEnter(Collider other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerEnterCall(transform);
        }

        OnEnterCall?.Invoke(other);
    }

    private void OnTriggerStay(Collider other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerStayCall(transform);
        }

        OnStayCall?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerExitCall(transform);
        }

        OnExitCall?.Invoke(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerEnter2DCall(transform);
        }

        OnEnterCall2D?.Invoke(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerStay2DCall(transform);
        }

        OnStayCall2D?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        List<TriggerBehaviourBase> triggerList = GetTriggerBases(other);
        if (triggerList == null || triggerList.Count == 0) return;

        for (int i = 0; i < triggerList.Count; i++)
        {
            triggerList[i].OnTriggerExit2DCall(transform);
        }

        OnExitCall2D?.Invoke(other);
    }

    private List<TriggerBehaviourBase> GetTriggerBases(Collider other)
    {
        TriggerColliderBase[] colliders = new TriggerColliderBase[triggerDic.Count];
        triggerDic.Keys.CopyTo(colliders, 0);
        TriggerColliderBase collider = colliders.Find(t => t.collider == other);
        if (collider != null) return triggerDic[collider];
        return null;
    }

    private List<TriggerBehaviourBase> GetTriggerBases(Collider2D other)
    {
        TriggerColliderBase[] colliders = new TriggerColliderBase[triggerDic.Count];
        triggerDic.Keys.CopyTo(colliders, 0);
        TriggerColliderBase collider = colliders.Find(t => t.collider2D == other);
        if (collider != null) return triggerDic[collider];
        return null;
    }
}

public enum ETriggerTargetTag
{
    Player = 1 << 0,
    Enemy = 1 << 1,
}

public enum ETriggerType
{
    InActive,//激活状态，可触发
    Actived,//已触发过，不再触发
    Active,//在激活状态中，可继续触发
}

[Serializable]
public class TriggerColliderBase
{
    public Collider collider;
    public Collider2D collider2D;

    public TriggerColliderBase(Collider collider, Collider2D collider2D)
    {
        this.collider = collider;
        this.collider2D = collider2D;
    }
}