using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HalfGripSlapPack
{
    internal static class SignalStalkArtwork
    {
        internal static void Attach(Transform parent, float length, Texture2D texture, int layer, List<Object> owned)
        {
            if (!texture) throw new InvalidOperationException("Missing sprites/turn-signal-stalk.png. Install the complete SlapPack folder, including sprites.");
            if (texture.width != SignalStalkLayout.TextureWidth || texture.height != SignalStalkLayout.TextureHeight)
                throw new InvalidOperationException("Unexpected turn-signal-stalk.png dimensions; install the matching source and sprites together.");
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var layout = new SignalStalkLayout(length);
            float[] cuts = { SignalStalkLayout.SourceLeft, SignalStalkLayout.GripCut, SignalStalkLayout.BootCut, SignalStalkLayout.SourceRight };
            var vertices = new Vector3[12]; var uv = new Vector2[12]; var indices = new int[18];
            for (int segment = 0; segment < 3; segment++)
            {
                int first = segment * 4, triangle = segment * 6;
                float x0 = cuts[segment], x1 = cuts[segment + 1];
                float y0 = SignalStalkLayout.SourceBottom, y1 = SignalStalkLayout.SourceTop;
                vertices[first] = new Vector3(layout.MapX(x0), SignalStalkLayout.MapY(y0), 0);
                vertices[first + 1] = new Vector3(layout.MapX(x1), SignalStalkLayout.MapY(y0), 0);
                vertices[first + 2] = new Vector3(layout.MapX(x1), SignalStalkLayout.MapY(y1), 0);
                vertices[first + 3] = new Vector3(layout.MapX(x0), SignalStalkLayout.MapY(y1), 0);
                uv[first] = new Vector2(x0 / texture.width, y0 / texture.height);
                uv[first + 1] = new Vector2(x1 / texture.width, y0 / texture.height);
                uv[first + 2] = new Vector2(x1 / texture.width, y1 / texture.height);
                uv[first + 3] = new Vector2(x0 / texture.width, y1 / texture.height);
                indices[triangle] = first; indices[triangle + 1] = first + 1; indices[triangle + 2] = first + 2;
                indices[triangle + 3] = first; indices[triangle + 4] = first + 2; indices[triangle + 5] = first + 3;
            }
            var mesh = new Mesh { name = "SlapPack pixel-art signal stalk" };
            owned.Add(mesh);
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
            mesh.RecalculateBounds(); mesh.RecalculateNormals();
            var go = new GameObject("SlapPack.SignalStalkArtwork");
            owned.Add(go); go.layer = layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = WindowMaterials.Transparent(Color.white, texture, owned);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
