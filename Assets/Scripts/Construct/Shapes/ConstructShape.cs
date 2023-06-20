﻿
using UnityEngine;


public class ConstructShape : MonoBehaviour, IObjectController
{
    public bool isActive { get; private set; }
    public virtual bool canActivate => true;


    public virtual bool SetActive(bool isActive_)
    {
        if (isActive == isActive_) return false;
        if (!canActivate && isActive_) return false;
        isActive = isActive_;
        return true;
    }


    public ObjectControllerType GetControllerType() => ObjectControllerType.SHAPE;
}


public class ShapeCoreAttachment : ConstructShape
{
    public override bool canActivate => base.canActivate && attachedCO != null && attachingCC != null;
    public ConstructCore attachingCC {  get; private set; }
    public ConstructObject attachedCO { get; private set; }


    public override bool SetActive(bool isActive_)
    {
        if (!base.SetActive(isActive_)) return false;
        if (isActive)
        {
            attachingCC.SetControlledBy(this);
            attachingCC.OnJoinShape(this);
            attachedCO.OnJoinShape(this);
        }
        else
        {
            attachingCC.SetControlledBy(null);
            attachingCC.OnLeaveShape(this);
            attachedCO.OnLeaveShape(this);
        }
        return true;
    }

    public void SetAttachingCC(ConstructCore attachingCC_)
    {
        if (attachingCC == attachingCC_) return;
        if (attachingCC != null && attachingCC_ != null) throw new System.Exception("Cannot SetAttachingCC() if already set.");
        attachingCC = attachingCC_;
    }
    
    public void SetAttachedCO(ConstructObject attachedCO_)
    {
        if (attachedCO == attachedCO_) return;
        if (attachedCO != null && attachedCO_ != null) throw new System.Exception("Cannot SetAttachedCO() if already set.");
        attachedCO = attachedCO_;
    }

    public void Clear()
    {
        // Clear variables
        attachingCC = null;
        attachedCO = null;
    }
}


public class ShapeHoverAttachment : ShapeCoreAttachment
{
    public override bool SetActive(bool isActive_)
    {
        if (!base.SetActive(isActive_)) return false;
        
        // Update attaching CC
        if (isActive)
        {
            attachingCC.baseWO.SetLoose(false);
            attachingCC.baseWO.SetFloating(true);
            attachingCC.baseWO.SetColliding(false);
            attachingCC.transform.parent = attachedCO.transform;
        }
        else
        {
            attachingCC.transform.parent = attachingCC.construct.transform;
        }

        return true;
    }
}