// Originally developed by Ardot66
// Modified and maintained by Jettcodey
// Licensed under the MIT License
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ardot.Jettcodey.REPO.MapUpgrade;

public static class Utils
{
    public static bool IsHost()
    {
        return SemiFunc.IsMasterClientOrSingleplayer();
    }

    public static void ForObjectsInTree(Transform root, Predicate<Transform> action)
    {
        var stack = new Stack<Transform>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Transform transform = stack.Pop();

            if (!action(transform))
                continue;

            for (int x = transform.childCount - 1; x >= 0; x--)
            {
                stack.Push(transform.GetChild(x));
            }
        }
    }
}
