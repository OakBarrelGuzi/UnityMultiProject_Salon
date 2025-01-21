using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DisplayNameUtils
{
    public static string ToDisplayFormat(string serverName)
    {
        if (string.IsNullOrEmpty(serverName)) return string.Empty;
        return serverName.Replace("_", "#");
    }

    public static string ToServerFormat(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return string.Empty;
        return displayName.Replace("#", "_");
    }
    public static string RemoveTag(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return string.Empty;
        return displayName.Split('_')[0];
    }

    public static bool IsValidDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return false;

        int hashCount = displayName.Split('#').Length - 1;
        if (hashCount != 1) return false;

        int hashIndex = displayName.IndexOf('#');
        if (hashIndex <= 0 || hashIndex >= displayName.Length - 1) return false;

        return true;
    }

    public static string GenerateDisplayName(string baseName, string tag)
    {
        return $"{baseName}#{tag}";
    }
}