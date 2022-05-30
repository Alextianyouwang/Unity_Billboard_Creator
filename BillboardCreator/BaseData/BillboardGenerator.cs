using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class BillboardGenerator 
{
    public Camera captureCamera;
    public int maxResolution = 256;
    public Shader depthShader;
    public static  Color emptySpaceColor = new Color(1, 0, 1);

    public GameObject targetObject;
    public Material boardMat;
    public Mesh boardMesh;
    public MeshRenderer boardRenderer;
    public MeshFilter boardFilter;
    private Material[] previousObjectMaterial;
    private Mesh previousObjectMesh;

    public Bounds targetObjectBounds;
    public Vector2[][] uvList;

    public Vector3 locationOffset;
    public Vector3 boundSize;

    public Texture2D capturedTexture;

    public bool includeChildren;
    public bool cullMiddle;
    public bool softNormal;

    

    public void StoreMaterialMesh() 
    {
            previousObjectMaterial = targetObject.GetComponent<MeshRenderer>().sharedMaterials;
            previousObjectMesh = targetObject.GetComponent<MeshFilter>().sharedMesh;
    }
    public void ReturnMateiralMesh(bool previous)
    {
        if (previousObjectMaterial != null && previousObjectMesh != null && previous) 
        {
            targetObject.GetComponent<MeshFilter>().sharedMesh = previousObjectMesh;
            targetObject.GetComponent<MeshRenderer>().sharedMaterials = previousObjectMaterial;
        }
    }
    public void SetReference() 
    {                               
        boardRenderer = targetObject.GetComponent<MeshRenderer>()? targetObject.GetComponent<MeshRenderer>(): targetObject.AddComponent<MeshRenderer>();
            boardFilter = targetObject.GetComponent<MeshFilter>() ? targetObject.GetComponent<MeshFilter>() : targetObject.AddComponent<MeshFilter>();
    }
    public void ClearMesh() 
    {
            boardMesh = null;
            boardFilter.sharedMesh = boardMesh;
        boardRenderer.sharedMaterial = null;
        ToggleChildrenVisibility(true);
       
    }
    public void ClearTexture() 
    {
        capturedTexture = null;
        boardMat.mainTexture = capturedTexture;
    }
    
    public void RemoveRenderer() 
    {
       GameObject.DestroyImmediate(targetObject.GetComponent<MeshFilter>());
       GameObject.DestroyImmediate(targetObject.GetComponent<MeshRenderer>());

    }


    public void GenerateTexture() 
    {
        targetObjectBounds = GetBounds(targetObject, includeChildren);
        Texture2D[] textures = new Texture2D[3];
        Vector2[] textureSizes = new Vector2[3];
        TexturePackage texturePack = new TexturePackage(textures, textureSizes);
        uvList = new Vector2[3][];
        Mesh currentMesh = targetObject.GetComponent<MeshFilter>().sharedMesh;
        //ClearBillboard
        if (includeChildren)
        {
            boardFilter.sharedMesh = null;
            ToggleChildrenVisibility(true);
        }
        else
        {
            ReturnMateiralMesh(true);
        }
       
        for (int i = 0; i < 3; i++) 
        {
            //PrepareCapture
            BoardInfo addCaptureInfo = PrepareCapture(targetObjectBounds, i);
            //CollectTexture
            textures[i] = Capture(addCaptureInfo.boardSize, addCaptureInfo.boardCenter, addCaptureInfo.boardFaceDirection);
            textureSizes[i] = addCaptureInfo.boardSize;

            //AlignUVs
            uvList[i] = PrepareUVs(texturePack.PrepareUVs2x2(), i);
            

        }
        capturedTexture = texturePack.PackSquareTexture2x2();
        SetMaterialDisplayProperties();
       
        //ResetBillboard
        if (includeChildren)
        {
            boardFilter.sharedMesh = currentMesh;
            boardRenderer.material = boardMat;
        }
        else
        {
            targetObject.GetComponent<MeshFilter>().sharedMesh = currentMesh;
            Material[] sharedMaterialList = targetObject.GetComponent<MeshRenderer>().sharedMaterials;
            for (int i = 0; i < sharedMaterialList.Length; i++)
            {
                sharedMaterialList[i] = boardMat;
            }
            targetObject.GetComponent<MeshRenderer>().sharedMaterials = sharedMaterialList;
        } 
    }
    private void SetMaterialDisplayProperties() 
    {
        boardMat.mainTexture = capturedTexture;
        locationOffset = targetObjectBounds.center - targetObject.transform.position;
        boundSize = targetObjectBounds.size;
        boardMat.SetVector("_Offset", locationOffset);
        boardMat.SetVector("_BoundSize", boundSize);
        boardMat.SetVector("_UV_Y_0", uvList[0][0]);
        boardMat.SetVector("_UV_Y_3", uvList[0][3]);
        boardMat.SetVector("_UV_Y_1", uvList[0][1]);
        boardMat.SetVector("_UV_Z_0", uvList[1][0]);
        boardMat.SetVector("_UV_Z_3", uvList[1][3]);
        boardMat.SetVector("_UV_Z_1", uvList[1][1]);
        boardMat.SetVector("_UV_X_0", uvList[2][0]);
        boardMat.SetVector("_UV_X_3", uvList[2][3]);
        boardMat.SetVector("_UV_X_1", uvList[2][1]);
    }

    private void SetMaterialSliceOffset(Vector3 newOffset) 
    {
        boardMat.SetVector("_Slice_Offset", newOffset);
    }

    public void ToggleChildrenVisibility(bool visible) 
    {
        if (includeChildren)
        {
            foreach (Transform c in targetObject.transform)
            {
                c.gameObject.SetActive(visible);
            }
        }
    }
    public void GenerateBillboard(float yOffset, float xOffset, float zOffset)
    {
        targetObjectBounds = GetBounds(targetObject, includeChildren);

        CombineInstance[] combine = new CombineInstance[3];
        for (int i = 0; i < 3; i++)
        {
            //GenerateMesh
            BoardInfo addMeshInfo = PrepareMesh(yOffset, xOffset, zOffset, targetObjectBounds, i);
            addMeshInfo.uvs = uvList[i];
            
            combine[i].mesh = addMeshInfo.GenerateMesh();
            combine[i].transform = targetObject.transform.worldToLocalMatrix;
            if (cullMiddle)
            {
                combine[0].mesh = new Mesh();
            }
        }
        boardMesh = new Mesh();
        boardMesh.CombineMeshes(combine);

        if (includeChildren)
        {
            foreach (Transform c in targetObject.transform)
            {
                c.gameObject.SetActive(false);
            }
            boardFilter.sharedMesh = boardMesh;
        }
        else
        {
            targetObject.GetComponent<MeshFilter>().sharedMesh = boardMesh;
        }
        SetMaterialSliceOffset(new Vector3 (xOffset,yOffset,zOffset));
    }
    //打包材质并获取UV信息。目前只支持三张贴图放在田字格里。
   /* public class TextureCoordinate 
    {
        int[,] arrangement;
        public TextureCoordinate(int width, int height) 
        {
            arrangement = new int[width, height];
        }

        public void AssignPosition() 
        {
            int index = 0;
            for (int y = 0; y < arrangement.GetLength(1); y++) 
            {
                for (int x = 0; x < arrangement.GetLength(0); x++) 
                {
                    index++;
                }
            }
        }
    }*/

    public class TexturePackage 
    {
        Texture2D[] textures;
        Vector2[] sizes;
        public Color[][] textureColor;
        public TextureFormat format;
        public int singleTexRes;

        public TexturePackage(Texture2D[] _textures,Vector2[] _sizes) 
        {
            textures = _textures;
            sizes = _sizes;
        }
        public void StoreColors() 
        {
            singleTexRes = textures[0].width;
            format = textures[0].format;
            textureColor = new Color[textures.Length][];

            for (int i = 0; i < textures.Length; i++)
            {
                textureColor[i] = new Color[singleTexRes * singleTexRes];
                for (int y = 0; y < singleTexRes; y++)
                {
                    for (int x = 0; x < singleTexRes; x++)
                    {
                        textureColor[i][x + singleTexRes * y] = textures[i].GetPixel(x, y);
                    }
                }
            }
        }
        public Texture2D PackSquareTexture2x2()
        {
            StoreColors();
            Texture2D combTex = new Texture2D(singleTexRes * 2, singleTexRes * 2, format, false);

            for (int y = 0; y < singleTexRes * 2; y++)
            {
                for (int x = 0; x < singleTexRes * 2; x++)
                {
                    // LeftBottom
                    if (x < singleTexRes && y < singleTexRes)
                    {
                        combTex.SetPixel(x, y, textureColor[0][x + singleTexRes * y]);
                    }
                    // RightBottom
                    if (x >= singleTexRes && x < singleTexRes * 2 && y < singleTexRes)
                    {
                        combTex.SetPixel(x, y, textureColor[1][x - singleTexRes + singleTexRes * y]);
                    }
                    // LeftTop
                    if (x < singleTexRes && y >= singleTexRes && y < singleTexRes * 2)
                    {
                        combTex.SetPixel(x, y, textureColor[2][x + singleTexRes * (y - singleTexRes)]);
                    }
                    // RightTop
                    if (x >= singleTexRes && y <= singleTexRes * 2 && y >= singleTexRes)
                    {
                        combTex.SetPixel(x, y, Color.magenta);
                    }
                }
            }
            combTex.filterMode = FilterMode.Point;
            combTex.wrapMode = TextureWrapMode.Repeat;
            combTex.Apply();
            return combTex;
        }

        public Vector2[][]PrepareUVs2x2()
        {
            Vector2[][] uvList = new Vector2[3][];
            float[] aspectRatios = new float[3];
            for (int i = 0; i < 3; i++)
            {
                aspectRatios[i] = sizes[i].x / sizes[i].y;
                float maxX = (aspectRatios[i] > 1 ? 1 : aspectRatios[i]);
                float maxY = (aspectRatios[i] < 1 ? 1 : 1 / aspectRatios[i]);
                float minX = (-maxX) * 0.5f + 0.5f;
                float minY = (-maxY) * 0.5f + 0.5f;
                maxX *= 0.5f;
                maxX += 0.5f;
                maxY *= 0.5f;
                maxY += 0.5f;

                uvList[i] = new Vector2[4];

                uvList[i][0] = new Vector2(maxX, maxY);
                uvList[i][1] = new Vector2(maxX, minY);
                uvList[i][2] = new Vector2(minX, minY);
                uvList[i][3] = new Vector2(minX, maxY);

                for (int j = 0; j < 4; j++) 
                {
                    uvList[i][j] *= 0.5f;
                }
            }

            for (int i = 0; i < 4; i++) 
            {
                uvList[1][i].x += 0.5f;
                uvList[2][i].y += 0.5f;
            }
            return uvList;
        }
    }
    //获取维度信息。
    public Bounds GetBounds(GameObject target,bool includeChildren)
    {

        MeshRenderer[] mrs = target.GetComponentsInChildren<MeshRenderer>();
        Bounds myBound = new Bounds(target.transform.position, Vector3.zero);

        if (includeChildren)
        {
            if (mrs.Length != 0)
            {
                List<MeshRenderer> parentRenderer = new List<MeshRenderer>();
                for (int i = 0; i < mrs.Length; i++) 
                {
                    if (mrs[i].transform.childCount != 0) 
                    {
                        parentRenderer.Add(mrs[i]);
                    }
                    if (!parentRenderer.Contains(mrs[i])&&mrs[i] != targetObject.GetComponent<MeshRenderer>())
                    {
                        myBound.Encapsulate(mrs[i].bounds);
                    }
                }
            }
        }
        else
        {
            Renderer rend = target.GetComponent<MeshRenderer>();
            if (rend)
            {
                myBound = rend.bounds;
            }
        }
        return myBound;
    }
    public float[] BoundInfo(Bounds objectBounds) 
    {
        float[] bI = new float[] {
                objectBounds.max.x,
                objectBounds.max.y,
                objectBounds.max.z,
                objectBounds.min.x,
                objectBounds.min.y,
                objectBounds.min.z,
                objectBounds.center.x,
                objectBounds.center.y,
                objectBounds.center.z
        };
        return bI;
    }
    //填充维度信息，准备拍摄。
    public BoardInfo PrepareCapture(Bounds objectBounds, int index)
    {
        BoardInfo myBoard = new BoardInfo();
        float[] bI = BoundInfo(objectBounds);
        Vector3[] sizes = new Vector3[3];
        sizes[0] = new Vector3(bI[0] - bI[3], bI[2] - bI[5], bI[1] - bI[7]);
        sizes[1] = new Vector3(bI[0] - bI[3], bI[1] - bI[4], bI[2] - bI[8]);
        sizes[2] = new Vector3(bI[2] - bI[5], bI[1] - bI[4], bI[0] - bI[6]);
        myBoard.boardSize = sizes[index];
        Vector3[] faceDirection = new Vector3[3];
        faceDirection[0] = Vector3.up;
        faceDirection[1] = Vector3.forward;
        faceDirection[2] = Vector3.right;
        
        myBoard.boardFaceDirection = faceDirection[index];
        myBoard.boardCenter = objectBounds.center;

        return myBoard;
    }
    //填充UV信息。
    public Vector2[] PrepareUVs( Vector2[][] uvList, int index)
    {
        Vector2[] uvs = uvList[index];
        return uvs;
    }
    //填充网格信息。
    public BoardInfo PrepareMesh(float ySlider,float xSlider,float zSlider, Bounds objectBounds, int index)
    {
        BoardInfo finalBoard = new BoardInfo();
        float[] bI = BoundInfo(objectBounds);
        
        finalBoard.verts = new Vector3[4];
        finalBoard.vertexColor = new Color[4];
        Vector2 boardSize = Vector2.zero;

        ySlider *= 0.5f; ySlider += 0.5f;
        xSlider *= 0.5f; xSlider += 0.5f;
        zSlider *= 0.5f; zSlider += 0.5f;

        float yOffset = Mathf.Lerp(-(bI[1]-bI[7]), (bI[1] - bI[7]), ySlider);
        float xOffset = Mathf.Lerp(-(bI[0] - bI[6]), (bI[0] - bI[6]), xSlider);
        float zOffset = Mathf.Lerp(-(bI[2] - bI[8]), (bI[2] - bI[8]), zSlider);

        finalBoard.tris = new int[] { 1, 2, 0, 3, 0, 2 };

        switch (index)
        {
            //XZ Plane Up
            case 0:
                finalBoard.verts[0] = new Vector3(bI[0], bI[7] + yOffset, bI[2]);
                finalBoard.verts[1] = new Vector3(bI[0], bI[7] + yOffset, bI[5]);
                finalBoard.verts[2] = new Vector3(bI[3], bI[7] + yOffset, bI[5]);
                finalBoard.verts[3] = new Vector3(bI[3], bI[7] + yOffset, bI[2]);
                for (int i = 0; i < 4; i++) 
                {
                    finalBoard.vertexColor[i] = new Color(0, 1, 0);
                }

                break;
            //XY Plane Front
            case 1:

                finalBoard.verts[0] = new Vector3(bI[3], bI[1], bI[8] + zOffset);
                finalBoard.verts[1] = new Vector3(bI[3], bI[4], bI[8] + zOffset);
                finalBoard.verts[2] = new Vector3(bI[0], bI[4], bI[8] + zOffset);
                finalBoard.verts[3] = new Vector3(bI[0], bI[1], bI[8] + zOffset);
                for (int i = 0; i < 4; i++)
                {
                    finalBoard.vertexColor[i] = new Color(0, 0, 1);
                }
                break;
            //YZ Plane Right
            case 2:
                finalBoard.verts[0] = new Vector3(bI[6] + xOffset, bI[1], bI[2]);
                finalBoard.verts[1] = new Vector3(bI[6] + xOffset, bI[4], bI[2]);
                finalBoard.verts[2] = new Vector3(bI[6] + xOffset, bI[4], bI[5]);
                finalBoard.verts[3] = new Vector3(bI[6] + xOffset, bI[1], bI[5]);
                for (int i = 0; i < 4; i++)
                {
                    finalBoard.vertexColor[i] = new Color(1, 0, 0);
                }
                break;
        }
        return finalBoard;
    }
  
    //储存网格信息并创建。
    public class BoardInfo
    {
        public Vector3[] verts;
        public int[] tris;
        public Vector2[] uvs;
        public Vector3 boardSize;
        public Vector3 boardCenter;
        public Vector3 boardFaceDirection;
        public Color[] vertexColor;
        public float camDistance;

        public BoardInfo() { }

        public Mesh GenerateMesh()
        {
            Mesh finalMesh = new Mesh();
            finalMesh.Clear();
            finalMesh.vertices = verts;
            finalMesh.triangles = tris;
            finalMesh.uv = uvs;
            finalMesh.colors = vertexColor;
            finalMesh.RecalculateBounds();
            finalMesh.RecalculateTangents();
            //finalMesh.RecalculateNormals();
            return finalMesh;
        }
    }
  
    //导入物体维度并获取相应朝向的材质。
    public Texture2D Capture(Vector3 boardSize, Vector3 facePosition, Vector3 faceDirection)
    {
        Vector3 _boardSize = boardSize;
        Vector3 _facePosition = facePosition;
        Vector3 _faceDirection = faceDirection;
        float maxBound = Mathf.Max(_boardSize.x, _boardSize.y);
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor =Color.magenta;
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = maxBound / 2;
        captureCamera.nearClipPlane = 0.5f;
        captureCamera.farClipPlane = _boardSize.z * 2f+0.6f;
        captureCamera.transform.position = _facePosition + _faceDirection * (boardSize.z+0.6f);
        captureCamera.transform.rotation = Quaternion.identity;
        captureCamera.transform.LookAt(_facePosition);

        captureCamera.ResetReplacementShader();
        RenderTexture colorTex = new RenderTexture(maxResolution, maxResolution, 1, RenderTextureFormat.ARGBFloat);
        captureCamera.targetTexture = colorTex;
        colorTex.filterMode = FilterMode.Point;
        captureCamera.Render();

        captureCamera.SetReplacementShader(depthShader, "");
        RenderTexture depthTex = new RenderTexture(maxResolution, maxResolution, 1, RenderTextureFormat.ARGBFloat);
        captureCamera.targetTexture = depthTex;
        colorTex.filterMode = FilterMode.Point;
        captureCamera.Render();

       

        return !softNormal? Combine(ConvertToTex2D(colorTex),RemapAlphaValue(ConvertToTex2D(depthTex)), Color.magenta) :
            BlurAlpha( Combine(ConvertToTex2D(colorTex), RemapAlphaValue(ConvertToTex2D(depthTex)) , Color.magenta));
    }
    Texture2D RemapAlphaValue(Texture2D tex) 
    {
        float heighestValue = 0;
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                Color c = tex.GetPixel(x,y);
                if (heighestValue < c.r&& c.r < 0.99f) 
                {
                    heighestValue = c.r;
                }
                //tex.SetPixel(x, y, c);
            }
            
        }
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                Color c = tex.GetPixel(x, y);
                //c.r = Remap(c.r, 0f, 1, 0f, heighestValue);
      /*          if (c.r == 1)
                {
                    c.r = 0;
                }*/
                //c.r = Remap(c.r, 0f, heighestValue, 0f, 0.5f);
              
                tex.SetPixel(x, y, c);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }
    Texture2D ConvertToTex2D(RenderTexture rTex)
    {
        RenderTexture.active = rTex;
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }
    Texture2D Combine(Texture2D colorTex, Texture2D alphaTex,Color backgroundColor)
    {
        for (int y = 0; y < colorTex.height; y++)
        {
            for (int x = 0; x < colorTex.width; x++)
            {
                Color c = colorTex.GetPixel(x, y);
                float alphaValue = alphaTex.GetPixel(x, y).r;
       
                c.a = alphaValue;
                colorTex.SetPixel(x, y, c);
            }
        }
        colorTex.Apply();
        return colorTex;
    }
    Texture2D BlurAlpha(Texture2D alpha) 
    {
        for (int y = 0; y < alpha.height; y++)
        {
            for (int x = 0; x < alpha.width; x++)
            {
                Color[] nearby = new Color[9];
                float average = 0;
                float passedPixels = 0;

                float leftCheck = x - 1 < 0 ? 1:0;
                float rightCheck = x + 1 > alpha.width ? 1 : 0;
                float topCheck = y + 1 > alpha.height ? 1 : 0;
                float bottomCheck = y - 1 < 0 ? 1 : 0;
                float pass = leftCheck + rightCheck + topCheck + bottomCheck;
                nearby[0] = pass > 0 ? Color.clear : alpha.GetPixel(x - 1, y + 1);
                nearby[1] = pass > 0 ? Color.clear : alpha.GetPixel(x + 1, y);
                nearby[2] = pass > 0 ? Color.clear : alpha.GetPixel(x + 1, y + 1);

                nearby[3] = pass > 0 ? Color.clear : alpha.GetPixel(x, y - 1);
                nearby[4] = alpha.GetPixel(x, y);
                nearby[5] = pass > 0 ? Color.clear : alpha.GetPixel(x, y + 1);

                nearby[6] = pass > 0 ? Color.clear : alpha.GetPixel(x - 1, y - 1);
                nearby[7] = pass > 0 ? Color.clear : alpha.GetPixel(x - 1, y);
                nearby[8] = pass > 0 ? Color.clear : alpha.GetPixel(x + 1, y - 1);

                foreach (Color c in nearby)
                {
                    if (c != Color.clear )
                    {
                        passedPixels += 1;
                        average += c.a;
                    }
                }
 
                average /= passedPixels;
                nearby[4].a = average;
                alpha.SetPixel(x, y, nearby[4]);

            }
        }
        alpha.filterMode = FilterMode. Point;
        alpha.Apply();
        return alpha;
    }
    public static float Remap( float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
