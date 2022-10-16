using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FFD))]
public class FFDInspector : Editor
{
    private FFD ffdGrid;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private int selectedIndex = -1;
    private void OnSceneGUI ()
    {
        ffdGrid = target as FFD;
        handleTransform = ffdGrid.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
        

        for (int i = 0; i < ffdGrid.ControlPointCount; i ++)
        {
            Vector3 p1 = ShowControlPoint(i);

            Handles.color = Color.blue;
        }
    }

    private Vector3 ShowControlPoint ( int index )
    {
        Vector3 point = handleTransform.TransformPoint(ffdGrid.GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);

        Handles.color = Color.blue;
        if (Handles.Button(point, handleRotation, handleSize * size, pickSize * size, Handles.DotHandleCap))
        {
            selectedIndex = index;
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ffdGrid, "Move Point");
                ffdGrid.SetControlPoint(index,handleTransform.InverseTransformPoint(point));
                EditorUtility.SetDirty(ffdGrid);
            }
        }
        return point;
    }


    public override void OnInspectorGUI ()
    {
        DrawDefaultInspector();
        ffdGrid = target as FFD;

        if (GUILayout.Button("Reset Control Points"))
        {
            Undo.RecordObject(ffdGrid, "Reset Position");
            EditorUtility.SetDirty(ffdGrid);
            ffdGrid.ResetControlPoints();
        }

        if (GUILayout.Button("Create FFD"))
        {
            Undo.RecordObject(ffdGrid, "Reset Position");
            EditorUtility.SetDirty(ffdGrid);
            ffdGrid.CreateFFDGrill();
        }

    }
}
