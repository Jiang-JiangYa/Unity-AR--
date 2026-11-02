using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public static class InspectionSetup
{
    [MenuItem("Inspection/Create Android AR Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Generated");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
        var root = new GameObject("XR Origin"); var origin = root.AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset"); offset.transform.SetParent(root.transform,false); origin.CameraFloorOffsetObject=offset;
        var cameraObject = new GameObject("AR Camera"); cameraObject.tag="MainCamera";cameraObject.transform.SetParent(offset.transform,false);
        var camera = cameraObject.AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;origin.Camera=camera;
        cameraObject.AddComponent<AudioListener>();cameraObject.AddComponent<ARCameraManager>();cameraObject.AddComponent<ARCameraBackground>();
        var driver = cameraObject.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(new InputAction("Position",InputActionType.Value,binding:"<HandheldARInputDevice>/devicePosition"));
        driver.rotationInput = new InputActionProperty(new InputAction("Rotation",InputActionType.Value,binding:"<HandheldARInputDevice>/deviceRotation"));
        // This handheld layout exposes pose, not the XRHMD trackingState control.
        // Placement/submission is gated by ARSession.state in InspectionController.
        driver.ignoreTrackingState = true;
        var planes = root.AddComponent<ARPlaneManager>(); planes.requestedDetectionMode=PlaneDetectionMode.Horizontal;
        var plane = new GameObject("Detected horizontal plane",typeof(ARPlane),typeof(MeshFilter),typeof(MeshRenderer),typeof(ARPlaneMeshVisualizer));
        const string materialPath="Assets/Generated/Plane.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Unlit/Color"));material.color=new Color(.1f,.65f,.58f);AssetDatabase.CreateAsset(material,materialPath);}
        plane.GetComponent<MeshRenderer>().sharedMaterial=material;
        planes.planePrefab=PrefabUtility.SaveAsPrefabAsset(plane,"Assets/Generated/Plane.prefab");Object.DestroyImmediate(plane);
        var controller=root.AddComponent<InspectionController>();controller.planes=planes;controller.raycasts=root.AddComponent<ARRaycastManager>();
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule))
            .GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var light = new GameObject("Light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
        PlayerSettings.productName="Park Inspection";PlayerSettings.companyName="Prototype";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.example.parkinspection");
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.insecureHttpOption=InsecureHttpOption.AlwaysAllowed;
        PlayerSettings.Android.forceInternetPermission=true;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        // Android uses one input backend for touch, UI and tracked camera pose.
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;settings.ApplyModifiedProperties();}
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Inspection.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Inspection.unity",true)};
        AssetDatabase.SaveAssets();
        Debug.Log("Scene created. Enable Android ARCore under XR Plug-in Management, use Input System Package (New), restart Editor if requested, then build Android.");
    }
}
