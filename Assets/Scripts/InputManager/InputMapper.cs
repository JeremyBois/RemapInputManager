using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class InputMapper : MonoBehaviour
{
    [SerializeField]
    InputManager inputMgr;

    private KeyCode keyPressed;
    private string  axisPressed;
    private bool    isWaiting;


    private IEnumerator coMapper;

    // Raised when player is check.
    public event Action<KeyCode> KeyPressed = delegate { };
    public event Action<string>  AxisPressed = delegate { };

    private HashSet<string> dontTouchAxis;
    private float deadZone = 0.1f;

    void Awake()
    {
        dontTouchAxis = new HashSet<string>
        {
            // Player 1
            "Joy_1_Axis_3",  // CamHorizontal
            "Joy_1_Axis_4",  // CamVertical
            "Joy_1_Axis_0",  // Horizontal
            "Joy_1_Axis_1",  // Vertical

            "Joy_1_Axis_2",  // Bumper unused

            // Player 2
            "Joy_2_Axis_3",  // CamHorizontal
            "Joy_2_Axis_4",  // CamVertical
            "Joy_2_Axis_0",  // Horizontal
            "Joy_2_Axis_1",  // Vertical

            "Joy_2_Axis_2",  // Bumper unused

        };

        axisPressed = string.Empty;
        keyPressed = KeyCode.None;
    }

    void Start()
    {
        isWaiting = false;
        coMapper = null;
    }

    IEnumerator WaitForKeyOrAxisPressed()
    {
        isWaiting = true;

        // Get all joystick axis
        var allAxis = InputManager.GetUnityJoystickAxis(PlayerID.One);
        allAxis.UnionWith(InputManager.GetUnityJoystickAxis(PlayerID.Two));
        allAxis.ExceptWith(dontTouchAxis);

        while (keyPressed == KeyCode.None && axisPressed == string.Empty)
        {
            foreach (KeyCode existingKey in InputManager.UnityKeyCodes)
            {
                if (Input.GetKeyDown(existingKey))
                {
                    keyPressed = existingKey;

                    KeyPressed.Invoke(keyPressed);

                    isWaiting = false;
                    keyPressed = KeyCode.None;
                    yield break;
                }
            }
            foreach (string axis in allAxis)
            {
                if (Input.GetAxisRaw(axis) >= deadZone || Input.GetAxisRaw(axis) <= -deadZone)
                {
                    axisPressed = axis;

#if (UNITY_WSA)
                AxisPressed.Invoke(string.Concat("", axisPressed));
#else
                AxisPressed.Invoke(string.Copy(axisPressed));
#endif


                    isWaiting = false;
                    axisPressed = string.Empty;
                    yield break;
                }
            }

            yield return null;
        }
    }

    public void GetKeyOrAxisPressed()
    {
        if (!isWaiting)
        {
            if (coMapper != null)
            {
                StopCoroutine(coMapper);
            }

            // Must be redefine
            coMapper = WaitForKeyOrAxisPressed();
            StartCoroutine(coMapper);
        }
    }
}
