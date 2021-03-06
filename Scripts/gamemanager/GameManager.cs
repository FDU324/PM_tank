﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using UnityEngine.SceneManagement;

public enum StateType { Prepare, Playing, Dead, End, Null};

public class GameManager : PunBehaviour
{
    private static GameManager gm;
    public UIController UICtrl;

    public const float photonCircleTime = 4294967.295f;
    
    
    [HideInInspector]
    public Dictionary<StateType, BaseState> stateMap;
    //[HideInInspector]
    //public Queue<StateType> stateQueue;
    public StateType curStateType;
    public BaseState curState;

    //public Camera mainCamera;
    [HideInInspector]
    public double startTimer = 0;
    [HideInInspector]
    public double endTimer = 0;
    [HideInInspector]
    public double countDown = 0;
    [HideInInspector]
    public bool timeCountDown = false;
    public double playingDuration = 600.0;
    public float gameOverTime = 10.0f;              //游戏结束时间
    public double checkPlayerTime = 5;
    [HideInInspector]
    public int loadedPlayerNum;
    [HideInInspector]
    public int readyPlayerNum;

    [HideInInspector]
    public bool waitingForInit = true;

    [HideInInspector]
    public bool allReady = false;

    [HideInInspector]
    public GameObject localTank;
    [HideInInspector]
    public TankModel tankModel;
    [HideInInspector]
    public HashSet<string> dead_tank_list = new HashSet<string>();
    [HideInInspector]
    public List<GameObject> tank_list = new List<GameObject>();
    [HideInInspector]
    public int livedTankCount;

    ExitGames.Client.Photon.Hashtable playerCustomProperties;

    private Vector3[] locations = {
        new Vector3 (758, -17.7f, 749.7f),
        new Vector3 (830.8f, -23.1f, 865.3f),
        new Vector3 (1198, -11.5f, 1274),
        new Vector3 (846, -21, 1361)
    };
    // Use this for initialization
    void Start()
    {
        gm = GetComponent<GameManager>();
        UICtrl = GetComponent<UIController>();
        UICtrl.loadingCanvas.SetActive(true);

        countDown = playingDuration;
        //UICtrl = GetComponent<UIController>();

        livedTankCount = PhotonNetwork.playerList.Length / 2;
        stateMap = new Dictionary<StateType, BaseState>();
        //stateQueue = new Queue<StateType>();
        InitStates();
        curStateType = StateType.Prepare;
        //curStateType = stateQueue.Peek();
        curState = stateMap[curStateType];
        curState.Enter();

        //mainCamera = Camera.main

    }

    // Update is called once per frame
    void Update()
    {

        if(curState != null)
        {
            
            if(curState.checkEndCondition())
            {
                StateChange();
            }
            curState.Update();
        }
        
    }

    public static GameManager GetGMInstance()
    {
        return gm;
    }

    private void InitStates() {
        stateMap.Add(StateType.Prepare, new PrepareState(this));
        //stateQueue.Enqueue(StateType.Prepare);
        stateMap.Add(StateType.Playing, new PlayingState(this));
        //stateQueue.Enqueue(StateType.Playing);
        stateMap.Add(StateType.Dead, new DeadState(this));
        //stateQueue.Enqueue(StateType.Dead);
        stateMap.Add(StateType.End, new EndState(this));
        //stateQueue.Enqueue(StateType.End);
    }

    void StateChange() {

        if (curState != null)
            curState.Exit();
        if(curStateType == StateType.End)
        {
            return;
        }
        switch (curStateType)
        {
            case StateType.Prepare:
                curStateType = StateType.Playing;
                break;
            case StateType.Playing:
                if (countDown == 0 || livedTankCount <= 1)
                    curStateType = StateType.End;
                else
                    curStateType = StateType.Dead;
                break;
            case StateType.Dead:
                curStateType = StateType.End;
                break;

        }
        curState = stateMap[curStateType];
        if(curState != null)
            curState.Enter();

        
        /*
        if(stateQueue.Count > 0)
        {
            curStateType = stateQueue.Peek();
            curState = stateMap[curStateType];
            curState.Enter();
        }
        */
    }
    //
    [PunRPC]
    public void ConfirmLoaded()
    {
        loadedPlayerNum++;
    }
    //
    [PunRPC]
    public void ConfirmReady()
    {
        readyPlayerNum++;
    }

    
    public override void OnPhotonPlayerDisconnected(PhotonPlayer other)
    {
        
    }
   
    //生成玩家对象
    public void InstantiateTanks()
    {
        int tankID = 0;
        //playerCustomProperties= PhotonNetwork.player.CustomProperties;	//获取玩家自定义属性
        ExitGames.Client.Photon.Hashtable CustomProperties;
        foreach (PhotonPlayer p in PhotonNetwork.playerList)
        {       //遍历房间内所有玩家
            if (p.CustomProperties["Character"].ToString() == "driver")
            {   //如果有人未准备
                GameObject tankOBJ = PhotonNetwork.Instantiate("tank", locations[tankID++], Quaternion.identity, 0);
                tankOBJ.GetPhotonView().TransferOwnership(p);
                CustomProperties = new ExitGames.Client.Photon.Hashtable() {	//初始化玩家自定义属性
					{ "Team",p.CustomProperties["Team"].ToString() },		//玩家队伍
					{"ID",tankOBJ.GetPhotonView().viewID},
                    { "Character", p.CustomProperties["Character"].ToString()},
                    { "TeamNum",(int)p.CustomProperties["TeamNum"] }		//玩家队伍序号
				};
                p.SetCustomProperties(CustomProperties);
                foreach (PhotonPlayer p1 in PhotonNetwork.playerList)
                {       //遍历房间内所有玩家
                    if (p1.CustomProperties["Team"].ToString() == p.CustomProperties["Team"].ToString() && p1.CustomProperties["Character"].ToString() == "shooter")
                    {
                        CustomProperties = new ExitGames.Client.Photon.Hashtable() {	//初始化玩家自定义属性
							{ "Team",p1.CustomProperties["Team"].ToString() },		//玩家队伍
							{"ID",tankOBJ.GetPhotonView().viewID},
                            { "Character", p1.CustomProperties["Character"].ToString()},
                            { "TeamNum",(int)p1.CustomProperties["TeamNum"] }		//玩家队伍序号
						};
                        p1.SetCustomProperties(CustomProperties);
                        tankOBJ.transform.Find("tower").gameObject.GetPhotonView().TransferOwnership(p1);
                        tankOBJ.transform.Find("tower/cannon").gameObject.GetPhotonView().TransferOwnership(p1);
                    }
                }
            }
        }

    }


    [PunRPC]
    public void BindPlayer2Tank()
    {


        foreach (GameObject tankOBJ in GameObject.FindGameObjectsWithTag("Player"))
        {
            tank_list.Add(tankOBJ);
            foreach (PhotonPlayer p in PhotonNetwork.playerList)
            {
                print((int)p.CustomProperties["ID"] + " :" + tankOBJ.GetPhotonView().viewID);
                if ((int)p.CustomProperties["ID"] == tankOBJ.GetPhotonView().viewID)
                {
                    tankOBJ.name = p.CustomProperties["Team"].ToString();
                    break;
                }
            }
            if ((int)PhotonNetwork.player.CustomProperties["ID"] == tankOBJ.GetPhotonView().viewID)
            {
                localTank = tankOBJ;
                tankModel = localTank.GetComponent<TankModel>();
                if (PhotonNetwork.player.CustomProperties["Character"].ToString() == "driver")
                {
                    UICtrl.EnableDriverCanvas();
                    UICtrl.DisableShooterCanvas();
                    
                    tankModel.driverCamera.SetActive(true);
                    localTank.GetComponent<DriverController>().enabled = true;
                    localTank.GetComponent<ShooterController>().enabled = false;

                }
                else if (PhotonNetwork.player.CustomProperties["Character"].ToString() == "shooter")
                {
                    UICtrl.DisableDriverCanvas();
                    UICtrl.EnableShooterCanvas();
                    
                    localTank.GetComponent<DriverController>().enabled = false;
                    localTank.GetComponent<ShooterController>().enabled = true;
                    tankModel.shooterCamera.SetActive(true);


                }
                UICtrl.EnableHPSlider();
                UICtrl.SetMaxHP(tankModel.maxHealth);
                UICtrl.SetMinHP(0);
                UICtrl.SetHP(tankModel.health);
                

            }
        }

        photonView.RPC("ConfirmReady", PhotonTargets.All);
    }

    public void InitGame()
    {
        if (PhotonNetwork.isMasterClient)
        {
            InstantiateTanks();
            photonView.RPC("BindPlayer2Tank", PhotonTargets.All);
        }
    }

    [PunRPC]
    public void DestroyTank(string tankName) {
        if (PhotonNetwork.isMasterClient)
        {
            GameObject tank = GameObject.Find(tankName);
            if(tank != null)
                PhotonNetwork.Destroy(tank);
        }
       
    }

    [PunRPC]
    public void UpdateDeadList(string tankName)
    {
        livedTankCount--;
        dead_tank_list.Add(tankName);
        UICtrl.ShowDeathInfo(tankName);
        
    }

    public GameObject SwitchCamera() {
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
        int index = (int)(Random.value * tanks.Length * 2);
        if(index % 2 == 0)
        {
            return tanks[index / 2].GetComponent<TankModel>().driverCamera;
        }
        else
        {
            return tanks[index / 2].GetComponent<TankModel>().shooterCamera;
        }
    }

    public bool IsAllPlayerLoaded()
    {
        return loadedPlayerNum == PhotonNetwork.playerList.Length;
    }

    [PunRPC]
    public void StartCountDown(double startTime)
    {
        startTimer = startTime;
        endTimer = startTimer + playingDuration;
    }

    //如果玩家断开与Photon服务器的连接，加载场景GameLobby
    public override void OnConnectionFail(DisconnectCause cause)
    {
        LeaveRoom();              //玩家离开游戏房间
    }

    //离开房间函数
    public void LeaveRoom()
    {
        //mainCamera.enabled = true;
        PhotonNetwork.LeaveRoom();              //玩家离开游戏房间
        SceneManager.LoadScene("LobbyScene");  //加载场景GameLobby
    }

}
