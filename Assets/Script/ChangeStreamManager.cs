using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;

public class ChangeStreamManager : MonoBehaviour
{

    public string IPAddress { get ; set ; }
    public int Port { get; set; }
    public string DbName { get; set; }
    public string CollName { get; set; }
    
    private MongoClient client = null;

    private IMongoDatabase database = null;

    public int millisToUpdateGraph = 1000;

    long timePassed = 0;

    public bool SetupChangeStream()
    {
        if (client == null)
        {
            client = new MongoClient("mongodb://"+DbName+":hardcodeYourPasswordHere@"+IPAddress+":"+Port+"/"+DbName);
            database = client.GetDatabase(DbName);

            GameManager.instance.SetErrorMessage("MongoDB Network Configuration - Pinging Database...");
            if (database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000))
            {
                GameManager.instance.SetErrorMessage("MongoDB Network Configuration - Ping OK");
                return true;
            }
            else
            {
                GameManager.instance.SetErrorMessage("MongoDB Network Configuration - it is not possible to connect to Database");
                client = null;
                return false;
            }
        }
        else
            return true;
    }

    public void StartWatch()
    {
        var collectionName = CollName;
        var collection = database.GetCollection<BsonDocument>(collectionName);
        Debug.Log("ChangeStreamManager - Starting watch Database");

        Vector4[] properties = new Vector4[64];
        List<KeyValuePair<UInt64, float>> wavelength_data = GameManager.instance.GetCurrentSensorWavelength();

        //TODO: Antonio - Better filter for watch?
        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Match("{ operationType: { $in: ['insert'] } }");
        while (!GameManager.instance.TermianteThread)
        {
            using (var cursor = collection.Watch(pipeline,options))
            {
                while (cursor.MoveNext() && cursor.Current.Count() == 0 && !GameManager.instance.TermianteThread)
                {
                    //Debug.Log("ChangeStreamManager : Waiting for data");
                } // keep calling MoveNext until we've read the first batch

                var result = cursor.Current;
                foreach (var elem in result)
                {
                    var next = elem.FullDocument;
                    int id = next.GetValue("index").AsInt32;
                    if (GameManager.instance.SensorsFromNetwork[id].Active)
                    {
                        float wavelenght = (float)next.GetValue("wavelength").AsDouble;
                        UInt64 timestamp = (UInt64)next.GetValue("timestamp").AsInt64;
                        Sensor sensorInfo = GameManager.instance.SensorsFromNetwork[id];

                        GameManager.instance.CurrentSensorWavelength[id] = new KeyValuePair<UInt64, float>(timestamp, wavelenght);
                        wavelength_data[id] = new KeyValuePair<UInt64, float>(timestamp, wavelenght);

                        float radius = 0;
                        float intensity = 0;

                        if (wavelenght != 0)
                        {
                            if (sensorInfo.WavelenghtIdle == 0)
                            {
                                sensorInfo.WavelenghtIdle = wavelenght;
                            }

                            if (wavelenght > sensorInfo.MaxWavelenght)
                            {
                                sensorInfo.MaxVariation = wavelenght - sensorInfo.WavelenghtIdle;
                            }

                            UnityMainThreadDispatcher.Instance().Enqueue(new Action(() =>
                            {
                                GameManager.instance.UpdateSensorInfo(sensorInfo);
                            }));

                            radius = (Mathf.Abs(wavelenght - sensorInfo.WavelenghtIdle) / GameManager.instance.globalMaxVariation) * (0.35f - 0.02f); //sensorInfo.MaxVariation) * (0.25f - 0.2f) + 0.2f;
                            intensity = (Mathf.Abs(wavelenght - sensorInfo.WavelenghtIdle) / GameManager.instance.globalMaxVariation) * (0.55f - 0.05f);//sensorInfo.MaxVariation) * (2.5f - 1.0f) + 1.0f;
                        }
                        properties[id] = new Vector2(radius, intensity);
                        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        if (now - timePassed > millisToUpdateGraph)
                        {
                            timePassed = now;
                            ulong currentTimestamp = (UInt64)next.GetValue("curr_time").AsInt64;
                            UnityMainThreadDispatcher.Instance().Enqueue(new Action(() =>
                            {
                                GameManager.instance.UpdateData(properties.ToArray(), wavelength_data, now, (long)currentTimestamp);
                            }));
                        }
                    }
                }
            }
        }
        Debug.Log("End of serve data client");
    }

    public void GetConfiguration()
    {
        List<Sensor> sensors = new List<Sensor>();
        var collectionName = CollName;
        var collection = database.GetCollection<BsonDocument>(collectionName);
        Debug.Log("ChangeStreamManager - Getting sensor configuration" + collectionName);
        var filter = Builders<BsonDocument>.Filter.Eq("type","config");
        var results = collection.Find(filter).ToList();
        foreach(BsonDocument doc in results)
        {
            int     channel         = doc.GetValue("channel").AsInt32;
            int     grating         = doc.GetValue("grating").AsInt32;
            bool    is_active       = doc.GetValue("is_active").AsBoolean;
            float   wavelength_idle = (float)doc.GetValue("wavelength_idle").AsDouble;
            float   wavelength_var  = (float)doc.GetValue("wavelength_var").AsDouble;
            Vector3 position        = new Vector3((float)doc.GetValue("position_x").AsDouble, (float)doc.GetValue("position_y").AsDouble, (float)doc.GetValue("position_z").AsDouble);

            Sensor sensor = new Sensor(grating, wavelength_idle, wavelength_var, is_active);
            sensor.Channel = channel;
            sensor.Position = position;
            sensors.Add(sensor);
        }
        UnityMainThreadDispatcher.Instance().Enqueue(new Action(() => {
            GameManager.instance.UpdateSensorsInfo(sensors);
        }));
    }
}
