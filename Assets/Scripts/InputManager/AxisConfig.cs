using UnityEngine;
using System;
using Newtonsoft.Json;


/// <summary>
/// Store configuration for a virtual axis (see InputType).
/// Wrapping allows runtime changes like remapping or updating parameters.
/// Mirrors Unity InputManager axes specifications.
/// </summary>
public sealed class AxisConfig
{
    // Helper for Analog buttons
    public const float Neutral = 0.0f;
    public const float Positive = 1.0f;
    public const float Negative = -1.0f;

    // Helper for rebinding of gamepads
    public const int FirstPadButtonIndex = 350;         // Joystick0Button0 == 350
    public const int NumberOfButtonPerPad = 20;         // 20 button per gamepad
    public const int TotalNumberOfPadButton = 80;       // NumberOfPadButton * 4

    // Unity limitations
    public const int   MaxMouseAxes = 3;
    public const int   MaxJoystickAxes = 28;
    public const int   MaxJoysticks = 4;

    // Mirrors Unity parameters for each axis
    public string  name;
    public string  description;
    public KeyCode positive;
    public KeyCode negative;
    public KeyCode altPositive;
    public KeyCode altNegative;

    /// <summary>
    /// The speed(in units/sec) at which a digital axis falls towards neutral.
    /// </summary>
    public float gravity = 1.0f;

    /// <summary>
    /// Minimal value before considering the input is active.
    /// </summary>
    public float deadZone;


    /// <summary>
    /// The speed(in units/sec) at which a digital axis moves towards the target value.
    /// </summary>
    public float sensitivity = 1.0f;

    /// <summary>
    /// If input switches direction and snap is true we first snap to neutral and continue from there.
    /// Only for digital axes.
    /// </summary>
    public bool snap;

    public bool invert;

    // @TODO Should be private
    public InputType type;
    public int       axisNb;
    public int       joystickNb;


    // Internal state
    private float       currentValue;
    private ButtonState analogButtonState;
    private float       lastUpdateTime;
    private float       currentDeltaTime;

    [JsonProperty]
    private string      unityAxisName;

    // // Keep track of config change
    // private InputType previousType;
    // private int       previousAxisNb;
    // private int       previousJoystickNb;


// ---------------------------------- Properties ------------------------------
    /// <summary>
    /// Return UnityManager axis name mapped to this axis.
    /// </summary>
    [JsonIgnore]
    public string UnityAxisName
    {
        get {return unityAxisName;}
    }


// ----------------------------- Constructor ----------------------------------
    /// <summary>
    /// Constructor to create a new axis.
    /// </summary>
    public AxisConfig(string name="New axis")
    {
        this.name = name;
        description = string.Empty;
        positive = KeyCode.None;
        altPositive = KeyCode.None;
        negative = KeyCode.None;
        altNegative = KeyCode.None;
        type = InputType.Button;
        deadZone = 0.0f;
        gravity = 1.0f;
        sensitivity = 1.0f;
        unityAxisName = string.Empty;
    }


// ------------------------- Utilities ----------------------------------------

    public static AxisConfig Duplicate(AxisConfig source)
    {
        var axisConfig = new AxisConfig();
        axisConfig.name = source.name;
        axisConfig.description = source.description;
        axisConfig.positive = source.positive;
        axisConfig.altPositive = source.altPositive;
        axisConfig.negative = source.negative;
        axisConfig.altNegative = source.altNegative;
        axisConfig.gravity = source.gravity;
        axisConfig.sensitivity = source.sensitivity;
        axisConfig.deadZone = source.deadZone;
        axisConfig.snap = source.snap;
        axisConfig.invert = source.invert;
        axisConfig.type = source.type;
        axisConfig.axisNb = source.axisNb;
        axisConfig.joystickNb = source.joystickNb;

        // @TODO should copy this internal state ?
        axisConfig.unityAxisName = source.unityAxisName;

        return axisConfig;
    }

    /// <summary>
    /// String to input type
    /// </summary>
    public static InputType ToInputType(string inputVal)
    {
        try
        {
            return (InputType)Enum.Parse(typeof(InputType), inputVal, true);
        }
        catch (ArgumentException)
        {
            return InputType.None;
        }
    }

    /// <summary>
    /// String to KeyCode
    /// </summary>
    public static KeyCode ToKeyType(string inputVal)
    {
        try
        {
            return (KeyCode)Enum.Parse(typeof(KeyCode), inputVal, true);
        }
        catch (ArgumentException)
        {
            return KeyCode.None;
        }
    }

    // @TODO USED FOR WHAT ???
    /// <summary>
    /// Construct axis name based on joystickNb and axisNb values.
    /// Must
    /// </summary>
    private void UpdateUnityAxisName()
    {

    }


// ----------------------------- Key state -------------------------------------
    /// <summary>
    /// Returns true while the user holds down any key positive or negative or other alternatives.
    /// Think auto fire.
    /// </summary>
    [JsonIgnore]
    public bool AnyKey
    {
        get
        {
            return Input.GetKey(positive) || Input.GetKey(altPositive) ||
                   Input.GetKey(negative) || Input.GetKey(altNegative);
        }
    }

    [JsonIgnore]
    public bool AnyKeyDown
    {
        get
        {
            return Input.GetKeyDown(positive) || Input.GetKeyDown(altPositive) ||
                   Input.GetKeyDown(negative) || Input.GetKeyDown(altNegative);
        }
    }

    [JsonIgnore]
    public bool AnyKeyUp
    {
        get
        {
            return Input.GetKeyUp(positive) || Input.GetKeyUp(altPositive) ||
                   Input.GetKeyUp(negative) || Input.GetKeyUp(altNegative);
        }
    }

    public bool GetButton()
    {
        // Keyboard
        if (type == InputType.Button)
        {
            return Input.GetKey(positive) || Input.GetKey(altPositive);
        }
        // Gamepad
        if (type == InputType.AnalogButton)
        {
            return analogButtonState == ButtonState.Pressed ||
                   analogButtonState == ButtonState.JustPressed;
        }

        return false;
    }

    public bool GetButtonDown()
    {
        // Keyboard
        if (type == InputType.Button)
        {
            return Input.GetKeyDown(positive) || Input.GetKeyDown(altPositive);
        }

        // Gamepad
        if (type == InputType.AnalogButton)
        {
            return analogButtonState == ButtonState.JustPressed;
        }

        return false;
    }

    public bool GetButtonUp()
    {
        // Keyboard
        if (type == InputType.Button)
        {
            return Input.GetKeyUp(positive) || Input.GetKeyUp(altPositive);
        }

        // Gamepad
        if (type == InputType.AnalogButton)
        {
            return analogButtonState == ButtonState.JustReleased;
        }

        return false;
    }

    public float GetAxis()
    {
        // Default to 0
        float axisValue = Neutral;

        // Keyboard
        if (type == InputType.DigitalAxis)
        {
            // Get from key
            axisValue = currentValue;
        }

        else if (unityAxisName != string.Empty)
        {
            axisValue = Input.GetAxis(unityAxisName);

            // Mouse
            if (type == InputType.MouseAxis)
            {
                axisValue *= sensitivity;
            }

            // Gamepad
            else if (type == InputType.AnalogAxis)
            {
                // Check if it's a parasite movement
                if (Mathf.Abs(axisValue) < deadZone)
                {
                    axisValue = Neutral;
                }
                // Always return a value in [-1, 1] @TODO FIX
                // axisValue = Mathf.Clamp(axisValue * sensitivity, -1, 1);
                axisValue *= sensitivity;
            }
        }
        return invert ? -axisValue : axisValue;
    }

    ///<summary>
    /// The value will be in the range -1...1 for keyboard and joystick input.
    /// Since input is not smoothed, keyboard input will always be either -1, 0 or 1.
    /// https://docs.unity3d.com/ScriptReference/Input.GetAxisRaw.html
    /// </summary>
    public float GetAxisRaw()
    {
        // Default to 0
        float axisValue = Neutral;

        // Keyboard
        if (type == InputType.DigitalAxis)
        {
            // Positive pressed --> return 1.0f
            if(Input.GetKey(positive) || Input.GetKey(altPositive))
            {
                axisValue = Positive;
            }
            // Negative pressed --> return -1.0f
            else if(Input.GetKey(negative) || Input.GetKey(altNegative))
            {
                axisValue = Negative;
            }
        }

        else if (unityAxisName != string.Empty && (type == InputType.MouseAxis || type == InputType.AnalogAxis))
        {
            // Always between -1 and 1
            axisValue = Input.GetAxis(unityAxisName);
        }

        return invert ? -axisValue : axisValue;
    }


// --------------------------------- State update -----------------------------
    /// <summary>
    /// Update state.
    /// </summary>
    public void Update()
    {
        // Get delta time since last update (not time scaled)
        currentDeltaTime = Time.realtimeSinceStartup - lastUpdateTime;
        lastUpdateTime = Time.realtimeSinceStartup;

        // // Check if any change in the configuration
        // if (previousType != type || previousAxisNb != axisNb || previousJoystickNb != joystickNb)
        // {
        //     // @TODO Reinitialize axis before updating
        //     // Init()
        // }

        if (type == InputType.DigitalAxis)
        {
            UpdateDigitalAxisValue();
        }
        if (type == InputType.AnalogButton)
        {
            UpdateAnalogButtonValue();
        }
    }

    /// <summary>
    /// Update the current value for keyboard axis (digital).
    /// Take account for snap / sensitivity / gravity
    /// </summary>
    private void UpdateDigitalAxisValue()
    {
        // Positive DOWN and negative UP
        if ((Input.GetKey(positive) || Input.GetKey(altPositive)) &&
            !(Input.GetKey(negative) || Input.GetKey(altNegative)))
        {
            if (currentValue < Neutral && snap)
            {
                // Previous negative so first reset to 0
                currentValue = Neutral;
            }

            // Make value time independant and clamp
            currentValue += sensitivity * currentDeltaTime;
            currentValue = Mathf.Min(currentValue, Positive);
        }
        // Positive UP and negative DOWN
        else if(Input.GetKey(negative) || Input.GetKey(altNegative))
        {
            if (currentValue > Neutral && snap)
            {
                // Previous positive so first reset to 0
                currentValue = Neutral;
            }

            // Make value time independant and clamp
            currentValue -= sensitivity * currentDeltaTime;
            currentValue = Mathf.Max(currentValue, Negative);
        }
        else
        {
            // Reset to neutral position based on gravity value
            if (currentValue < Neutral)
            {
                currentValue += gravity * currentDeltaTime;
                currentValue = Mathf.Min(currentValue, Neutral);
            }
            else if ( currentValue > Neutral)
            {
                currentValue -= gravity * currentDeltaTime;
                currentValue = Mathf.Max(currentValue, Neutral);
            }
        }
    }

    /// <summary>
    /// Update analog button for gamepads.
    /// </summary>
    private void UpdateAnalogButtonValue()
    {
        // Works in a similar way as axis but we only want a bool value not a float
        float axisValue = Input.GetAxisRaw(unityAxisName);

        // Pressed
        if (axisValue >= 1.0f)
        {
            // Already pressed ?
            if (analogButtonState == ButtonState.JustPressed)
            {
                analogButtonState = ButtonState.Pressed;
            }
            // Just pressed at this frame ?
            else if (analogButtonState == ButtonState.JustReleased || analogButtonState == ButtonState.Released)
            {
                analogButtonState = ButtonState.JustPressed;
            }
        }

        // Released
        else
        {
            // Already released ?
            if (analogButtonState == ButtonState.JustReleased)
            {
                analogButtonState = ButtonState.Released;
            }
            // Just released at this frame ?
            else if (analogButtonState == ButtonState.JustPressed || analogButtonState == ButtonState.Pressed)
            {
                analogButtonState = ButtonState.JustReleased;
            }
        }
    }



    /// <summary>
    /// Init axe with default values.
    /// </summary>
    public void Init()
    {
        // Start as the center
        currentValue = Neutral;

        // Stick or not to stick ...
        analogButtonState = ButtonState.Released;

        // Init timer with unscaled value of Unity time
        lastUpdateTime = Time.realtimeSinceStartup;


        // @TODO Init previous state to current ?
    }


    /// <summary>
    /// Change input type and check for correct mapping with Unity InputManager.
    /// </summary>
    public bool SetType(InputType newType, string unityAxis="")
    {
        // No need to map to InputManager
        if (newType == InputType.DigitalAxis ||
            newType == InputType.Button)
        {
            type = newType;
            return true;
        }

        // Check mapping for Joysticks
        if (newType == InputType.AnalogButton ||
            newType == InputType.AnalogAxis)
        {
            if (unityAxis.Contains("Joy"))
            {
                type = newType;
                unityAxisName = unityAxis;
                return true;
            }

            return false;
        }

        // Check mapping for Mouse
        if (newType == InputType.MouseAxis &&
            unityAxis.Contains("Mouse"))
        {
            type = newType;
            unityAxisName = unityAxis;
            return true;
        }

        // Cannot set type with correct Unity InputManager
        return false;
    }

    /// <summary>
    /// Remap joystick to corrected player index (id).
    /// </summary>
    public bool AdjustJoystickConfig(PlayerID id, int shiftID)
    {
        bool hasBeenUpdated = false;

        // Correct player index based on number of pad connected
        int correctPlayerIndex = (int)id - shiftID;

        // Remapping of joystick axis if needed
        if (unityAxisName.Contains("Joy_") && GetJoystickAxePlayerIndex(unityAxisName) != correctPlayerIndex)
        {
            int axeIndex = GetJoystickAxeIndex(unityAxisName);
            unityAxisName = InputManager.GetUnityJoyStickAxis(correctPlayerIndex, axeIndex);
            hasBeenUpdated = true;

            // if (name == "Horizontal")
            // {
            //     Debug.Log("Horizontal");
            //     Debug.Log(correctPlayerIndex);
            //     Debug.Log(axeIndex);
            //     Debug.Log(unityAxisName);
            // }
        }

        // Remapping of joystick buttons (4)
        else
        {
            // Gamepad buttons index interval (based on Unity KeyCode rules)
            const int minIndex = FirstPadButtonIndex;
            const int maxIndex = FirstPadButtonIndex + TotalNumberOfPadButton;

            // if (name == "Action")
            // {
            //     Debug.Log(id);
            //     Debug.Log(GetJoystickButtonIndex(positive));
            //     Debug.Log(correctPlayerIndex);
            //     Debug.Log(positive);
            // }

            if ((int)positive >= minIndex && (int)positive <= maxIndex &&
                GetJoystickButtonPlayerIndex(positive) != correctPlayerIndex)
            {
                int buttonID = GetJoystickButtonIndex(positive);
                positive = InputManager.GetJoystickButton(correctPlayerIndex, buttonID);
                hasBeenUpdated = true;
            }
            if ((int)altPositive >= minIndex && (int)altPositive <= maxIndex &&
                GetJoystickButtonPlayerIndex(altPositive) != correctPlayerIndex)
            {
                int buttonID = GetJoystickButtonIndex(altPositive);
                altPositive = InputManager.GetJoystickButton(correctPlayerIndex, buttonID);
                hasBeenUpdated = true;
            }

            if ((int)negative >= minIndex && (int)negative <= maxIndex &&
                GetJoystickButtonPlayerIndex(negative) != correctPlayerIndex)
            {
                int buttonID = GetJoystickButtonIndex(negative);
                negative = InputManager.GetJoystickButton(correctPlayerIndex, buttonID);
                hasBeenUpdated = true;
            }

            if ((int)altNegative >= minIndex && (int)altNegative <= maxIndex &&
                GetJoystickButtonPlayerIndex(altNegative) != correctPlayerIndex)
            {
                int buttonID = GetJoystickButtonIndex(altNegative);
                altNegative = InputManager.GetJoystickButton(correctPlayerIndex, buttonID);
                hasBeenUpdated = true;
            }
        }

        return hasBeenUpdated;
    }

    /// <summary>
    /// Extract button index from joystick button keycode.
    /// </summary>
    public static int GetJoystickButtonIndex(KeyCode joyButton)
    {
        string buttonName = joyButton.ToString();
        try
        {
            return int.Parse(buttonName.Substring(15));
        }
        catch (FormatException)
        {
            return int.Parse(buttonName.Substring(14));
        }
    }

    /// <summary>
    /// Extract player index from unity button serialization.
    /// </summary>
    public static int GetJoystickButtonPlayerIndex(KeyCode joyButton)
    {
        string buttonName = joyButton.ToString();
        return int.Parse(buttonName.Substring(8, 1));
    }

    /// <summary>
    /// Extract button index from joystick button keycode.
    /// </summary>
    public static int GetJoystickButtonIndex(string buttonName)
    {
        try
        {
            return int.Parse(buttonName.Substring(15));
        }
        catch (FormatException)
        {
            return int.Parse(buttonName.Substring(14));
        }
    }

    /// <summary>
    /// Extract player index from unity button serialization.
    /// </summary>
    public static int GetJoystickButtonPlayerIndex(string buttonName)
    {
        return int.Parse(buttonName.Substring(8, 1));
    }


    /// <summary>
    /// Extract axe index from joystick axis name.
    /// </summary>
    public static int GetJoystickAxeIndex(string joystickName)
    {
        // Last element is axis index
        // Add 1 because of name inside the Input Manager (index 4 point to axis 5)
        return int.Parse(joystickName.Split('_')[3]) + 1;
    }

    /// <summary>
    /// Extract player index from joystick axis name.
    /// </summary>
    public static int GetJoystickAxePlayerIndex(string joystickName)
    {
        // Second element is player index
        return int.Parse(joystickName.Split('_')[1]);
    }
}
