using UnityEditor;

[InitializeOnLoad]
internal static class McpForUnityProjectConfiguration
{
    private const string StartupRewriteSessionKey = "MCPForUnity.StartupConfigRewrite.Ran";

    static McpForUnityProjectConfiguration()
    {
        // MCP clients are configured from the repository. Prevent MCPForUnity from
        // recreating global Claude/Codex entries during each Editor session.
        SessionState.SetBool(StartupRewriteSessionKey, true);
    }
}
