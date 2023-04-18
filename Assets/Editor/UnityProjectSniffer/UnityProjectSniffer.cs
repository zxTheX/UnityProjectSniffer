#define FLOAT32
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if FLOAT32
using Number = System.Single;
#else
using Number = System.Double;
#endif

namespace ZXthex.UnityProjectSniffer
{
    using System.Linq;
    using ZXthex.Math;

    public class SnifferEditorWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/Sniffer %#z")]
        static void CreateWindow()
        {
            var window = CreateInstance<SnifferEditorWindow>();

            window.titleContent = new GUIContent("Sniffer");
            window.startView = true;
            window.snifferItems = new SnifferTypeItem[] {
                                    new MeshTrianglesSnifferItem(),
                                    new FileSnifferItem(),
                                };

            window.Init();
            window.Show();
        }
        GUIContent[] popupContents;
        Int64Calc calc;
        TreeNodeGUI root;

        void Init()
        {
            popupContents = (from item in snifferItems select item.CreateGUIContent()).ToArray();
            CheckStyles();
        }

        #region Styles

        //TODO: provide ChangeSize option
        public static int titleSize = 12;

        static void CheckStyles()
        {
            if (outside_inactive?.normal.background == null)
            {
                InitStyles();
                outside_active.fontSize = outside_inactive.fontSize = mix_active.fontSize = mix_inactive.fontSize = titleSize;
            }
        }

        static void InitStyles()
        {
            //Base
            outside_active = new GUIStyle("button");
            outside_active.normal.textColor = outside_active.hover.textColor = outside_active.active.textColor = Color.black;
            outside_inactive = new GUIStyle(outside_active);
            mix_active = new GUIStyle(outside_active);
            mix_inactive = new GUIStyle(outside_active);
            mix_inside = new GUIStyle(outside_active);
            inside = new GUIStyle(outside_active);
            inside_last = new GUIStyle(outside_active);

            //Alignment
            outside_active.alignment = outside_inactive.alignment =
                mix_active.alignment = mix_inactive.alignment = TextAnchor.UpperLeft;
            inside.alignment = inside_last.alignment =
                mix_inside.alignment = TextAnchor.MiddleCenter;

            var normal = CreateBoxTexture2D(new Color(0.75f, 0.61f, 0.45f, 1.0f));
            var mix = CreateBoxTexture2D(new Color(0.63f, 0.85f, 0.64f, 1.0f));
            var normal_last = CreateBoxTexture2D(new Color(0.5f, 0.67f, 0.84f, 1.0f));


            var hover = CreateBoxTexture2D(new Color(0.9f, 0.732f, 0.54f, 1.0f));
            var hover_mix = CreateBoxTexture2D(new Color(0.75f, 1.0f, 0.77f, 1.0f));
            var hover_last = CreateBoxTexture2D(new Color(0.6f, 0.804f, 1.0f, 1.0f));

            var press = CreateBoxTexture2D(new Color(0.96f, 0.96f, 0.96f, 1.0f));
            outside_inactive.normal.background = normal;
            outside_inactive.hover.background = normal;
            outside_inactive.active.background = normal;
            mix_inactive.normal.background = mix;
            mix_inactive.hover.background = mix;
            mix_inactive.active.background = mix;

            outside_active.normal.background = inside.normal.background = normal;
            outside_active.hover.background = inside.hover.background = hover;
            outside_active.active.background = inside.active.background =
                inside_last.active.background = mix_active.active.background =
                    mix_inside.active.background = press;
            inside_last.normal.background = normal_last;
            inside_last.hover.background = hover_last;
            mix_active.normal.background = mix_inside.normal.background = mix;
            mix_active.hover.background = mix_inside.hover.background = hover_mix;

            outside_active.padding = outside_inactive.padding =
                mix_active.padding = mix_inactive.padding = new RectOffset(6, 6, 6, 6);


            progress_label_style = new GUIStyle();
            progress_label_style.alignment = TextAnchor.MiddleCenter;
            progress_label_style.normal.background = CreateColorTexture2D(Color.clear);
            progress_bg_style = new GUIStyle();
            progress_bg_style.normal.background = CreateColorTexture2D(Color.grey);
            progress_front_style = new GUIStyle();
            progress_front_style.normal.background = CreateColorTexture2D(new Color(0.2f, 0.5f, 0.6f, 1.0f));
        }
        static GUIStyle progress_label_style, progress_bg_style, progress_front_style;

        static Texture2D CreateColorTexture2D(Color c)
        {
            var t2d = new Texture2D(1, 1);
            t2d.filterMode = FilterMode.Point;
            for (int i = 0; i < t2d.width; i++)
                for (int j = 0; j < t2d.height; j++)
                    t2d.SetPixel(i, j, c);
            t2d.Apply();
            return t2d;
        }

        static Texture2D CreateBoxTexture2D(Color c)
        {
            var t2d = new Texture2D(8, 8);
            t2d.filterMode = FilterMode.Point;
            for (int i = 0; i < t2d.width; i++)
                for (int j = 0; j < t2d.height; j++)
                    if (i == 0 || j == 0 || i == t2d.width - 1 || j == t2d.height - 1)
                        t2d.SetPixel(i, j, Color.black);
                    else
                        t2d.SetPixel(i, j, c);
            t2d.Apply();
            return t2d;
        }

        static GUIStyle outside_active, outside_inactive, mix_active, mix_inactive, mix_inside, inside, inside_last;
        #endregion

        bool startView;

        int popIndex = 0;

        SnifferTypeItem[] snifferItems;

        private void OnGUI()
        {
            CheckStyles();

            if (startView)
            {
                OnGUI_StartView(base.position);
            }
            else
            {
                OnGUI_Running(base.position);
            }

        }

        void OnGUI_StartView(Rect wholeR)
        {
            var popR = new Rect(wholeR.width * 0.3f, wholeR.height * 0.3f, wholeR.width * 0.4f, 36f);
            popIndex = EditorGUI.Popup(popR, popIndex, popupContents);

            var buttonR = new Rect(wholeR.width * 0.3f, wholeR.height * 0.5f, wholeR.width * 0.4f, 36f);
            if (GUI.Button(buttonR, L10n.Tr("Start")))
            {
                startView = false;
                calc = snifferItems[popIndex].GetItemCalc();
                root = calc.rootGUI;
            }
        }

        float progress_100;

        void OnGUI_Running(Rect wholeR)
        {
            var rect = EditorGUILayout.GetControlRect();

            // Draw progress bar
            {
                var label_rect = rect;
                EditorGUI.LabelField(label_rect, " ", progress_bg_style);
                var prog_rect = label_rect;
                prog_rect.width = (float)(progress_100 * rect.width / 100);
                EditorGUI.LabelField(prog_rect, " ", progress_front_style);
                EditorGUI.LabelField(label_rect, progress_100.ToString("N2") + "%", progress_label_style);
            }

            wholeR = new Rect(rect.x, rect.y + rect.height, rect.width, wholeR.height - rect.y - rect.height);

            if (progress_100 < 100)
            {
                calc.Update();
                progress_100 = 100 * calc.progress_0_1;
            }

            {
                calc.SquarifyGUI(wholeR);
                UpdateNodeHover(root);
                ShowNode(root);
            }

            //TODO: Decrease repaint calls
            if (progress_100 < 100 || this.position.Contains(Event.current.mousePosition + this.position.position))
                this.Repaint();
        }

        private bool UpdateNodeHover(TreeNodeGUI node)
        {
            bool cursorInChildren = false;
            if (node.expanded && node.children.Count > 0)
                for (int i = 0; i < node.children.Count; i++)
                {
                    if (UpdateNodeHover(node.children[i]))
                    {
                        cursorInChildren = true;
                        break;
                    }
                }
            node.curosrInChildren = cursorInChildren;
            cursorInChildren = node.position.Contains(Event.current.mousePosition) || node.curosrInChildren;

            return cursorInChildren;
        }

        private void ShowNode(TreeNodeGUI node)
        {
            var thisR = node.position;

            if (thisR.size.magnitude == 0)
                return;

            void locateGameObject(TreeNodeGUI tng)
            {
                if (tng.model.referenceObject != null)
                {
                    Selection.activeObject = tng.model.referenceObject;
                    EditorGUIUtility.PingObject(tng.model.referenceObject);
                }
            }

            if (node.expanded && node.children.Count > 0)
            {
                var split = string.IsNullOrEmpty(node.title) || string.IsNullOrEmpty(node.text) ? "" : " - ";
                var content = new GUIContent(node.title + split + node.text, node.tooltip);
                if (node.curosrInChildren)
                {
                    content.tooltip = null;
                    GUI.Box(thisR.ToRect(), content, node.model.selfValue == 0 ? outside_inactive : mix_inactive);
                }
                else if (GUI.Button(thisR.ToRect(), content, node.model.selfValue == 0 ? outside_active : mix_active))
                {
                    if (Event.current.button == 0)
                        foreach (var child in node.children)
                        {
                            child.expanded = false;
                            child.CloseChildrenExpand();
                        }
                    else if (Event.current.button == 1)
                    {
                        locateGameObject(node);
                    }
                }
                foreach (var child in node.children)
                {
                    ShowNode(child);
                }
            }
            else
            {
                var content = new GUIContent(node.title + "\n" + node.text, node.tooltip);
                if (node.children.Count > 0)
                {
                    if (GUI.Button(thisR.ToRect(), content, node.model.selfValue == 0 ? inside : mix_inside))
                    {
                        if (Event.current.button == 0)
                            node.expanded = true;
                        else if (Event.current.button == 1)
                        {
                            locateGameObject(node);
                        }
                    }
                }
                else if (GUI.Button(thisR.ToRect(), content, inside_last))
                {
                    locateGameObject(node);
                }
            }
        }
    }

    internal abstract class SnifferTypeItem
    {
        public GUIContent CreateGUIContent()
        {
            return new GUIContent(L10n.Tr(title));
        }

        protected abstract string title { get; }

        public abstract void DrawItemGUI();

        public abstract Int64Calc GetItemCalc();
    }

    internal abstract class Int64Calc
    {
        const double kFPS = 60.0;

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        protected TreeNodel rootModel => rootGUI.model;
        public TreeNodeGUI rootGUI { get; private set; } = new TreeNodeGUI { expanded = true, model = new TreeNodel() };
        public float progress_0_1 { get; protected set; } = 0.0f;

        IEnumerator updateObject;
        protected abstract IEnumerator UpdateNumber();
        public void Update()
        {
            watch.Reset();
            watch.Start();
            if (updateObject == null)
                updateObject = UpdateNumber();


            while (watch.Elapsed.TotalMilliseconds < 1.0 / kFPS)
                updateObject.MoveNext();

            watch.Stop();
        }

        //TODO: Seperate GUI and calcs
        Queue<TreeNodeGUI> queue = new Queue<TreeNodeGUI>(1024 * 1024);
        List<int> insideList = new List<int>(8);
        List<RectX> outsideOptionRs = new List<RectX>(8);
        List<RectX> insideOptionRs = new List<RectX>(8);

        public void SquarifyGUI(Rect size)
        {
            queue.Clear();

            rootGUI.localPosition = new RectX(0, 0, 1, 1);
            rootGUI.UpdateSelfPosition(new RectX(size.position, size.size), new RectOffset());

            queue.Enqueue(rootGUI);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                current.text = current.model.totalValue.ToString("N0");
                if (current.model.selfValue > 0)
                    current.text += " (" + L10n.Tr("Self") + ": " + current.model.selfValue.ToString("N0") + ")";

                if (!current.expanded)
                    continue;

                long total = current.model.totalValue;
                long self = current.model.selfValue;

                if (total == 0)
                    continue;

                Number child_height_offset_local = (Number)self / total;

                if (child_height_offset_local > 0.1)
                {
                    new object();
                }

                //TODO: convert offset(float) to offset(number)
                var offset = new RectOffset(4, 4, SnifferEditorWindow.titleSize * 2 + 2, 4);
                RectX areaR = current.position;
                areaR = new RectX(areaR.x + offset.left, areaR.y + offset.top,
                                    areaR.width - offset.horizontal, areaR.height - offset.vertical);
                areaR = TreeNodeGUI.FromLocalToGlobal(areaR,
                        new RectX(0, child_height_offset_local, 1, 1 - child_height_offset_local));

                Number AreaPerNumber = areaR.width * areaR.height / (total - self);
                RectX outsideR = areaR;
                RectX insideR = new RectX();
                insideList.Clear();

                current.children.Sort((x, y) => y.model.totalValue.CompareTo(x.model.totalValue));

                bool number_equal(Number a, Number b) => System.Math.Round((a + (Number)1) / (b + (Number)1) * 10000) == 10000;

                Number calcAverageRatio(IEnumerable<RectX> optionRs)
                {
                    Number avg = (Number)0;
                    int count = 0;
                    foreach (var rect in optionRs)
                    {
                        if (rect.height * rect.width <= 0)
                            break;
                        Number ratio = System.Math.Max(rect.width / rect.height, rect.height / rect.width);
                        avg += ratio;
                        count++;
                    }
                    return avg / count;
                }

                void squarifyMethod()
                {
                    for (int i = 0; i < current.children.Count; i++)
                    {
                        var child = current.children[i];

                        Number childArea = child.model.totalValue * AreaPerNumber;

                        if (childArea == 0)
                            break;


                        Number ratio_outside, ratio_inside;
                        ratio_outside = ratio_inside = Number.MaxValue;

                        //if (outsideR.size.sqrMagnitude > 0)
                        {
                            //outside way
                            RectX optionOut;
                            outsideOptionRs.Clear();
                            foreach (var id in insideList)
                            {
                                outsideOptionRs.Add(current.children[id].position);
                            }
                            if (outsideR.width > outsideR.height)
                            {
                                optionOut = new RectX(outsideR.x, outsideR.y, childArea / outsideR.height, outsideR.height);
                            }
                            else
                            {
                                optionOut = new RectX(outsideR.x, outsideR.y, outsideR.width, childArea / outsideR.width);
                            }

                            ratio_outside = insideList.Count == 0 ? 1 : calcAverageRatio(outsideOptionRs);
                            outsideOptionRs.Add(optionOut);
                        }

                        if (insideR.size.sqrMagnitude > 0)
                        {
                            //inside way
                            insideOptionRs.Clear();
                            bool outside_is_down = insideR.x == outsideR.x;
                            Number v_sum = (Number)0;
                            foreach (var id in insideList)
                            {
                                var v = current.children[id].model.totalValue;
                                v_sum += v;
                            }
                            Number area_scale = (Number)(v_sum + child.model.totalValue) / v_sum;
                            Vector2X pivot = current.children[insideList[0]].position.position;
                            RectX childR;
                            if (outside_is_down) //add child on right side
                            {
                                foreach (var id in insideList)
                                {
                                    var r = current.children[id].position;
                                    r = new RectX(
                                        pivot.x + (r.position.x - pivot.x) / area_scale,
                                        pivot.y + (r.position.y - pivot.y) * area_scale,
                                        r.width / area_scale,
                                        r.height * area_scale
                                        );
                                    insideOptionRs.Add(r);
                                }
                                var lastInsideR = insideOptionRs[insideOptionRs.Count - 1];
                                childR = new RectX(lastInsideR.x + lastInsideR.width, lastInsideR.y, childArea / lastInsideR.height, lastInsideR.height);
                            }
                            else //add child on down side
                            {
                                foreach (var id in insideList)
                                {
                                    var r = current.children[id].position;
                                    r = new RectX(
                                        pivot.x + (r.position.x - pivot.x) * area_scale,
                                        pivot.y + (r.position.y - pivot.y) / area_scale,
                                        r.width * area_scale,
                                        r.height / area_scale
                                        );
                                    insideOptionRs.Add(r);
                                }
                                var lastInsideR = insideOptionRs[insideOptionRs.Count - 1];
                                childR = new RectX(lastInsideR.x, lastInsideR.y + lastInsideR.height, lastInsideR.width, childArea / lastInsideR.width);
                            }
                            insideOptionRs.Add(childR);
                            ratio_inside = calcAverageRatio(insideOptionRs);

                        }



                        if (ratio_outside < ratio_inside) //outside option Win
                        {
                            insideList.Clear();
                            insideList.Add(i);

                            var newInsideR = outsideOptionRs[outsideOptionRs.Count - 1];
                            outsideR = number_equal(outsideR.xMax, newInsideR.xMax)
                                        ? new RectX(outsideR.x, outsideR.y + newInsideR.height, outsideR.width, outsideR.height - newInsideR.height)
                                        : new RectX(outsideR.x + newInsideR.width, outsideR.y, outsideR.width - newInsideR.width, outsideR.height);

                            insideR = newInsideR;

                            var targetPos = TreeNodeGUI.FromGlobalToLocal(current.position, insideR);
                            child.localPosition = targetPos;

                        }
                        else        //inside option win
                        {
                            var first = insideOptionRs[0];
                            var last = insideOptionRs[insideOptionRs.Count - 1];
                            var newSize = new Vector2X(last.xMax - first.xMin, last.yMax - first.yMin);
                            var sizeDelta = newSize - insideR.size;
                            insideR = new RectX(insideR.position, newSize);
                            outsideR = number_equal(first.x, last.x)
                                        ? new RectX(outsideR.x + sizeDelta.x, outsideR.y, outsideR.width - sizeDelta.x, outsideR.height)
                                        : new RectX(outsideR.x, outsideR.y + sizeDelta.y, outsideR.width, outsideR.height - sizeDelta.y);
                            for (int x = 0; x < insideList.Count; x++)
                            {
                                var id = insideList[x];
                                current.children[id].localPosition = TreeNodeGUI.FromGlobalToLocal(current.position, insideOptionRs[x]);
                                current.children[id].UpdateSelfPosition(current.position, new RectOffset());
                            }
                            insideList.Add(i);
                            child.localPosition = TreeNodeGUI.FromGlobalToLocal(current.position, insideOptionRs[insideOptionRs.Count - 1]);
                        }
                        child.UpdateSelfPosition(current.position, new RectOffset());

                    }
                }

                squarifyMethod();

                for (int i = 0; i < current.children.Count; i++)
                {
                    var child = current.children[i];
                    Number childArea = child.model.totalValue * AreaPerNumber;
                    if (childArea == 0)
                        break;
                    queue.Enqueue(child);
                }

            }
        }

    }



    internal class TreeNodeGUI : TreeNode<TreeNodeGUI>
    {
        public RectX localPosition { private get; set; }
        public RectX position { get; private set; }
        public void UpdateSelfPosition(RectX parentR, RectOffset offset)
        {
            parentR.x += offset.left;
            parentR.y += offset.top;
            parentR.xMax -= offset.right;
            parentR.yMax -= offset.bottom;
            position = FromLocalToGlobal(parentR, localPosition);
        }

        public static RectX FromLocalToGlobal(RectX parentR, RectX localPosition)
        {
            return new RectX(parentR.x + localPosition.x * parentR.width,
                            parentR.y + localPosition.y * parentR.height,
                            parentR.width * localPosition.width,
                            parentR.height * localPosition.height);
        }

        public static RectX FromGlobalToLocal(RectX parentR, RectX position)
        {
            return new RectX((position.x - parentR.x) / parentR.width,
                            (position.y - parentR.y) / parentR.height,
                            position.width / parentR.width,
                            position.height / parentR.height);
        }

        public string title;
        public string text;
        public string tooltip;
        public bool expanded;
        public void CloseChildrenExpand()
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].expanded = false;
                children[i].CloseChildrenExpand();
            }
        }
        public bool curosrInChildren;
        public TreeNodel model;
    }

    internal class TreeNodel : TreeNode<TreeNodel>
    {
        public long selfValue;
        public long totalValue;
        public Object referenceObject;
    }

    internal class TreeNode<T> where T : TreeNode<T>
    {
        const int kDefaultNodeCapacity = 8;
        public List<T> children = new List<T>(kDefaultNodeCapacity);
    }
}

static class L10n
{
    static Dictionary<SystemLanguage, Dictionary<string, string>> database = new Dictionary<SystemLanguage, Dictionary<string, string>>
    {
        [SystemLanguage.English] = new Dictionary<string, string>
        {
            ["(Meshes referenced by MeshFilter and SkinnedMeshRenderer).triangles.Length | Scene"] = "(Meshes referenced by MeshFilter and SkinnedMeshRenderer).triangles.Length | Scene",
            ["File Size | Assets"] = "File Size | Assets",
            ["Start"] = "Start",
            ["Self"] = "Self",
        },
        [SystemLanguage.ChineseSimplified] = new Dictionary<string, string>
        {
            ["(Meshes referenced by MeshFilter and SkinnedMeshRenderer).triangles.Length | Scene"] = "三角形数 （MeshFilter和SkinnedMeshRenderer） | 场景",
            ["File Size | Assets"] = "文件大小 | 资产",
            ["Start"] = "开始",
            ["Self"] = "自身",
        },
        [SystemLanguage.ChineseTraditional] = new Dictionary<string, string>
        {
            ["(Meshes referenced by MeshFilter and SkinnedMeshRenderer).triangles.Length | Scene"] = "三角形數 (MeshFilter and SkinnedMeshRenderer) | 場景",
            ["File Size | Assets"] = "文件大小 | 資產",
            ["Start"] = "開始",
            ["Self"] = "自身",
        },

    };
    public static string Tr(string text)
    {
        var lang = currentEditorLanguage;
        //lang = SystemLanguage.ChineseTraditional;
        if (database.ContainsKey(lang) && database[lang].ContainsKey(text))
        {
            return database[lang][text];
        }
        return UnityEditor.L10n.Tr(text);
    }

    public static SystemLanguage currentEditorLanguage
    {
        get
        {
            var assem = typeof(UnityEditor.Editor).Assembly;
            var t = assem.GetType("UnityEditor.LocalizationDatabase");
            var pi = t.GetProperty("currentEditorLanguage");
            return (SystemLanguage)pi.GetValue(null);
        }
    }
}



namespace ZXthex.Math
{
    struct RectX
    {
        public Number xMin;
        public Number yMin;
        public Number xMax;
        public Number yMax;
        public Number x
        {
            get => xMin;
            set => xMin = value;
        }
        public Number y
        {
            get => yMin;
            set => yMin = value;
        }
        public Number width
        {
            get => xMax - xMin;
        }
        public Number height => yMax - yMin;
        public Vector2X position => new Vector2X(x, y);

        public RectX(Number x, Number y, Number width, Number height)
        {
            this.xMin = x;
            this.yMin = y;
            this.xMax = x + width;
            this.yMax = y + height;
        }

        public RectX(Vector2 position, Vector2 size)
            : this(position.x, position.y, size.x, size.y)
        {
        }

        public RectX(Vector2X position, Vector2X size)
            : this(position.x, position.y, size.x, size.y)
        {
        }

        public Vector2X size => new Vector2X(width, height);

        public bool Contains(Vector2 point)
            => point.x >= xMin && point.x < xMax && point.y >= yMin && point.y < yMax;

        public bool Contains(Vector2X point)
            => point.x >= xMin && point.x < xMax && point.y >= yMin && point.y < yMax;

        public Rect ToRect()
            => new Rect((int)xMin, (int)yMin, (int)xMax + 1 - (int)xMin, (int)yMax + 1 - (int)yMin);

        public override string ToString()
            => $"(x:{x}, y:{y}, width:{width}, height:{height}) [xMax: {xMax}]";
    }

    struct Vector2X
    {
        public Number x, y;
        public Vector2X(Number x, Number y)
        {
            this.x = x;
            this.y = y;
        }

        public Number sqrMagnitude => x * x + y * y;
        public Number magnitude => (Number)System.Math.Sqrt(sqrMagnitude);

        public static Vector2X operator +(Vector2X a, Vector2X b)
            => new Vector2X(a.x + b.x, a.y + b.y);

        public static Vector2X operator -(Vector2X a, Vector2X b)
            => new Vector2X(a.x - b.x, a.y - b.y);
    }
}
