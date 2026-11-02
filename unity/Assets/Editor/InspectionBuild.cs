using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class InspectionBuild
{
    [MenuItem("Inspection/Build USB Android APK")]
    public static void BuildUsbAndroid()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Inspection.unity");
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var events in root.GetComponentsInChildren<EventSystem>(true))
            {
                var legacy = events.GetComponent<StandaloneInputModule>();
                if (legacy != null) Object.DestroyImmediate(legacy);
                var module = events.GetComponent<InputSystemUIInputModule>();
                if (module == null) module = events.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
            foreach (var controller in root.GetComponentsInChildren<InspectionController>(true))
                controller.apiBaseUrl = "http://127.0.0.1:8080";
        }
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        settings.FindProperty("activeInputHandler").intValue = 1;
        settings.ApplyModifiedProperties();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Inspection.unity" },
            locationPathName = "Builds/inspection.apk",
            target = BuildTarget.Android, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Android APK build failed; inspect the Console and Editor.log.");
        Debug.Log("APK_BUILD_SUCCEEDED: " + Path.GetFullPath("Builds/inspection.apk"));
    }
}
