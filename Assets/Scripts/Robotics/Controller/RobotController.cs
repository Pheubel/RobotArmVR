using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RobotController : MonoBehaviour
{

    public Transform[] bones = new Transform[8];

    public int selectedBone = 0;

    public float moveSpeed;
    public float rotateSpeed;

    public float modifier;

    private Vector3 axis;

    private XRCustomController controllerLeft, controllerRight;

    private CustomInteractor interactor;

    [field: SerializeField]
    private TextUpdater textUpdater;

    [SerializeField]float minTreshHoldTrigger, maxTreshHoldTrigger;


    [field: SerializeField]
    private Image testImage;

    private bool pressureButtonHeld = false;

    private void Start()
    {
        controllerLeft = GameObject.FindGameObjectWithTag("ControllerLeft").GetComponent<XRCustomController>();
        controllerRight = GameObject.FindGameObjectWithTag("ControllerRight").GetComponent<XRCustomController>();
        controllerLeft.triggerHoldAction.action.performed += TriggerValue;
        controllerLeft.triggerHoldAction.action.canceled += TriggerValue;
        controllerRight.thumbstickValueAction.action.performed += ThumbstickAction;
        controllerRight.changeAxisAction.action.started += ChangeAxisAction;
        interactor = GetComponent<CustomInteractor>();
    }

    private void ChangeAxisAction(InputAction.CallbackContext obj)
    {
        selectedBone = (selectedBone + 1) % bones.Length;
        ChangeAxis();
        textUpdater.UpdateText((selectedBone + 1).ToString());
    }

    private void TriggerValue(InputAction.CallbackContext obj)
    {
        if (interactor.HeldObject == null)
            return;
        if (interactor.HeldObject.transform.name == "Flexpendant")
        {
            pressureButtonHeld = obj.ReadValue<float>() == 1 ? true : false;
            testImage.color = pressureButtonHeld ? Color.green : Color.red;
        }
    }

    private void ThumbstickAction(InputAction.CallbackContext obj)
    {
        modifier = obj.ReadValue<Vector2>().x;
    }


    private void FixedUpdate()
    {
        if (Math.Abs(modifier) > 0.01 && pressureButtonHeld)
        {
            bones[selectedBone].Rotate(axis, rotateSpeed * modifier * Time.deltaTime);
        }
    }

    private void ChangeAxis()
    {
        if (selectedBone == 0 || selectedBone == 1)
        {
            axis = Vector3.forward;
        }
        else if (selectedBone == 3 || selectedBone == 5)
        {
            axis = Vector3.up;
        }
        else if (selectedBone == 2 || selectedBone == 4)
        {
            axis = Vector3.right;
        }
        else
        {
            throw new ArgumentException("Selected bone case unsupported.");
        }
    }
}
