// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace GLTFast
{
    /// <summary>A 3 component vector of unsigned 16-bit integers.</summary>
    struct ushort3
    {
        /// <summary>x component of the vector.</summary>
        public ushort x;
        /// <summary>y component of the vector.</summary>
        public ushort y;
        /// <summary>z component of the vector.</summary>
        public ushort z;

        /// <summary>
        /// Converts 3 component vector from unsigned short in glTF space to
        /// float3 in Unity space.
        /// </summary>
        /// <returns>3 component vector in Unity space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GltfToUnityFloat3()
        {
            return new float3(-x, y, z);
        }

        /// <summary>
        /// Converts 3 component vector from unsigned short in glTF space to
        /// normalized float vector in Unity space.
        /// </summary>
        /// <returns>Normalized 3 component vector in Unity space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GltfToUnityNormalizedFloat3()
        {
            return new float3(
                -(x / (float)ushort.MaxValue),
                y / (float)ushort.MaxValue,
                z / (float)ushort.MaxValue
            );
        }

        /// <summary>
        /// Converts triangle indices from unsigned short in glTF space to
        /// signed int indices in Unity space.
        /// </summary>
        /// <returns>Triangle indices vector in Unity space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 GltfToUnityTriangleIndies()
        {
            return new int3(x, z, y);
        }
    }
}
