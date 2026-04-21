using UnityEngine;
using System.Runtime.InteropServices;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [SerializeField] private string fallbackProjectId = "";

    public bool IsAuthenticated { get; private set; } = false;

    public string UserId { get; private set; } = "";

    public string DisplayName { get; private set; } = "Player";

    public string IdToken { get; private set; } = "";

    public string ProjectId { get; private set; } = "";


#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void InitFirebaseBridge();
    [DllImport("__Internal")] private static extern void SubmitScoreToFirestore(string jsonBody);
#else
    private static void InitFirebaseBridge() 
     => Debug.Log("Firebase bridge is only available in WebGL builds.");

    private static void SubmitScoreToFirestore(string jsonBody)
     => Debug.Log("SubmitScoreToFirestore Stub");
#endif


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitFirebaseBridge();
    }

    public void OnAuthReceived(string json)
    {
        Debug.Log($"Auth Received: {json} ");

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("Empty auth payload received.");
            ResetAuthState();
            return;
        }

        var data = JsonUtility.FromJson<AuthPayLoad>(json);
        if (data == null)
        {
            Debug.LogWarning("Invalid auth payload JSON.");
            ResetAuthState();
            return;
        }

        UserId = data.uid ?? "";
        IdToken = data.idToken ?? "";
        DisplayName = string.IsNullOrEmpty(data.displayName) ? "Player" : data.displayName;
        ProjectId = string.IsNullOrEmpty(data.projectId) ? fallbackProjectId : data.projectId;
        IsAuthenticated = !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(IdToken);

        Debug.Log($"User Authenticated as {DisplayName}, UID {UserId}");
    }

    public void SubmitScore(int score, int pipes, int duration)
    {
        if (!IsAuthenticated)
        {
            Debug.Log("Not authenticated. Score not submitted.");
            return;
        }

        var payload = new ScorePayload
        {
            score = score,
            pipes = pipes,
            duration = duration,
            projectId = ProjectId
        };

        var jsonBody = JsonUtility.ToJson(payload);
        SubmitScoreToFirestore(jsonBody);
    }

    private void ResetAuthState()
    {
        IsAuthenticated = false;
        UserId = "";
        IdToken = "";
        DisplayName = "Player";
        ProjectId = fallbackProjectId;
    }

    [System.Serializable]
    private class AuthPayLoad
    {
        public string uid;
        public string idToken;
        public string displayName;
        public string projectId;
    }


    [System.Serializable]
    private class ScorePayload
    {
        public int score;
        public int pipes;
        public int duration;
        public string projectId;

    }
}
