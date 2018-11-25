using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Linq;


/// <summary>
/// A wrapper of Unity Input and InputManager allowing runtime changes for all
/// kind of axis (see InputType for a complete list).
/// Helper methods allow an easy managment to add / see / update / reset / delete
/// any axis for any configurations.
/// Mapping is dispatched by player (up to 4 as Unity does not support more)
/// making multiplayer mapping more friendly.
/// </summary>
public partial class InputManager : MonoBehaviour
{
    // Used to control how to react to different events
    public event Action<PlayerID> ConfigurationChange = delegate {};

// Change default based on platform
#if UNITY_WSA
    private ControllerType[] defaultController =
    {
        ControllerType.Gamepad,
        ControllerType.Gamepad,
        ControllerType.Gamepad,
        ControllerType.Gamepad,
    };
#else
    // [SerializeField]
    private ControllerType[] defaultController =
    {
        ControllerType.Keyboard,
        ControllerType.Keyboard,
        ControllerType.Keyboard,
        ControllerType.Keyboard,
    };
#endif

    // Singleton
    private static InputManager instance;

    private InputConfig playerOneConfig;
    private InputConfig playerTwoConfig;
    private InputConfig playerThreeConfig;
    private InputConfig playerFourConfig;

    // Keep track of all axis and keys
    private string[]  unityMouseAxis;
    private string[]  unityJoystickAxes;
    private KeyCode[] unityKeys;

    // Serialization
    private string savepathTemplate = "player{0}_{1}.json";


    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Multiple InputManager instances in the same scene!", gameObject);
            Destroy(this);
        }
        else if (instance == null)
        {
            instance = this;
            // Get a list of Unity existing keycodes
            unityKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

            MirrorUnityAxisNames();

            // Debug.Log(Application.persistentDataPath);
            // StreamHelper.WriteFile(Application.persistentDataPath + "\\" + "playerOne_Gamepad.json", "");
            // StreamHelper.WriteFile(Application.persistentDataPath + "\\" + "playerTwo_Gamepad.json", "");

            // Init then check if saved config
            playerOneConfig = new InputConfig("Player One config");
            playerTwoConfig = new InputConfig("Player Two config");
            playerThreeConfig = new InputConfig("Player Three config");
            playerFourConfig = new InputConfig("Player Four config");

            // @TODO CALLED MULTIPLE TIMES ON XBOX DEBUG BUILD ONLY
            // Load saved config
            foreach (PlayerID pID in Enum.GetValues(typeof(PlayerID)))
            {
                if (!ConfigExists(pID, ControllerType.Keyboard))
                {
                    InitPlayer(pID, ControllerType.Keyboard);
                }

                if (!ConfigExists(pID, ControllerType.Gamepad))
                {
                    InitPlayer(pID, ControllerType.Gamepad);
                }

// Check for gamepad except on XBOX
#if (!UNITY_WSA)
                CheckInitForGamepads();
#endif

                // Load default
                LoadInputConfig(pID, defaultController[(int)pID - 1]);
            }

            // Subscribe to plug and play inputs
            PlugAndPlayHelper plugAndPlayscript = PlugAndPlayHandler;
            if (plugAndPlayscript != null)
            {
                plugAndPlayscript.GamepadDisconnected   += OnGamePadDisconnected;
                plugAndPlayscript.GamepadReConnected    += OnGamePadReConnected;
                plugAndPlayscript.GamepadFirstConnected += OnGamePadFirstConnected;
            }

            // Debug.Log("UNITY ASSIGNED AXIS");
            // Debug.Log(GetAxisConfig(PlayerID.One, "Vertical").UnityAxisName);
            // Debug.Log(GetAxisConfig(PlayerID.One, "Horizontal").UnityAxisName);
        }
    }

    private void OnDestroy()
    {
        // Don't forget to unsubscribe
        PlugAndPlayHelper plugAndPlayscript = PlugAndPlayHandler;
        if (plugAndPlayscript != null)
        {
            plugAndPlayscript.GamepadDisconnected   -= OnGamePadDisconnected;
            plugAndPlayscript.GamepadReConnected    -= OnGamePadReConnected;
            plugAndPlayscript.GamepadFirstConnected -= OnGamePadFirstConnected;
        }
    }


    /// <summary>
    /// Setup axis names defined in the Unity InputManager.
    /// Used internally to test axes inputs.
    /// This fonction does not add axes to Unity, it just reflect them.
    /// Axes must be added by user
    /// Replace your `InputManager.asset` with `InputManager.asset.allAxis`.
    /// </summary>
    private void MirrorUnityAxisNames()
    {
        unityMouseAxis = new string[AxisConfig.MaxMouseAxes];
        for(int i = 0; i < unityMouseAxis.Length; i++)
        {
            unityMouseAxis[i] = string.Concat("Mouse_Axis_", i);
        }

        // Joysticks in range [1, 4] inclusive
        unityJoystickAxes = new string[AxisConfig.MaxJoysticks * AxisConfig.MaxJoystickAxes];
        for(int i = 0; i < AxisConfig.MaxJoysticks; i++)
        {
            for(int j = 0; j < AxisConfig.MaxJoystickAxes; j++)
            {
                unityJoystickAxes[i * AxisConfig.MaxJoystickAxes + j] = string.Concat("Joy_", (i + 1), "_Axis_", j);
            }
        }
    }

    /// <summary>
    /// Get corresponding Unity InputManager joystick axis.
    /// </summary>
    public static string GetUnityJoyStickAxis(int joystickNb, int axisNb)
    {
        int row = joystickNb - 1;
        return instance.unityJoystickAxes[row * AxisConfig.MaxJoystickAxes + (axisNb - 1)];
    }

    /// <summary>
    /// Get corresponding Unity InputManager mouse axis.
    /// </summary>
    public static string GetUnityMouseAxis(int nbAxis)
    {
        return instance.unityMouseAxis[nbAxis];
    }

    /// <summary>
    /// Construct unity button keycode based on player and button index.
    /// </summary>
    public static KeyCode GetJoystickButton(int joystickNb, int buttonID)
    {
        return (KeyCode)Enum.Parse(typeof(KeyCode), GetJoystickButtonName(joystickNb, buttonID));
    }


    /// <summary>
    /// Serialize to unity button format.
    /// </summary>
    public static string GetJoystickButtonName(int joystickNb, int buttonID)
    {
        return string.Format("Joystick{0}Button{1}", joystickNb, buttonID);
    }


    // Update is called once per frame
    private void Update ()
    {
        UpdateInputConfig(playerOneConfig);
        UpdateInputConfig(playerTwoConfig);
        UpdateInputConfig(playerThreeConfig);
        UpdateInputConfig(playerFourConfig);
    }

// -------------------------- Properties ---------------------------------------
    /// <summary>
    /// A reference to the input manager instance.
    /// </summary>
    public static InputManager Instance
    {
        get { return instance; }
    }

    /// <summary>
    /// Get Unity axis for all existing joysticks
    /// </summary>
    public static HashSet<string> UnityJoystickAxis
    {
        get {return new HashSet<string>(instance.unityJoystickAxes);}
    }

    /// <summary>
    /// Get player One configuration.
    /// </summary>
    [SerializeField]
    public InputConfig PlayerOneConfig
    {
        get { return playerOneConfig; }
    }
    /// <summary>
    /// Get player Two configuration.
    /// </summary>
    public InputConfig PlayerTwoConfig
    {
        get { return playerTwoConfig; }
    }
    /// <summary>
    /// Get player Third configuration.
    /// </summary>
    public InputConfig PlayerThirdConfig
    {
        get { return playerThreeConfig; }
    }
    /// <summary>
    /// Get player Fourth configuration.
    /// </summary>
    public InputConfig PlayerFourthConfig
    {
        get { return playerFourConfig; }
    }

    /// <summary>
    /// Get player Fourth configuration.
    /// </summary>
    public PlugAndPlayHelper PlugAndPlayHandler
    {
        get { return GetComponent<PlugAndPlayHelper>(); }
    }


// ----------------------- Accessor to configurations --------------------------

    public InputConfig GetInputConfig(PlayerID playerID)
    {
        switch (playerID)
         {
             case PlayerID.One:
                 return playerOneConfig;
             case PlayerID.Two:
                 return playerTwoConfig;
             case PlayerID.Three:
                 return playerThreeConfig;
             case PlayerID.Four:
                 return playerFourConfig;
             default:
                 return null;
         }
    }

    public ControllerType GetInputController(PlayerID playerID)
    {
        return defaultController[(int)playerID - 1];
    }

    public bool SetInputController(PlayerID playerID, ControllerType conType)
    {
        var oldCon = defaultController[(int)playerID - 1];
        if (oldCon != conType)
        {
            defaultController[(int)playerID - 1] = conType;
            // Notification
            ConfigurationChange.Invoke(playerID);
            return true;
        }

        return false;
    }


    /// <summary>
    /// Return axis configuration for the player (playerID).
    /// </summary>
    public static AxisConfig GetAxisConfig(PlayerID playerID, string axisName)
    {
        try
        {
            var inputConfig = instance.GetInputConfig(playerID);
            var playerConf = inputConfig.Axes;
            return playerConf[axisName];
        }
        catch (NullReferenceException)
        {
            Debug.LogWarning(string.Format("Cannot find input configuration for {0}.",
                                           playerID));
            return null;
        }
        catch (KeyNotFoundException)
        {
            Debug.LogWarning(string.Format("{0} is not contained in player {1} configuration",
                                           axisName, playerID));
            return null;
        }
    }

    /// <summary>
    /// Return axis configuration for the player (playerID).
    /// </summary>
    private bool AxisConfigExist(PlayerID playerID, string axisName)
    {
        try
        {
            var inputConf = instance.GetInputConfig(playerID);
            return inputConf.ContainsKey(axisName);
        }
        catch (NullReferenceException)
        {
            Debug.LogWarning(string.Format("Cannot find input configuration for {0}.",
                                           playerID));
            return false;
        }
    }

    public static HashSet<string> GetUnityJoystickAxis(PlayerID pID)
    {
        int inputID = (int)pID - 1;
        return new HashSet<string>
            (
                instance.unityJoystickAxes.Skip(inputID * AxisConfig.MaxJoystickAxes).Take(AxisConfig.MaxJoystickAxes)
            );
    }



// ----------------------------- Accessor to inputs  ---------------------------

    /// <summary>
    /// Return axis value if set else 0.
    /// </summary>
    public static float GetAxis(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) ? axis.GetAxis() : 0.0f;
    }

    /// <summary>
    /// Return axis raw value if set else 0.
    /// </summary>
    public static float GetAxisRaw(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) ? axis.GetAxisRaw() : 0.0f;
    }

    /// <summary>
    /// Return true if button is pressed (auto fire).
    /// </summary>
    public static bool GetButton(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.GetButton();
    }

    /// <summary>
    /// Return true if button was pressed in this frame (just pressed).
    /// </summary>
    public static bool GetButtonDown(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.GetButtonDown();
    }

    /// <summary>
    /// Return true if button was released in this frame (just released).
    /// </summary>
    public static bool GetButtonUp(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.GetButtonUp();
    }

    /// <summary>
    /// Return true if any key defined in the axis are pressed.
    /// </summary>
    public static bool AnyKey(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.AnyKey;
    }

    /// <summary>
    /// Return true if any key defined in the axis was pressed in this frame (just pressed).
    /// </summary>
    public static bool AnyKeyDown(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.AnyKeyDown;
    }

    /// <summary>
    /// Return true if any key defined in the axis was released in this frame (just released).
    /// </summary>
    public static bool AnyKeyUp(PlayerID playerID, string axisName)
    {
        AxisConfig axis = GetAxisConfig(playerID, axisName);
        return (axis != null) && axis.AnyKeyUp;
    }

    /// <summary>
    /// Return all existing Unity keys.
    /// </summary>
    public static KeyCode[] UnityKeyCodes
    {
        get {return instance.unityKeys;}
    }

// ------------------------- Mirror Unity Input fonctions ----------------------

    /// <summary>
    /// Wrap Input.GetKeyDown(KeyCode key)
    /// </summary>
    public static bool GetKeyDown(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }

    /// <summary>
    /// Wrap Input.GetKeyUp(KeyCode key)
    /// </summary>
    public static bool GetKeyUp(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }

    /// <summary>
    /// Wrap Input.GetKey(KeyCode key)
    /// </summary>
    public static bool GetKey(KeyCode key)
    {
        return Input.GetKey(key);
    }


// ----------------------- Setters to configurations ---------------------------


    /// <summary>
    /// Internal constructor template to construct an new Axis with specialized methods.
    /// </summary>
    private AxisConfig ConstructAxis(string nameAxis, InputType type, string unityAxis="",
                                     KeyCode posKey=KeyCode.None, KeyCode negKey=KeyCode.None,
                                     KeyCode altposKey=KeyCode.None, KeyCode altnegKey=KeyCode.None,
                                     float sensitivity=1.0f, int axisNb=0, int joystickNb=0,
                                     float gravity=1.0f, bool snap=true, float deadZone=0.2f,
                                     bool invert=false, string description="")
    {
        var newAxis = new AxisConfig(nameAxis);
        newAxis.description = description;
        newAxis.positive = posKey;
        newAxis.negative = negKey;
        newAxis.altPositive = altposKey;
        newAxis.altNegative = altnegKey;
        newAxis.sensitivity = sensitivity;
        newAxis.gravity = gravity;
        newAxis.deadZone = deadZone;
        newAxis.axisNb = axisNb;
        newAxis.joystickNb = joystickNb;
        newAxis.snap = snap;
        newAxis.invert=invert;

        return (newAxis.SetType(type, unityAxis)) ? newAxis : null;
    }

    public AxisConfig ConstructDigitalAxis(string nameAxis,
                                            KeyCode posKey_, KeyCode negKey_,
                                            float sensitivity_=3.0f, float gravity_=1.0f, bool snap_=true,
                                            KeyCode altposKey_=KeyCode.None, KeyCode altnegKey_=KeyCode.None,
                                            string description_="")
    {
        return ConstructAxis(nameAxis, InputType.DigitalAxis, posKey:posKey_, negKey:negKey_,
                            sensitivity:sensitivity_, gravity:gravity_, snap:snap_, altposKey:altposKey_,
                            altnegKey:altnegKey_, description:description_);
    }


    public AxisConfig ConstructAnalogButton(string nameAxis, int joystickNb, int axisNb_,
                                             bool invert_=false, string description_="")
    {
        string unityAxis;

        try
        {
            unityAxis = GetUnityJoyStickAxis(joystickNb, axisNb_);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.LogWarning(string.Format("<{0}> is not an existing axis number. Select a value in [0, {1}]",
                                         axisNb_, AxisConfig.MaxJoystickAxes));
            return null;
        }

        return ConstructAxis(nameAxis, InputType.AnalogButton, unityAxis, invert:invert_,
                             joystickNb:joystickNb, axisNb:axisNb_,
                             description:description_);
    }


    public AxisConfig ConstructButton(string nameAxis, KeyCode key_, KeyCode altKey_=KeyCode.None, string description_="")
    {
        return ConstructAxis(nameAxis, InputType.Button, posKey:key_, altposKey:altKey_,
                             description:description_);
    }


    public AxisConfig ConstructAnalogAxis(string nameAxis, int joystickNb, int axisNb_,
                                          float deadZone_=0.2f, float sensitivity_=1.0f, bool invert_=false,
                                          string description_="")
    {
        string unityAxis;

        try
        {
            unityAxis = GetUnityJoyStickAxis(joystickNb, axisNb_);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.LogWarning(string.Format("<{0}> is not an existing axis number. Select a value in [0, {1}]",
                                         axisNb_, AxisConfig.MaxJoystickAxes));
            return null;
        }

        return ConstructAxis(nameAxis, InputType.AnalogAxis, unityAxis, deadZone:deadZone_,
                             joystickNb:joystickNb, axisNb:axisNb_,
                             sensitivity:sensitivity_, invert:invert_, description:description_);
    }


    public AxisConfig ConstructMouseAxis(string nameAxis, int axisNb_,
                                          float sensitivity_=0.1f, bool invert_=false,
                                          string description_="")
    {
        string unityAxis;

        try
        {
            unityAxis = GetUnityMouseAxis(axisNb_);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.LogWarning(string.Format("<{0}> is not an existing axis number. Select a value in [0, {1}]",
                                         axisNb_, AxisConfig.MaxMouseAxes));
            return null;
        }

        return ConstructAxis(nameAxis, InputType.MouseAxis, unityAxis, sensitivity:sensitivity_,
                             axisNb:axisNb_, invert:invert_, description:description_);
    }


    /// <summary>
    /// Override existing configuration for playerID.
    /// </summary>
    public void SetInputConfig(PlayerID playerID, InputConfig config)
    {
        switch (playerID)
        {
            case PlayerID.One:
                playerOneConfig = config;
                break;
            case PlayerID.Two:
                playerTwoConfig = config;
                break;
            case PlayerID.Three:
                playerThreeConfig = config;
                break;
            case PlayerID.Four:
                playerFourConfig = config;
                break;
            default:
                return;
        }

        instance.ConfigurationChange.Invoke(playerID);

    }

    /// <summary>
    /// De-serialized a configuration define as a string and override playerID
    /// existing configuration.
    /// </summary>
    public void SetInputConfig(PlayerID playerID, string serializedConfig)
    {
        switch (playerID)
        {
            case PlayerID.One:
                playerOneConfig = JsonConvert.DeserializeObject<InputConfig>(serializedConfig);
                break;
            case PlayerID.Two:
                playerTwoConfig = JsonConvert.DeserializeObject<InputConfig>(serializedConfig);
                break;
            case PlayerID.Three:
                playerThreeConfig = JsonConvert.DeserializeObject<InputConfig>(serializedConfig);
                break;
            case PlayerID.Four:
                playerFourConfig = JsonConvert.DeserializeObject<InputConfig>(serializedConfig);
                break;
        }
    }



    /// <summary>
    /// De-serialized a configuration and override playerID existing configuration.
    /// </summary>
    public bool LoadInputConfig(PlayerID playerID, ControllerType controller)
    {
        string savName = string.Format(savepathTemplate, playerID,
                                       ControllerToString(controller));

        string savPath = Application.persistentDataPath + "\\" + savName;

        // Check path and load if exist
        if (!StreamHelper.FileExist(savPath))
        {
            return false;
        }
        var jsonString = StreamHelper.ReadFile(savPath);


        switch (playerID)
        {
            case PlayerID.One:
                playerOneConfig = JsonConvert.DeserializeObject<InputConfig>(jsonString);
                break;
            case PlayerID.Two:
                playerTwoConfig = JsonConvert.DeserializeObject<InputConfig>(jsonString);
                break;
            case PlayerID.Three:
                playerThreeConfig = JsonConvert.DeserializeObject<InputConfig>(jsonString);
                break;
            case PlayerID.Four:
                playerFourConfig = JsonConvert.DeserializeObject<InputConfig>(jsonString);
                break;
            default:
                return false;
        }

        // Update controller type and raise an event if not already done
        if (!SetInputController(playerID, controller))
        {
            instance.ConfigurationChange.Invoke(playerID);
        }

        return true;
    }

    /// <summary>
    /// De-serialized a configuration and return it.
    /// </summary>
    public static InputConfig LoadInputConfig(string path)
    {
        InputConfig config;

        // Check path and load if exist
        if (!StreamHelper.FileExist(path))
        {
            return null;
        }

        var jsonString = StreamHelper.ReadFile(path);
        config = JsonConvert.DeserializeObject<InputConfig>(jsonString);

        return config;
    }

    /// <summary>
    /// Serialized a configuration and save it.
    /// </summary>
    public bool SaveInputConfig(PlayerID playerID, ControllerType controller)
    {
        string savName = string.Format(savepathTemplate, playerID,
                                       ControllerToString(controller));

        string jsonStr;

        switch (playerID)
        {
            case PlayerID.One:
                jsonStr = JsonConvert.SerializeObject(playerOneConfig, Formatting.Indented);
                break;
            case PlayerID.Two:
                jsonStr = JsonConvert.SerializeObject(playerTwoConfig, Formatting.Indented);
                break;
            case PlayerID.Three:
                jsonStr = JsonConvert.SerializeObject(playerThreeConfig, Formatting.Indented);
                break;
            case PlayerID.Four:
                jsonStr = JsonConvert.SerializeObject(playerFourConfig, Formatting.Indented);
                break;
            default:
                // Preserve old data for corrupted request
                return false;
        }
        StreamHelper.WriteFile(Application.persistentDataPath + "\\" + savName, jsonStr);
        return true;
    }

    /// <summary>
    /// Serialized a configuration and save it.
    /// </summary>
    public static bool SaveInputConfig(InputConfig config, string savPath)
    {
        StreamHelper.WriteFile(savPath, JsonConvert.SerializeObject(config));
        return true;
    }

   /// <summary>
   /// Define a new Digital Axis (two/four keyboard keys working like an axis) for a specific player (playerID).
   /// </summary>
   public static bool AddDigitalAxis(PlayerID playerID, string name,
                                     KeyCode posKey, KeyCode negKey,
                                     float sensitivity=3.0f, float gravity=1.0f, bool snap=true,
                                     KeyCode altposKey=KeyCode.None, KeyCode altnegKey=KeyCode.None,
                                     string description="")
   {
        if (instance.AxisConfigExist(playerID, name))
        {
            Debug.LogWarning(string.Format("< {0} > already exists in player {1} input configuration.", name, playerID));
            return false;
        }

        AxisConfig newAxis = instance.ConstructDigitalAxis(name, posKey, negKey, sensitivity,
                                                           gravity, snap, altposKey, altnegKey,
                                                           description);
        if (newAxis != null)
        {
            InputConfig playerConfig = instance.GetInputConfig(playerID);
            playerConfig.Add(name, newAxis);

            // Notification about a new axis created
            instance.ConfigurationChange.Invoke(playerID);

            return true;
        }

        return false;
   }

   /// <summary>
   /// Define a new Analog Button based on an axis gamepad for a specific player (playerID).
   /// Can be used to emulate a button with an axis. To access a gamepad button see `AddButton`.
   /// </summary>
   public static bool AddAnalogButton(PlayerID playerID, string name, int joystickNb, int axisNb, bool invert=false,
                                         string description="")
   {
        if (instance.AxisConfigExist(playerID, name))
        {
            Debug.LogWarning(string.Format("<{0}> already exists in player {1} input configuration.", name, playerID));
            return false;
        }
        else
        {
            AxisConfig newAxis = instance.ConstructAnalogButton(name, joystickNb, axisNb,
                                                                invert, description);
            if (newAxis != null)
            {
                InputConfig playerConfig = instance.GetInputConfig(playerID);
                playerConfig.Add(name, newAxis);

                // Notification about a new axis created
                instance.ConfigurationChange.Invoke(playerID);

                return true;
            }
        }

        return false;
   }

   /// <summary>
   /// Define a new Digital Button (keyboard key or gamepad button) for a specific player (playerID).
   /// To use a Gamepad axis as a button see `AddAnalogButton`.
   /// </summary>
   public static bool AddButton(PlayerID playerID, string name, KeyCode key, KeyCode altKey=KeyCode.None,
                                   string description="")
   {
        if (instance.AxisConfigExist(playerID, name))
        {
            Debug.LogWarning(string.Format("< {0} > already exists in player {1} input configuration.", name, playerID));
            return false;
        }

        AxisConfig newAxis = instance.ConstructButton(name, key, altKey, description);
        if (newAxis != null)
        {
            InputConfig playerConfig = instance.GetInputConfig(playerID);
            playerConfig.Add(name, newAxis);

            // Notification about a new axis created
            instance.ConfigurationChange.Invoke(playerID);

            return true;
        }

        return false;
   }

   /// <summary>
   /// Define a new Analog Axis (gamepad) for a specific player (playerID).
   /// </summary>
   public static bool AddAnalogAxis(PlayerID playerID, string name, int joystickNb, int axisNb, float deadZone=0.2f,
                                float sensitivity=2.0f, bool invert=false, string description="")
   {
        if (instance.AxisConfigExist(playerID, name))
        {
            Debug.LogWarning(string.Format("<{0}> already exists in player {1} input configuration.", name, playerID));
            return false;
        }
        else
        {
            AxisConfig newAxis = instance.ConstructAnalogAxis(name, joystickNb, axisNb, deadZone,
                                                              sensitivity, invert, description);
            if (newAxis != null)
            {
                InputConfig playerConfig = instance.GetInputConfig(playerID);
                playerConfig.Add(name, newAxis);

                // Notification about a new axis created
                instance.ConfigurationChange.Invoke(playerID);

                return true;
            }
        }

        return false;
   }

   /// <summary>
   /// Define a new Mouse Axis for a specific player (playerID).
   /// </summary>
   public static bool AddMouseAxis(PlayerID playerID, string name, int axisNb,
                                      float sensitivity=0.1f, bool invert=false,
                                      string description="")
   {
        if (instance.AxisConfigExist(playerID, name))
        {
            Debug.LogWarning(string.Format("< {0} > already exists in player {1} input configuration.", name, playerID));
            return false;
        }
        else
        {
            AxisConfig newAxis = instance.ConstructMouseAxis(name, axisNb, sensitivity, invert, description);
            if (newAxis != null)
            {
                InputConfig playerConfig = instance.GetInputConfig(playerID);
                playerConfig.Add(name, newAxis);

                // Notification about a new axis created
                instance.ConfigurationChange.Invoke(playerID);

                return true;
            }
        }

        return false;
   }

   /// <summary>
   /// Remove `axisName` from `playerID` input configuration.
   /// </summary>
   public static bool RemoveAxis(PlayerID playerID, string axisName)
   {
        bool result = instance.GetInputConfig(playerID).Remove(axisName);
        if (result)
        {
            // Notification about a new axis created
            instance.ConfigurationChange.Invoke(playerID);
        }
        return result;
   }

   /// <summary>
   /// Return a deep copy for `axisName` from `playerID` input configuration.
   /// </summary>
   public static AxisConfig DuplicateAxis(PlayerID playerID, string axisName)
   {
        return GetAxisConfig(playerID, axisName);
   }

   /// <summary>
   /// Remove `axisName` from `playerID` input configuration and return a deep copy of it.
   /// </summary>
   public static AxisConfig PopAxis(PlayerID playerID, string axisName)
   {
        // First duplicate it
        var axis = GetAxisConfig(playerID, axisName);
        // Then delete it
        bool result = instance.GetInputConfig(playerID).Remove(axisName);
        if (result)
        {
            // Notification about a new axis created
            instance.ConfigurationChange.Invoke(playerID);
        }
        return axis;
   }

   /// <summary>
   /// Reset `axisName` from `playerID` input configuration to InputType default.
   /// Only reset internal parameters. Mapping remained intact.
   /// </summary>
   public static bool ResetAxis(PlayerID playerID, string axisName)
   {
        AxisConfig foundConf = GetAxisConfig(playerID, axisName);
        if (foundConf == null)
        {
            return false;
        }

        switch (foundConf.type)
        {
            case InputType.DigitalAxis:
                foundConf.sensitivity = 3.0f;
                foundConf.gravity = 1.0f;
                foundConf.snap = true;
                break;
            case InputType.AnalogButton:
                foundConf.invert = false;
                break;
            case InputType.Button:
                break;
            case InputType.AnalogAxis:
                foundConf.deadZone = 0.2f;
                foundConf.sensitivity = 1.0f;
                foundConf.invert = false;
                break;
            case InputType.MouseAxis:
                foundConf.sensitivity = 0.1f;
                foundConf.invert = false;
                break;
            default:
                return false;
        }

        // Notification about a new axis created
        instance.ConfigurationChange.Invoke(playerID);

        return true;
   }


// --------------------------------- Update inputs states ----------------------

    /// <summary>
    /// Update state for each configuration.
    /// </summary>
    private void UpdateInputConfig(InputConfig config)
    {
        if (config != null)
        {
            foreach(KeyValuePair<string, AxisConfig> virtualAxe in config.Axes)
            {
                virtualAxe.Value.Update();
            }
        }
    }

    private string ControllerToString(ControllerType controller)
    {
        switch (controller)
        {
            case ControllerType.Keyboard:
                return "Keyboard";
            case ControllerType.Gamepad:
                return "Gamepad";
            default:
                Debug.LogError("Unknown controller");
                return "";
        }
    }

    private bool ConfigExists(PlayerID playerID, ControllerType controller)
    {
        string savPath = string.Format(savepathTemplate, playerID,
                                       ControllerToString(controller));


        // Check if path exists
        return StreamHelper.FileExist(Application.persistentDataPath + "\\" + savPath);
    }

    public static void RaiseEventConfigurationChange(PlayerID pID)
    {
        instance.ConfigurationChange.Invoke(pID);
    }

    private void OnGamePadDisconnected(int index)
    {
        Debug.Log("Controller: " + index + " is disconnected.");
    }
    private void OnGamePadReConnected(int index)
    {
        Debug.Log("Controller: " + index + " is reconnected.");
    }
    private void OnGamePadFirstConnected(int index)
    {
// // Check for gamepad except on XBOX
// #if (!UNITY_WSA)
//                 CheckInitForGamepads();
// #endif
        Debug.Log("Controller: " + index + " is just connected.");
    }

    private void CheckInitForGamepads()
    {
        // Get Joystick Names
        string[] joystickNames = Input.GetJoystickNames();
        int nbGamepads = 0;

        if (joystickNames.Length > 0)
        {
            // Iterate over every element
            for (int i = 0; i < joystickNames.Length; ++i)
            {
                // Check if the string is empty or not
                if (!string.IsNullOrEmpty(joystickNames[i]))
                {
                    nbGamepads++;
                }
            }
        }


        // If gamepads connected assign them to players
        switch (nbGamepads)
        {
            case 4:
                SetInputController(PlayerID.One, ControllerType.Gamepad);
                SetInputController(PlayerID.Two, ControllerType.Gamepad);
                SetInputController(PlayerID.Three, ControllerType.Gamepad);
                SetInputController(PlayerID.Four, ControllerType.Gamepad);
                break;
            case 3:
                SetInputController(PlayerID.One, ControllerType.Gamepad);
                SetInputController(PlayerID.Two, ControllerType.Gamepad);
                SetInputController(PlayerID.Three, ControllerType.Gamepad);
                break;
            case 2:
                SetInputController(PlayerID.One, ControllerType.Gamepad);
                SetInputController(PlayerID.Two, ControllerType.Gamepad);
                break;
            case 1:
                SetInputController(PlayerID.One, ControllerType.Gamepad);
                break;
        }
    }
}
