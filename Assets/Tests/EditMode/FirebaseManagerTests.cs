using System.Security.Permissions;
using UnityEngine;
using NUnit.Framework;

[TestFixture]
public class FirebaseManagerTests
{
    private GameObject firebaseObj;
    private FirebaseManager firebaseManager;

    [SetUp]
    public void SetUp()
    { 
        firebaseObj = new GameObject("TestFirebaseManager");
        firebaseManager = firebaseObj.AddComponent<FirebaseManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(firebaseObj);

        ResetSingleton<FirebaseManager>("Instance");
    }

    private void ResetSingleton<T>(string propertyName) where T : class
    {
        var prop = typeof(T).GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static); 

                prop?.SetValue(null, null);
                
    }

    [Test]
    public void InitialState_IsNotAuthenticated()
    {
        Assert.IsFalse(firebaseManager.IsAuthenticated);
    }


    [Test]
    public void InitialDisplayName_IsPlayer()
    {
        Assert.AreEqual("Player", firebaseManager.DisplayName);
    }

    [Test]
    public void InitialUserId_IsEmpty()
    {
        Assert.AreEqual("", firebaseManager.UserId);
    }

    [Test]
    public void InitialIdToken_IsEmpty()
    {
        Assert.AreEqual("", firebaseManager.IdToken);
    }

    [Test]
    public void InitialProjectId_IsEmpty()
    {
        Assert.AreEqual("", firebaseManager.ProjectId);
    }

    
    [Test]
    public void OnAuthReceived_ValidPayload_SetsAuthenticated()
    {
        string json = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        firebaseManager.OnAuthReceived(json);
        Assert.AreEqual("1234", firebaseManager.UserId);
    }

    [Test]
    public void OnAuthReceived_SetsDisplayName()
    {
        string json = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        firebaseManager.OnAuthReceived(json);
        Assert.AreEqual("Sponder", firebaseManager.DisplayName);
    }

    [Test]
    public void OnAuthReceived_SetsIdToken()
    {
        string json = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        firebaseManager.OnAuthReceived(json);
        Assert.AreEqual("token_1234", firebaseManager.IdToken);
    }

    [Test]
    public void OnAuthReceived_SetsProjectId()
    {
        string json = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        firebaseManager.OnAuthReceived(json);
        Assert.AreEqual("beetle-ball", firebaseManager.ProjectId);
    }

    [Test]
    public void OnAuthReceived_EmptyToken_IsNotAuthenticated()
    {
        string json = BuildAuthJson("1234", "", "X", "proj");
        firebaseManager.OnAuthReceived(json);
        Assert.IsFalse(firebaseManager.IsAuthenticated);
    }

    [Test]
    public void OnAuthReceived_CalledTwice_OverwritesFirstAuth()
    {
        string json1 = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        string json2 = BuildAuthJson("4321", "token_4321", "Spencer", "bug-ball");

        firebaseManager.OnAuthReceived(json1);
        firebaseManager.OnAuthReceived(json2);

        Assert.AreEqual("4321", firebaseManager.UserId);
        Assert.AreEqual("Spencer", firebaseManager.DisplayName);
    }

    [Test]
    public void SubmitScore_WhenNotAuthenticated_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => firebaseManager.SubmitScore(10, 10, 30));
    }

    [Test]
    public void SubmitScore_WhenAuthenticated_DoesNotThrow()
    {
        string json = BuildAuthJson("1234", "token_1234", "Sponder", "beetle-ball");
        firebaseManager.OnAuthReceived(json);
        Assert.DoesNotThrow(() => firebaseManager.SubmitScore(10, 10, 30));
    }


    [Test]
    public void Singleton_IsSetAfterAwake()
    {
        Assert.AreEqual(firebaseManager, FirebaseManager.Instance);
    }



    private string BuildAuthJson(string uid, string idToken, string displayName, string projectId)
    {
        return $"{{\"uid\":\"{uid}\",\"idToken\":\"{idToken}\",\"displayName\":\"{displayName}\",\"projectId\":\"{projectId}\"}}";
    }
}
