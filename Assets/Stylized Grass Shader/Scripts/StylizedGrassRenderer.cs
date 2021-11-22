//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if URP
using UnityEngine.Rendering.Universal;
#endif

namespace StylizedGrass
{
    [ExecuteInEditMode]
    [AddComponentMenu("Stylized Grass/Stylized Grass Renderer")]
    public class StylizedGrassRenderer : MonoBehaviour
    {
        public static StylizedGrassRenderer Instance;
#if URP
        public ScriptableRendererData bendRenderer;
#endif

        public const int TexelsPerMeter = 16;

        public bool debug = false;
        [Tooltip("The renderer will follow the position of the camera currently rendering.")]
        public bool followCamera = true;
        [Tooltip("Controls how large the render area is. Small is better, since a large area thins out the rendering resolution")]
        [Range(8, 512)]
        public float renderExtends = 32f;
        [Tooltip("The renderer will follow this Transform's position. Ideally set to the player's transform.")]
        public Transform followTarget;
        public RenderTexture vectorRT;
        public Camera renderCam;

        public int resolution = 1024;
        private int m_resolution;

        [Tooltip("When a color map is assigned, this will be set as the active color map.\n\nHaving the Color Map Renderer component present would not longer be required.")]
        public GrassColorMap colorMap;
        [Tooltip("When enabled the grass Ambient and Gust strength values are multiplied by the WindZone's Main value")]
        public bool listenToWindZone;
        public WindZone windZone;

        //GrassBender will register itself to this list
        public static SortedDictionary<int, List<GrassBender>> GrassBenders = new SortedDictionary<int, List<GrassBender>>();
        private static List<GrassBender> BenderLayer;
        public static int benderCount;

        private Vector3 targetPosition;
        [NonSerialized] //Constantly changes, so don't save
        private Bounds bounds;
        private Vector4 uv = new Vector4();

        public static void RegisterBender(GrassBender gb)
        {
            if (GrassBenders.ContainsKey(gb.sortingLayer))
            {
                GrassBenders.TryGetValue(gb.sortingLayer, out BenderLayer);

                if (BenderLayer.Contains(gb) == false)
                {
                    BenderLayer.Add(gb);
                    GrassBenders[gb.sortingLayer] = BenderLayer;
                    benderCount++;
                }
            }
            //Create new layer
            else
            {
                BenderLayer = new List<GrassBender>();
                BenderLayer.Add(gb);

                GrassBenders.Add(gb.sortingLayer, BenderLayer);
                benderCount++;
            }
        }

        public static void UnRegisterBender(GrassBender gb)
        {
            if (GrassBenders.ContainsKey(gb.sortingLayer))
            {
                GrassBenders.TryGetValue(gb.sortingLayer, out BenderLayer);

                BenderLayer.Remove(gb);
                benderCount--;

                //If layer is now empty, remove it
                if (GrassBenders[gb.sortingLayer].Count == 0) GrassBenders.Remove(gb.sortingLayer);
            }
        }

        public static int _BendMap = Shader.PropertyToID("_BendMap");
        public static int _BendMapUV = Shader.PropertyToID("_BendMapUV");
        private static Color neutralVector = new Color(0.5f, 0f, 0.5f, 0f);

        public void OnEnable()
        {
            Instance = this;

            RenderPipelineManager.beginCameraRendering += OnCameraRender;

            Init();

            if (colorMap)
            {
                colorMap.SetActive();
            }
            else
            {
                if (!GrassColorMapRenderer.Instance) GrassColorMap.DisableGlobally();
            }

#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
#endif
        }

        public void OnDisable()
        {
            Instance = null;

            RenderPipelineManager.beginCameraRendering -= OnCameraRender;

            //Shader needs to disable texture reading, since default global textures are gray
            uv.w = 0;
            Shader.SetGlobalVector(_BendMapUV, uv);

#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
#endif

            if (renderCam)
            {
                DestroyImmediate(renderCam.gameObject);
                DestroyImmediate(vectorRT);
                renderCam = null;
            }

            Shader.SetGlobalVector(_GlobalWindParams, Vector4.zero);
        }

        /// <summary>
        /// Sets the center position for the bending render area. If the "Follow target" field has a Transform assigned, this will have no effect
        /// </summary>
        /// <param name="position"></param>
        public static void SetPosition(Vector3 position)
        {
            if (!Instance)
            {
                Debug.LogWarning("[Stylized Grass Renderer] Tried to set  follow target, but no instance is present");
                return;
            }
            if (Instance.followTarget)
            {
                Debug.LogWarning("[Stylized Grass Renderer] Tried to set position, but it is following " + Instance.followTarget.name, Instance.followTarget);
                return;
            }

            Instance.transform.position = position;
        }

        /// <summary>
        /// Sets the target transform the renderer has to follow. If the "Follow Camera" option is used, this will have no effect
        /// </summary>
        /// <param name="transform"></param>
        public static void SetFollowTarget(Transform transform)
        {
            if (!Instance)
            {
                Debug.LogWarning("[Stylized Grass Renderer] Tried to set follow target, but no instance is present");
                return;
            }

            Instance.followTarget = transform;
        }

        public static void SetWindZone(WindZone windZone)
        {
            if (!Instance)
            {
                Debug.LogWarning("Tried to set Stylized Grass Renderer wind zone, but no instance is present");
                return;
            }

            Instance.windZone = windZone;
        }

        private void Init()
        {
            m_resolution = resolution;

            CreateVectorMap();
        }

        private static int _GlobalWindParams = Shader.PropertyToID("_GlobalWindParams");
        private static int _BendRenderParams = Shader.PropertyToID("_BendRenderParams");

        private void Update()
        {
            if (renderCam)
            {
                UpdateCamera();
            }
            else
            {
                renderCam = CreateCamera();
            }

            //Assign to all shaders
            if (vectorRT)
            {
                Shader.SetGlobalTexture(_BendMap, vectorRT);
            }

            if (listenToWindZone)
            {
                if (windZone) Shader.SetGlobalVector(_GlobalWindParams, new Vector4(windZone.windMain, 0f, 0f, 1f));
            }
            else
            {
                Shader.SetGlobalVector(_GlobalWindParams, Vector4.zero);
            }

            Shader.SetGlobalVector(_BendMapUV, uv);
            Shader.SetGlobalVector(_BendRenderParams, new Vector4(this.transform.position.y, renderExtends, 0f, 0f));
        }

        private Camera CreateCamera()
        {
            Camera cam = new GameObject().AddComponent<Camera>();
            cam.gameObject.name = "GrassBendCamera " + GetInstanceID();
            cam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            cam.gameObject.hideFlags = HideFlags.HideAndDontSave;

            cam.orthographic = true;
            cam.depth = -100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = 0;
            //Neutral bend direction and zero strength/mask
            cam.backgroundColor = neutralVector;

            cam.useOcclusionCulling = false;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            cam.forceIntoRenderTexture = true;

#if URP
            UniversalAdditionalCameraData camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderShadows = false;
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;
            camData.requiresColorOption = CameraOverrideOption.Off;
            camData.requiresDepthOption = CameraOverrideOption.Off;
            camData.requiresColorTexture = false;
            camData.requiresDepthTexture = false;

            if (UniversalRenderPipeline.asset)
            {
#if UNITY_EDITOR
                //Only runs in editor, but will be referenced in instance from there on
                if (!bendRenderer) bendRenderer = PipelineUtilities.GetRenderer((DrawGrassBenders.RendererGUID));
                PipelineUtilities.ValidatePipelineRenderers(bendRenderer);
#endif

               PipelineUtilities.AssignRendererToCamera(camData, bendRenderer);
            }
            else
            {
                Debug.LogError("[StylizedGrassRenderer] No Universal Render Pipeline is currently active.");
            }
#endif

            return cam;
        }

        public static int CalculateResolution(float size)
        {
            int res = Mathf.RoundToInt(size * TexelsPerMeter);
            res = Mathf.NextPowerOfTwo(res);
            res = Mathf.Clamp(res, 256, 2048);
            return res;
        }

        //Create the influence map for the shaders
        private void CreateVectorMap()
        {
            if (vectorRT != null)
            {
                if (renderCam) renderCam.targetTexture = null;
                DestroyImmediate(vectorRT);
            }

            RenderTextureDescriptor rtDsc = new RenderTextureDescriptor();
            rtDsc.width = resolution;
            rtDsc.height = resolution;
            rtDsc.depthBufferBits = 0;

            rtDsc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;

            rtDsc.enableRandomWrite = false; //Not supported on OpenGL
            rtDsc.autoGenerateMips = false;
            rtDsc.useMipMap = false;
            rtDsc.volumeDepth = 1;
            rtDsc.msaaSamples = 1;
            rtDsc.dimension = TextureDimension.Tex2D;
            rtDsc.sRGB = false;
            rtDsc.vrUsage = VRTextureUsage.None;
            rtDsc.bindMS = false;
            rtDsc.memoryless = RenderTextureMemoryless.None;
            rtDsc.shadowSamplingMode = ShadowSamplingMode.None;

            //vectorRT = new RenderTexture(rtDsc)
            vectorRT = new RenderTexture(rtDsc)
            {
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,

                name = "BendMap" + GetInstanceID(),
                isPowerOfTwo = true,
                hideFlags = HideFlags.DontSave
            };
        }
        
        private void OnCameraRender(ScriptableRenderContext context, Camera currentCam)
        {
            if (!followCamera)
            {
                targetPosition = followTarget ? followTarget.transform.position : this.transform.position;
                return;
            }
            
            //Testing
            //if (currentCam.cameraType == CameraType.SceneView) return;
            
            //Skip for any special use camera's (except scene view camera)
            if (currentCam.cameraType != CameraType.SceneView && (currentCam.cameraType == CameraType.Reflection || currentCam.cameraType == CameraType.Preview || currentCam.hideFlags != HideFlags.None)) return;
            
            //Align bounds to camera frustrum to minimize wasted space
            targetPosition = currentCam.transform.position + (currentCam.transform.forward * renderExtends);
        }

        private void UpdateCamera()
        {
            if (!renderCam) return;

            //renderCam.cullingMask = 1 << renderLayer;
            renderCam.targetTexture = vectorRT;
            renderCam.orthographicSize = renderExtends;
            renderCam.farClipPlane = 1000f;
            
            //Snap position to texels to avoid pixel swimming artifacts
            targetPosition = SnapToTexel(targetPosition, (renderExtends * 2f) / resolution);

            renderCam.transform.position = targetPosition + (Vector3.up * renderExtends);

            bounds = new Bounds(new Vector3(targetPosition.x, targetPosition.y, targetPosition.z), Vector3.one * (renderExtends * 2f));

            //When changing resolution
            if (m_resolution != resolution) CreateVectorMap();
            m_resolution = resolution;

            uv.x = 1f - bounds.center.x - 1f + renderExtends;
            uv.y = 1f - bounds.center.z - 1f + renderExtends;
            uv.z = renderExtends * 2;
            uv.w = 1f; //Enable bend map sampling in shader
        }
        
        private static Vector3 SnapToTexel(Vector3 pos, float texelSize)
        {
            return new Vector3(SnapToTexel(pos.x, texelSize), SnapToTexel(pos.y, texelSize), SnapToTexel(pos.z, texelSize));
        }

        private static float SnapToTexel(float pos, float texelSize)
        {
            return Mathf.FloorToInt(pos / texelSize) * (texelSize) + (texelSize * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!renderCam) return;

            Gizmos.color = new Color(1, 1, 1, 1f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI() //Has unwanted overhead, so exclude from build
        {
            DrawDebugGUI(false);
        }

        void DrawDebugGUI(bool sceneView)
        {
            if (vectorRT == null) return;

            Rect imgRect = new Rect(5, 5, 256, 256);
            //Set to UI debug image
            if (debug && !sceneView)
            {
                GUI.DrawTexture(imgRect, vectorRT);
            }
            
            #if UNITY_EDITOR
            if (debug && sceneView)
            {
                Handles.BeginGUI();

                GUILayout.BeginArea(imgRect);

                EditorGUI.DrawTextureTransparent(imgRect, vectorRT);
                GUILayout.EndArea();
                Handles.EndGUI();
            }
            #endif
        }
#endif
        
#if UNITY_EDITOR
        private void OnSceneGUI(SceneView sceneView)
        {
            DrawDebugGUI(true);
        }
#endif
    }
}