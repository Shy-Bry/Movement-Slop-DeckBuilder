using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Dashing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Dashing")]
    public float dashForce;
    public float dashUpwardForce;
    public float dashDuration;
    public float backPeddleForce;

    [Header("Setting")]
    public bool useCameraForward = true;
    public bool allowAllDirections = true;
    

    [Header("Cooldown")]
    public float dashCd;
    public float dashCdTimer;
    
    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if(Input.GetKeyDown(dashKey))
            Dash();

        if(dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;
    }

    private void Dash()
    {
        if(dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        pm.dashing = true;

        UnityEngine.Vector3 forceToApply = orientation.forward * dashForce + orientation.up * dashUpwardForce;

        UnityEngine.Vector3 backPeddle = orientation.forward * -backPeddleForce;

        delayedForceToApply = forceToApply;

        delayedBackPeddle = backPeddle;

        Invoke(nameof(DelayedDashForce), 0.025f);

        Invoke(nameof(ResetDash), dashDuration);
    }   

    private UnityEngine.Vector3 delayedForceToApply;
    private UnityEngine.Vector3 delayedBackPeddle;

    private void DelayedDashForce()
    {
        rb.AddForce(delayedBackPeddle, ForceMode.Impulse);
        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        pm.dashing = false;
    }
}