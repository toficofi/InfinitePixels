// C#
// ClipboardHelper.cs
using UnityEngine;

public class ClipboardHelper
{
    public static string clipBoard
    {
        get
        {
            return GUIUtility.systemCopyBuffer;
        }
        set
        {
            GUIUtility.systemCopyBuffer = value;
        }
    }
}
