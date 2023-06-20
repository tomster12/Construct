
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;


public class ClassInspectorObject : ClassInspector
{
    [SerializeField] private ConstructObject obj;


    protected virtual void Start()
    {
        AddVariable("controlledBy", "null");
        AddVariable("isConstructed", "false");
        AddVariable("isControlled", "false");
    }

    protected void Update()
    {
        SetVariable("controlledBy", obj.controlledBy == null ? "NA" : obj.controlledBy.GetControllerType().ToString());
        SetVariable("isConstructed", obj.isConstructed.ToString());
        SetVariable("isControlled", obj.isControlled.ToString());
    }
}
