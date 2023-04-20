using System.Collections;
using System.Collections.Generic;
using System.IO;
using ZXthex.UnityProjectSniffer;
class FileSnifferItem : SnifferTypeItem
{
    protected override string title => "File Size | Assets";

    public override Int64Calc GetItemCalc()
    {
        return new FileSizeInAssetsCalc();
    }
}


class FileSizeInAssetsCalc : Int64Calc
{
    const string kUnityAssetsDir = "Assets";
    public FileSizeInAssetsCalc()
    {
        rootGUI.title = rootGUI.tooltip = kUnityAssetsDir;
    }
    protected override IEnumerator UpdateNumber()
    {
        var files = System.IO.Directory.GetFiles(kUnityAssetsDir, "*", System.IO.SearchOption.AllDirectories);
        Stack<string> temp = new Stack<string>(1024);
        yield return null;

        void AppendNode(string path, long fileSize)
        {
            var model = new TreeNodel();
            model.selfValue = model.totalValue = fileSize;
            var gui = new TreeNodeGUI();
            gui.title = Path.GetFileName(path);
            gui.tooltip = path;
            gui.model = model;

            //collect self and parent nodes
            {
                temp.Clear();
                do
                {
                    var name = Path.GetFileName(path);
                    temp.Push(name);
                    path = Path.GetDirectoryName(path);
                } while (path != kUnityAssetsDir && !string.IsNullOrEmpty(path));
            }

            //check create parene nodes
            {
                var current = rootGUI;
                TreeNodeGUI CheckChildDirCreated(string name)
                {
                    foreach (var child in current.children)
                    {
                        if (string.Compare(child.title, name, true) == 0)
                            return child;
                    }
                    var dir_path = Path.Combine(current.tooltip, name);
                    var dir_gui = new TreeNodeGUI();
                    dir_gui.title = name;
                    dir_gui.tooltip = dir_path;
                    dir_gui.model = new TreeNodel();
                    dir_gui.model.selfValue = dir_gui.model.totalValue = new FileInfo(dir_path + ".meta").Length;
                    current.children.Add(dir_gui);
                    return dir_gui;
                }

                current.model.totalValue += fileSize;
                while (temp.Count > 1)
                {
                    var name = temp.Pop();
                    current = CheckChildDirCreated(name);
                    current.model.totalValue += fileSize;
                }
                current.children.Add(gui);

            }
        }


        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            if (!file.EndsWith(".meta"))
            {
                long fileSize = new System.IO.FileInfo(file).Length + new System.IO.FileInfo(file+".meta").Length;
                AppendNode(file, fileSize);
            }
            progress_0_1 = (i + 1.0f) / files.Length;
            yield return null;
        }

        yield return null;
        progress_0_1 = 1;
    }
}