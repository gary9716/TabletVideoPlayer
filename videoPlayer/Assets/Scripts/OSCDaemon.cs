using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityOSC;
using System.IO;
using UnityEngine.Networking;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FunPlus.DeviceUtils;

class OSCDaemon : MonoBehaviour
{
	public int oscPort = 9000;
	private OSCReciever reciever;
	public Image bg;
	public Text info;
	public Text indicator;
	public Camera cam;

	public VideoPlayer noCachePlayer;

	private string targetServerIP;
	private int port;
	private OSCClient oscClient;

	public Dictionary<string, VideoPlayer> playerCache;
	VideoPlayer curPlayer = null;
	private string myIP;

	void Start()
	{
		info.enabled = false;
		indicator.enabled = false;
		cam = GetComponent<Camera>();
		playerCache = new Dictionary<string, VideoPlayer>();
		if(reciever == null) {
			reciever = new OSCReciever();
			reciever.Open(oscPort);
		}
		myIP = GetLocalIPAddress();
		DeviceUtils.SetScreenBrightness(255);
	}

	IEnumerator DownloadFileToPath(string url, string path, System.Action cb) {
		var interval = new WaitForSeconds(0.1f);
		using(UnityWebRequest req = UnityWebRequest.Get(url)) {
			if(req == null) {
				info.text = "url is invalid";
				yield break;
			}
			req.downloadHandler =  new DownloadHandlerFile(path);
			req.SendWebRequest();
			info.enabled = true;
			while(!req.isDone && !req.isNetworkError && !req.isHttpError) {
				info.text = "Progress:" + ((int)(req.downloadProgress * 100));
				yield return interval;
			}
			info.text = "";
			
			if (req.isNetworkError || req.isHttpError) {
				info.text = req.error;	
			}
			else {
				info.enabled = false;
				if(cb != null)
					cb();
			}
			
		}
	}

	void PlayVideo(string url, bool isLooping) {
		var prePlayer = curPlayer;
		if(playerCache.ContainsKey(url)) {
			curPlayer = playerCache[url];
		}
		else {
			curPlayer = CachePlayer(url);
		}

		if(curPlayer == null) return;
		if(prePlayer != null && curPlayer != prePlayer && prePlayer.isPlaying) {
			prePlayer.Pause();
			prePlayer.frame = 0;
		}

		curPlayer.isLooping = isLooping;
		bg.enabled = false;
		
		if(downloadRoutine != null) {
			StopCoroutine(downloadRoutine);
			downloadRoutine = null;
		}
		
		if(curPlayer.isPlaying) {
			curPlayer.Pause();
			curPlayer.frame = 0;	
			curPlayer.Play();
		}
		else
			curPlayer.Play();
	}

	void PlayVideoNoCache(string url, bool isLooping) {
		noCachePlayer.isLooping = isLooping;
		if(noCachePlayer.url.Equals(url)) {
			if(noCachePlayer.isPlaying) {
				noCachePlayer.Pause();
				noCachePlayer.frame = 0;
			}
			noCachePlayer.Play();
		}
		else {
			noCachePlayer.Stop();
			noCachePlayer.url = url;
			noCachePlayer.Play();
		}
		curPlayer = noCachePlayer;
		bg.enabled = false;
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
						PlayVideoNoCache(url, val == 1);
					else 
						PlayVideoNoCache(url, false);
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
				if(curPlayer != null && !curPlayer.isPlaying) curPlayer.Play();
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
					bool testPlay = false;
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
						var fileExist = File.Exists(filePath);
						if(testPlay && fileExist) {
							StopVideo();
							PlayVideo("file://" + filePath, false);
						}

						if(oscClient != null) {
							OSCMessage message = new OSCMessage("/download-done");
							message.Append(myIP);
							message.Append(fileExist? "succeed" : "failed"); 
							oscClient.Send(message);
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
					}
					catch(System.Exception e) {

					}
					
				}
				else
				{
					return;
				}
			}
			else if(msg.Address.Equals("/set-brightness")) {
				if(count > 0) {
					int val = 0;
					if(int.TryParse(dataList[0].ToString(),out val)) {
						DeviceUtils.SetScreenBrightness(val);
					}
				}
				else
				{
					return;
				}
			}
			else if(msg.Address.Equals("/set-id")) {
				if(count > 0) {
					indicator.enabled = !indicator.enabled;
					if(indicator.enabled) {
						indicator.text = dataList[0].ToString();
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

	public static string GetLocalIPAddress()
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				return ip.ToString();
			}
		}
		throw new System.Exception("No network adapters with an IPv4 address in the system!");
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
		/*
		player.prepareCompleted += (VideoPlayer curPlayer) => {
			if(oscClient != null) {
				OSCMessage message = new OSCMessage("/prepare-done");
				message.Append(myIP);
				message.Append(curPlayer.url); 
				oscClient.Send(message);
			}
		};
		*/
		player.loopPointReached += EndReached;
		player.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
		player.Prepare();
		return player;
	}

	/// <summary>
	/// This function is called when the object becomes enabled and active.
	/// </summary>
	void OnEnable()
	{
		if(reciever == null) {
			reciever = new OSCReciever();
			reciever.Open(oscPort);
		}
	}

	/// <summary>
	/// This function is called when the behaviour becomes disabled or inactive.
	/// </summary>
	void OnDisable()
	{
		if(reciever != null) {
			reciever.Close();
			reciever = null;
		}

		if(oscClient != null) {
			oscClient.Close();
			oscClient = null;
		}
	}

	void EndReached(UnityEngine.Video.VideoPlayer vp)
	{
		if(!vp.isLooping) {
			ShowBG();	
		}

		/*
		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/end-reach");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}
		*/
	}

	void ShowBG() {
		bg.enabled = true;
	}

	public void onValChanged(System.Single val) {
		DeviceUtils.SetScreenBrightness((int)val);
	}

}
