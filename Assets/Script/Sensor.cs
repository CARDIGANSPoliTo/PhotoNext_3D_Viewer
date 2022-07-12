using UnityEngine;

/// <summary>
/// Class that contains all the sensors information
/// </summary>
public struct Sensor {
    // Private Variables
    //-----------------------------------------------------------------------------
    private float _maxVariation;
    private float _wavelenghtIdle;
    private Vector3 position;

    public float MaxWavelenght;

    // Public Properties
    //-----------------------------------------------------------------------------
    public int SensorID { get; set; }
    public int Channel  { get; set; }
    public bool Active  { get; set; }

    public float WavelenghtIdle {
        get { return _wavelenghtIdle; }
        set {
            _wavelenghtIdle = value;
            
            MaxWavelenght = _wavelenghtIdle + _maxVariation;
            if (GameManager.instance.globalMaxVariation < _maxVariation) {
                GameManager.instance.globalMaxVariation = _maxVariation;
            }
        }
    }

    public float MaxVariation {
        get { return _maxVariation; }
        set {
            _maxVariation = value;
            
            MaxWavelenght = WavelenghtIdle + _maxVariation;
            if (GameManager.instance.globalMaxVariation < _maxVariation) {
                GameManager.instance.globalMaxVariation = _maxVariation;
            }
        }
    }

    public Vector3 Position {
        get {
            return position;
        }
        set {
            position = value;
        }
    }

    /// <summary>
    /// Sensor public constructor
    /// </summary>
    /// <param name="id">Id sensor in channel</param>
    /// <param name="idle">Wavelenght idle value</param>
    /// <param name="variation">Wavelenght variation</param>
    /// <param name="isActive">True if the sensor is active, false otherwise</param>
    public Sensor ( int id, float idle, float variation, bool isActive) {
        SensorID = id;
        _wavelenghtIdle = idle;
        _maxVariation = variation;
        MaxWavelenght = idle + variation;
        position = Vector3.zero;
        Channel = 1;
        Active = isActive;
    }

    /// <summary>
    /// Update sensor position
    /// </summary>
    /// <param name="position">Coordinate position</param>
    public void UpdatePosition ( Vector3 position ) {
        Position = position;
    }
}
