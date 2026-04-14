using UnityEngine;

/// <summary>
/// Place an instance at Resources/CloudBackendConfig (name must match) or assign via inspector on MainMenu.
/// Do not commit production keys in a public repo — use a local-only asset or gitignored copy.
/// </summary>
[CreateAssetMenu(fileName = "CloudBackendConfig", menuName = "CS4483/Cloud Backend Config")]
public class CloudBackendConfig : ScriptableObject
{
    [Tooltip("https://YOUR_PROJECT.supabase.co")]
    public string supabaseUrl = "";

    [Tooltip("Project API anon public key from Supabase dashboard.")]
    public string supabaseAnonKey = "";

    public bool useCloudSaves = true;

    public bool IsConfigured =>
        useCloudSaves &&
        !string.IsNullOrWhiteSpace(supabaseUrl) &&
        !string.IsNullOrWhiteSpace(supabaseAnonKey);
}
