using UnityEngine;

public partial class InputManager
{

    private void InitPlayerOneKeyBoard()
    {
        playerOneConfig = new InputConfig("Player One config");

        // Add mouse virtual axis for player one
        InputManager.AddMouseAxis(PlayerID.One, "CamHorizontal", 0, 0.2f, false, "Player 1 mouse horizontal mapping");
        InputManager.AddMouseAxis(PlayerID.One, "CamVertical", 1, 0.2f, false, "Player 1 mouse vertical mapping");

        //Add ZQSD axis
        InputManager.AddDigitalAxis(PlayerID.One, "Horizontal", KeyCode.D, KeyCode.Q, 2.0f, 4.0f,
                                    altposKey: KeyCode.RightArrow, altnegKey: KeyCode.LeftArrow,
                                    description: "Player 1 horizontal mapping.");
        InputManager.AddDigitalAxis(PlayerID.One, "Vertical", KeyCode.Z, KeyCode.S, 2.0f, 4.0f,
                                    altposKey: KeyCode.UpArrow, altnegKey: KeyCode.DownArrow,
                                    description: "Player 1 vertical mapping.");

        //Add buttons
        //switch
        InputManager.AddButton(PlayerID.One, "Switch", KeyCode.Tab, description: "Player 1 switch interface.");
        //attack left hand
        InputManager.AddButton(PlayerID.One, "Attack left", KeyCode.Mouse0, description: "Player 1 Left Hand interface.");
        //attack right hand
        InputManager.AddButton(PlayerID.One, "Attack right", KeyCode.Mouse1, description: "Player 1 Right Hand interface.");
        //dodge
        InputManager.AddButton(PlayerID.One, "Dodge", KeyCode.Space, description: "Player 1 dodge interface.");
        //pause
        InputManager.AddButton(PlayerID.One, "Pause", KeyCode.Escape, description: "Player 1 pause interface.");
        //action
        InputManager.AddButton(PlayerID.One, "Action", KeyCode.E, description: "Player 1 action interface.");

        SaveInputConfig(PlayerID.One, ControllerType.Keyboard);
    }

    private void InitPlayerOneGamePad()
    {
        playerOneConfig = new InputConfig("Player One config");

        // Buttons
        AddButton(PlayerID.One, "Switch", KeyCode.Joystick1Button3, description:"Switch player avatar");
        AddButton(PlayerID.One, "Dodge", KeyCode.Joystick1Button0, description: "Dodge action");
        AddButton(PlayerID.One, "Action", KeyCode.Joystick1Button1, description:"Action for avatar");
        AddButton(PlayerID.One, "Pause",  KeyCode.Joystick1Button7, description:"Pause Menu");

        // Right stick
        AddAnalogAxis(PlayerID.One, "CamHorizontal", 1, 4, description:"Horizontal camera movement", sensitivity : 1);
        AddAnalogAxis(PlayerID.One, "CamVertical", 1, 5, description:"Vertical camera movement", sensitivity : 1);

        // Left stick
        AddAnalogAxis(PlayerID.One, "Horizontal", 1, 1, description:"Horizontal movement");
        AddAnalogAxis(PlayerID.One, "Vertical", 1, 2, description:"Vertical movement", invert : false);

        // Using Bumpers
        AddAnalogButton(PlayerID.One, "Attack left", 1, 9, description:"Attack left");
        AddAnalogButton(PlayerID.One, "Attack right", 1, 10, description:"Attack right");

        SaveInputConfig(PlayerID.One, ControllerType.Gamepad);

        }

    private void InitPlayerTwoGamepad()
    {
        playerTwoConfig = new InputConfig("Player Two config");

        // Buttons
        AddButton(PlayerID.Two, "Switch", KeyCode.Joystick2Button3, description:"Enter multiplayer");
        AddButton(PlayerID.Two, "Dodge", KeyCode.Joystick2Button0, description: "Dodge action");
        AddButton(PlayerID.Two, "Action", KeyCode.Joystick2Button1, description:"Action for avatar");
        AddButton(PlayerID.Two, "Pause",  KeyCode.Joystick2Button7, description:"Pause Menu");

        // Right stick
        AddAnalogAxis(PlayerID.Two, "CamHorizontal", 2, 4, description:"Horizontal camera movement", sensitivity : 1);
        AddAnalogAxis(PlayerID.Two, "CamVertical", 2, 5, description:"Vertical camera movement", sensitivity : 1);

        // Left stick
        AddAnalogAxis(PlayerID.Two, "Horizontal", 2, 1, description:"Horizontal movement");
        AddAnalogAxis(PlayerID.Two, "Vertical", 2, 2, description:"Vertical movement", invert: false);

        // Using Bumpers
        AddAnalogButton(PlayerID.Two, "Attack left", 2, 9, description:"Attack left");
        AddAnalogButton(PlayerID.Two, "Attack right", 2, 10, description:"Attack right");

        SaveInputConfig(PlayerID.Two, ControllerType.Gamepad);
    }

    private void InitPlayerTwoKeyBoard()
    {
        playerTwoConfig = new InputConfig("Player Two config");

        // Add mouse virtual axis for player one
        InputManager.AddMouseAxis(PlayerID.Two, "CamHorizontal", 0, 0.2f, false, "Player 1 mouse horizontal mapping");
        InputManager.AddMouseAxis(PlayerID.Two, "CamVertical", 1, 0.2f, false, "Player 1 mouse vertical mapping");

        //Add ZQSD axis
        InputManager.AddDigitalAxis(PlayerID.Two, "Horizontal", KeyCode.D, KeyCode.Q, 2.0f, 4.0f,
                                    altposKey: KeyCode.RightArrow, altnegKey: KeyCode.LeftArrow,
                                    description: "Player 1 horizontal mapping.");
        InputManager.AddDigitalAxis(PlayerID.Two, "Vertical", KeyCode.Z, KeyCode.S, 2.0f, 4.0f,
                                    altposKey: KeyCode.UpArrow, altnegKey: KeyCode.DownArrow,
                                    description: "Player 1 vertical mapping.");
        //Add buttons
        //switch
        InputManager.AddButton(PlayerID.Two, "Switch", KeyCode.CapsLock, description: "Player 2 enter the game.");
        //attack left hand
        InputManager.AddButton(PlayerID.Two, "Attack left", KeyCode.Mouse0, description: "Player 1 Left Hand interface.");
        //attack right hand
        InputManager.AddButton(PlayerID.Two, "Attack right", KeyCode.Mouse1, description: "Player 1 Right Hand interface.");
        //dodge
        InputManager.AddButton(PlayerID.Two, "Dodge", KeyCode.Space, description: "Player 1 dodge interface.");
        //pause
        InputManager.AddButton(PlayerID.Two, "Pause", KeyCode.Escape, description: "Player 1 pause interface.");
        //action
        InputManager.AddButton(PlayerID.Two, "Action", KeyCode.E, description: "Player 1 action interface.");

        SaveInputConfig(PlayerID.Two, ControllerType.Keyboard);
    }

    public bool InitPlayer(PlayerID playerID, ControllerType controller)
    {
        switch (playerID)
        {
            // Player 1
            case PlayerID.One:
                switch (controller)
                {
                    case ControllerType.Keyboard:
                        InitPlayerOneKeyBoard();
                        return true;
                    case ControllerType.Gamepad:
                        InitPlayerOneGamePad();
                        return true;
                    default:
                        return false;
                }

            // Player 2
            case PlayerID.Two:
                switch (controller)
                {
                    case ControllerType.Keyboard:
                        InitPlayerTwoKeyBoard();
                        return true;
                    case ControllerType.Gamepad:
                        InitPlayerTwoGamepad();
                        return true;
                    default:
                        return false;
                }
        }

        return false;
    }
}

