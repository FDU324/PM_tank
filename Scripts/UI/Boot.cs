﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class Boot : MonoBehaviour {
    public GameObject LoginWin;
    public GameObject LobbyWin;
    public Text loginInfo;
    

    Dictionary<string, string> launchArgs = new Dictionary<string, string>();

    // Use this for initialization
    void Start () {
        string username = null;
        string nickname = null;

        #if UNITY_STANDALONE
            string[] CommandLineArgs = Environment.GetCommandLineArgs();       
            username = getArgFromCMD(CommandLineArgs, "--username");
            nickname = getArgFromCMD(CommandLineArgs, "--nickname");          
        #endif
        #if UNITY_ANDROID
        using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    username = jo.Call<string>("getArg", "username");
                    nickname = jo.Call<string>("getArg", "nickname");
                    loginInfo.text = username+","+nickname;
                }
            }
        #endif
        
        
        if (username != null && nickname != null) {
            PlayFabUserData.username = username;
            //连接Photon服务器
            PhotonNetwork.ConnectUsingSettings("1.0");
            //PhotonNetwork.automaticallySyncScene = false;
            PhotonNetwork.autoCleanUpPlayerObjects = false;   //玩家离开时不清除游戏对象
            PhotonNetwork.player.NickName = nickname;          //设置玩家昵称
            Debug.Log(PhotonNetwork.connectionStateDetailed.ToString());
            LoginWin.SetActive(false);
            LobbyWin.SetActive(true);
        }
        else
        {
            LoginWin.SetActive(true);
            LobbyWin.SetActive(false);
        }
        
    }

    private string getArgFromCMD(string[] CommandLineArgs, string arg) {
        for (int i = 0; i < CommandLineArgs.Length; i++)
        {
            if (CommandLineArgs[i] == arg && i != CommandLineArgs.Length - 1)
                return CommandLineArgs[i + 1];
        }
        return null;
    }

   
}
