using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {
        GameObject CreateMeshObject(string name, Material material, Mesh mesh)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();

            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.GetComponent<MeshRenderer>().material = material;

            var meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;

            obj.layer = LayerMask.NameToLayer("Terrain");

            return obj;
        }
    }
}
