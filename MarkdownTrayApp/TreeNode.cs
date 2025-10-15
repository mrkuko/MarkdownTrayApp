using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownTrayApp
{
    public class TreeNode
    {
        public string DisplayText { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } =
            new ObservableCollection<TreeNode>();
        public bool? IsTask { get; set; }
        public string OriginalLine { get; set; }
    }
}
