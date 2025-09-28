# Mesh LOD Generation for glTFast

This feature adds automatic mesh Level of Detail (LOD) generation to the Unity glTFast importer, similar to Unity's built-in ModelImporter.

## Overview

The mesh LOD generation feature automatically creates multiple levels of detail for imported glTF/glb meshes, reducing polygon count for distant objects to improve rendering performance.

## Features

- **Automatic LOD Generation**: Leverages Unity's `MeshLodUtility.GenerateMeshLods` method
- **Configurable LOD Levels**: Set maximum LOD levels or use unlimited generation
- **Editor UI Integration**: Simple toggle and numeric field in the importer inspector
- **Conditional UI**: LOD level setting only appears when LOD generation is enabled
- **Robust Error Handling**: Proper validation and error reporting
- **Asset Management**: Proper dirty marking and asset saving

## Usage

### Editor Import Settings

1. Select a `.gltf` or `.glb` file in the Project window
2. In the Inspector, locate the **Import Settings** section
3. Enable **Generate Mesh LODs** toggle
4. Optionally adjust **Maximum Mesh LOD** (default: 32)
5. Optionally toggle **Discard Odd Levels** (default: enabled)
6. Click **Apply** to reimport with LOD generation

### Settings

- **Generate Mesh LODs**: Enable/disable automatic LOD generation for all meshes (default: false)
- **Maximum Mesh LOD**: Maximum number of LOD levels to generate (default: 32)
  - `0`: Only original mesh (no LODs)
  - `1+`: Specific maximum LOD levels
- **Discard Odd Levels**: Discard odd LOD levels to reduce memory usage (default: true)
  - When enabled, only even LOD levels (0, 2, 4, etc.) are kept
  - Reduces memory footprint while maintaining performance benefits

## Implementation Details

### Core Components

1. **EditorImportSettings**: Extended with LOD configuration options
2. **GltfImporter**: Enhanced with LOD generation logic
3. **GltfImporterEditor**: UI controls and conditional visibility
4. **UI Integration**: UXML template updates for LOD controls

### Validation and Error Handling

The implementation includes several validation checks:

- Mesh readability verification
- Vertex count validation
- Exception handling for LOD generation failures
- Proper logging for success and error cases

### Asset Management

- Proper mesh dirty marking with `EditorUtility.SetDirty`
- Automatic bounds recalculation after LOD generation
- Normal recalculation for meshes without normals
- Integration with Unity's asset import pipeline

## Performance Considerations

- LOD generation only occurs during asset import (design-time)
- No runtime performance impact
- Meshes must be readable for LOD generation to work
- LOD generation may increase import time for complex meshes

## Limitations

- Requires readable meshes (checked automatically)
- Only works with meshes that have vertices
- LOD generation may not work well with certain mesh types
- Some very low-poly meshes may not benefit from LOD generation

## Technical Integration

The feature integrates with Unity's existing LOD system:
- Compatible with Unity's `LODGroup` component
- Uses standard Unity mesh LOD storage
- Follows Unity's LOD naming conventions
- Integrates with Unity's rendering pipeline

## Error Handling

Common error scenarios and handling:

1. **Non-readable mesh**: Warning logged, LOD generation skipped
2. **Empty mesh**: Warning logged, LOD generation skipped
3. **LOD generation failure**: Error logged with details
4. **Invalid LOD settings**: Graceful fallback behavior

## Future Enhancements

Potential improvements for future versions:

- LOD quality settings (aggressive, balanced, conservative)
- Custom LOD screen percentages
- Support for blend shapes in LODs
- LOD generation statistics and metrics
- Integration with Unity's LOD Group auto-generation