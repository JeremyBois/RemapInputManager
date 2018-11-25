using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


// ShortCut
using InputM = InputManager;


public class InputTests : MonoBehaviour
{
    private float horizontalMovement1, horizontalMovement2;

	// Use this for initialization
	void Start ()
    {
        // Add two button for player one and two
        InputM.AddButton(PlayerID.One, "Jump", KeyCode.Alpha1, description:"Player 1 jump interface.");
        InputM.AddButton(PlayerID.Two, "Jump", KeyCode.Alpha2, KeyCode.Space, "Player 2 jump interface.");

        // Add mouse virtual axis for player one
        InputM.AddMouseAxis(PlayerID.One, "Horizontal", 0, 0.2f, false, "Player 1 horizontal mapping");

        // Add a digital axis for player two
        InputM.AddDigitalAxis(PlayerID.Two, "Horizontal", KeyCode.RightArrow, KeyCode.LeftArrow,
                                 2.0f, 4.0f, description:"Player 2 horizontal mapping.");

        // Axis 4 for xbox controller
        InputM.AddAnalogAxis(PlayerID.One, "Horizontal J", 1, 4, 0.2f, 1.0f, false, "Joystick");

        // Add button to change first button mapping
        InputM.AddButton(PlayerID.One, "Mapping", KeyCode.Keypad1, KeyCode.None, "Player 1 mapping interface.");
        InputM.AddButton(PlayerID.Two, "Mapping", KeyCode.Keypad2, KeyCode.None, "Player 2 mapping interface.");

        // Check mapping protection
        bool result = InputM.AddButton(PlayerID.One, "Jump", KeyCode.G, KeyCode.F, "Player 1 mapping unprotected.");
        Debug.Log("Mapping for Jump erased ? ----------> " + result);


        // Check creation
        Debug.Log("Player ONE owns " + InputM.Instance.PlayerOneConfig.Count + " axis");
        Debug.Log("Player TWO owns " + InputM.Instance.PlayerTwoConfig.Count + " axis");
        Debug.Log("\n--------------\n");


        // Jsonization
        InputM.Instance.SaveInputConfig(PlayerID.One, ControllerType.Keyboard);
        InputM.Instance.SaveInputConfig(PlayerID.Two, ControllerType.Keyboard);
        InputM.Instance.SaveInputConfig(PlayerID.Three, ControllerType.Keyboard);
        InputM.Instance.SaveInputConfig(PlayerID.Four, ControllerType.Keyboard);

        // De-Jsonization
        InputM.Instance.LoadInputConfig(PlayerID.One, ControllerType.Keyboard);
        InputM.Instance.LoadInputConfig(PlayerID.Two, ControllerType.Keyboard);
        InputM.Instance.LoadInputConfig(PlayerID.Three, ControllerType.Keyboard);
        InputM.Instance.LoadInputConfig(PlayerID.Four, ControllerType.Keyboard);
	}

	// Update is called once per frame
	void Update ()
    {
        // Check access Button access
        if (InputM.GetButtonDown(PlayerID.One, "Jump"))
        {
            Debug.Log(horizontalMovement1);
        }
        if (InputM.GetButtonDown(PlayerID.Two, "Jump"))
        {
            Debug.Log(horizontalMovement2);
        }

        // Check Axis access
        horizontalMovement1 += InputM.GetAxis(PlayerID.One, "Horizontal");
        horizontalMovement2 += InputM.GetAxis(PlayerID.Two, "Horizontal");

        horizontalMovement1 += InputM.GetAxis(PlayerID.One, "Horizontal J");

        // Dynamic remapping with button
        if (InputM.GetButtonDown(PlayerID.One, "Mapping"))
        {
            Debug.Log("\nRemmaping 1 to 3\n");
            InputM.GetAxisConfig(PlayerID.One, "Jump").positive = KeyCode.Alpha3;
        }
        if (InputM.GetButtonDown(PlayerID.Two, "Mapping"))
        {
            Debug.Log("\nRemmaping 2 to 4\n");
            InputM.Instance.PlayerTwoConfig.Axes["Jump"].positive = KeyCode.Alpha4;
        }

        // Dynamic remapping with digital axis
        if (InputM.GetButtonDown(PlayerID.Two, "Mapping"))
        {
            Debug.Log("\nRemmaping rightArrow to D and leftArrow to A\n");
            InputM.Instance.PlayerTwoConfig.Axes["Horizontal"].positive = KeyCode.D;
            InputM.Instance.PlayerTwoConfig.Axes["Horizontal"].negative = KeyCode.A;
        }

        // KeyCode.None cannot return true
        if (Input.GetKey(KeyCode.None))
        {
            Debug.Log("You should not be able to see this.");
        }

	}
}
