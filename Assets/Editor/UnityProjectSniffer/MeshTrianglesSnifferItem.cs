using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZXthex.UnityProjectSniffer
{

    class MeshTrianglesSnifferItem : SnifferTypeItem
    {
        protected override string title => "(Meshes referenced by MeshFilter and SkinnedMeshRenderer).triangles.Length | Scene";
        public override void DrawItemGUI()
        {
        }

        public override Int64Calc GetItemCalc()
        {
            return new TrianglesInSceneMeshCalc(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }


    class TrianglesInSceneMeshCalc : Int64Calc
    {
        public enum ActiveMode
        {
            All,
            ActiveGameObject,
            ActiveAndEnabled,
        }

        GameObject[] rootObjects;
        ActiveMode mode;
        public TrianglesInSceneMeshCalc(UnityEngine.SceneManagement.Scene scene, ActiveMode mode = ActiveMode.All)
        {
            rootGUI.title = scene.name;
            rootObjects = scene.GetRootGameObjects();
            this.mode = mode;
        }

        protected override IEnumerator UpdateNumber()
        {
            int all_count = 0;
            foreach (var go in rootObjects)
            {
                var trans = go.GetComponentsInChildren<Transform>(true);
                all_count += trans.Length;
            }

            var meshCache = new Dictionary<Mesh, int>(all_count * 2 + 1);
            foreach (var go in rootObjects)
            {
                var mfs = go.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in mfs)
                {
                    var m = mf.sharedMesh;
                    if (!m || meshCache.ContainsKey(m))
                        continue;
                    meshCache.Add(m, m.triangles.Length);
                }
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    var m = smr.sharedMesh;
                    if (meshCache.ContainsKey(m))
                        continue;
                    meshCache.Add(m, m.triangles.Length);
                }
            }

            yield return null;
            var open = new Stack<GameObject>(all_count);
            var map = new Dictionary<GameObject, (TreeNodel treeN, TreeNodeGUI treeG)>(all_count * 2 + 1);

            void pushAndBindGUI(in GameObject go, (TreeNodel n, TreeNodeGUI g) parent)
            {
                open.Push(go);

                var model = new TreeNodel();
                model.referenceObject = go;
                var gui = new TreeNodeGUI();
                gui.title = go.name;
                //tooltip
                {
                    int sibling = go.transform.GetSiblingIndex();
                    gui.tooltip = UnityUtils.GetGameObjectPath(go, 40) +" [" + sibling+"]";
                }
                gui.model = model;
                map[go] = (model, gui);
                parent.n.children.Add(model);
                parent.g.children.Add(gui);
            }

            int deal_count = 0;
            int total_tris = 0;

            void doCalc(GameObject current)
            {
                Mesh mesh = null;

                deal_count++;
                progress_0_1 = (float)deal_count / all_count;

                if ((mode == ActiveMode.ActiveAndEnabled || mode == ActiveMode.ActiveGameObject) && !current.activeInHierarchy)
                {
                    return;
                }

                var mf = current.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    mesh = mf.sharedMesh;
                }
                else
                {
                    var smr = current.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null && (smr.enabled || mode == ActiveMode.All))
                        mesh = smr.sharedMesh;
                }

                if (mesh != null)
                {
                    var tris = meshCache[mesh];
                    total_tris += tris;
                    var treeN = map[current].treeN;
                    treeN.selfValue = treeN.totalValue = tris;
                    if (tris > 0)
                    {
                        var trans = current.transform;
                        while (trans.parent != null)
                        {
                            trans = trans.parent;
                            map[trans.gameObject].treeN.totalValue += tris;
                        }
                        rootModel.totalValue += tris;
                    }
                }
            }

            for (int i = 0; i < rootObjects.Length; i++)
            {

                GameObject current = (rootObjects[i]);
                pushAndBindGUI(current, (rootModel, rootGUI));

                do
                {
                    current = open.Pop();

                    doCalc(current);

                    for (int x = 0; x < current.transform.childCount; x++)
                    {
                        pushAndBindGUI(current.transform.GetChild(x).gameObject, map[current]);
                    }
                    yield return null;
                } while (open.Count > 0);
            }

            yield return null;
            progress_0_1 = 1;
        }
    }

    static class UnityUtils
    {
        static System.Text.StringBuilder strB = new System.Text.StringBuilder(1024);
        static Stack<string> stack = new Stack<string>(32);
        public static string GetGameObjectPath(GameObject go, int maxLength = 1024)
        {
            strB.Clear();
            stack.Clear();

            var trans = go.transform;
            int length = 0;
            do
            {
                var name = trans.name;
                int newSize = length + 1 + name.Length;
                if (newSize > maxLength)
                {
                    stack.Push("...");
                    break;
                }

                stack.Push(name);
                stack.Push("/");
                trans = trans.parent;
            } while (trans != null);

            while (stack.Count > 0)
            {
                strB.Append(stack.Pop());
            }
            return strB.ToString();
        }
    }
}