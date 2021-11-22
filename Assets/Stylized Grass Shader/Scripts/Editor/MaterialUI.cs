//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA
//#define DEFAULT_GUI

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if URP
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif
using UI = StylizedGrass.StylizedGrassGUI;

namespace StylizedGrass
{
    [HelpURL(("http://staggart.xyz/unity/stylized-grass-shader/sgs-docs/?section=grass-shader"))]
    public class MaterialUI : ShaderGUI
    {
#if URP
        Material targetMat;

        private Vector4 windParams;
        private Vector4 natureRendererParams;

        private MaterialProperty baseMap;
        private MaterialProperty bumpMap;
        private MaterialProperty alphaCutoffProp;
        private MaterialProperty alphaToCoverage;
        private MaterialProperty color;

        private MaterialProperty hueColor;
        private MaterialProperty colorMapStrength;
        private MaterialProperty colorMapHeight;
        private MaterialProperty scalemapInfluence;
        private MaterialProperty ambientOcclusion;
        private MaterialProperty vertexDarkening;
        private MaterialProperty smoothness;
        private MaterialProperty translucency;
        private MaterialProperty _TranslucencyIndirect;
        private MaterialProperty _TranslucencyOffset;
        private MaterialProperty _TranslucencyFalloff;
        private MaterialProperty _NormalParams;

        private MaterialProperty bendMode;
        private MaterialProperty bendPushStrength;
        private MaterialProperty bendFlattenStrength;
        private MaterialProperty perspCorrection;
        private MaterialProperty _BendTint;

        private MaterialProperty windAmbientStrength;
        private MaterialProperty windSpeed;
        private MaterialProperty windDirection;
        private MaterialProperty windVertexRand;
        private MaterialProperty windObjectRand;
        private MaterialProperty windRandStrength;
        private MaterialProperty windSwinging;
        private MaterialProperty windGustTex;
        private MaterialProperty windGustStrength;
        private MaterialProperty windGustFreq;
        private MaterialProperty windGustTint;

        private MaterialProperty _CurvedWorldBendSettings;

        private bool enableDistFade;
        private bool invertFading;
        private float fadeStartDist;
        private float fadeEndDist;
        private MaterialProperty _FadeParams;
        private MaterialProperty culling;
        public enum CullingMode
        {
            Both, Front, Back
        }

        public MaterialEditor materialEditor;
        private MaterialProperty disableShadows;
        private MaterialProperty lightingMode;
        private MaterialProperty castShadows;
        private MaterialProperty environmentReflections;
        private MaterialProperty _SpecularHighlights;
        private MaterialProperty scaleMap;
        private MaterialProperty _Billboard;
        private MaterialProperty _AngleFading;
        //private MaterialProperty _DisableDecals;

        private UI.Material.Section renderingSection;
        private UI.Material.Section mapsSection;
        private UI.Material.Section colorSection;
        private UI.Material.Section shadingSection;
        private UI.Material.Section verticesSection;
        private UI.Material.Section windSection;

        private GUIContent unlitLightingContent;
        private GUIContent simpleLightingContent;
        private GUIContent advancedLightingContent;

        private float spherifyNormals;
        private float flattenNormalsLighting;
        private float normalVertexColorMask;
        private float flattenNormalsGeometry;

        private bool initliazed;

        private void OnEnable()
        {
            renderingSection = new StylizedGrassGUI.Material.Section(materialEditor, "RENDERING", "Rendering");
            mapsSection = new StylizedGrassGUI.Material.Section(materialEditor,"MAPS", "Main maps");
            colorSection = new StylizedGrassGUI.Material.Section(materialEditor,"COLOR", "Color");
            shadingSection = new StylizedGrassGUI.Material.Section(materialEditor,"SHADING", "Shading");
            verticesSection = new StylizedGrassGUI.Material.Section(materialEditor,"VERTICES", "Vertices");
            windSection = new StylizedGrassGUI.Material.Section(materialEditor,"WIND", "Wind");
        }
        public void FindProperties(MaterialProperty[] props)
        {
            ShaderConfigurator.GetConfiguration((materialEditor.target as Material).shader);

            culling = FindProperty("_Cull", props);
            windParams = Shader.GetGlobalVector("_GlobalWindParams");
            natureRendererParams = Shader.GetGlobalVector("GlobalWindDirectionAndStrength");

            baseMap = FindProperty("_BaseMap", props);
            alphaCutoffProp = FindProperty("_Cutoff", props);
            alphaToCoverage = FindProperty("_AlphaToCoverage", props);
            bumpMap = FindProperty("_BumpMap", props);
            color = FindProperty("_BaseColor", props);
            hueColor = FindProperty("_HueVariation", props);

            colorMapStrength = FindProperty("_ColorMapStrength", props);
            colorMapHeight = FindProperty("_ColorMapHeight", props);
            scalemapInfluence = FindProperty("_ScalemapInfluence", props);

            ambientOcclusion = FindProperty("_OcclusionStrength", props);
            vertexDarkening = FindProperty("_VertexDarkening", props);
            smoothness = FindProperty("_Smoothness", props);
            translucency = FindProperty("_Translucency", props);
            _TranslucencyIndirect = FindProperty("_TranslucencyIndirect", props);
            _TranslucencyOffset = FindProperty("_TranslucencyOffset", props);
            _TranslucencyFalloff = FindProperty("_TranslucencyFalloff", props);
            _NormalParams = FindProperty("_NormalParams", props);
            spherifyNormals = _NormalParams.vectorValue.x;
            flattenNormalsLighting = _NormalParams.vectorValue.y;
            normalVertexColorMask = _NormalParams.vectorValue.z;
            flattenNormalsGeometry = _NormalParams.vectorValue.w;

            windAmbientStrength = FindProperty("_WindAmbientStrength", props);
            windSpeed = FindProperty("_WindSpeed", props);
            windDirection = FindProperty("_WindDirection", props);
            windVertexRand = FindProperty("_WindVertexRand", props);
            windObjectRand = FindProperty("_WindObjectRand", props);
            windRandStrength = FindProperty("_WindRandStrength", props);
            windSwinging = FindProperty("_WindSwinging", props);

            bendMode = FindProperty("_BendMode", props);
            bendPushStrength = FindProperty("_BendPushStrength", props);
            bendFlattenStrength = FindProperty("_BendFlattenStrength", props);
            perspCorrection = FindProperty("_PerspectiveCorrection", props);
            _BendTint = FindProperty("_BendTint", props);

            windGustTex = FindProperty("_WindMap", props);
            windGustStrength = FindProperty("_WindGustStrength", props);
            windGustFreq = FindProperty("_WindGustFreq", props);
            windGustTint = FindProperty("_WindGustTint", props);

            _FadeParams = FindProperty("_FadeParams", props);
            enableDistFade = _FadeParams.vectorValue.z == 1f;
            fadeStartDist = _FadeParams.vectorValue.x;
            fadeEndDist = _FadeParams.vectorValue.y;

            lightingMode = FindProperty("_LightingMode", props);
            disableShadows = FindProperty("_ReceiveShadows", props);
            castShadows = FindProperty("_ReceiveShadows", props);
            environmentReflections = FindProperty("_EnvironmentReflections", props);
            _SpecularHighlights = FindProperty("_SpecularHighlights", props);
            scaleMap = FindProperty("_Scalemap", props);
            _Billboard = FindProperty("_Billboard", props);
            _AngleFading = FindProperty("_AngleFading", props);
            //_DisableDecals = FindProperty("_DisableDecals", props);
            if(targetMat.HasProperty("_CurvedWorldBendSettings")) _CurvedWorldBendSettings = FindProperty("_CurvedWorldBendSettings", props);

            unlitLightingContent = new GUIContent("None", 
                "No lighting is applied, grass rendered purely with its base color");
            
            simpleLightingContent = new GUIContent("Simple", "" +
               "Diffuse shading\n\n" +
               "" +
               "Lightmaps\n" +
               "Point and spot lights (per object)");

            advancedLightingContent = new GUIContent("Advanced",
                "Physically-based shading\n\n" +
                "" +
                "Lightmaps\n" +
                "Point and spot lights (per pixel/vertex)\n" +

                "Global Illumination\n" +
                "Specular reflections\n" +
                "Environment reflections\n" +
                "Light Probes\n");

            initliazed = true;
        }

        public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
            GUI.DrawTexture(r, UI.AssetIcon, ScaleMode.ScaleToFit);

            GUIContent c = new GUIContent("Version " + AssetInfo.INSTALLED_VERSION);
            r.width = EditorStyles.miniLabel.CalcSize(c).x + 7f;
            r.x += EditorGUIUtility.currentViewWidth - (r.width * 2f);
            r.y -= 10f;
            GUI.Label(r, c, EditorStyles.miniLabel);

            //TODO: Draw overlay for help button
        }
        
        //https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/648184ec8405115e2fcf4ad3023d8b16a191c4c7/com.unity.render-pipelines.universal/Editor/ShaderGUI/BaseShaderGUI.cs
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            this.materialEditor = materialEditor;

            materialEditor.SetDefaultGUIWidths();
            materialEditor.UseDefaultMargins();
            EditorGUIUtility.labelWidth = 0f;

            targetMat = materialEditor.target as Material;

            if (!initliazed)
            {
                OnEnable();
                initliazed = true;
            }
            
            FindProperties(props);

#if DEFAULT_GUI
            base.OnGUI(materialEditor, props);
            return;
#endif

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Lighting mode", GUILayout.Width(EditorGUIUtility.labelWidth));
                lightingMode.floatValue = (float)GUILayout.Toolbar((int)lightingMode.floatValue,
                    new GUIContent[] { unlitLightingContent, simpleLightingContent, advancedLightingContent }
                    );
            }
            EditorGUILayout.Space();

            DrawRendering();
            DrawMaps();
            DrawColor();
            if(lightingMode.floatValue > 0) DrawShading();
            DrawVertices();
            DrawWind();

            EditorGUILayout.Space();

            materialEditor.EnableInstancingField();
            if (!materialEditor.IsInstancingEnabled()) EditorGUILayout.HelpBox("GPU Instancing is highly recommended for optimal performance", MessageType.Warning);
            materialEditor.RenderQueueField();
            materialEditor.DoubleSidedGIField();

            if (EditorGUI.EndChangeCheck())
            {
                ApplyChanges();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Third party integration:", EditorStyles.boldLabel, GUILayout.MaxWidth(200f));
                EditorGUILayout.LabelField(ShaderConfigurator.CurrentConfig.ToString(), GUILayout.MaxWidth(125f));
                if (GUILayout.Button("Change"))
                {
                    GenericMenu menu = new GenericMenu();
                    if (ShaderConfigurator.CurrentConfig == ShaderConfigurator.Configuration.VegetationStudio)
                    {
                        menu.AddDisabledItem(new GUIContent("Switch to Vegetation Studio integration"));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Switch to Vegetation Studio integration"), false, ShaderConfigurator.ConfigureForVegetationStudio);
                    }
                    if (ShaderConfigurator.CurrentConfig == ShaderConfigurator.Configuration.GPUInstancer)
                    {
                        menu.AddDisabledItem(new GUIContent("Switch to GPU Instancer integration"));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Switch to GPU Instancer integration"), false, ShaderConfigurator.ConfigureForGPUInstancer);
                    }
                    if (ShaderConfigurator.CurrentConfig == ShaderConfigurator.Configuration.NatureRenderer)
                    {
                        menu.AddDisabledItem(new GUIContent("Switch to Nature Renderer integration"));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Switch to Nature Renderer integration"), false, ShaderConfigurator.ConfigureForNatureRenderer);
                    }

                    menu.ShowAsContext();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Staggart Creations -", EditorStyles.centeredGreyMiniLabel);

        }

        private void ApplyChanges()
        {
#if URP
            targetMat.mainTexture = baseMap.textureValue;
            targetMat.SetTexture("_WindMap", windGustTex.textureValue);
            targetMat.SetTexture("_BumpMap", bumpMap.textureValue);


            //Keywords
            if (bumpMap.textureValue) CoreUtils.SetKeyword(targetMat, "_NORMALMAP", bumpMap.textureValue);
            CoreUtils.SetKeyword(targetMat, "_SIMPLE_LIGHTING", targetMat.GetFloat("_LightingMode") == 1.0f);
            CoreUtils.SetKeyword(targetMat, "_ADVANCED_LIGHTING", targetMat.GetFloat("_LightingMode") == 2.0f);
            
            CoreUtils.SetKeyword(targetMat, "_RECEIVE_SHADOWS_OFF", targetMat.GetFloat("_ReceiveShadows") == 0.0f);
            CoreUtils.SetKeyword(targetMat, "_ENVIRONMENTREFLECTIONS_OFF", targetMat.GetFloat("_EnvironmentReflections") == 1.0f);
            CoreUtils.SetKeyword(targetMat, "_SPECULARHIGHLIGHTS_OFF", targetMat.GetFloat("_SpecularHighlights") == 0.0f);

            CoreUtils.SetKeyword(targetMat, "_SCALEMAP", targetMat.GetFloat("_Scalemap") == 1.0f);
            CoreUtils.SetKeyword(targetMat, "_BILLBOARD", targetMat.GetFloat("_Billboard") == 1.0f);
            CoreUtils.SetKeyword(targetMat, "_ANGLE_FADING", targetMat.GetFloat("_AngleFading") == 1.0f);
            //CoreUtils.SetKeyword(targetMat, "_DISABLE_BENDING", targetMat.GetFloat("_DisableBending") == 1.0f);
            //if (targetMat.HasProperty("_DisableDecals")) CoreUtils.SetKeyword(targetMat, "_DISABLE_DECALS", targetMat.GetFloat("_DisableDecals") == 1.0f);

            //Packed vectors
            _NormalParams.vectorValue = new Vector4(spherifyNormals, flattenNormalsLighting, normalVertexColorMask, flattenNormalsGeometry);
            _FadeParams.vectorValue = new Vector4(fadeStartDist, fadeEndDist, enableDistFade ? 1f : 0f, invertFading ? 1f : 0f);

            EditorUtility.SetDirty(targetMat);
#endif
        }


        private void DrawRendering()
        {
            renderingSection.Expanded = UI.Material.DrawHeader(renderingSection.title, renderingSection.Expanded, () => SwitchSection(renderingSection));
            renderingSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(renderingSection.anim.faded))
            {
                EditorGUILayout.Space();
                
                var cullingMode = (int)culling.floatValue;

                cullingMode = EditorGUILayout.Popup("Culling", cullingMode, new string[] { "Double-sided", "Front-faces", "Back-faces" });

                culling.floatValue = cullingMode;

                if (lightingMode.floatValue > 0)
                {
                    materialEditor.ShaderProperty(disableShadows, "Receive Shadows");
                }

                materialEditor.ShaderProperty(alphaToCoverage, new GUIContent("Alpha to coverage", "Reduces aliasing when using MSAA"));
                if (alphaToCoverage.floatValue > 0 && UniversalRenderPipeline.asset.msaaSampleCount == 1) EditorGUILayout.HelpBox("MSAA is disabled, alpha to coverage will have no effect", MessageType.None);

                UI.Material.Toggle(_Billboard, tooltip:"Force the Z-axis of the mesh to face the camera (Requires the GrassBillboardQuad mesh!)");
                
                EditorGUILayout.Space();
                
                UI.Material.Toggle(_AngleFading, tooltip:"Fadeout the mesh's facing that aren't facing the camera using dithering)");
                
                enableDistFade = EditorGUILayout.Toggle(new GUIContent("Distance fading", "Reduces the alpha clipping based on camera distance." +
                    "\n\nNote that this does not improve performance, only pixels are being hidden, meshes are still being rendered, " +
                    "best to match these settings to your maximum grass draw distance"), enableDistFade);
                if (enableDistFade)
                {
                    EditorGUI.indentLevel++;
                    invertFading = _FadeParams.vectorValue.w == 1f;
                    invertFading = EditorGUILayout.Toggle("Invert", invertFading);
                    UI.Material.DrawMinMaxSlider("Start/End", ref fadeStartDist, ref fadeEndDist);
                    EditorGUI.indentLevel--;
                }
                /*
                #if !URP_12_0_OR_NEWER
                using (new EditorGUI.DisabledScope(true))
                #endif
                {
                    UI.Material.Toggle(_DisableDecals, tooltip:"Disables decals applying to this material");

                    #if !URP_12_0_OR_NEWER
                    EditorGUILayout.HelpBox("Only has effect in Unity 2021.2+", MessageType.None);
                    #endif
                }
                */
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();
            
        }

        private void DrawMaps()
        {
            mapsSection.Expanded = UI.Material.DrawHeader(mapsSection.title, mapsSection.Expanded, () => SwitchSection(mapsSection));
            mapsSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(mapsSection.anim.faded))
            {
                EditorGUILayout.Space();
                
                materialEditor.TextureProperty(baseMap, "Texture (A=Alpha)");
                materialEditor.ShaderProperty(alphaCutoffProp, "Alpha clipping");
                materialEditor.TextureProperty(bumpMap, "Normal map");
                
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawColor()
        {
            colorSection.Expanded = UI.Material.DrawHeader(colorSection.title, colorSection.Expanded, () => SwitchSection(colorSection));
            colorSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(colorSection.anim.faded))
            {
                EditorGUILayout.Space();
                
                UI.Material.DrawColorField(color, true, tooltip:"This color is multiplied with the texture. Use a white texture to color the grass by this value entirely.");
                UI.Material.DrawColorField(hueColor, false, tooltip:"Every object will receive a random color between this color, and the main color. The alpha channel controls the intensity");

                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Color map", EditorStyles.boldLabel);
                if (!GrassColorMap.Active) EditorGUILayout.HelpBox("No color map is currently active", MessageType.None);
                UI.Material.DrawSlider(colorMapStrength, "Strength", tooltip:"Controls the much the color map influences the material. Overrides any other colors");
                UI.Material.DrawSlider(colorMapHeight, "Height", tooltip:"Controls which part of the mesh is affected, from bottom to top (based on the mesh's red vertex colors)");

                EditorGUILayout.Space();

                UI.Material.DrawSlider(ambientOcclusion, tooltip:"Darkens the mesh based on the red vertex color painted into the mesh");
                UI.Material.DrawSlider(vertexDarkening, tooltip:"Gives each vertex a random darker tint. Use in moderation to slightly break up visual repetition");
                UI.Material.DrawColorField(_BendTint, true, tooltip:"Multiplies the base color by this color, where ever something is bending the grass." +
                                                                    "\n\nThis only applies to the top of the grass, so the difference in color doesn't clash with the underlying terrain color");

                EditorGUILayout.Space();

            }
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawShading()
        {
            shadingSection.Expanded = UI.Material.DrawHeader(shadingSection.title, shadingSection.Expanded, () => SwitchSection(shadingSection));
            shadingSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(shadingSection.anim.faded))
            {
                EditorGUILayout.Space();

                if (lightingMode.floatValue == 2f)
                {
                    //materialEditor.ShaderProperty(environmentReflections, environmentReflections.displayName);
                    UI.Material.Toggle(environmentReflections, tooltip: "Enables reflections from skybox and reflection probes");
                    #if UNITY_2022_1_OR_NEWER
                    var customReflectionUsed = RenderSettings.customReflectionTexture;
                    #else
                    var customReflectionUsed = RenderSettings.customReflection;
                    #endif
                    if (environmentReflections.floatValue == 1f && RenderSettings.defaultReflectionMode == DefaultReflectionMode.Custom && !customReflectionUsed)
                    {
                        EditorGUILayout.HelpBox("Environment reflection source is set to \"Custom\" but no cubemap is assigned. ", MessageType.Warning);

                    }
                    UI.Material.Toggle(_SpecularHighlights, tooltip: "Enables specular reflections from lights");

                    UI.Material.DrawSlider(smoothness, tooltip: "Controls how strongly the skybox and reflection probes affect the material (similar to PBR smoothness)");
                }
                
                EditorGUILayout.Space();

                UI.Material.DrawSlider(translucency, tooltip:"Simulates sun light passing through the grass. Most noticeable at glancing or low sun angles\n\nControls the strength of light hitting the BACK");
                UI.Material.DrawSlider(_TranslucencyIndirect, tooltip:"Simulates sun light passing through the grass. Most noticeable at glancing or low sun angles\n\nControls the strength of light hitting the FRONT");
                EditorGUI.indentLevel++;
                UI.Material.DrawSlider(_TranslucencyFalloff, "Exponent", "Controls the size of the effect");
                UI.Material.DrawSlider(_TranslucencyOffset, "Offset", "Controls how much the effect wraps around the mesh. This at least requires spherical normals to take effect");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(new GUIContent("Normals", "Normals control the orientation of the vertices for lighting effect"), EditorStyles.boldLabel);
                flattenNormalsLighting = EditorGUILayout.Slider(new GUIContent("Flatten normals (lighting)", "Gradually has the normals point straight up, this will help match the shading to the surface the grass is placed on."), flattenNormalsLighting, 0f, 1f);
                spherifyNormals = EditorGUILayout.Slider(new GUIContent("Spherify normals", "Gradually has the normals point away from the object's pivot point. For grass this results in fluffy-like shading"), spherifyNormals, 0f, 1f);
                EditorGUI.indentLevel++;
                normalVertexColorMask = EditorGUILayout.Slider(new GUIContent("Tip mask", "Only apply spherifying to the top of the mesh (based on the red vertex color channel of the mesh)"), normalVertexColorMask, 0f, 1f);
                EditorGUI.indentLevel--;
                #if !URP_10_0_OR_NEWER
                using (new EditorGUI.DisabledScope(true))
                #endif
                {
                    flattenNormalsGeometry = EditorGUILayout.Slider(new GUIContent("Flatten normals (geometry)", "(When rendering the object during the Depth Normals prepass). Gradually has the normals point straight up, this determines how SSAO will affect the grass"), flattenNormalsGeometry, 0f, 1f);
                    #if !URP_10_0_OR_NEWER
                    EditorGUILayout.HelpBox("Only has effect in Unity 2020.2+", MessageType.None);
                    #endif
                }
                EditorGUILayout.Space();

            }
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawVertices()
        {
            verticesSection.Expanded = UI.Material.DrawHeader(verticesSection.title, verticesSection.Expanded, () => SwitchSection(verticesSection));
            verticesSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(verticesSection.anim.faded))
            {
                EditorGUILayout.Space();
                
                 if (targetMat.HasProperty("_CurvedWorldBendSettings"))
                {
                    EditorGUILayout.LabelField("Curved World 2020", EditorStyles.boldLabel);
                    materialEditor.ShaderProperty(_CurvedWorldBendSettings, _CurvedWorldBendSettings.displayName);
                    EditorGUILayout.Space();
                }
                 
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Bend Mode", GUILayout.Width(EditorGUIUtility.labelWidth));
                    bendMode.floatValue = (float)GUILayout.Toolbar((int)bendMode.floatValue,
                        new GUIContent[]
                        {
                            new GUIContent("Per-vertex", "Bending is applied on a per-vertex basis"), new GUIContent("Whole object", "Applied to all vertices at once, use this for plants/flowers to avoid distorting the mesh")
                        }
                        );
                }
                UI.Material.DrawSlider(bendPushStrength, tooltip: "The amount of pushing the material should receive by Grass Benders");
                UI.Material.DrawSlider(bendFlattenStrength, tooltip: "A multiplier for how much the material is flattened by Grass Benders");

                UI.Material.DrawSlider(perspCorrection, tooltip:"The amount by which the grass is gradually bent away from the camera as it looks down. Useful for better coverage in top-down perspectives");
                if (GrassColorMap.Active && GrassColorMap.Active.hasScalemap == false) EditorGUILayout.HelpBox("Active color map has no scale information", MessageType.None);
                if (!GrassColorMap.Active) EditorGUILayout.HelpBox("No color map is currently active", MessageType.None);
                materialEditor.ShaderProperty(scaleMap, new GUIContent("Apply scale map", "Enable scaling through terrain-layer heightmap"));
                if (scaleMap.floatValue == 1)
                {
                    EditorGUI.indentLevel++;
                    UI.Material.DrawVector3(scalemapInfluence, "Scale influence", "Controls the scale strength of the heightmap per axis");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

            }
            EditorGUILayout.EndFadeGroup();
        }

        private void DrawWind()
        {
            windSection.Expanded = UI.Material.DrawHeader(windSection.title, windSection.Expanded, () => SwitchSection(windSection));
            windSection.SetTarget();

            if (EditorGUILayout.BeginFadeGroup(windSection.anim.faded))
            {
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
                    if (windParams.x > 0f) EditorGUILayout.HelpBox("Wind strength is multiplied by " + Shader.GetGlobalVector("_GlobalWindParams").x.ToString() + " (Set by external script)", MessageType.Info);
                    if (natureRendererParams.w > 0f) EditorGUILayout.HelpBox("Nature Renderer wind strength and speed are added to these values", MessageType.Info);

                    UI.Material.DrawSlider(windAmbientStrength, tooltip:"The amount of wind that is applied without gusting");
                    UI.Material.DrawSlider(windSpeed, tooltip:"The speed the wind and gusting moves at");
                    UI.Material.DrawVector3(windDirection, windDirection.displayName, null);
                    UI.Material.DrawSlider(windSwinging, tooltip:"Controls the amount the grass is able to spring back against the wind direction");

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Randomization", EditorStyles.boldLabel);

                    UI.Material.DrawSlider(windObjectRand, "Per-object", "Adds a per-object offset, making each object move randomly rather than in unison");
                    UI.Material.DrawSlider(windVertexRand, "Per-vertex", "Adds a per-vertex offset");
                    UI.Material.DrawSlider(windRandStrength, tooltip:"Gives each object a random wind strength. This is useful for breaking up repetition and gives the impression of turbulence");

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Gusting", EditorStyles.boldLabel);
                    materialEditor.TexturePropertySingleLine(new GUIContent("Gust texture (Grayscale)"), windGustTex);

                    UI.Material.DrawSlider(windGustStrength, "Strength", "Gusting add wind strength based on the gust texture, which moves over the grass");
                    UI.Material.DrawSlider(windGustFreq, "Frequency", "Controls the tiling of the gusting texture, essentially setting the size of the gusting waves");
                    UI.Material.DrawSlider(windGustTint, "Color tint", "Uses the gusting texture to add a brighter tint based on the gusting strength");
                    
                EditorGUILayout.Space();

            }
            EditorGUILayout.EndFadeGroup();
        }
        
        private void SwitchSection(UI.Material.Section s)
        {
            /*
            renderingSection.Expanded = (s == renderingSection) ? !renderingSection.Expanded : false;
            mapsSection.Expanded = (s == mapsSection) ? !mapsSection.Expanded : false;
            colorSection.Expanded = (s == colorSection) ? !colorSection.Expanded : false;
            shadingSection.Expanded = (s == shadingSection) ? !shadingSection.Expanded : false;
            verticesSection.Expanded = (s == verticesSection) ? !verticesSection.Expanded : false;
            windSection.Expanded = (s == windSection) ? !windSection.Expanded : false;
            */
        }
#else
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            EditorGUILayout.HelpBox("The Universal Render Pipeline v" + AssetInfo.MIN_URP_VERSION + " is not installed", MessageType.Error);
        }
#endif
    }
}