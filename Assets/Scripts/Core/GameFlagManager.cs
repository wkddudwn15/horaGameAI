using System.Collections.Generic;
using UnityEngine;

public class GameFlagManager : MonoBehaviour
{
    private readonly HashSet<string> flags = new HashSet<string>();

    public void SetFlag(string flagId)
    {
        if (!string.IsNullOrWhiteSpace(flagId))
        {
            flags.Add(flagId);
        }
    }

    public void ClearFlag(string flagId)
    {
        if (!string.IsNullOrWhiteSpace(flagId))
        {
            flags.Remove(flagId);
        }
    }

    public bool HasFlag(string flagId)
    {
        return !string.IsNullOrWhiteSpace(flagId) && flags.Contains(flagId);
    }
}
