using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine;

[CustomEditor(typeof(SplineShape))]
public class SplineShapeEditor : Editor
{
    void OnEnable()
    {
        foreach(var t in targets)
            ( (SplineShape) t ).spline.changed += UpdateProBuilderDisplay;
    }

    void OnDisable()
    {
        foreach(var t in targets)
        {
            if(t != null && ((SplineShape)t).spline != null)
                ((SplineShape)t).spline.changed -= UpdateProBuilderDisplay;
        }
    }

    void UpdateProBuilderDisplay()
    {
        EditorApplication.delayCall += () => UpdateProBuilderEditor();
    }

    void UpdateProBuilderEditor()
    {
        ProBuilderEditor.Refresh(true);
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if(EditorGUI.EndChangeCheck())
        {
            UpdateProBuilderEditor();
        }

        foreach(var t in targets)
        {
            if(((SplineShape)t).isPBEditorDirty)
            {
                UpdateProBuilderEditor();
                ( (SplineShape) t ).isPBEditorDirty = false;
            }
        }
    }

}
