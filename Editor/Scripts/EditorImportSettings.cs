// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast.Editor
{

    /// <summary>
    /// Editor Import specific settings (not relevant at runtime)
    /// </summary>
    [Serializable]
    class EditorImportSettings
    {

        /// <summary>
        /// Creates a secondary UV set on all meshes, if there is none present already.
        /// Often used for lightmaps.
        /// </summary>
        [Tooltip("Generate Lightmap UVs")]
        public bool generateSecondaryUVSet;

        /// <summary>
        /// Generate mesh LODs automatically for imported meshes.
        /// </summary>
        [Tooltip("Generate Mesh LODs")]
        public bool generateMeshLods;

        /// <summary>
        /// Maximum number of LOD levels to generate.
        /// </summary>
        [Tooltip("Maximum Mesh LOD levels")]
        public int maximumMeshLod = 32;

        /// <summary>
        /// LOD generation flags controlling the generation behavior.
        /// </summary>
        [Tooltip("Discard odd LOD levels to reduce memory usage")]
        public bool discardOddLevels = true;
    }
}
