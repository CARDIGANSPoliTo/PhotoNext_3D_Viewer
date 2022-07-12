using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class LogManager {
    int activeSensors = 0;

    /// <summary>
    /// Init log manager instance
    /// </summary>
    public void InitLog (){
        if(GameManager.instance.statusGame != Status.MONITORING) {
            GameManager.instance.SetErrorMessage("LogManager Error - Start Log when the system is not monitoring ");
            return;
        }

        DateTime startData = DateTime.Now;
        string filename =(new DateTimeOffset(startData).ToUnixTimeSeconds()).ToString() + "_simulation_log.csv";
        StreamWriter logFile = File.CreateText(filename);

        string filenameMesaurament = (new DateTimeOffset(startData).ToUnixTimeSeconds()).ToString() + "_measurement_log.csv";
        StreamWriter measurementFile = File.CreateText(filenameMesaurament);
        string measurementHeader = "App Timestamp, Client Timestamp, Difference(modulo)";


        string sensorHeader = "Timestamp"; 
        
        List<Sensor> sensors = GameManager.instance.GetSensorInfo().Where(e => e.Active).ToList();
        activeSensors = sensors.Count;
        foreach(Sensor s in sensors) {
            sensorHeader += $",Ch{s.Channel + 1}Gr{s.SensorID + 1}";
        }

        logFile.WriteLine(sensorHeader);
        measurementFile.WriteLine(measurementHeader);
        Debug.Log("Start simulation thread");
        Task.Run(() => { WriteOnLog(logFile, startData, filename, measurementFile,filenameMesaurament); }); 
    }

    UInt64 ts = 0;
    Int64 ts_m = 0;

    // Update is called once per frame
    void WriteOnLog (StreamWriter logFile, DateTime startData, string namefile, StreamWriter measureFile, string measureNameFile) {
        int countMeasure = 0;
        while(!GameManager.instance.TermianteThread && GameManager.instance.statusGame == Status.MONITORING) {
            string currentLine = "";
            if(GameManager.instance.LogDataQueue.Count >0 ){ //if (GameManager.instance.changeProperty) {
                countMeasure = 1;
                List<KeyValuePair<UInt64, float>> sensors;
                GameManager.instance.LogDataQueue.TryDequeue(out sensors);//.GetCurrentSensorWavelength();
                UInt64 tm = sensors.ToList().Where(e => e.Value != 0.0f).First().Key;

                if (tm > ts) {
                    ts = tm;
                    sensors = sensors.Where(e => e.Value > 0.0f).ToList();

                    if (sensors.Count != activeSensors) continue;
                    currentLine += $"{sensors[0].Key}";
                    foreach (KeyValuePair<UInt64, float> w in sensors) {
                        if (w.Value <= 0) Debug.Log(w.Value);
                        currentLine += $",{w.Value.ToString("0000.0000").Replace(",", ".")}";
                    }
                    currentLine = currentLine.Substring(0, currentLine.Length - 2);
                    logFile.WriteLine(currentLine);
                }
            }

            if (GameManager.instance.MeasurementLatencyQueue.Count > 0) { //if (GameManager.instance.changeProperty) {
                countMeasure = 1;
                KeyValuePair<Int64, Int64> coupleTimestamp;
                GameManager.instance.MeasurementLatencyQueue.TryDequeue(out coupleTimestamp);//.GetCurrentSensorWavelength();
                if ((coupleTimestamp.Value - ts_m) > 0) {
                    long difference = Math.Abs(coupleTimestamp.Value - coupleTimestamp.Key);
                    string measurementLine = $"{coupleTimestamp.Key},{coupleTimestamp.Value},{difference}";
                    measureFile.WriteLine(measurementLine);
                }
            }

        }

        while(GameManager.instance.LogDataQueue.Count > 0) { //if (GameManager.instance.changeProperty) {
            countMeasure = 1;
            List<KeyValuePair<UInt64, float>> sensors;
            GameManager.instance.LogDataQueue.TryDequeue(out sensors);//.GetCurrentSensorWavelength();
            UInt64 tm = sensors.ToList().Where(e => e.Value != 0.0f).First().Key;
            string currentLine = "";

            if (tm > ts) {
                ts = tm;
                sensors = sensors.Where(e => e.Value > 0.0f).ToList();

                if (sensors.Count != activeSensors) continue;
                currentLine += $"{sensors[0].Key}";
                foreach (KeyValuePair<UInt64, float> w in sensors) {
                    if (w.Value <= 0) Debug.Log(w.Value);
                    currentLine += $",{w.Value.ToString("0000.0000").Replace(",", ".")}";
                }
                currentLine = currentLine.Substring(0, currentLine.Length - 2);
                logFile.WriteLine(currentLine);
            }
        }

        while (GameManager.instance.MeasurementLatencyQueue.Count > 0) { //if (GameManager.instance.changeProperty) {
            countMeasure = 1;
            KeyValuePair<Int64, Int64> coupleTimestamp;
            GameManager.instance.MeasurementLatencyQueue.TryDequeue(out coupleTimestamp);//.GetCurrentSensorWavelength();
            if ((coupleTimestamp.Value - ts_m) > 0) {
                long difference = Math.Abs(coupleTimestamp.Value - coupleTimestamp.Key);
                string measurementLine = $"{coupleTimestamp.Key},{coupleTimestamp.Value},{difference}";
                measureFile.WriteLine(measurementLine);
            }
        }
        logFile.Close();
        measureFile.Close();
        if (countMeasure == 0) {
            File.Delete(namefile);
            File.Delete(measureNameFile);
        }
        else {
            // Create graph using a phython script
            Task.Run(() => {
                string[] parts = namefile.Split('.');
                parts = parts.Take(parts.Count() - 1).ToArray();
                string onlyname = string.Join(".", parts);

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C python create_graph.py " + ".\\" + namefile + " " + onlyname + "_graph";
                process.StartInfo = startInfo;
                process.Start();
                //process.StandardInput.Flush();
                //process.StandardInput.Close();
                process.WaitForExit();
            });
        }
        Debug.Log("End simulation thread");
    }
}
