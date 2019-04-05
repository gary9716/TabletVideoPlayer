using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityOSC;
class OSCDaemon : MonoBehaviour
{
	public int oscPort = 9000;
	private OSCReciever reciever;
	public VideoPlayer videoPlayer;
	public Image bg;
	public Camera cam;

	private string targetServerIP;
	private int port;
	private OSCClient oscClient;

	public Dictionary<string, VideoPlayer> playerCache;
	VideoPlayer curPlayer = null;
	void Start()
	{
		cam = GetComponent<Camera>();
		playerCache = new Dictionary<string, VideoPlayer>();
		videoPlayer.loopPointReached += EndReached;
		reciever = new OSCReciever();
		reciever.Open(oscPort);
	}

	void Update () {
		
		if(reciever.hasWaitingMessages()){
			OSCMessage msg = reciever.getNextMessage();
			var dataList = msg.Data;
			int count = dataList.Count;
			if(msg.Address.Equals("/play-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					if(playerCache.ContainsKey(url)) {
						curPlayer = playerCache[url];
					}
					else {
						videoPlayer.url = url;
						curPlayer = videoPlayer;
					}
				}
				else 
					return;

				if(curPlayer.isPlaying) return;

				int val;
				if(count > 1 && int.TryParse(dataList[1].ToString(), out val)) 
					curPlayer.isLooping =  val == 1;//1 means isLooping
				else 
					curPlayer.isLooping = false;
				bg.enabled = false;
				
				curPlayer.Play();
			}
			else if(msg.Address.Equals("/stop-video")) {
				if(curPlayer != null) {
					curPlayer.Pause();
					curPlayer.frame = 0;
				}
				bg.enabled = true;
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
						var newPlayer = CreateNewPlayer(url);
						if(newPlayer != null) playerCache.Add(url, newPlayer);
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
