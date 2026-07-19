#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.IO;

// ============================================================
// VRPipelineSetup — Editor tool to configure URP 
// for the flat-shaded retro aesthetic.
// Run from Unity menu: Tools > Virtua Racing > Setup Pipeline
// ============================================================

namespace VirtuaRacing
{
    public static class VRPipelineSetup
    {
        [MenuItem("Tools/Virtua Racing/Setup URP Pipeline")]
        public static void SetupPipeline()
        {
            // Create URP asset
            var pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            
            // Configure for flat-shaded retro look
            var so = new SerializedObject(pipelineAsset);
            
            // Quality settings
            so.FindProperty("m_SupportsHDR").boolValue = false;
            so.FindProperty("m_SupportsDynamicBatching").boolValue = true;
            
            // Anti-aliasing: OFF (crisp polygons are the aesthetic)
            so.FindProperty("m_MSAA").intValue = 1; // No MSAA
            var aaType = so.FindProperty("m_Antialiasing");
            if (aaType != null) aaType.intValue = 0; // No AA
            
            // Shadows: OFF (performance + authentic Model 1)
            so.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            so.FindProperty("m_SupportsMainLightShadows").boolValue = false;
            so.FindProperty("m_ShEvalMode").intValue = 0;
            
            // Render scale
            so.FindProperty("m_RenderScale").floatValue = 0.85f; // 85% for mobile performance
            
            // Depth texture: needed for some effects, but off for performance
            so.FindProperty("m_SupportsDepthTexture").boolValue = false;
            so.FindProperty("m_SupportsOpaqueTexture").boolValue = false;
            
            // SRP Batcher: ON for performance
            so.FindProperty("m_UseSRPBatcher").boolValue = true;
            
            so.ApplyModifiedProperties();
            
            // Save the asset
            string path = "Assets/Settings/VR_URP_Pipeline.asset";
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            AssetDatabase.CreateAsset(pipelineAsset, path);
            
            // Set as current pipeline
            GraphicsSettings.renderPipelineAsset = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
            
            // Create URP Renderer Data
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            var rendererSo = new SerializedObject(rendererData);
            
            // Configure renderer features
            rendererSo.FindProperty("m_RendererFeatures").ClearArray();
            rendererSo.FindProperty("m_PostProcessData").objectReferenceValue = null;
            
            rendererSo.ApplyModifiedProperties();
            
            string rendererPath = "Assets/Settings/VR_URP_Renderer.asset";
            AssetDatabase.CreateAsset(rendererData, rendererPath);
            
            // Link renderer to pipeline
            var rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipelineAsset);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[VRPipeline] URP configured for flat-shaded rendering. " +
                     "AA: OFF | Shadows: OFF | HDR: OFF | Render Scale: 85%");
        }
        
        [MenuItem("Tools/Virtua Racing/Create Flat-Shaded Material")]
        public static void CreateFlatShadedMaterial()
        {
            var shader = Shader.Find("VirtuaRacing/FlatShaded");
            if (shader == null)
            {
                Debug.LogError("FlatShaded shader not found! Ensure it's in Assets/Shaders/");
                return;
            }
            
            var material = new Material(shader);
            material.name = "VR_FlatShaded_Mat";
            
            string path = "Assets/Materials/VR_FlatShaded.mat";
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[VRPipeline] Created flat-shaded material at " + path);
        }
        
        [MenuItem("Tools/Virtua Racing/Setup All")]
        public static void SetupAll()
        {
            SetupPipeline();
            CreateFlatShadedMaterial();
            
            // Configure project settings
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            
            // Set target framerate
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            
            Debug.Log("[VRPipeline] Full setup complete. Ready to build Virtua Racing!");
        }
    }
}
#endif
