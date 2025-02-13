﻿using System.Collections.Concurrent;
using System.Numerics;
using NoitaMap.Map;
using NoitaMap.Viewer;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Graphics.Atlases;

public class PixelSceneAtlasBuffer : PackedAtlasedQuadBuffer
{
    private readonly PathService PathService;

    private readonly List<PixelScene> PixelScenes = new List<PixelScene>();

    private readonly ConcurrentQueue<PixelScene> ThreadedPixelSceneQueue = new ConcurrentQueue<PixelScene>();

    public PixelSceneAtlasBuffer(ViewerDisplay viewerDisplay)
        : base(viewerDisplay)
    {
        PathService = viewerDisplay.PathService;
    }

    public void AddPixelScene(PixelScene pixelScene)
    {
        if (pixelScene.AtlasTexturePath is null)
        {
            return;
        }

        if (PathService.DataPath is null)
        {
            return;
        }

        string? path = null;

        if (pixelScene.AtlasTexturePath.StartsWith("data/"))
        {
            path = Path.Combine(PathService.DataPath, pixelScene.AtlasTexturePath.Remove(0, 5));
        }

        if (!File.Exists(path))
        {
            return;
        }

        ThreadedPixelSceneQueue.Enqueue(pixelScene);
    }

    public void Update()
    {
        bool needsUpdate = false;

        while (ThreadedPixelSceneQueue.TryDequeue(out PixelScene? pixelScene))
        {
            ProcessPixelScene(pixelScene);

            needsUpdate = true;
        }

        if (needsUpdate)
        {
            TransformBuffer.UpdateInstanceBuffer();
        }
    }

    public void ProcessPixelScene(PixelScene pixelScene)
    {
        if (pixelScene.AtlasTexturePath is null)
        {
            throw new Exception("pixelScene.AtlasTexturePath was null when it shouldn't have been");
        }

        string? path = null;

        if (pixelScene.AtlasTexturePath.StartsWith("data/"))
        {
            path = Path.Combine(PathService.DataPath!, pixelScene.AtlasTexturePath.Remove(0, 5));
        }

        using Image<Rgba32> image = LoadPixelSceneImage(path!);

        image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);

        ResourcePosition resourcePosition = AddTextureToAtlas(image.Width, image.Height, path!.GetHashCode(), memory.Span);

        PixelScenes.Add(pixelScene);

        TransformBuffer.InsertInstance(resourcePosition.InstanceIndex, new VertexInstance()
        {
            Transform = Matrix4x4.CreateScale(image.Width, image.Height, 1f) * Matrix4x4.CreateTranslation(pixelScene.X, pixelScene.Y, 0f),
            TexturePosition = resourcePosition.UV,
            TextureSize = resourcePosition.UVSize
        });

        InstancesPerAtlas[resourcePosition.AtlasIndex]++;
    }

    private static Image<Rgba32> LoadPixelSceneImage(string path)
    {
        Configuration configuration = Configuration.Default;

        configuration.PreferContiguousImageBuffers = true;

        Image<Rgba32> image = Image.Load<Rgba32>(new DecoderOptions() { Configuration = configuration }, path);

        return image;
    }
}
