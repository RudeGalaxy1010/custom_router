namespace custom_router.logic.impl;

public static class PathNormalization
{
    public static string Normalize(string path) => path.Length > 1 && path.EndsWith('/')
        ? path[..^1]
        : path;
}