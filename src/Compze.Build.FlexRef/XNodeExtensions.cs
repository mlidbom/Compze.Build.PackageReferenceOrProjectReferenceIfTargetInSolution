using System.Xml.Linq;

namespace Compze.Build.FlexRef;

static class XNodeCE
{
    public static void RemoveWithPrecedingComment(this XNode @this)
    {
        if(@this.PreviousNode is XComment)
            @this.PreviousNode.Remove();
        @this.Remove();
    }
}
