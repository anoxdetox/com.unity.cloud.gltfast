// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GLTFast.Editor
{
    /// <summary>
    /// Selection-based mesh preview enhancement that provides preview functionality specifically
    /// for glTF imported meshes without interfering with Unity's built-in mesh inspector.
    /// </summary>
    [InitializeOnLoad]
    public static class GltfMeshPreviewIntegration
    {
        // Static preview management
        static Dictionary<Object, GltfMeshPreview> s_MeshPreviews = new Dictionary<Object, GltfMeshPreview>();
        static Object[] s_LastSelection;

        static GltfMeshPreviewIntegration()
        {
            // Subscribe to selection changes
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            // Initial selection check
            OnSelectionChanged();
        }

        static void OnSelectionChanged()
        {
            var newSelection = Selection.objects;

            // Clean up previous previews if selection changed
            if (s_LastSelection != newSelection)
            {
                CleanupUnusedPreviews(newSelection);
                s_LastSelection = newSelection;
            }

            // Create previews for newly selected glTF meshes
            foreach (var obj in newSelection)
            {
                if (obj is Mesh mesh && IsGltfImportedMesh(mesh))
                {
                    if (!s_MeshPreviews.ContainsKey(obj))
                    {
                        s_MeshPreviews[obj] = new GltfMeshPreview(mesh);
                    }
                }
            }
        }

        static void OnHierarchyChanged()
        {
            // Clean up previews for deleted objects
            CleanupUnusedPreviews(Selection.objects);
        }

        static void CleanupUnusedPreviews(Object[] currentSelection)
        {
            var toRemove = new List<Object>();

            foreach (var kvp in s_MeshPreviews)
            {
                bool isStillSelected = false;
                if (currentSelection != null)
                {
                    foreach (var selected in currentSelection)
                    {
                        if (selected == kvp.Key)
                        {
                            isStillSelected = true;
                            break;
                        }
                    }
                }

                if (!isStillSelected || kvp.Key == null)
                {
                    kvp.Value?.Dispose();
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                s_MeshPreviews.Remove(key);
            }
        }

        public static bool IsGltfImportedMesh(Mesh mesh)
        {
            if (mesh == null)
                return false;

            var assetPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var importer = AssetImporter.GetAtPath(assetPath);
            return importer is GltfImporter;
        }

        public static GltfMeshPreview GetPreviewForMesh(Mesh mesh)
        {
            return s_MeshPreviews.TryGetValue(mesh, out var preview) ? preview : null;
        }
    }

    /// <summary>
    /// Custom mesh inspector that only handles glTF imported meshes, leaving Unity's built-in
    /// inspector to handle all other meshes properly.
    /// </summary>
    [CustomEditor(typeof(Mesh), true)]
    [CanEditMultipleObjects]
    class GltfMeshInspector : UnityEditor.Editor
    {
        bool m_IsGltfImportedMesh = false;
        UnityEditor.Editor m_DefaultInspector;

        void OnEnable()
        {
            // Check if ANY of the targets are glTF imported
            m_IsGltfImportedMesh = false;
            foreach (var obj in targets)
            {
                if (obj is Mesh mesh && GltfMeshPreviewIntegration.IsGltfImportedMesh(mesh))
                {
                    m_IsGltfImportedMesh = true;
                    break;
                }
            }

            // If not glTF meshes, create Unity's default inspector
            if (!m_IsGltfImportedMesh)
            {
                CreateCachedEditor(targets, null, ref m_DefaultInspector);
            }
        }

        void OnDisable()
        {
            if (m_DefaultInspector != null)
            {
                DestroyImmediate(m_DefaultInspector);
                m_DefaultInspector = null;
            }
        }

        public override bool HasPreviewGUI()
        {
            if (m_IsGltfImportedMesh)
            {
                return target != null && target is Mesh;
            }
            return m_DefaultInspector?.HasPreviewGUI() ?? false;
        }

        public override void OnPreviewSettings()
        {
            if (m_IsGltfImportedMesh)
            {
                var mesh = target as Mesh;
                var preview = GltfMeshPreviewIntegration.GetPreviewForMesh(mesh);
                preview?.OnPreviewSettings();
            }
            else
            {
                m_DefaultInspector?.OnPreviewSettings();
            }
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (m_IsGltfImportedMesh)
            {
                var mesh = target as Mesh;
                var preview = GltfMeshPreviewIntegration.GetPreviewForMesh(mesh);
                if (preview != null)
                {
                    preview.OnPreviewGUI(rect, background);
                }
                else
                {
                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40),
                            "Mesh preview not available");
                }
            }
            else
            {
                m_DefaultInspector?.OnPreviewGUI(rect, background);
            }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (m_IsGltfImportedMesh)
            {
                var mesh = target as Mesh;
                var preview = GltfMeshPreviewIntegration.GetPreviewForMesh(mesh);
                return preview?.RenderStaticPreview(width, height);
            }
            return m_DefaultInspector?.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        public override void OnInspectorGUI()
        {
            if (m_IsGltfImportedMesh)
            {
                DrawGltfInspectorGUI();
            }
            else
            {
                m_DefaultInspector?.OnInspectorGUI();
            }
        }

        void DrawGltfInspectorGUI()
        {
            GUI.enabled = true;

            if (targets?.Length > 1)
            {
                DrawMultiSelectionInfo();
                return;
            }

            var mesh = target as Mesh;
            if (mesh == null)
                return;

            DrawSingleMeshInfo(mesh);
        }

        void DrawMultiSelectionInfo()
        {
            long totalVertices = 0;
            long totalIndices = 0;

            foreach (var obj in targets)
            {
                if (obj is Mesh m)
                {
                    totalVertices += m.vertexCount;
                    totalIndices += CalcTotalIndices(m);
                }
            }

            EditorGUILayout.LabelField($"Total Vertices: {totalVertices:N0}");
            EditorGUILayout.LabelField($"Total Triangles: {totalIndices / 3:N0}");
        }

        void DrawSingleMeshInfo(Mesh mesh)
        {
            EditorGUILayout.LabelField("glTF Mesh", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount:N0}");
            EditorGUILayout.LabelField($"Triangles: {CalcTotalIndices(mesh) / 3:N0}");
            EditorGUILayout.LabelField($"Sub Meshes: {mesh.subMeshCount}");

            if (mesh.blendShapeCount > 0)
                EditorGUILayout.LabelField($"Blend Shapes: {mesh.blendShapeCount}");

            // Check for LOD information
            var lodInfo = GetMeshLodInfo(mesh);
            if (!string.IsNullOrEmpty(lodInfo))
                EditorGUILayout.LabelField(lodInfo);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Read/Write: {(mesh.isReadable ? "Yes" : "No")}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounds:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Center: {mesh.bounds.center}");
            EditorGUILayout.LabelField($"Size: {mesh.bounds.size}");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Imported from glTF/glb using glTFast", MessageType.Info);
        }

        long CalcTotalIndices(Mesh mesh)
        {
            long totalIndices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                totalIndices += mesh.GetIndexCount(i);
            }
            return totalIndices;
        }

        string GetMeshLodInfo(Mesh mesh)
        {
            try
            {
                var meshType = mesh.GetType();
                var lodCountProperty = meshType.GetProperty("lodCount");
                if (lodCountProperty != null)
                {
                    var lodCount = (int)lodCountProperty.GetValue(mesh);
                    if (lodCount > 1)
                        return $"LOD Levels: {lodCount}";
                }
            }
            catch
            {
                // LOD functionality might not be available
            }
            return "";
        }

        public override string GetInfoString()
        {
            if (m_IsGltfImportedMesh)
            {
                var mesh = target as Mesh;
                var preview = GltfMeshPreviewIntegration.GetPreviewForMesh(mesh);
                return preview?.GetInfoString() ?? GenerateMeshInfoString(mesh);
            }
            return m_DefaultInspector?.GetInfoString() ?? "";
        }

        string GenerateMeshInfoString(Mesh mesh)
        {
            if (mesh == null)
                return "";

            var triangleCount = CalcTotalIndices(mesh) / 3;
            string info = $"{mesh.vertexCount:N0} Vertices, {triangleCount:N0} Triangles";

            if (mesh.subMeshCount > 1)
                info += $", {mesh.subMeshCount} Sub Meshes";

            if (mesh.blendShapeCount > 0)
                info += $", {mesh.blendShapeCount} Blend Shapes";

            var lodInfo = GetMeshLodInfo(mesh);
            if (!string.IsNullOrEmpty(lodInfo))
                info += $", {lodInfo.Replace("LOD Levels: ", "")} LODs";

            return info;
        }
    }

    /// <summary>
    /// Custom mesh preview implementation that provides all the features of Unity's built-in MeshPreview
    /// but works correctly with glTFast imported meshes.
    /// </summary>
    public class GltfMeshPreview : System.IDisposable
    {
        // Preview settings
        class Settings
        {
            public Vector2 previewDir = new Vector2(130, 0);
            public Vector2 lightDir = new Vector2(-40, -40);
            public float zoomFactor = 1.0f;
            public Vector3 pivotPositionOffset = Vector3.zero;
            public bool drawWire = false;
        }

        static readonly GUIContent s_WireframeToggle = EditorGUIUtility.TrIconContent("wireframe", "Show wireframe");

        Mesh m_Mesh;
        PreviewRenderUtility m_PreviewUtility;
        Settings m_Settings;
        Material m_WireMaterial;
        Material m_ShadedMaterial;

        public GltfMeshPreview(Mesh mesh)
        {
            m_Mesh = mesh;
            m_PreviewUtility = new PreviewRenderUtility();
            m_Settings = new Settings();

            // Setup camera
            m_PreviewUtility.camera.fieldOfView = 30.0f;
            m_PreviewUtility.camera.nearClipPlane = 0.01f;
            m_PreviewUtility.camera.farClipPlane = 1000f;

            // Create materials
            CreateMaterials();
        }

        void CreateMaterials()
        {
            // Try multiple shader options for shaded material
            string[] shadedShaders = {
                "Standard",
                "Legacy Shaders/Diffuse",
                "Hidden/Internal-PreviewShader",
                "Unlit/Color"
            };

            foreach (var shaderName in shadedShaders)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    m_ShadedMaterial = new Material(shader);
                    m_ShadedMaterial.hideFlags = HideFlags.HideAndDontSave;
                    m_ShadedMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);

                    if (m_ShadedMaterial.HasProperty("_Metallic"))
                        m_ShadedMaterial.SetFloat("_Metallic", 0.1f);
                    if (m_ShadedMaterial.HasProperty("_Smoothness"))
                        m_ShadedMaterial.SetFloat("_Smoothness", 0.3f);
                    if (m_ShadedMaterial.HasProperty("_Color"))
                        m_ShadedMaterial.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f, 1.0f));

                    break;
                }
            }

            // Create wireframe material
            CreateWireframeMaterial();
        }

        void CreateWireframeMaterial()
        {
            // Try multiple shader options for wireframe
            string[] wireframeShaders = {
                "Hidden/Internal-Colored",
                "Internal-Colored",
                "Unlit/Color",
                "Sprites/Default"
            };

            foreach (var shaderName in wireframeShaders)
            {
                var wireShader = Shader.Find(shaderName);
                if (wireShader != null)
                {
                    m_WireMaterial = new Material(wireShader);
                    m_WireMaterial.hideFlags = HideFlags.HideAndDontSave;
                    m_WireMaterial.color = new Color(0, 0, 0, 0.3f);

                    // Set additional properties if they exist
                    if (m_WireMaterial.HasProperty("_ZWrite"))
                        m_WireMaterial.SetFloat("_ZWrite", 0.0f);
                    if (m_WireMaterial.HasProperty("_ZBias"))
                        m_WireMaterial.SetFloat("_ZBias", -1.0f);

                    break;
                }
            }
        }

        public void Dispose()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            if (m_ShadedMaterial != null)
            {
                Object.DestroyImmediate(m_ShadedMaterial);
                m_ShadedMaterial = null;
            }

            if (m_WireMaterial != null)
            {
                Object.DestroyImmediate(m_WireMaterial);
                m_WireMaterial = null;
            }
        }

        public void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            GUI.enabled = true;

            // Wireframe toggle
            var newDrawWire = GUILayout.Toggle(m_Settings.drawWire, s_WireframeToggle, EditorStyles.toolbarButton);
            if (newDrawWire != m_Settings.drawWire)
            {
                m_Settings.drawWire = newDrawWire;
            }
        }

        public void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40),
                        "Mesh preview requires\nrender texture support");
                return;
            }

            HandleMouseInput(rect);

            if (Event.current.type == EventType.Repaint)
            {
                m_PreviewUtility.BeginPreview(rect, background);
                DoRenderPreview();
                m_PreviewUtility.EndAndDrawPreview(rect);
            }
        }

        void HandleMouseInput(Rect rect)
        {
            var evt = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && (evt.button == 0 || evt.button == 1))
                    {
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Convert mouse delta to rotation values
                        var deltaX = evt.delta.x / rect.width * 360f;
                        var deltaY = evt.delta.y / rect.height * 360f;

                        if (evt.button == 0)
                        {
                            // Left mouse - rotate view
                            m_Settings.previewDir.x += deltaX;
                            m_Settings.previewDir.y += deltaY;
                            m_Settings.previewDir.x = Mathf.Repeat(m_Settings.previewDir.x, 360f);
                            m_Settings.previewDir.y = Mathf.Clamp(m_Settings.previewDir.y, -90f, 90f);
                        }
                        else if (evt.button == 1)
                        {
                            // Right mouse - adjust lighting
                            m_Settings.lightDir.x += deltaX;
                            m_Settings.lightDir.y += deltaY;
                        }

                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    if (rect.Contains(evt.mousePosition))
                    {
                        float zoomDelta = -HandleUtility.niceMouseDeltaZoom * 0.05f;
                        m_Settings.zoomFactor = Mathf.Clamp(m_Settings.zoomFactor + zoomDelta, 0.1f, 10.0f);
                        evt.Use();
                    }
                    break;
            }

            // Frame selected (F key)
            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand) &&
                evt.commandName == "FrameSelected")
            {
                FrameObject();
                evt.Use();
            }
        }

        void FrameObject()
        {
            m_Settings.zoomFactor = 1.0f;
            m_Settings.pivotPositionOffset = Vector3.zero;
        }

        void DoRenderPreview()
        {
            if (m_Mesh == null || m_PreviewUtility == null)
                return;

            var bounds = m_Mesh.bounds;
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = 4.0f * halfSize;

            // Setup camera
            var rot = Quaternion.Euler(m_Settings.previewDir.y, 0, 0) *
                     Quaternion.Euler(0, m_Settings.previewDir.x, 0);
            var pos = rot * (-bounds.center);

            var camTransform = m_PreviewUtility.camera.transform;
            var camPosition = rot * Vector3.forward * (-distance * m_Settings.zoomFactor) + m_Settings.pivotPositionOffset;

            camTransform.position = camPosition;
            camTransform.rotation = rot;

            // Setup lighting
            if (m_PreviewUtility.lights.Length > 0)
            {
                m_PreviewUtility.lights[0].intensity = 1.1f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(-m_Settings.lightDir.y, -m_Settings.lightDir.x, 0);
            }

            if (m_PreviewUtility.lights.Length > 1)
            {
                m_PreviewUtility.lights[1].intensity = 1.1f;
                m_PreviewUtility.lights[1].transform.rotation = Quaternion.Euler(m_Settings.lightDir.y, m_Settings.lightDir.x, 0);
            }

            m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);

            // Always render shaded mesh first
            var transformation = Matrix4x4.TRS(pos, rot, Vector3.one);

            if (m_ShadedMaterial != null)
            {
                // Clear and render shaded
                m_PreviewUtility.camera.clearFlags = CameraClearFlags.Skybox;

                for (int i = 0; i < m_Mesh.subMeshCount; ++i)
                {
                    m_PreviewUtility.DrawMesh(m_Mesh, transformation, m_ShadedMaterial, i);
                }

                m_PreviewUtility.Render();
            }

            // Render wireframe overlay if enabled
            if (m_Settings.drawWire && m_WireMaterial != null)
            {
                m_PreviewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                GL.wireframe = true;

                for (int i = 0; i < m_Mesh.subMeshCount; ++i)
                {
                    var topology = m_Mesh.GetTopology(i);
                    if (topology != MeshTopology.Lines && topology != MeshTopology.LineStrip && topology != MeshTopology.Points)
                    {
                        m_PreviewUtility.DrawMesh(m_Mesh, transformation, m_WireMaterial, i);
                    }
                }

                m_PreviewUtility.Render();
                GL.wireframe = false;
            }
        }

        public Texture2D RenderStaticPreview(int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            DoRenderPreview();
            return m_PreviewUtility.EndStaticPreview();
        }

        public string GetInfoString()
        {
            if (m_Mesh == null)
                return "";

            long totalIndices = 0;
            for (int i = 0; i < m_Mesh.subMeshCount; i++)
            {
                totalIndices += m_Mesh.GetIndexCount(i);
            }

            var triangleCount = totalIndices / 3;
            string info = $"{m_Mesh.vertexCount:N0} Vertices, {triangleCount:N0} Triangles";

            if (m_Mesh.subMeshCount > 1)
                info += $", {m_Mesh.subMeshCount} Sub Meshes";

            if (m_Mesh.blendShapeCount > 0)
                info += $", {m_Mesh.blendShapeCount} Blend Shapes";

            return info;
        }
    }
}