using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateLargeSceneHelper
{

    const string demo_scene_name = "sniffer demo";
    const string demo_dir = "Assets/Demo/";
    const string demo_scene_path = demo_dir + demo_scene_name + ".unity";
    [InitializeOnLoadMethod]
    static void CreateLargeSceneAtBeginning()
    {
        if (!System.IO.File.Exists(demo_scene_path))
        {
            var mi = typeof(UnityEditor.SceneManagement.EditorSceneManager).
                GetMethod("CreateSceneAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            mi.Invoke(null, new object[] { demo_scene_path, true });
        }
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) =>
           {
               if (scene.name == demo_scene_name)
               {
                   if (scene.rootCount > 2)
                       return;

                   var yes = EditorUtility.DisplayDialog("Create Large Scene", "Create Large Scene?", "Yes", "No");
                   if (yes)
                   {
                       CreateLargeScene(Random.Range(0, 21));
                       UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                   }
               }
           };
    }

    static void CreateLargeScene(int seed)
    {
        Random.InitState(seed);
        new GameObject("Seed: " + seed);
        CreatePrimitives(PrimitiveType.Cube, 100).transform.position = Random.insideUnitSphere * 5;
        CreatePrimitives(PrimitiveType.Sphere, 1000).transform.position = Random.insideUnitSphere * 5;
        CreatePrimitives(PrimitiveType.Capsule, 10000).transform.position = Random.insideUnitSphere * 5;
        var len = Random.Range(1, 18);
        EditorUtility.DisplayProgressBar("Creating... ", "", 0.0f);
        for (int i = 0; i < len; i++)
        {
            CreateRandom().transform.position = Random.insideUnitSphere * 5;
            EditorUtility.DisplayProgressBar("Creating... ", "", (i + 1.0f) / len);
        }
        EditorUtility.ClearProgressBar();
    }

    static GameObject CreateRandom()
    {
        var option = Random.Range(0, 3);
        if (option == 0)
            return CreatePrimitives(RandomPrimitiveType(), Random.Range(1, 21) * 100);
        else if (option == 1)
            return CreateCustomMeshObject(Random.Range(1, 10) * (1 << Random.Range(0, 8)));
        else
        {
            var empty = new GameObject("random");
            var child = CreateRandom();
            child.transform.SetParent(empty.transform, false);
            return empty;
        }
    }

    static GameObject CreatePrimitives(PrimitiveType type, int count, bool? hasCollider = null)
    {
        GameObject go = new GameObject();
        go.name = count + " " + type + "s";
        if (count <= 500)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.CreatePrimitive(type);
                obj.transform.SetParent(go.transform);
                obj.transform.localPosition = Random.insideUnitSphere * 2;
                obj.transform.localRotation = Random.rotation;
                obj.transform.localScale = Vector3.one * 0.02f;
                if (!hasCollider.HasValue)
                    hasCollider = Random.Range(0, 1) == 1;
                if (!hasCollider.Value)
                    Object.DestroyImmediate(obj.GetComponent<Collider>());
            }
        }
        else
        {
            int subcount = count > 10000 ? count / 500 : count > 2000 ? 20 : 10;
            int count_per_sub = count / subcount;

            void generate_sub(int number)
            {
                var sub_hasCollider = hasCollider ?? RandomNullableBoolean();
                var sub = CreatePrimitives(type, number, sub_hasCollider);
                sub.transform.SetParent(go.transform);
                sub.transform.localPosition = Random.insideUnitSphere * 2;
            }

            for (int i = 0; i < subcount; i++)
            {
                generate_sub(count_per_sub);
            }
            {
                int remains = count - subcount * count_per_sub;
                if (remains > 0)
                    generate_sub(remains);
            }

        }
        return go;
    }

    static GameObject CreateCustomMeshObject(int grids)
    {
        const string meshDir = demo_dir + "Meshes/";
        var meshName = "auto." + grids + ".mesh";
        var path = meshDir + meshName;
        Mesh mesh;
        if (File.Exists(path))
        {
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshDir);
        }
        else
        {
            var vs = new Vector3[(grids + 1) * (grids + 1)];
            var ts = new int[grids * grids * 2 * 3];
            mesh = new Mesh();
            mesh.vertices = vs;
            mesh.triangles = ts;
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
        }
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Grids " + grids;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        return go;
    }

    static PrimitiveType RandomPrimitiveType()
    {
        var result = Random.Range(0, 6);
        return (PrimitiveType)result;
    }

    static bool? RandomNullableBoolean()
    {
        var result = Random.Range(0, 3);
        switch (result)
        {
            case 0: return false;
            case 1: return true;
            default: return null;
        }
    }
}
