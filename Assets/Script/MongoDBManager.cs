using System;

using System.Collections.Generic;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

using UnityEngine;

public class MongoDBManager : MonoBehaviour {
    // Class Variables
    //-----------------------------------------------------------------------------
    IMongoClient                   client = null;
    IMongoDatabase                 db;
    IMongoCollection<BsonDocument> collection;
    public bool localDatabase = true;

    public MongoInformation mongoInfo { get; set; }

    // Init method
    //-----------------------------------------------------------------------------
    #region INIT_METHOD
    /// <summary>
    /// Method that initialize the mongo's variable
    /// </summary>
    /// <returns>Return true if the mongo client was created, 
    ///          false otherwise </returns>
    public bool InitMongoDB () {
        if (client == null) {
            client = new MongoClient("mongodb://" + mongoInfo.DBName + ":hardcodeYourPasswordHere@" + mongoInfo.IPAddress + ":" + mongoInfo.Port + "/" + mongoInfo.DBName);
            db = client.GetDatabase(mongoInfo.DBName);

            GameManager.instance.SetErrorMessage("MongoDB Network Configuration - Pinging " + mongoInfo.DBName + " ...");
            if (db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000)) {
                GameManager.instance.SetErrorMessage("Network Configuration Mongo - Connected to " + mongoInfo.DBName);

                GameManager.instance.SetErrorMessage("MongoDB Network Configuration - You must check if collection " + mongoInfo.CollectionName + "  exists");
                bool exists = db.ListCollections(new ListCollectionsOptions { Filter =  new BsonDocument("name", mongoInfo.CollectionName)}).Any();
                if (exists) {
                    GameManager.instance.SetErrorMessage("MongoDB Network Configuration - The collection " + mongoInfo.CollectionName + " already exists");
                    collection = db.GetCollection<BsonDocument>(mongoInfo.CollectionName);
                    return true;
                }
                else {
                    GameManager.instance.SetErrorMessage("MongoDB Network Configuration - The collection " + mongoInfo.CollectionName + " does not exist: operation aborted");
                    client = null;
                    db = null;
                    return false;
                }
            }
            else {
                GameManager.instance.SetErrorMessage("MongoDB Network Configuration - it is not possible to connect to " + mongoInfo.DBName);
                client = null;
                return false;
            }
        }
        else
            return true;
    }
    #endregion

    // Manage information methods
    //-----------------------------------------------------------------------------
    #region QUERY_METHOD
    /// <summary>
    /// Method to get the sensors configurations
    /// </summary>
    public void RequestSensorsConfiguration () {
        List<Sensor> sensors = new List<Sensor>();
        var filter = Builders<BsonDocument>.Filter.Eq("type", "config");
        var results = collection.Find(filter).ToList();
        foreach (BsonDocument doc in results)
        {
            int channel = doc.GetValue("channel").AsInt32;
            int grating = doc.GetValue("grating").AsInt32;
            bool is_active = doc.GetValue("is_active").AsBoolean;
            float wavelength_idle = (float)doc.GetValue("wavelength_idle").AsDouble;
            float wavelength_var = (float)doc.GetValue("wavelength_var").AsDouble;
            Vector3 position = new Vector3((float)doc.GetValue("position_x").AsDouble, (float)doc.GetValue("position_y").AsDouble, (float)doc.GetValue("position_z").AsDouble);

            Sensor sensor = new Sensor(grating, wavelength_idle, wavelength_var, is_active);
            sensor.Channel = channel;
            sensor.Position = position;
            sensors.Add(sensor);
        }
        UnityMainThreadDispatcher.Instance().Enqueue(new Action(() => {
            GameManager.instance.UpdateSensorsInfo(sensors);
        }));

    }

    /// <summary>
    /// Method to start thread that get data sensors from database
    /// </summary>
    /// <returns>True if the task was safetly created, 
    ///          false otherwise</returns>
    public bool StartRequestSensorData () {
        try {
            Task.Run(() => UpdateSensorInformtion());
        }
        catch (Exception e) {
            Debug.Log($"Exception on StartRequestSensorData - {e}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Method that ask to the database
    /// </summary>
    void UpdateSensorInformtion () {
        Vector4[] properties = new Vector4[64];
        
        int id;
        int channel;
        float radius;
        float intensity;
        float temperature;
        Sensor sensorInfo;

        for(int i=0; i< properties.Length; i++)
            properties[i] = new Vector2(GameManager.instance.defaultProperties.x, GameManager.instance.defaultProperties.y);

        while (true) {
            if (GameManager.instance.TermianteThread) {
                Debug.Log("Terminate second thread\n");
                break;
            }

            System.Threading.Thread.Sleep(100);

            var filter = Builders<BsonDocument>.Filter.Exists("event", true);//.Empty;// Eq("temperature", 23.7);
            var result = collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("event.timestamp")).Limit(64).ToList();

            foreach (var doc in result) {
                var evdata = doc["event"];
                id = evdata["sensor_id"].ToInt32();
                channel = evdata["channel_id"].ToInt32();

                sensorInfo = GameManager.instance.SensorsFromNetwork[channel * 16 + id];

                //float temperature = GameManager.instance.CurrentSensorTemperature[i] = (float)evdata["temperature"].ToDouble();
                GameManager.instance.CurrentSensorWavelength[channel * id + 16] = new KeyValuePair<UInt64, float>(Convert.ToUInt64(evdata["timestamp"].ToString()),(float)evdata["temperature"].ToDouble());
                temperature = (float)evdata["temperature"].ToDouble();

                if (temperature == 0) {
                    radius = 0;
                    intensity = 0;
                }
                else {
                    if (sensorInfo.WavelenghtIdle == 0)
                        sensorInfo.WavelenghtIdle = temperature;

                    if (temperature > sensorInfo.MaxWavelenght)
                        sensorInfo.MaxVariation = temperature - sensorInfo.WavelenghtIdle;

                    UnityMainThreadDispatcher.Instance().Enqueue(new Action(() => {
                        GameManager.instance.UpdateSensorInfo(sensorInfo);
                    }));

                    radius    = ((temperature - sensorInfo.WavelenghtIdle) / GameManager.instance.globalMaxVariation) * (0.75f - 0.2f);
                    intensity = ((temperature - sensorInfo.WavelenghtIdle) / GameManager.instance.globalMaxVariation) * (2.5f - 0.5f);
                }
                UnityMainThreadDispatcher.Instance().Enqueue(new Action(() => {
                    GameManager.instance.SensorRadiuses[channel * 16 + id] = radius;
                    GameManager.instance.SensorIntensities[channel * 16 + id] = intensity;
                }));
                properties[channel * 16 + id] = new Vector2(radius, intensity);

            }
            UnityMainThreadDispatcher.Instance().Enqueue(new Action(() => {
                GameManager.instance.SetNewSensorProperty(properties);
            }));
        }
    }
    #endregion
}
