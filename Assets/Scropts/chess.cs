using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class chess : MonoBehaviour {

	//四个锚点位置，用于计算棋子落点
	public GameObject LeftTop;
	public GameObject RightTop;
	public GameObject LeftBottom;
	public GameObject RightBottom;

	//主摄像机
	public Camera cam;

	//锚点在屏幕上的映射位置
	Vector3 LTPos;
	Vector3 RTPos;
	Vector3 LBPos;
	Vector3 RBPos;

	Vector3 PointPos;//当前点选的位置
	float gridWidth = 1;//棋盘网格宽度
	float girdHeight = 1;//棋盘网格高度
	float minGridDis;//网格宽和高中较小的一个
	Vector2[,] chessPos;//存储在棋盘上所有可以落子的位置
	int[,] chessState;//存储棋盘位置上的落子状态
	enum turn {
		black, white
	}
	turn chessTurn;//落子顺序
	public Texture2D white;//白棋子
	public Texture2D black;//黑棋子
	public Texture2D blackWin;//黑旗子获胜图
	public Texture2D whiteWin;//白棋子获胜图
	int winner = 0;//获胜方，1:黑棋子；-1:白棋子
	bool isPlaying = true;//是否处于对弈状态

	// Use this for initialization
	void Start () {
		chessPos = new Vector2[15, 15];
		chessState = new int[15, 15];
		chessTurn = turn.black;
	}
	
	// Update is called once per frame
	void Update () {
		//计算锚点位置
		LTPos = cam.WorldToScreenPoint(LeftTop.transform.position);
		RTPos = cam.WorldToScreenPoint(RightTop.transform.position);
		LBPos = cam.WorldToScreenPoint(LeftBottom.transform.position);
		RBPos = cam.WorldToScreenPoint(RightBottom.transform.position);

		//计算网格宽度
		gridWidth = (RTPos.x - LTPos.x) / 14;
		girdHeight = (LTPos.y - LBPos.y) / 14;
		minGridDis = girdHeight < gridWidth ? girdHeight : gridWidth;

		//计算落子位置
		for (int i = 0; i < 15; i++) {
			for (int j = 0; j < 15; j++) {
				chessPos[i, j] = new Vector2(LBPos.x + gridWidth * i, LBPos.y + girdHeight * j);
			}
		}

		//检测鼠标输入并确定落子状态
		if (isPlaying && Input.GetMouseButtonDown(0)) {
			PointPos = Input.mousePosition;
			for (int i = 0; i < 15; i++) {
				for (int j = 0; j < 15; j++) {
					//找到最接近鼠标点击点的落子点,如果空则落子
					if (Dis(PointPos, chessPos[i, j]) < minGridDis / 2 && chessState[i, j] == 0) {
						//根据下棋顺序确定落子颜色
						chessState[i, j] = chessTurn == turn.black ? 1 : -1;
						//落子成功，更换下棋顺序
						chessTurn = chessTurn == turn.black ? turn.white : turn.black;
					}
				}
			}

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

		//按下空格重新开始游戏
		if (Input.GetKeyDown(KeyCode.Space)) {
			for (int i = 0; i < 15; i++) {
				for (int j = 0; j < 15; j++) {
					chessState[i, j] = 0;
				}
			}
			isPlaying = true;
			chessTurn = turn.black;
			winner = 0;
		}
	}

	//计算平面距离函数
	float Dis(Vector3 mPos, Vector2 gridPos) {
		return Mathf.Sqrt(Mathf.Pow(mPos.x - gridPos.x, 2) + Mathf.Pow(mPos.y - gridPos.y, 2));
	}

	void OnGUI() {
		//绘制棋子
		for (int i = 0; i < 15; i++) {
			for (int j = 0; j < 15; j++) {
				if (chessState[i, j] == 1) {
					GUI.DrawTexture(new Rect(chessPos[i, j].x - gridWidth / 2, Screen.height - chessPos[i, j].y - girdHeight / 2, gridWidth, girdHeight), black);
				}
				if (chessState[i, j] == -1) {
					GUI.DrawTexture(new Rect(chessPos[i, j].x - gridWidth / 2, Screen.height - chessPos[i, j].y - girdHeight / 2, gridWidth, girdHeight), white);
				}
			}
		}

		//根据胜者状态，弹出信息
		if (winner == 1) {
			GUI.DrawTexture(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.25f), blackWin);
		}
		if (winner == -1) {
			GUI.DrawTexture(new Rect(Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.5f, Screen.height * 0.25f), whiteWin);
		}
	}

	//检测获胜的条件
	int result() {
		//如果当前该白棋落子，标定黑棋刚刚下完一步，此时应该判断黑棋是否获胜
		if (chessTurn == turn.white) {
			for (int i = 0; i < 11; i++) {
				for (int j = 0; j < 15; j++) {
					if (j < 4) {
						if (resultLandscape(i, j, 1) == 1 || resultPortrait(i, j, 1) == 1 || resultRightOblique(i, j, 1) == 1) {
							return 1;
						}
					} else if (j >= 4 && j < 11) {
						if (resultLandscape(i, j, 1) == 1 || resultPortrait(i, j, 1) == 1 || resultRightOblique(i, j, 1) == 1 || resultLeftOblique(i, j, 1) == 1) {
							return 1;
						}
					} else {
						if (resultPortrait(i, j, 1) == 1 || resultLeftOblique(i, j, 1) == 1) {
							return 1;
						}
					}
				}
			}
			for (int i = 11; i < 15; i++) {
				for (int j = 0; j < 11; j++) {
					if (resultLandscape(i, j, 1) == 1) {
						return 1;
					}
				}
			}
		} else if (chessTurn == turn.black) {	//如果当前该黑棋落子，标定白棋刚刚下完一步，此时应该判断白棋是否获胜
			for (int i = 0; i < 11; i++) {
				for (int j = 0; j < 15; j++) {
					if (j < 4) {
						if (resultLandscape(i, j, -1) == -1 || resultPortrait(i, j, -1) == -1 || resultRightOblique(i, j, -1) == -1) {
							return -1;
						}
					} else if (j >= 4 && j < 11) {
						if (resultLandscape(i, j, -1) == -1 || resultPortrait(i, j, -1) == -1 || resultRightOblique(i, j, -1) == -1 || resultLeftOblique(i, j, -1) == -1) {
							return -1;
						}
					} else {
						if (resultPortrait(i, j, -1) == -1 || resultLeftOblique(i, j, -1) == -1) {
							return -1;
						}
					}
				}
			}
			for (int i = 11; i < 15; i++) {
				for (int j = 0; j < 11; j++) {
					if (resultLandscape(i, j, -1) == -1) {
						return -1;
					}
				}
			}
		}
		return 0;
	}

	//判断黑色横
	int resultLandscape(int i, int j, int re) {
		if (chessState[i, j] == re && chessState[i, j + 1] == re && chessState[i, j + 2] == re && chessState[i, j + 3] == re && chessState[i, j + 4] == re) {
			return re;
		}
		return 0;
	}
	//黑色纵
	int resultPortrait(int i, int j, int re) {
		//纵向 
		if (chessState[i, j] == re && chessState[i + 1, j] == re && chessState[i + 2, j] == re && chessState[i + 3, j] == re && chessState[i + 4, j] == re) {
			return re;
		}
		return 0;
	}
	//黑色左斜
	int resultLeftOblique(int i, int j, int re) {
		//左斜线
		if (chessState[i, j] == re && chessState[i + 1, j - 1] == re && chessState[i + 2, j - 2] == re && chessState[i + 3, j - 3] == re && chessState[i + 4, j - 4] == re) {
			return re;
		}
		return 0;
	}
	//黑色右斜
	int resultRightOblique(int i, int j, int re) {
		//右斜线
		if (chessState[i, j] == re && chessState[i + 1, j + 1] == re && chessState[i + 2, j + 2] == re && chessState[i + 3, j + 3] == re && chessState[i + 4, j + 4] == re) {
			return re;
		}
		return 0;
	}


}
