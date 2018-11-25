using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A container of AxisConfig.
/// </summary>
public sealed class InputConfig
{
	public string name;

    /// <summary>
    /// Container of axis to handle in this configuration
    /// </summary>
    [SerializeField]
    private Dictionary<string, AxisConfig> axes = new Dictionary<string, AxisConfig>();

    public InputConfig(string name="New Configuration")
    {
        axes = new Dictionary<string, AxisConfig>();
        this.name = name;
    }

    /// <summary>
    /// Allows to duplicate an existing configuration.
    /// </summary>
    public static InputConfig Duplicate(InputConfig refConfig)
    {
        var inputConfig = new InputConfig();
        inputConfig.name = refConfig.name;
        inputConfig.axes = new Dictionary<string, AxisConfig>();

        // Populate with source axis configurations
        foreach(KeyValuePair<string, AxisConfig> virtualAxe in refConfig.axes)
        {
            inputConfig.axes.Add(virtualAxe.Key,
                                 AxisConfig.Duplicate(virtualAxe.Value));
        }

        return inputConfig;
    }

    /// <summary>
    /// Remove `axisName` from `refConfig` input configuration and return a deep copy of it.
    /// </summary>
    public static AxisConfig PopAxis(InputConfig refConfig, string axisName)
    {
        AxisConfig axisCopy = null;
        if (refConfig.Axes.ContainsKey(axisName))
        {
            axisCopy = AxisConfig.Duplicate(refConfig.Axes[axisName]);
            refConfig.Remove(axisName);
        }

        return axisCopy;
    }

    public Dictionary<string, AxisConfig> Axes
    {
        get {return axes;}
    }

    public bool Add(string name, AxisConfig config)
    {
        if (axes.ContainsKey(name))
        {
            return false;
        }
        axes.Add(name, config);
        return true;
    }

    public bool ContainsKey(string name)
    {
        return axes.ContainsKey(name);
    }

    public int Count
    {
        get {return axes.Count;}
    }

    public bool Remove(string name)
    {
        try
        {
            axes.Remove(name);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }


    // // -------------------------------------------------------------------------
    // // Support for a One level deep Serialization in Unity
    // [SerializeField]
    // private List<string> _keys = new List<string>();

    // [SerializeField]
    // private List<AxisConfig> _values = new List<AxisConfig>();

    // public void OnBeforeSerialize()
    // {
    //     _keys.Clear();
    //     _values.Clear();
    //     foreach (KeyValuePair<string, AxisConfig> pair in axes)
    //     {
    //         _keys.Add(pair.Key);
    //         _values.Add(pair.Value);
    //     }
    // }

    // public void OnAfterDeserialize()
    // {
    //     axes.Clear();

    //     if (_keys.Count != _values.Count)
    //     {
    //         // Bad serialization
    //         string message = "There are {0} _keys and {1} _values after deserialization.";
    //         throw new System.Exception(string.Format(message, _keys.Count, _values.Count));
    //     }


    //     for (int i = 0; i < _keys.Count; i++)
    //     {
    //         axes.Add(_keys[i], _values[i]);
    //     }
    // }
    // // -------------------------------------------------------------------------

}
