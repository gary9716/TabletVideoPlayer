using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityOSC;
using System.IO;
using UnityEngine.Networking;
class OSCDaemon : MonoBehaviour
{
	public int oscPort = 9000;
	private OSCReciever reciever;
	public Image bg;
	public Text info;
	public Camera cam;

	private string targetServerIP;
	private int port;
	private OSCClient oscClient;

	public Dictionary<string, VideoPlayer> playerCache;
	VideoPlayer curPlayer = null;
	void Start()
	{
		info.enabled = false;
		cam = GetComponent<Camera>();
		playerCache = new Dictionary<string, VideoPlayer>();
		reciever = new OSCReciever();
		reciever.Open(oscPort);
	}

	IEnumerator DownloadFileToPath(string url, string path, System.Action cb) {
		var interval = new WaitForSeconds(0.1f);
		using(UnityWebRequest req = UnityWebRequest.Get(url)) {
			req.downloadHandler =  new DownloadHandlerFile(path);
			
			req.SendWebRequest();
			info.enabled = true;
			while(!req.isDone) {
				info.text = "Progress:" + ((int)(req.downloadProgress * 100));
				yield return interval;
			}
			info.text = "";
			info.enabled = false;
			
			if (req.isNetworkError || req.isHttpError)
				Debug.LogError(req.error);
			else {
				Debug.Log("File successfully downloaded and saved to " + path);
				if(cb != null)
					cb();
			}
			
		}
	}

	void PlayVideo(string url, bool isLooping) {
		if(playerCache.ContainsKey(url)) {
			curPlayer = playerCache[url];
		}
		else {
			curPlayer = CachePlayer(url);
		}

		if(curPlayer == null || curPlayer.isPlaying) return;
		curPlayer.isLooping = isLooping;
		bg.enabled = false;
		
		if(downloadRoutine != null) {
			StopCoroutine(downloadRoutine);
			downloadRoutine = null;
		}

		curPlayer.Play();
	}

	Coroutine downloadRoutine;

	void StopVideo() {
		if(curPlayer != null) {
			curPlayer.Pause();
			curPlayer.frame = 0;
		}
		bg.enabled = true;
	}

	void Update () {
		
		if(reciever.hasWaitingMessages()){
			OSCMessage msg = reciever.getNextMessage();
			var dataList = msg.Data;
			int count = dataList.Count;
			if(msg.Address.Equals("/play-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();	
					if(!url.Contains("http")) {
						var fileName = Path.GetFileName(url);
						var fullPath = Path.Combine(Application.persistentDataPath, fileName);
						url = "file://" + fullPath;
						if(!File.Exists(fullPath)) return;
					}

					int val;
					if(count > 1 && int.TryParse(dataList[1].ToString(), out val)) 
						PlayVideo(url, val == 1);
					else 
						PlayVideo(url, false);
				}
				else 
					return;

			}
			else if(msg.Address.Equals("/stop-video")) {
				StopVideo();
			}
			else if(msg.Address.Equals("/pause-video")) {
				if(curPlayer != null) curPlayer.Pause();
			}
			else if(msg.Address.Equals("/continue-video")) {
				bg.enabled = false;
				if(curPlayer != null) curPlayer.Play();
			}
			else if(msg.Address.Equals("/cache-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					if(playerCache.ContainsKey(url)) return;
					else {
						CachePlayer(url);
					}
				}
				else 
					return;
			}
			else if(msg.Address.Equals("/release-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					if(!playerCache.ContainsKey(url)) return;
					else {
						//Debug.Log("release " + url);
						var oldPlayer = playerCache[url];
						oldPlayer.Stop();
						playerCache.Remove(url);
						Destroy(oldPlayer);
					}
				}
				else 
					return;
			}
			else if(msg.Address.Equals("/download-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					var filename = Path.GetFileName(url);
					var filePath = Path.Combine(Application.persistentDataPath, filename);
					int val;
					bool testPlay = true;
					bool overwrite = true;
					bool interruptDuringDownload = false;

					if(count > 1 && int.TryParse(dataList[1].ToString(), out val)) {
						interruptDuringDownload = val == 1;
					}
					
					if(count > 2 && int.TryParse(dataList[2].ToString(), out val)) {
						overwrite = val == 1;
					}

					if(count > 3 && int.TryParse(dataList[3].ToString(), out val)) {
						testPlay = val == 1;
					}

					if(!overwrite && File.Exists(filePath)) return;
					if(downloadRoutine != null) {
						if(interruptDuringDownload)
							StopCoroutine(downloadRoutine);
						else 
							return;
					}
					downloadRoutine = StartCoroutine(DownloadFileToPath(url, filePath, () => {
						if(testPlay && File.Exists(filePath)) {
							StopVideo();
							PlayVideo("file://" + filePath, false);
						}
					}));
				}
				else 
					return;
			}
			else if(msg.Address.Equals("/set-bg-color")) {
				if(count > 0) {
					int val = 0;
					Color color = Color.white;
					int i = 0;
					foreach(var valStr in dataList) {
						if(int.TryParse(valStr.ToString(), out val)) {
							if(i == 0)
								color.r = val/255f;
							else if(i == 1) {
								color.g = val/255f;
							}
							else if(i == 2) {
								color.b = val/255f;
							}
							else {
								color.a = val/255f;
							}
						}
						i++;
					}
					bg.color = color;
				}
				else 
					return;
			}
			else if(msg.Address.Equals("/set-server")) {
				if(count > 1) {
					try {
						if(oscClient != null) {
							oscClient.Close();
							oscClient = null;
						}

						targetServerIP = dataList[0].ToString();
						if(int.TryParse(dataList[1].ToString(),out port)) {
							oscClient = new OSCClient(System.Net.IPAddress.Parse(targetServerIP), port);
							oscClient.Connect();
						}
						//Debug.Log(targetServerIP + ":" + port);
					}
					catch(System.Exception e) {

					}
					
				}
				else
				{
					return;
				}
			}
			
			
		}
	}

	VideoPlayer CachePlayer(string url) {
		var newPlayer = CreateNewPlayer(url);
		if(newPlayer != null) playerCache.Add(url, newPlayer);
		return newPlayer;
	}

	VideoPlayer CreateNewPlayer(string url) {
		var player = gameObject.AddComponent<VideoPlayer>();
		player.source = VideoSource.Url;
		player.url = url;
		player.playOnAwake = false;
		player.waitForFirstFrame = true;
		player.isLooping = false;
		player.aspectRatio = VideoAspectRatio.FitOutside;
		player.targetCamera = cam;
		player.renderMode = VideoRenderMode.CameraNearPlane;
		player.prepareCompleted += (VideoPlayer curPlayer) => {
			if(oscClient != null) {
				OSCMessage message = new OSCMessage("/prepare-done");
				message.Append(curPlayer.url); 
				oscClient.Send(message);
			}
		};
		player.loopPointReached += EndReached;

		player.Prepare();
		return player;
	}

	/// <summary>
	/// This function is called when the behaviour becomes disabled or inactive.
	/// </summary>
	void OnDisable()
	{
		if(reciever != null) {
			reciever.Close();
		}

		if(oscClient != null) {
			oscClient.Close();
		}
	}

	void EndReached(UnityEngine.Video.VideoPlayer vp)
	{
		if(!vp.isLooping) ShowBG();
	}

	void ShowBG() {
		bg.enabled = true;
	}
}
