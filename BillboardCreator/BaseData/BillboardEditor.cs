    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEngine.SceneManagement;
public class BillboardEditor : EditorWindow
{
    static BillboardGenerator billboardGen = new BillboardGenerator();

    private GameObject autoAddedCamera;
    private GameObject previousObject;
    private GameObject targetObject;
    private GameObject anyObject;
    private Material autoMaterial;
    private Material boardMaterial;

    private Shader autoShader;
    private Shader depthShader;

    private Shader boardShader;

    private Camera captureCam;

    private GameObject[] allObject;
    private Light[] allLights;

    private bool hasGeneratedMesh = false;

    private int resolutionLevel = 8;
    private int finalRes;
    private int previousResolution;

    private float yOffset = 0f;
    private float xOffset = 0f;
    private float zOffset = 0f;

    private float normalSample = 0.5f;
    private float transitionScale = 0.5f;
    private float depthX = 1;
    private float depthY = 1;
    private float depthZ = 1;

    private string originalName;

    private string filePath = "BillboardCreator/Resources";
    private string textureName;
    private string meshName;
    private string prefabName;
    private string materialName;
    private string finalMaterialName;

    private string furnaceState;

    private bool cullMiddle = false;
    private bool hasPressedPreviewButton = false;
    private bool includeMaterial = false;
    private bool manualName = false;
    private bool manualSave = false;
    private bool softNormal = false;
    private bool previousSoftNormal = false;
    private bool detailNormal = false;
    private float blend = 0.3f;
    private Vector3 offset;

    [MenuItem("Tools/Billboard Tool")]
    public static void OpenWindow() 
    {
        BillboardEditor window = GetWindow<BillboardEditor>("Billboard Tool");
        window.minSize = new Vector2(400, 700);
        window.Show();
    }

    private void AddCamera() 
    {
        GameObject newCam = new GameObject();
        newCam.name = "*Billboard_Capture_Camera";
        captureCam = newCam.AddComponent<Camera>();
        captureCam.clearFlags = CameraClearFlags.Nothing;
        autoAddedCamera = newCam;
        billboardGen.captureCamera = captureCam;
        Debug.Log("Auto added capture camera:" + captureCam.name);

    }
    private void OnEnable()
    {
        
        billboardGen = new BillboardGenerator();

        AddCamera();
       
        AssetDatabase.Refresh();
      
        string[] paths =Directory.GetFiles(Application.dataPath, "M_Billboard.mat", SearchOption.AllDirectories);
        if (paths.Length == 0) 
        {
            Debug.Log("Couldn't find 'M_Billboard' material，please add it manually");
        }
        string materialPath = paths[0].Replace("\\","/").Replace(Application.dataPath,"");
        Material targetMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets"+materialPath, typeof(Material));
        if (targetMaterial != null)
        {
            autoMaterial = targetMaterial;
            boardShader = autoMaterial.shader;
            Debug.Log("Auto added material: M_Billboard");
        }

        autoShader = Shader.Find("Billboard/S_ShowDepth");
        

        allObject = FindObjectsOfType<GameObject>();

    }
    private void OnDisable()
    {
        ToggleLighting(true);
        if (targetObject != null) 
        {
            Reset(targetObject);
            targetObject.name = originalName;
        }
        if (autoAddedCamera != null) 
        {
            DestroyImmediate(autoAddedCamera);
        }
       
        billboardGen = null;
    }

    
    private void Reset( GameObject _targetObject)
    {
        billboardGen.targetObject = _targetObject;
        billboardGen.includeChildren = _targetObject.transform.childCount != 0;

        yOffset = 0;
        xOffset = 0;
        zOffset = 0;
        
        if (_targetObject.transform.childCount != 0)
        {
            billboardGen.SetReference();
            billboardGen.ClearMesh();
            billboardGen.RemoveRenderer();

        }
        else 
        {
            billboardGen.ReturnMateiralMesh(_targetObject == previousObject);
        }
        hasGeneratedMesh = false;
        billboardGen.ClearTexture();
        AssetDatabase.Refresh();
    }

    private void OnHierarchyChange()
    {

        if (targetObject != null)
        {
            allObject = FindObjectsOfType<GameObject>();
            foreach (GameObject g in allObject)
            {
                if (g.GetComponent<MeshRenderer>()) 
                {
                    if (g.GetComponent<MeshRenderer>().sharedMaterial ==boardMaterial &&
                        g.GetInstanceID()!= targetObject.GetInstanceID()) 
                    {
                        g.name = "*Please Replace Material";
                        g.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                        g.SetActive(false);
                        Debug.LogWarning("Only one gameobject in the hierarchy can take 'M_Billboard'");
                        break;
                    }          
                }
                if (g.name.Contains("(Billboard)") &&
                    g.GetInstanceID() != targetObject.GetInstanceID()) 
                {
                    g.name = g.name.Replace("(Billboard)", "");
                }
               
            }
            if (targetObject.name != originalName + "(Billboard)") 
            {
                targetObject.name = originalName + "(Billboard)";
                Debug.LogWarning("Target name is not editable");
            }
        }
        if (captureCam == null) 
        {
            AddCamera();
        }
        
    }
    private void OnGUI()
    {
        if (billboardGen == null) { Debug.LogError("Could not find 'BillboardGenerator' class"); return; }

        previousObject = targetObject;
        previousResolution = resolutionLevel;
        previousSoftNormal = softNormal;

        EditorGUILayout.Space();
       
        if (autoMaterial != null)
        {
            boardMaterial = autoMaterial;
        }
        else 
        {
            EditorGUILayout.LabelField("Please manually add billboard material", EditorStyles.boldLabel);

            boardMaterial = (Material)EditorGUILayout.ObjectField(boardMaterial, typeof(Material), false);
            autoMaterial = boardMaterial;
        }
        if (autoShader != null)
        {
            depthShader = autoShader;
        }
        else 
        {
            depthShader = (Shader)EditorGUILayout.ObjectField("Depth Shader", depthShader, typeof(Shader), true);
            autoShader = depthShader;
        }

        

        if (boardMaterial != null) 
        {          
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(targetObject == null?"Select target object":"Target Object: "+targetObject.name.Replace("(Billboard)",""), EditorStyles.boldLabel);
            anyObject = (GameObject)EditorGUILayout.ObjectField(anyObject, typeof(GameObject), true);
        }
        

        if (anyObject != null)
        {
            GameObject examObject = anyObject.transform.root.gameObject;
            if (
              examObject.transform.childCount == 0 &&
              (!examObject.GetComponent<MeshRenderer>() ||
              !examObject.GetComponent<MeshFilter>()))
            {
                Debug.LogWarning("This is probably not a 3D gameobject.");

                anyObject = previousObject == null ? null : previousObject;
            }
            else
            {
                targetObject = examObject;
            }
        }
        else 
        {
            if (targetObject != null) 
            {
                anyObject = targetObject;
            }
        
        }

        furnaceState = hasPressedPreviewButton ? "Lights Out" : "Lights Back";
        if (GUILayout.Button(furnaceState)) 
        {
           
            if (!hasPressedPreviewButton)
            {
                Debug.Log("Furnace Ambient has been set up");
                hasPressedPreviewButton = true;
                ToggleLighting(false);
               
            }
            else 
            {
                Debug.Log("Furnace Ambient has been shut down");

                hasPressedPreviewButton = false;
                ToggleLighting(true);
                
            }
        }

        if (targetObject != null) 
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Refresh Mesh：", EditorStyles.boldLabel);
            cullMiddle = EditorGUILayout.Toggle("Cull center board", cullMiddle);
            billboardGen.cullMiddle = cullMiddle;

            if (!cullMiddle)
            {

                yOffset = EditorGUILayout.Slider("Y-Shift", yOffset, -1f, 1f);
            }

            xOffset = EditorGUILayout.Slider("X-Shift", xOffset, -1f, 1f);
            zOffset = EditorGUILayout.Slider("Z-Shift", zOffset, -1f, 1f);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Refresh Texture：", EditorStyles.boldLabel);
            resolutionLevel = EditorGUILayout.IntSlider("Texture Resolution", resolutionLevel, 3, 8);
            finalRes = (int)Mathf.Pow(2, resolutionLevel);

            softNormal = EditorGUILayout.Toggle("Soften Normal", softNormal);
            billboardGen.softNormal = softNormal;
            if (softNormal != previousSoftNormal)
            {
                billboardGen.ToggleChildrenVisibility(true);
                billboardGen.GenerateTexture();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Post-Processes：", EditorStyles.boldLabel);
            
            transitionScale = EditorGUILayout.Slider(transitionScale == 0f ? "Dither pattern is off":"Dither pattern resolution", transitionScale,0f,1f);
            billboardGen.boardMat.SetFloat("_ToonScale", transitionScale);

            offset.y = EditorGUILayout.Slider("Spherical normal height",offset.y,-billboardGen.boundSize.y/2, billboardGen.boundSize.y / 2);

            detailNormal = EditorGUILayout.Toggle("Normal details", detailNormal);
            if (detailNormal) 
            {
                normalSample = EditorGUILayout.Slider("Normla sample interval", normalSample, 0f, 1f);
                blend = EditorGUILayout.Slider("Detail blend", blend, 0f, 1f);
                depthY = EditorGUILayout.Slider("Y-Depth", depthY, 0f, 1f);
                depthX = EditorGUILayout.Slider("X-Depth", depthX, 0f, 1f);
                depthZ = EditorGUILayout.Slider("Z-Depth", depthZ, 0f, 1f);

                billboardGen.boardMat.SetFloat("_Precision", normalSample);
                billboardGen.boardMat.SetFloat("_DepthY", depthY);
                billboardGen.boardMat.SetFloat("_DepthX", depthX);
                billboardGen.boardMat.SetFloat("_DepthZ", depthZ);
                billboardGen.boardMat.SetFloat("_Blend", blend);
            }
            billboardGen.boardMat.SetFloat ("_Detail", detailNormal? 1 : 0);
            billboardGen.boardMat.SetVector("_SOffset", offset);


        }

      
        if (billboardGen.targetObject == null || billboardGen.targetObject == previousObject)
        {
            billboardGen.targetObject = targetObject;
        }
        if (billboardGen.targetObject != null) 
        {
            billboardGen.targetObject.transform.rotation = Quaternion.identity;
            billboardGen.targetObject.transform.localScale = Vector3.one;
        }
        if (billboardGen.targetObject != previousObject)
        {

            prefabName = null;
            if (previousObject == null)
            {
                if (targetObject.transform.childCount == 0)
                {
                    Debug.Log("Target added" + targetObject.name +"(Single Object)");
                    billboardGen.includeChildren = false;
                }
                else
                {
                    Debug.Log("Target added" + targetObject.name +"(Children Group)");
                    billboardGen.includeChildren = true;
                }
            }
            else
            {
                if (targetObject.transform.childCount == 0)
                {
                    Debug.Log("Target has been changed" + targetObject.name + "(Single Object)");
                    billboardGen.includeChildren = false;
                }
                else
                {
                    Debug.Log("Target has been changed" + targetObject.name + "(Children Group)");
                    billboardGen.includeChildren = true;
                }

            }
            if (billboardGen.targetObject == null)
            {
                Debug.Log("Target has lost");
            }
            else
            {
                if (targetObject != null && previousObject != null)
                {
                    Reset(previousObject);
                    Reset(targetObject);
                    previousObject.name = originalName;
                }

                if (targetObject.transform.childCount == 0 && targetObject != null)
                {
                    billboardGen.StoreMaterialMesh();
                }
            }

            originalName = (string)targetObject.name.Clone();
            targetObject.name = originalName + "(Billboard)";
            EditorGUIUtility.PingObject(targetObject);
        }
        billboardGen.boardMat = boardMaterial;
        billboardGen.depthShader = depthShader;
        billboardGen.maxResolution = finalRes;

        if (hasGeneratedMesh)
        {
            billboardGen.ToggleChildrenVisibility(true);
            billboardGen.GenerateBillboard(yOffset, xOffset, zOffset);
        }

        if (resolutionLevel != previousResolution)
        {

            if (hasGeneratedMesh)
            {
                billboardGen.ToggleChildrenVisibility(true);
                billboardGen.GenerateTexture();
            }
        }
        

        if (hasGeneratedMesh) 
        {

            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Folder：", EditorStyles.boldLabel);

            filePath = EditorGUILayout.TextField("Assets/", filePath);
            {
                EditorGUILayout.Space();
                manualName = EditorGUILayout.Toggle("Manual Prefix", manualName);

                if (manualName)
                {
                    textureName = EditorGUILayout.TextField("Texture Prefix", textureName == null? "T":textureName);
                    materialName = EditorGUILayout.TextField("Material Prefix", materialName == null? "M":materialName);
                    meshName = EditorGUILayout.TextField("Mesh Prefix", meshName == null? "B":meshName);
                }
                else 
                {
                    textureName = null;
                    materialName = null;
                    meshName = null;
                }
            }
            EditorGUILayout.Space();

            manualSave = EditorGUILayout.Toggle("Manual Save", manualSave);
            if (manualSave) 
            {
                if (GUILayout.Button("Save Meshes"))
                {
                    if (billboardGen.boardMesh != null)
                    {

                        SaveMesh(filePath, false);

                    }
                    else
                    {
                        Debug.LogWarning("No mesh to save");
                    }

                }

                if (GUILayout.Button("Save Textures"))
                {
                    if (billboardGen.capturedTexture != null)
                    {
                        SaveTexture(filePath, includeMaterial);


                    }
                    else
                    {
                        Debug.LogWarning("No texture to save");
                    }

                }
                includeMaterial = EditorGUILayout.Toggle("Also Generate Material", includeMaterial);
            }
        }
        GUILayout.FlexibleSpace();
        if (targetObject != null) 
        {
            if (!hasGeneratedMesh)
            {

                if (GUILayout.Button("Generate Billboard"))
                {

                    if (billboardGen.targetObject != null && billboardGen.boardMat != null)
                    {
                        if (!hasGeneratedMesh)
                        {
                            if (targetObject.transform.childCount != 0)
                            {
                                billboardGen.SetReference();
                            }
                            else
                            {
                                billboardGen.StoreMaterialMesh();
                            }
                            billboardGen.GenerateTexture();

                            billboardGen.GenerateBillboard(yOffset, xOffset, zOffset);

                            hasGeneratedMesh = true;
                            Debug.Log("Billboard Generated, Texture resolution：" + resolutionLevel + "*" + resolutionLevel);
                        }
                        else
                        {
                            Debug.LogWarning("Billboard has been generated");
                        }

                    }
                    else if (billboardGen.targetObject == null && billboardGen.boardMat == null)
                    {
                        Debug.LogWarning("Please select billboard target object and material");
                    }
                    else if (billboardGen.targetObject != null && billboardGen.boardMat == null)
                    {
                        Debug.LogWarning("Please select billboard material");
                    }
                    else if (billboardGen.targetObject == null && billboardGen.boardMat != null)
                    {
                        Debug.LogWarning("Please select a target");
                    }
                }
            }
            else
            {
                prefabName = EditorGUILayout.TextField("Change Naming Scheme：", prefabName);
                EditorGUILayout.LabelField(prefabName == "" || prefabName == null ? targetObject.name.Replace("(Billboard)", "") : prefabName, EditorStyles.boldLabel);
                if (GUILayout.Button("Generate Prefab"))
                {
                    if (billboardGen.capturedTexture != null && billboardGen.boardMesh != null)
                    {
                        SaveTexture(filePath, true);
                        SaveMesh(filePath, true);
                    }
                }
            }


            if (GUILayout.Button("Clear Billboard"))
            {
                if (billboardGen.targetObject != null)
                {
                    if (hasGeneratedMesh)
                    {
                        ResetTarget();
                        Debug.Log("Cleard successfully");
                    }
                    else
                    {
                        Debug.LogWarning("Billboard has been cleared");
                    }
                }
                else
                {
                    Debug.LogWarning("No billboard to clear");
                }
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

    }

    public void ResetTarget() 
    {
        yOffset = 0;
        xOffset = 0;
        zOffset = 0;
        normalSample = 0.5f;
        transitionScale = 0.5f;

        if (targetObject.transform.childCount != 0)
        {
            billboardGen.ClearMesh();
        }
        else
        {
            billboardGen.ReturnMateiralMesh(true);
        }
        hasGeneratedMesh = false;
        billboardGen.ClearTexture();
    }

    public void ToggleLighting(bool on) 
    {
        allLights = GameObject.FindObjectsOfType<Light>();
        if (on)
        {
            RenderSettings.ambientMode =  UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            foreach (Light l in allLights)
            {
                l.enabled = true;
            }

        }
        else 
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.ambientEquatorColor = Color.white;
            RenderSettings.ambientGroundColor= Color.white;
            RenderSettings.ambientSkyColor = Color.white;
            foreach (Light l in allLights)
            {
                l.enabled = false;
            }
        }
    }
    public void SaveTexture(string filePath,bool generateMat)
    {
        if (billboardGen.capturedTexture != null)
        {
            string preName = prefabName == null ?  targetObject.name.Replace("(Billboard)", "") : prefabName;
            string finalPath = "Assets/" + filePath + "/" + "R_" + preName + "/";
            byte[] bytes = billboardGen.capturedTexture.EncodeToPNG();
            
            if (!Directory.Exists(finalPath))
            {
                Directory.CreateDirectory(finalPath);
            }
            AssetDatabase.Refresh();

            string name = textureName == null ? "T_" + preName : textureName+"_"+preName;

            string finalTextureName = AddFileIndex(finalPath, name, ".png");
            File.WriteAllBytes(finalPath + finalTextureName + ".png", bytes);

            AssetDatabase.Refresh();
            
            TextureImporter importer = AssetImporter.GetAtPath(finalPath + finalTextureName+ ".png") as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            else 
            {
                Debug.Log("Material file path is null, could not adjust import info, try restart the tool");
            }
            
            AssetDatabase.Refresh();
            Debug.Log("Texture" + name + " has been saved to Assets/" + filePath + "folder");
            if (generateMat)
            {
                string matName = materialName == null? "M_"+ preName:materialName + "_" + preName;
                finalMaterialName = AddFileIndex(finalPath, matName, ".asset");
                Texture2D assetTex = (Texture2D)AssetDatabase.LoadAssetAtPath(finalPath + finalTextureName + ".png", typeof(Texture2D));
                Material boardMat = new Material(boardShader);
                boardMat.mainTexture = assetTex;
                boardMat.SetVector("_Offset", billboardGen.locationOffset);
                boardMat.SetVector("_BoundSize", billboardGen.boundSize);
                boardMat.SetVector("_UV_Y_0", billboardGen.uvList[0][0]);
                boardMat.SetVector("_UV_Y_3", billboardGen.uvList[0][3]);
                boardMat.SetVector("_UV_Y_1", billboardGen.uvList[0][1]);
                boardMat.SetVector("_UV_Z_0", billboardGen.uvList[1][0]);
                boardMat.SetVector("_UV_Z_3", billboardGen.uvList[1][3]);
                boardMat.SetVector("_UV_Z_1", billboardGen.uvList[1][1]);
                boardMat.SetVector("_UV_X_0", billboardGen.uvList[2][0]);
                boardMat.SetVector("_UV_X_3", billboardGen.uvList[2][3]);
                boardMat.SetVector("_UV_X_1", billboardGen.uvList[2][1]);
                boardMat.SetFloat("_Precision", normalSample);
                boardMat.SetFloat("_ToonScale", transitionScale);
                boardMat.SetFloat("_DepthY", depthY);
                boardMat.SetFloat("_DepthX", depthX);
                boardMat.SetFloat("_DepthZ", depthZ);
                boardMat.SetFloat("_Blend", blend);
                boardMat.SetFloat("_Detail", detailNormal ? 1 : 0);
                boardMat.SetVector("_SOffset", offset);
                boardMat.SetVector("_Slice_Offset", new Vector3(xOffset, yOffset, zOffset));

            AssetDatabase.CreateAsset(boardMat, finalPath + finalMaterialName + ".asset");

                AssetDatabase.Refresh();
                Debug.Log("Material" + matName + "has been saved to Assets/" + filePath + "folder");
            }   
        }
        else 
        {
            Debug.LogWarning("Texture target lost");
        }
        
    }

    public void SaveMesh(string filePath,bool instantiate)
    {
        string preName = prefabName == null ? targetObject.name.Replace("(Billboard)", "") : prefabName;
        string name = meshName == null ? "B_" + preName : meshName + "_" + preName;
        string finalPath = "Assets/" +filePath + "/" + "R_" + preName + "/";
      
        if (!Directory.Exists(finalPath))
        {
            Directory.CreateDirectory(finalPath);
        }
        AssetDatabase.Refresh();
        string finalMeshName = AddFileIndex(finalPath, name, ".asset");
        AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(billboardGen.boardMesh,finalPath + finalMeshName +".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Mesh" + name + "has been saved to Assets/" + filePath + "folder");
        if (instantiate) 
        {
           
            Mesh savedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(finalPath + finalMeshName + ".asset",typeof(Mesh));
            GameObject boardObj = new GameObject();
            MeshFilter mf = boardObj.AddComponent<MeshFilter>();
            MeshRenderer mr = boardObj.AddComponent<MeshRenderer>();
            mf.mesh = savedMesh;
            mr.sharedMaterial = (Material)AssetDatabase.LoadAssetAtPath(finalPath + finalMaterialName+ ".asset", typeof(Material));
            
            string prefabPath = "Assets/" + filePath + "/" + "Prefabs/";
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }
            
            string finalPrefabName = AddFileIndex(prefabPath, preName,".prefab");
            PrefabUtility.CreatePrefab(prefabPath + finalPrefabName + ".prefab", boardObj);
            Debug.Log("Successfully created" + targetObject.name.Replace("(Billboard)", "") + "prefab and saved to" + prefabPath + "folder");
            DestroyImmediate(boardObj);
        }
        
        AssetDatabase.Refresh();

        ResetTarget();
    }

    private string AddFileIndex(string filePath, string originalName,string type) 
    {
        string[] allFiles = Directory.GetFiles(filePath);
        
        int index = 0;
        foreach (string fileName in allFiles)
        {
            string prefix = fileName.Replace(filePath, "");
            if ((prefix.Replace(type, "") == originalName|| prefix.Replace(type,"").Contains(originalName + "(" )) && !prefix.Replace(type, "").EndsWith(".meta"))
            {
                index += 1;
                
            }
        }

        string finalName = index == 0 ? originalName : originalName + "(" + index.ToString() + ")";
        return finalName;
    }
  
}

