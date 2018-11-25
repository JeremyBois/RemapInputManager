using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlugAndPlayHelper : MonoBehaviour
{
    // Raise event on connection or deconnection
    public event Action<int> GamepadDisconnected = delegate {};
    public event Action<int> GamepadReConnected = delegate {};
    public event Action<int> GamepadFirstConnected = delegate {};

    private InputManager inputMgr;

    [HideInInspector]
    public bool _checkControllers;
    private IEnumerator plugHandler;

    private int      nbJoystickPlugged;
    private bool[]   joystickState;

    private void Awake()
    {
        nbJoystickPlugged = Input.GetJoystickNames().Length;

        // Init state with empty for up to four gamepads
        joystickState = new [] {false, false, false, false};
    }

	// Use this for initialization
	private void Start ()
    {
        inputMgr = InputManager.Instance;

        plugHandler = PlugAndPlayHandler();
        _checkControllers = true;
        StartCoroutine(plugHandler);
	}

    private void CorrectGamepadsAssignation()
    {
        // Assign correct index for controllers
        int[] padShift = {0, 0, 0, 0};
        int padID = 0;

        // Compute index shift for each Gamepad
        for (int controllerID = 0; controllerID < padShift.Length; controllerID++)
        {
            // defaultController[(int)pID - 1]
            var pID = (PlayerID)(controllerID + 1);

            if (inputMgr.GetInputController(pID) == ControllerType.Gamepad)
            {
                bool updated = false;

                foreach (AxisConfig conf in inputMgr.GetInputConfig(pID).Axes.Values)
                {
                    bool hasChange = conf.AdjustJoystickConfig(pID, padShift[padID]);
                    if (hasChange)
                    {
                        updated = true;
                    }
                }

                // Raise event to notify of configuration changes
                if (updated)
                {
                    InputManager.RaiseEventConfigurationChange(pID);
                }

                // Switch to next gamepad
                padID++;
            }

            if (inputMgr.GetInputController(pID) == ControllerType.Keyboard)
            {
                padShift[padID]++;
            }
        }
    }

    private void HandleConnectionDeconnection()
    {
        // Get Joystick Names
        string[] joystickNames = Input.GetJoystickNames();

        // Gamepad added
        int count = joystickNames.Length;

        // Handle multiple new connections (!!! first time only !!!!)
        while (nbJoystickPlugged < count)
        {
            nbJoystickPlugged++;
            joystickState[nbJoystickPlugged - 1] = true;
            GamepadFirstConnected.Invoke(nbJoystickPlugged - 1);
            // Debug.Log("New controller connected = " + nbJoystickPlugged);
        }

        // Check for deconnected gamepads
        if (joystickNames.Length > 0)
        {
            // Iterate over every element
            for (int i = 0; i < joystickNames.Length; ++i)
            {
                // Check if the string is empty or not
                if (string.IsNullOrEmpty(joystickNames[i]) && joystickState[i])
                {
                    // i has been disconnected
                    joystickState[i] = false;
                    GamepadDisconnected.Invoke(i);
                    // Debug.Log("Controller: " + i + " is disconnected.");
                }
                else if (!string.IsNullOrEmpty(joystickNames[i]) && !joystickState[i])
                {
                    // i has been reconnected
                    joystickState[i] = true;
                    GamepadReConnected.Invoke(i);
                    // Debug.Log("Controller " + i + " reconnected using: " + joystickNames[i]);
                }
            }
        }
    }

    private IEnumerator PlugAndPlayHandler()
    {
        while (_checkControllers)
        {
            // Check twice more for plug and play
            HandleConnectionDeconnection();
            CorrectGamepadsAssignation();
            yield return new WaitForSeconds(1.0f);
            HandleConnectionDeconnection();
            yield return new WaitForSeconds(1.0f);
        }
    }
}
