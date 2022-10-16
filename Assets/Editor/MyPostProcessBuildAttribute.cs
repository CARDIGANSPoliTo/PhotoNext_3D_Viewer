using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class MyPostProcessBuildAttribute
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject) {
        string pathPy = Path.Combine(Directory.GetCurrentDirectory(), "create_graph.py");

        if (File.Exists(pathPy)) {
            string desthPy = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "create_graph.py");

            Debug.Log(desthPy);
            if (!File.Exists(desthPy)) {
                using (var fileS = File.OpenRead(pathPy)) {
                    using (var fileD = File.Create(desthPy)) {
                        fileS.CopyTo(fileD);
                    }
                }
            }
        }
    }
}
