using System;
using UnityEngine;

namespace SRXDCustomLeaderboard;

public class Player_Custom : UnityBaseProvider, IProvider, IServiceWithStates, IService
{
    public bool RequiresDataStore
    {
        get
        {
            return false;
        }
    }
    
    public int Priority
    {
        get
        {
            return 2;
        }
    }
    public string _serviceName { get; set; }
    
    private string displayName;
    private string customId;

    private bool _initialized;
    
    public bool InitService()
    {
        return true;
    }

    public void UpdateService()
    {
    }

    public void ShutdownService()
    {
    }
    
    public string GetDisplayName()
    {
        return this.displayName;
    }

    public bool IsSignedIn<T>() where T : class, IService
    {
        return this is T && this._initialized;
    }

    public void Login(LoginCallback callback)
    {
        PSMLoginResult psmloginResult = default(PSMLoginResult);
        callback(psmloginResult);
    }

    public void Logout()
    {
    }

    public void SetDisplayName(string displayName)
    {
    }
}