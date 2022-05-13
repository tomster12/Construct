﻿
using System.Collections;
using UnityEngine;


public class CCMovementHover : COMovementHover, ICCMovement
{
    // Declare references, variables
    [SerializeField] private CameraEffects camFX;
    [SerializeField] private AudioClip coreAttachSFX;
    [SerializeField] private AudioClip coreChargeSFX;
    private AudioSource sfxAudio;
    private ConstructCore baseCC;


    protected override void Awake()
    {
        base.Awake();

        // Initialize references
        SetConstructCore(GetComponent<ConstructCore>());
        sfxAudio = gameObject.AddComponent<AudioSource>();
    }


    public void AttachCore(ConstructObject targetCO, Vector3 targetPos) { StartCoroutine(AttachCoreIE(targetCO, targetPos)); }

    public IEnumerator AttachCoreIE(ConstructObject targetCO, Vector3 targetPos)
    {
        if (baseCC.state == CoreState.Detached)
        {
            // Turn off physics / colliders, update state
            baseCC.state = CoreState.Attaching;
            baseCC.baseWO.rb.useGravity = false;
            baseCC.baseWO.rb.isKinematic = true;
            baseCC.baseWO.cl.enabled = false;
            if (coreChargeSFX != null) sfxAudio.PlayOneShot(coreChargeSFX);
            overrideControl = true;

            // Move backwards, start spinning and point at targetCO
            Coroutine moveBackwardsCR = StartCoroutine(_AttachCoreIEMoveBackwards(targetCO, targetPos));
            Coroutine lookAtCR = StartCoroutine(_AttachCoreIELookAt(targetCO, targetPos));
            yield return moveBackwardsCR;
            yield return lookAtCR;

            // Jab forwards into targetCO
            Coroutine jabIntoCR = StartCoroutine(_AttachCoreIEJabInto(targetCO, targetPos));
            yield return jabIntoCR;

            // Play VFX (chromatic aberration / camera shake) and play SFX
            StartCoroutine(camFX.Vfx_Shake(0.15f, 0.05f));
            StartCoroutine(camFX.Vfx_Chromatic(0.4f, 0.65f));
            if (coreAttachSFX != null) sfxAudio.PlayOneShot(coreAttachSFX);

            // Update parent object, pass over control, update state
            baseCC.state = CoreState.Attached;
            targetCO.SetConstruct(baseCC.construct);
            targetCO.SetControlled(true);
            baseCC.SetControlled(false);
            baseCC.baseWO.transform.parent = targetCO.transform;
            baseCC.baseWO.rb.useGravity = false;
            baseCC.baseWO.rb.isKinematic = true;
            baseCC.attachedCO = targetCO;
            overrideControl = false;
        }
    }

    private IEnumerator _AttachCoreIEMoveBackwards(ConstructObject targetCO, Vector3 targetPos)
    {
        // Initialize variables
        Vector3 rawOffset = Quaternion.Inverse(targetCO.transform.rotation) * (targetPos - targetCO.transform.position);
        float startDist = (targetPos - baseCC.transform.position).magnitude;
        Vector3 dir, start, end;

        // Move towards a point which is start + 1.0 distance away
        for (float t = 0, movePct; t < 0.65f;)
        {
            Vector3 newTargetPos = targetCO.transform.position + targetCO.transform.rotation * rawOffset;
            dir = newTargetPos - baseCC.transform.position;
            start = newTargetPos + -dir.normalized * startDist;
            end = start + -dir.normalized * 1.0f;

            movePct = Easing.EaseOutSine(Mathf.Min(t, 0.65f) / 0.65f);
            baseCC.baseWO.transform.position = Vector3.Lerp(start, end, movePct);

            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator _AttachCoreIELookAt(ConstructObject targetCO, Vector3 targetPos)
    {
        // Initialize variables
        Vector3 rawOffset = Quaternion.Inverse(targetCO.transform.rotation) * (targetPos - targetCO.transform.position);
        Vector3 dir, startUp = baseCC.baseWO.transform.up;

        // Lerp rotate local y towards targetCO, lerp rotate around local y
        for (float t = 0, aimPct, spinPct; t < 0.85f;)
        {
            Vector3 newTargetPos = targetCO.transform.position + targetCO.transform.rotation * rawOffset;
            dir = newTargetPos - baseCC.transform.position;

            aimPct = Easing.EaseOutSine(Mathf.Min(t / 0.65f, 1.0f));
            spinPct = Easing.EaseInSine(Mathf.Min(t / 0.85f, 1.0f));
            baseCC.baseWO.transform.up = Vector3.Lerp(startUp, dir, aimPct);
            baseCC.baseWO.transform.rotation *= Quaternion.AngleAxis(360 * spinPct, Vector3.up);

            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator _AttachCoreIEJabInto(ConstructObject targetCO, Vector3 targetPos)
    {
        // Initialize variables
        Vector3 rawOffset = Quaternion.Inverse(targetCO.transform.rotation) * (targetPos - targetCO.transform.position);
        Vector3 dir;
        float speed;

        // Raycast then move towards targetCO
        // TODO: Fix and change to trigger / collision based
        while (true)
        {
            Vector3 newTargetPos = targetCO.transform.position + targetCO.transform.rotation * rawOffset;
            dir = newTargetPos - baseCC.transform.position;

            speed = 12.0f * Time.deltaTime;
            bool reached = dir.magnitude < speed;
            baseCC.baseWO.transform.position += dir.normalized * Mathf.Min(dir.magnitude, speed);

            if (reached) break;
            yield return null;
        }
    }


    public void DetachCore() { StartCoroutine(DetachCoreIE()); }

    public IEnumerator DetachCoreIE()
    {
        Vector3 popDir = (baseCC.baseWO.transform.position - baseCC.attachedCO.transform.position).normalized;

        // Detach but without control
        baseCC.state = CoreState.Detaching;
        baseCC.baseWO.rb.isKinematic = false;
        baseCC.baseWO.rb.useGravity = true;
        baseCC.baseWO.cl.enabled = true;

        // Apply popping force and torque and wait 0.5s
        float prevDrag = baseCC.baseWO.rb.angularDrag;
        baseCC.baseWO.rb.angularDrag = 0.0f;
        baseCC.baseWO.rb.AddForce(popDir * 2.5f, ForceMode.VelocityChange);
        baseCC.baseWO.rb.AddTorque(transform.right * 15.0f, ForceMode.VelocityChange); // FIX
        yield return new WaitForSeconds(0.5f);

        // Reactive moveset and angular drag
        baseCC.state = CoreState.Detached;
        baseCC.attachedCO.SetConstruct(null);
        baseCC.attachedCO.SetControlled(false);
        baseCC.SetControlled(true);
        baseCC.baseWO.rb.angularDrag = prevDrag;
        baseCC.attachedCO = null;
    }


    private void SetConstructCore(ConstructCore baseCC_) { SetConstructObject(baseCC_); baseCC = baseCC_; }
}
