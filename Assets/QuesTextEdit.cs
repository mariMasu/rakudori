﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;

public class QuesTextEdit : MonoBehaviour
{

	public GameObject inputQuesText;

	GameObject es;
	SceneData sd;

	//string QuesKeyCommon = "###";
	string AnsKeyCommon = "$$$";
	string ExpKeyCommon = ":::";
	string DummyKeyCommon = "%%%";
	string SepKeyCommon = "&&&";
	string PerKeyCommon = ";;;";
	string NullKeyCommon = "!!!";


	void Awake ()
	{
		es = GameObject.Find ("EventSystem");
		sd = es.GetComponent<SceneData> ();
	}

	public void QaSend ()
	{
		TextCheck ();
		List<string> q = TextToArray (sd.InputPat);

		if (q.Count == 0) {
			return;
		}

		foreach (string d in q) {
			Debug.Log (d);

			QuesArray qa = DbTextToQA.DbToQA (d);

			Debug.Log ("問題文：" + qa.Ques);

			foreach (string s in qa.Ans) {
				Debug.Log ("正答：" + s);

			}

			if (qa.Dummy.Length > 0) {
				foreach (string s in qa.Dummy) {
					
					Debug.Log ("ダミー：" + s);
				}
			} else {
				Debug.Log ("ダミーなし");

			}

			if (qa.Exp.Length > 0) {

				Debug.Log ("解説：" + qa.Exp);
			} else {
				Debug.Log ("解説なし");

			}
		}
	}

	public List<string> TextToArray (int mode = 0)
	{

		string text = inputQuesText.GetComponent<InputField> ().text;
		string sentaku;

		List<string> q = new List<string> ();

		string[] que = { sd.QuesKey };
		string[] rowText = text.Split (que, StringSplitOptions.RemoveEmptyEntries);

		List<string> listRow = new List<string> ();
		List<string> listQ = new List<string> ();
		List<string> listA = new List<string> ();
		List<string> listD = new List<string> ();
		List<string> listE = new List<string> ();

		foreach (string s in rowText) {
			if (s != "\n") {
				listRow.Add (s);
			}
		}

		//選択肢があればダミー用を作成
		if (SceneData.strNull (sd.Sentaku) == false) {
			string[] kanma = { "," };
			string[] sen = sd.Sentaku.Split (kanma, StringSplitOptions.RemoveEmptyEntries);

			sentaku = sen [0];

			for (int i = 1; i < sen.Length; i++) {
				sentaku += SepKeyCommon + sen [i];
			}

		} else {
			sentaku = "";
		}
		//問題ー解答ー問題ー、全問題ー全回答
		if (mode < 2) {

			if (listRow.Count % 2 != 0) {
				es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n問題のない正答か\n正答のない問題があります");
				return new List<string> ();
			}
			if (sd.InputPat == 0) {
				for (int i = 0; i < listRow.Count; i++) {
					if (i % 2 == 0) {
						listQ.Add (listRow [i]);
					} else {
						listA.Add (listRow [i]);
					}

				}
			} else {
				for (int i = 0; i < listRow.Count; i++) {
					if (i <= (listRow.Count / 2) - 1) {
						listQ.Add (listRow [i]);
					} else {
						listA.Add (listRow [i]);
					}

				}
			}

			//ダミーかあれば分離
			SeparateDummy (ref listA, ref listD);

			RemoveEnter (ref listQ, false);

			//完全一致コマンドの置き換え
			ConvertPerfectCommand (ref listQ);

			if (SceneData.strNull (sd.SepKey) == false) {

				//解答群の分割
				for (int i = 0; i < listQ.Count; i++) {
					
					string str = listQ [i];

					string[] sep = { sd.SepKey };
					string[] rowAS = listA [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

					List<string> listAS = new List<string> ();

					foreach (string s in rowAS) {
						listAS.Add (RemoveEnterAll (s));
					}
						
					//### Q $$$ A &&& A &&& A
					str += AnsKeyCommon + MergeList (listAS, SepKeyCommon);

					//ダミー解答の分割
					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						string[] rowDS = listD [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);
						List<string> listDS = new List<string> ();

						foreach (string s in rowDS) {
							listDS.Add (s);
						}
						RemoveEnter (ref listDS);

						//### Q $$$ A &&& A &&& A %%% D &&& D &&& D
						str += DummyKeyCommon + MergeList (listDS, SepKeyCommon);


						//選択肢があれば結合
						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {
						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}
					q.Add (str);
				}
			} else {
				RemoveEnter (ref listA);

				for (int i = 0; i < listQ.Count; i++) {


					string str = listQ [i];

					str += AnsKeyCommon + listA [i];

					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						str += DummyKeyCommon + RemoveEnterAll (listD [i]);

						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {

						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					q.Add (str);
				}
			}




			//問題ー正答ー解説ー問題
		} else if (mode == 2) {

			for (int i = 0; i < listRow.Count; i++) {
				
				string[] sep = { sd.AnsKey };
				string[] rowAS = listRow [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

				if (rowAS.Length > 2) {
					es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に複数の正答開始キーが\n使われています（" + i + "番目の問題）\n分割キーに置き換えてください");
					return new List<string> ();
				} else if (rowAS.Length < 2) {
					es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n問題のない正答か正答のない\n問題があります（" + i + "番目の問題）");
					return new List<string> ();
				} else {

					listQ.Add (rowAS [0]);
					listA.Add (rowAS [1]);
				}

			}

			//解説の分離
			if (SceneData.strNull (sd.ExpKey) == false) {

				for (int i = 0; i < listA.Count; i++) {

					string[] sep = { sd.ExpKey };
					string[] rowAS = listA [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

					if (rowAS.Length == 2) {
						listA [i] = rowAS [0];
						listE.Add (rowAS [1]);
					} else if (rowAS.Length == 1) {
						listE.Add (NullKeyCommon);
					} else if (rowAS.Length > 2) {

						es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に複数の解説開始キーが\n使われています（\" + i + \"番目の問題）");
						return new List<string> ();
					}
				}

				RemoveEnter (ref listE, false);
			}

			//ダミーかあれば分離
			SeparateDummy (ref listA, ref listD);

			RemoveEnter (ref listQ, false);

			//完全一致コマンドの置き換え
			ConvertPerfectCommand (ref listQ);

			if (SceneData.strNull (sd.SepKey) == false) {

				//解答群の分割
				for (int i = 0; i < listQ.Count; i++) {

					string str = listQ [i];

					string[] sep = { sd.SepKey };
					string[] rowAS = listA [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

					List<string> listAS = new List<string> ();

					foreach (string s in rowAS) {
						listAS.Add (RemoveEnterAll (s));
					}

					//### Q $$$ A &&& A &&& A
					str += AnsKeyCommon + MergeList (listAS, SepKeyCommon);

					//ダミー解答の分割
					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						string[] rowDS = listD [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);
						List<string> listDS = new List<string> ();

						foreach (string s in rowDS) {
							listDS.Add (s);
						}
						RemoveEnter (ref listDS);

						//### Q $$$ A &&& A &&& A %%% D &&& D &&& D
						str += DummyKeyCommon + MergeList (listDS, SepKeyCommon);


						//選択肢があれば結合
						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {
						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					//説明文結合
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}

					q.Add (str);
				}
			} else {
				RemoveEnter (ref listA);

				for (int i = 0; i < listQ.Count; i++) {


					string str = listQ [i];

					str += AnsKeyCommon + listA [i];

					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						str += DummyKeyCommon + RemoveEnterAll (listD [i]);

						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {

						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}
						
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}


					q.Add (str);
				}
			}










			//問題ー解説ー正答ー問題
		} else if (mode == 3) {

			for (int i = 0; i < listRow.Count; i++) {

				string[] sep = { sd.AnsKey };
				string[] rowAS = listRow [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

				if (rowAS.Length > 2) {
					es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に複数の正答開始キーが\n使われています（" + i + "番目の問題）\n分割キーに置き換えてください");
					return new List<string> ();
				} else if (rowAS.Length < 2) {
					es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n問題のない正答か正答のない\n問題があります（" + i + "番目の問題）");
					return new List<string> ();
				} else {

					listQ.Add (rowAS [0]);
					listA.Add (rowAS [1]);
				}

			}

			//解説の分離
			if (SceneData.strNull (sd.ExpKey) == false) {

				for (int i = 0; i < listA.Count; i++) {

					string[] sep = { sd.ExpKey };
					string[] rowQE = listQ [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

					if (rowQE.Length == 2) {
						listQ [i] = rowQE [0];
						listE.Add (rowQE [1]);
					} else if (rowQE.Length == 1) {
						listE.Add (NullKeyCommon);
					} else if (rowQE.Length > 2) {

						es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に複数の解説開始キーが\n使われています（\" + i + \"番目の問題）");
						return new List<string> ();
					}
				}

				RemoveEnter (ref listE, false);
			}

			//ダミーかあれば分離
			SeparateDummy (ref listA, ref listD);

			RemoveEnter (ref listQ, false);

			//完全一致コマンドの置き換え
			ConvertPerfectCommand (ref listQ);

			if (SceneData.strNull (sd.SepKey) == false) {

				//解答群の分割
				for (int i = 0; i < listQ.Count; i++) {

					string str = listQ [i];

					string[] sep = { sd.SepKey };
					string[] rowAS = listA [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);

					List<string> listAS = new List<string> ();

					foreach (string s in rowAS) {
						listAS.Add (RemoveEnterAll (s));
					}

					//### Q $$$ A &&& A &&& A
					str += AnsKeyCommon + MergeList (listAS, SepKeyCommon);

					//ダミー解答の分割
					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						string[] rowDS = listD [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);
						List<string> listDS = new List<string> ();

						foreach (string s in rowDS) {
							listDS.Add (s);
						}
						RemoveEnter (ref listDS);

						//### Q $$$ A &&& A &&& A %%% D &&& D &&& D
						str += DummyKeyCommon + MergeList (listDS, SepKeyCommon);


						//選択肢があれば結合
						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {
						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					//説明文結合
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}

					q.Add (str);
				}
			} else {
				RemoveEnter (ref listA);

				for (int i = 0; i < listQ.Count; i++) {


					string str = listQ [i];

					str += AnsKeyCommon + listA [i];

					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						str += DummyKeyCommon + RemoveEnterAll (listD [i]);

						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {

						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}
						
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}


					q.Add (str);
				}
			}









			//全問題ー正答ー解説ー正答
		} else if (mode == 4) {


			string[] sep = { sd.AnsKey };
			string[] rowA = listRow [listRow.Count - 1].Split (sep, StringSplitOptions.RemoveEmptyEntries);

			if (rowA.Length == listRow.Count + 1) {

				listRow [listRow.Count - 1] = rowA [0];

				for (int i = 1; i < rowA.Length; i++) {

					listA.Add (rowA [i]);

				}

				for (int i = 0; i < listRow.Count; i++) {

					listQ.Add (listRow [i]);

				}


			} else {
				es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n問題と正答の数が合いません");
				return new List<string> ();
			}



			//解説の分離
			if (SceneData.strNull (sd.ExpKey) == false) {

				for (int i = 0; i < listA.Count; i++) {

					string[] sep2 = { sd.ExpKey };
					string[] rowAS = listA [i].Split (sep2, StringSplitOptions.RemoveEmptyEntries);

					if (rowAS.Length == 2) {
						listA [i] = rowAS [0];
						listE.Add (rowAS [1]);
					} else if (rowAS.Length == 1) {
						listE.Add (NullKeyCommon);
					} else if (rowAS.Length > 2) {

						es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に複数の解説開始キーが\n使われています（\" + i + \"番目の問題）");
						return new List<string> ();
					}
				}

				RemoveEnter (ref listE, false);
			}

			//ダミーがあれば分離
			SeparateDummy (ref listA, ref listD);

			RemoveEnter (ref listQ, false);

			//完全一致コマンドの置き換え
			ConvertPerfectCommand (ref listQ);

			if (SceneData.strNull (sd.SepKey) == false) {

				//解答群の分割
				for (int i = 0; i < listQ.Count; i++) {

					string str = listQ [i];

					string[] sep2 = { sd.SepKey };
					string[] rowAS = listA [i].Split (sep2, StringSplitOptions.RemoveEmptyEntries);

					List<string> listAS = new List<string> ();

					foreach (string s in rowAS) {
						listAS.Add (RemoveEnterAll (s));
					}

					//### Q $$$ A &&& A &&& A
					str += AnsKeyCommon + MergeList (listAS, SepKeyCommon);

					//ダミー解答の分割
					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						string[] rowDS = listD [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);
						List<string> listDS = new List<string> ();

						foreach (string s in rowDS) {
							listDS.Add (s);
						}
						RemoveEnter (ref listDS);

						//### Q $$$ A &&& A &&& A %%% D &&& D &&& D
						str += DummyKeyCommon + MergeList (listDS, SepKeyCommon);


						//選択肢があれば結合
						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {
						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					//説明文結合
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}

					q.Add (str);
				}
			} else {
				RemoveEnter (ref listA);

				for (int i = 0; i < listQ.Count; i++) {


					string str = listQ [i];

					str += AnsKeyCommon + listA [i];

					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						str += DummyKeyCommon + RemoveEnterAll (listD [i]);

						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {

						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}


					q.Add (str);
				}
			}










			//全問題ー解説ー正答ー解説
		} else if (mode == 5) {
			string[] sep = { sd.AnsKey };
			string[] rowA = listRow [listRow.Count - 1].Split (sep, StringSplitOptions.RemoveEmptyEntries);

			listRow [listRow.Count - 1] = rowA [0];

			if (rowA.Length == listRow.Count + 1) {
				
				//解説の分離[0]
				if (SceneData.strNull (sd.ExpKey) == false) {
					string[] sep2 = { sd.ExpKey };
					string lr = listRow [listRow.Count - 1];
					string[] rowE = lr.Split (sep2, StringSplitOptions.RemoveEmptyEntries);

					if (rowE.Length > 1) {
						listRow [listRow.Count - 1] = rowE [0];
						listE.Add (rowE [1]);
					}
				}

				for (int i = 1; i < rowA.Length; i++) {

					listA.Add (rowA [i]);

				}

				for (int i = 0; i < listRow.Count; i++) {

					listQ.Add (listRow [i]);

				}


			} else {
				es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n問題と正答の数が合いません");
				return new List<string> ();
			}



			//解説の分離
			if (SceneData.strNull (sd.ExpKey) == false) {

				for (int i = 0; i < listA.Count - 1; i++) {

					string[] sep2 = { sd.ExpKey };
					string[] rowAS = listA [i].Split (sep2, StringSplitOptions.RemoveEmptyEntries);

					if (rowAS.Length == 2) {
						listA [i] = rowAS [0];
						listE.Add (rowAS [1]);
					} else if (rowAS.Length == 1) {
						listE.Add (NullKeyCommon);
					} else if (rowAS.Length > 2) {

						es.GetComponent<PopupWindow> ().PopupCaution ("エラー\n一つの問題に開始キーが重複して\n使われています（" + i + "番目の問題付近）");
						return new List<string> ();
					}
				}

				RemoveEnter (ref listE, false);
			}

			//ダミーがあれば分離
			SeparateDummy (ref listA, ref listD);

			RemoveEnter (ref listQ, false);

			//完全一致コマンドの置き換え
			ConvertPerfectCommand (ref listQ);

			if (SceneData.strNull (sd.SepKey) == false) {

				//解答群の分割
				for (int i = 0; i < listQ.Count; i++) {

					string str = listQ [i];

					string[] sep2 = { sd.SepKey };
					string[] rowAS = listA [i].Split (sep2, StringSplitOptions.RemoveEmptyEntries);

					List<string> listAS = new List<string> ();

					foreach (string s in rowAS) {
						listAS.Add (RemoveEnterAll (s));
					}

					//### Q $$$ A &&& A &&& A
					str += AnsKeyCommon + MergeList (listAS, SepKeyCommon);

					//ダミー解答の分割
					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						string[] rowDS = listD [i].Split (sep, StringSplitOptions.RemoveEmptyEntries);
						List<string> listDS = new List<string> ();

						foreach (string s in rowDS) {
							listDS.Add (s);
						}
						RemoveEnter (ref listDS);

						//### Q $$$ A &&& A &&& A %%% D &&& D &&& D
						str += DummyKeyCommon + MergeList (listDS, SepKeyCommon);


						//選択肢があれば結合
						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {
						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					//説明文結合
					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}

					q.Add (str);
				}
			} else {
				RemoveEnter (ref listA);

				for (int i = 0; i < listQ.Count; i++) {


					string str = listQ [i];

					str += AnsKeyCommon + listA [i];

					if (listD.Count > 0 && listD [i] != NullKeyCommon) {
						str += DummyKeyCommon + RemoveEnterAll (listD [i]);

						if (sentaku.Length > 0) {
							str += SepKeyCommon + sentaku;
						}

					} else {

						if (sentaku.Length > 0) {
							str += DummyKeyCommon + sentaku;
						}
					}

					if (listE.Count > 0 && listE [i] != NullKeyCommon) {
						str += ExpKeyCommon + listE [i];
					}


					q.Add (str);
				}
			}

		}

		return q;
	}

	public bool TextCheck ()
	{
		string text = inputQuesText.GetComponent<InputField> ().text;
		string[] used = { AnsKeyCommon, DummyKeyCommon, SepKeyCommon, ExpKeyCommon, PerKeyCommon };
		int f = 0;

		foreach (string s in used) {
			if (text.IndexOf (s) > -1) {
				f++;
			}
		}

		if (f != 0) {
			es.GetComponent<PopupWindow> ().PopupCaution ("エラー\nテキスト内に" + AnsKeyCommon + ",\n" + ExpKeyCommon + "," + DummyKeyCommon + "," + SepKeyCommon + "," + PerKeyCommon + "は使えません");
			return false;
		}

		return true;
	}

	public string MergeList (List<string> list, string key)
	{
		string mlist = "";
		foreach (string s in list) {
			mlist += key + s;
		}

		return mlist.Substring (key.Length);
	}

	public string RemoveEnterZengo (string s)
	{
		string kaigyo = "\n";

		while (s.Substring (0, kaigyo.Length) == kaigyo) {
			s = s.Substring (kaigyo.Length);
		}
		while (s.Substring (s.Length - kaigyo.Length) == kaigyo) {
			s = s.Substring (0, s.Length - kaigyo.Length);
		}

		return s;

	}

	public string RemoveEnterAll (string s)
	{
		string kaigyo = "\n";
		string ns = "";
		string[] spl = Regex.Split (s, kaigyo);

		foreach (string sp in spl) {
			if (SceneData.strNull (sp)) {
			} else {
				ns += sp;
			}
		}

		return ns;

	}

	public void RemoveEnter (ref List<string> list, bool b = true)
	{

		for (int i = 0; i < list.Count; i++) {
			if (b) {
				list [i] = RemoveEnterAll (list [i]);
			} else {
				list [i] = RemoveEnterZengo (list [i]);
			}
		}

	}

	public void RemoveEnter (ref string[] list, bool b = true)
	{
		
		for (int i = 0; i < list.Length; i++) {
			if (b) {
				list [i] = RemoveEnterAll (list [i]);
			} else {
				list [i] = RemoveEnterZengo (list [i]);
			}
		}

	}

	public void SeparateDummy (ref List<string> listA, ref List<string> listD)
	{
		if (SceneData.strNull (sd.DummyKey) == false) {
			string[] dum = { sd.DummyKey };
			for (int i = 0; i < listA.Count; i++) {
				string[] strD = listA [i].Split (dum, StringSplitOptions.RemoveEmptyEntries);
				if (strD.Length > 1) {
					listA [i] = strD [0];
					listD.Add (strD [1]);
				} else {
					listD.Add (NullKeyCommon);
				}
			}
		}
	}

	public void ConvertPerfectCommand (ref List<string> listQ)
	{
		if (SceneData.strNull (sd.PerKey) == false) {

			for (int i = 0; i < listQ.Count; i++) {
				if (listQ [i].Substring (0, sd.PerKey.Length) == sd.PerKey) {
					listQ [i] = listQ [i].Substring (sd.PerKey.Length);
					listQ [i] = PerKeyCommon + listQ [i];
				}
			}	
		}
	}



}