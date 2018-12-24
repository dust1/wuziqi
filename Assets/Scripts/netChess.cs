using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class netChess : NetworkBehaviour {

	//四个锚点位置
	GameObject LeftTop;
	GameObject RightTop;
	GameObject LeftBottom;
	GameObject RightBottom;

	//主摄像机
	Camera cam;

	//锚点在屏幕上的映射位置
	Vector3 LTPos;
	Vector3 RTPos;
	Vector3 LBPos;
	Vector3 RBPos;

	//当前点选的位置
	Vector3 PointPos;

	//棋盘网格宽度
	float gridWidth = 1;

	//棋盘网格高度
	float gridHeight = 1;

	//网格宽和高中较小的一个
	float minGridDis;

	//落子状态
	enum turn {black, white};

	//存储在棋盘上的落子位置
	Vector2[,] chessPos;

	//存储在棋盘上的落子状态
	int[,] chessState;

	//白棋子贴图
	public Texture2D white;

	//黑棋子贴图
	public Texture2D black;

	//记录棋子状态
	public SyncListInt cs = new SyncListInt();

	//同步的获胜方 1：黑方，-1：白方
	[SyncVar]
	public int winner = 0;

	//是否处于对弈状态
	[SyncVar]
	public bool isPlaying = true;

	//落子状态
	[SyncVar]
	turn chessTurn;

	//是否需要重新开始
	[SyncVar]
	public bool isRestart;

	// Use this for initialization
	void Start () {
		for (int i = 0; i < 225; i++) {
			cs.Add(0);
		}
		LeftTop = GameObject.Find("Main Camera/Plane/LeftTop");
		RightTop = GameObject.Find("Main Camera/Plane/RightTop");
		LeftBottom = GameObject.Find("Main Camera/Plane/LeftBottom");
		RightBottom = GameObject.Find("Main Camera/Plane/RightBottom");
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();

		chessPos = new Vector2[15, 15];
		chessState = new int[15, 15];
		chessTurn = turn.black;

		//计算锚点位置
		LTPos = cam.WorldToScreenPoint(LeftTop.transform.position);
		RTPos = cam.WorldToScreenPoint(RightTop.transform.position);
		LBPos = cam.WorldToScreenPoint(LeftBottom.transform.position);
		RBPos = cam.WorldToScreenPoint(RightBottom.transform.position);

		//计算网格宽度
		gridWidth = (RTPos.x - LTPos.x) / 14;
		gridHeight = (LTPos.y - LBPos.y) / 14;
		minGridDis = gridHeight < gridWidth ? gridHeight : gridWidth;

		//计算落子位置
		for (int i = 0; i < 15; i++) {
			for (int j = 0; j < 15; j++) {
				chessPos[i, j] = new Vector2(LBPos.x + gridWidth * i, LBPos.y + gridHeight * j);
			}
		}

	}
	
	// Update is called once per frame
	void Update () {
		
		//不是server端-执白子，轮到黑子的时候不能下子
		if (chessTurn == turn.black && !isServer) {
			return;
		}

		//server端-执黑子，轮到白子的时候不能下子
		if (chessTurn == turn.white && isServer) {
			return;
		}

		// Debug.Log("当前棋子：" + chessTurn + ";是否开始游玩:" + isPlaying + ";是否是服务器:" + isServer);
		//检测鼠标输入并确定落子状态
		if (isPlaying && Input.GetMouseButtonDown(0)) {
			PointPos = Input.mousePosition;
			for (int i = 0; i < 15; i++) {
				for (int j = 0; j < 15; j++) {
					//找到最接近鼠标点击点的落子点,如果空则落子
					if (Dis(PointPos, chessPos[i, j]) < minGridDis / 2 && chessState[i, j] == 0) {

						//------修改本地数据------
						//根据下棋顺序确定落子颜色
						//chessState[i, j] = chessTurn == turn.black ? 1 : -1;
						//落子成功，更换下棋顺序
						//chessTurn = chessTurn == turn.black ? turn.white : turn.black;
						//----------------------

						//--------不修改本地数据，直接在服务端修改数据然后同步到本地---------
						//本地数据修改完成后需要将信息同步到服务器
						CmdFallingChild(i, j);
						
					}
				}
			}
		}

		if (isRestart) {
			restart();
		}

	}

	[Command]
	void CmdFallingChild(int i, int j) {
		
		//-------该数据在服务端修改----------
		chessState[i, j] = chessTurn == turn.black ? 1 : -1;
		//落子成功，更换下棋顺序
		chessTurn = chessTurn == turn.black ? turn.white : turn.black;
		cs[(i * 15 + j)] = chessState[i, j];

		// Debug.Log("落子坐标：{index:" + (i * 15 + j) + ";落子:" + v +"};下一个棋子：" + chessTurn);
		//调用判断函数，确定是否有胜利方
		int re = result();
		if (re == 1) {
			Debug.Log("黑棋胜利");
			winner = 1;
			isPlaying = false;
		} else if (re == -1) {
			Debug.Log("白棋胜利");
			winner = -1;
			isPlaying = false;
		}
	}

	//计算平面距离函数
	float Dis(Vector3 mPos, Vector2 gridPos) {
		return Mathf.Sqrt(Mathf.Pow(mPos.x - gridPos.x, 2) + Mathf.Pow(mPos.y - gridPos.y, 2));
	}

	void OnGUI() {
        // 绘制棋子
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (cs[i * 15 + j] == 1)
                {
                    GUI.DrawTexture(new Rect(chessPos[i, j].x - gridWidth / 2, Screen.height - chessPos[i, j].y - gridHeight / 2, gridWidth, gridHeight), black);
                }
                if (cs[i * 15 + j] == -1)
                {
                    GUI.DrawTexture(new Rect(chessPos[i, j].x - gridWidth / 2, Screen.height - chessPos[i, j].y - gridHeight / 2, gridWidth, gridHeight), white);
                }
            }
        }

        //根据获胜状态，弹出相应的胜利图片
        if (winner == 1)
        {
            if (GUI.Button(new Rect(Screen.width * 0.45f, Screen.height * 0.45f, Screen.width * 0.1f, Screen.height * 0.1f), "黑子胜利！"))
                isRestart = true;
        }
        else if (winner == -1)
        {
            if (GUI.Button(new Rect(Screen.width * 0.45f, Screen.height * 0.45f, Screen.width * 0.1f, Screen.height * 0.1f), "白子胜利！"))
                isRestart = true;
        }
        else
            isRestart = false;
    }
	
	void restart() {
		for (int i = 0; i < 15; i++) {
			for (int j = 0; j < 15; j++) {
				chessState[i, j] = 0;
				cs[i * 15 + j] = 0;
			}
		}
		isPlaying = true;
		chessTurn = turn.black;
		winner = 0;
	}

    int result()
    {
        for (int i = 0; i < 15; i++)
            for (int j = 0; j < 15; j++)
            {
                int sum = 0;
                if (j < 11)
                {
                    //横向 →
                    sum = chessState[i, j] + chessState[i, j + 1] + chessState[i, j + 2] + chessState[i, j + 3] + chessState[i, j + 4];
                    if (sum == 5)  return 1;    // 黑子胜
                    if (sum == -5) return -1;   // 白子胜
                }
                if (i < 11)
                {
                    //纵向 ↓
                    sum = chessState[i, j] + chessState[i + 1, j] + chessState[i + 2, j] + chessState[i + 3, j] + chessState[i + 4, j];
                    if (sum == 5) return 1;
                    if (sum == -5) return -1;
                }
                if (i < 11 && j < 11)
                {
                    // 右斜线 ↘
                    sum = chessState[i, j] + chessState[i + 1, j + 1] + chessState[i + 2, j + 2] + chessState[i + 3, j + 3] + chessState[i + 4, j + 4];
                    if (sum == 5) return 1;
                    if (sum == -5) return -1;
                }
                if (i >= 4 && j < 11)
                {
                    // 左斜线 ↗
                    sum = chessState[i, j] + chessState[i - 1, j + 1] + chessState[i - 2, j + 2] + chessState[i - 3, j + 3] + chessState[i - 4, j + 4];
                    if (sum == 5) return 1;
                    if (sum == -5) return -1;
                }
            }
        return 0; // 胜负未分
    }

}
