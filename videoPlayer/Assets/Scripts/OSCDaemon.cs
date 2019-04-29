#define EFFECT_TEST
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
using Lean.Pool;

public class OSCDaemon : MonoBehaviour
{
	public int oscPort = 9000;
	public Image bg;
	public Text info;
	public Text indicator;
	public Camera cam;
	public VideoPlayer noCachePlayer;
	public VideoPlayer playerPrefab;
	public Transform playerRoot;
	public RawImage videoImage;
	public Image stillImage;
	public KTEffectBase[] effectList;

	private OSCReciever reciever;
	private string targetServerIP;
	private int port;
	private OSCClient oscClient;
	VideoPlayer curPlayer = null;
	private string myIP;
	private RenderTexture videoTex;
	private Dictionary<string, VideoPlayer> playerCache = new Dictionary<string, VideoPlayer>();
	private Dictionary<string, Sprite> imgCache = new Dictionary<string, Sprite>();

	private float loopingStartPos = 0;

	EffectParamsRecording paramRecording = new EffectParamsRecording();
	
	bool isPlayingRecord {
		get {
			return paramRecording.state == EffectParamsRecording.State.Playing && curPlayer != null && curPlayer.isPlaying;
		}
	}

	void Start()
	{
		videoTex = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
		
		noCachePlayer.targetTexture = videoTex;
		noCachePlayer.loopPointReached += EndReached;
		noCachePlayer.started += OnVideoStarted;
		noCachePlayer.prepareCompleted += OnVideoPrepared;
		noCachePlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;

		videoImage.texture = videoTex;

		info.enabled = false;
		indicator.enabled = false;
		cam = GetComponent<Camera>();
		if(reciever == null) {
			reciever = new OSCReciever();
			reciever.Open(oscPort);
		}

		myIP = GetLocalIPAddress();
		DeviceUtils.SetScreenBrightness(255);

		#if EFFECT_TEST && UNITY_EDITOR
		PlayVideo("file://" + Application.persistentDataPath + "/faceNegative1-low.mp4", true, false);
		#endif
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

	Sprite LoadSprite(string fileName) {
		var imgPath = Path.Combine(Application.persistentDataPath, fileName);
		if(!File.Exists(imgPath)) {
			return null;
		}

		var imgBytes = File.ReadAllBytes(imgPath);
		Texture2D tex = new Texture2D(1280, 720);
		tex.LoadImage(imgBytes, false);
		return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
	}

	VideoPlayer CacheVideoPlayer(string url) {
		var newPlayer = LeanPool.Spawn<VideoPlayer>(playerPrefab);
		newPlayer.url = url;
		newPlayer.loopPointReached += EndReached;
		newPlayer.started += OnVideoStarted;
		newPlayer.prepareCompleted += OnVideoPrepared;
		newPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
		newPlayer.targetTexture = videoTex;
		newPlayer.name = Path.GetFileNameWithoutExtension(url);
		newPlayer.transform.parent = playerRoot;
		playerCache.Add(url, newPlayer);
		return newPlayer;
	}

	void ReleaseVideoPlayer(string url) {
		if(playerCache.ContainsKey(url)) {
			var player = playerCache[url];
			playerCache.Remove(url);
			player.url = null;
			player.Stop();
			player.name = "VideoPlayer(Cloned)";
			LeanPool.Despawn(player);	
		}
	}

	void PlayVideo(string url, bool isLooping, bool cache, int frame = -1) {
		var prevPlayer = curPlayer;
		if(!cache) {
			curPlayer = noCachePlayer;
			curPlayer.url = url;
		}
		else if(playerCache.ContainsKey(url)) {
			curPlayer = playerCache[url];
		}
		else {
			curPlayer = CacheVideoPlayer(url);
		}

		if(prevPlayer != null && prevPlayer.isPlaying) {
			prevPlayer.Pause();
			prevPlayer.frame = 0;
		}

		curPlayer.isLooping = isLooping;
		if(frame >= 0) curPlayer.frame = frame;
		curPlayer.Prepare();
	}
	Coroutine downloadRoutine;

	void StopVideo() {
		if(curPlayer != null) {
			curPlayer.Pause();
			curPlayer.frame = 0;
		}
		bg.enabled = true;
		videoImage.enabled = false;
	}

	OSCClient CreateOSCClient(string ip, int port) {
		var client = new OSCClient(System.Net.IPAddress.Parse(ip), port);
		client.Connect();
		return client;
	}

	const int maxSpeed = 3;

	public void SetVideoSpeed(float factor) {
		if(curPlayer != null && curPlayer.canSetPlaybackSpeed) {
			curPlayer.playbackSpeed = 1 + (maxSpeed - 1) * factor;
		}
	}

	void Update () {
		if(reciever.hasWaitingMessages()) {
			OSCMessage msg = reciever.getNextMessage();
			var dataList = msg.Data;
			int count = dataList.Count;
			var address = msg.Address;

			if(address.Equals("/play-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();	
					if(!url.Contains("http")) {
						if(url.Equals("-1")) {
							StopVideo();
							return;
						}
						var fileName = Path.GetFileName(url);
						var fullPath = Path.Combine(Application.persistentDataPath, fileName);
						url = "file://" + fullPath;
						if(!File.Exists(fullPath)) return;
					}

					var isLooping = false;
					int val = 0;
					if(count > 1 && int.TryParse(dataList[1].ToString(), out val)) 
						isLooping = val == 1;
					
					PlayVideo(url, isLooping, false);
				}
				else 
					return;

			}
			else if(address.Equals("/stop-video")) {
				StopVideo();
			}
			else if(address.Equals("/pause-video")) {
				if(curPlayer != null) curPlayer.Pause();
			}
			else if(address.Equals("/continue-video")) {
				bg.enabled = false;
				if(curPlayer != null && !curPlayer.isPlaying) curPlayer.Play();
			}
			else if(address.Equals("/download-file")) {
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
							PlayVideo("file://" + filePath, false, false);
						}

						if(oscClient != null) {
							OSCMessage message = new OSCMessage("/download-done");
							message.Append(myIP);
							message.Append(filename);
							message.Append(fileExist? "succeed" : "failed"); 
							oscClient.Send(message);
						}
					}));
				}
				else 
					return;
			}
			else if(address.Equals("/set-server")) {
				if(count > 1) {
					if(oscClient != null) {
						oscClient.Close();
						oscClient = null;
					}

					targetServerIP = dataList[0].ToString();
					if(int.TryParse(dataList[1].ToString(), out port)) {
						oscClient = CreateOSCClient(targetServerIP, port);
						var oscMsg = new OSCMessage("/set-server-response");
						oscMsg.Append(myIP);
						oscMsg.Append("connected");
						oscClient.Send(oscMsg);
					}
				}
				else
				{
					return;
				}
			}
			else if(address.Equals("/set-id")) {
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
			else if(address.Equals("/set-effect")) {
				if(count > 0) {
					int effectIndex = -1;
					if(int.TryParse(dataList[0].ToString(), out effectIndex)) {
						if(effectIndex >= 0 && effectIndex < effectList.Length) {
							EffectParamsRecording.EffectParams paramFrame = null;
							if(paramRecording.state == EffectParamsRecording.State.Recording && curPlayer != null && curPlayer.isPlaying) {
								paramFrame = new EffectParamsRecording.EffectParams();
								paramRecording.seq.Add(paramFrame);
							}
							var effect = effectList[effectIndex];
							float val = -1;
							int paramIndex = -1;

							if(paramFrame != null) {
								paramFrame.effectIndex = effectIndex;
								paramFrame.videoFrame = curPlayer.frame;
							}

							for(int i = 1;i < count;) {
								var data = dataList[i].ToString();
								if(data.Equals("i")) {
									if((i + 2) < count && int.TryParse(dataList[i + 1].ToString(), out paramIndex) && float.TryParse(dataList[i + 2].ToString(), out val)) {
										effect.SetParameter(paramIndex, val);
										if(paramFrame != null) {
											paramFrame.paramIndex.Add(paramIndex);
											paramFrame.paramVal.Add(val);
										}
									}
									i += 3;
								}
								else if(data.Equals("s")) {
									int startIndex = 0;
									int paramCount = 0;
									if((i + 2) < count && int.TryParse(dataList[i + 1].ToString(), out startIndex) && int.TryParse(dataList[i + 2].ToString(), out paramCount)) {
										if(i + 2 + paramCount < count)
											for(int k = 0;k < paramCount;k++) {
												if(float.TryParse(dataList[k + i + 3].ToString(), out val)) {
													paramIndex = startIndex + k;
													effect.SetParameter(paramIndex, val);
													if(paramFrame != null) {
														paramFrame.paramIndex.Add(paramIndex);
														paramFrame.paramVal.Add(val);
													}
												}
											}
									}
									i += (3 + paramCount);
								}
								else if(float.TryParse(data, out val)) {
									paramIndex = i - 1;
									effect.SetParameter(paramIndex, val);
									if(paramFrame != null) {
										paramFrame.paramIndex.Add(paramIndex);
										paramFrame.paramVal.Add(val);
									}

									i++;
								}
								else {
									i++;
								}
							}
							effect.SetEffectActive(true);
						}
					}
				}
				else
				{
					return;
				}
			}
			else if(address.Equals("/record-params")) {
				paramRecording.Reset();
				paramRecording.state = EffectParamsRecording.State.Recording;
				if(curPlayer != null && curPlayer.url != null)
					PlayVideo(curPlayer.url, true, false);
			}
			else if(address.Equals("/clear-recording")) {
				paramRecording.Reset();
			}
			else if(address.Equals("/set-image")) {
				if(count > 0) {
					var imgName = dataList[0].ToString();
					if(imgName.Equals("-1")) {
						stillImage.sprite = null;
					}
					else if(imgCache.ContainsKey(imgName)) {
						var img = imgCache[imgName];
						stillImage.sprite = img;
					}
					else {
						var sprite = LoadSprite(imgName);
						if(sprite != null) {
							imgCache.Add(imgName, sprite);
							stillImage.sprite = sprite;
						}
					}
				}
				else return;
			}
			else if(address.Equals("/load-image")) {
				if(count > 0) {
					var imgName = dataList[0].ToString();
					var img = LoadSprite(imgName);
					if(imgCache.ContainsKey(imgName)) {
						imgCache[imgName] = img;
					}
					else {
						imgCache.Add(imgName, img);
					}
				}
				else return;
			}
			else if(address.Equals("/disable-effect")) {
				if(count > 0) {
					if(dataList[0].ToString().Equals("-1")) {
						for(int i = 0;i < effectList.Length;i++) {
							effectList[i].SetEffectActive(false);
						}
					}
					else {
						for(int i = 0;i < count;i++) {
							int effectIndex = -1;
							if(int.TryParse(dataList[i].ToString(), out effectIndex)) {
								if(effectList.Length > effectIndex && effectIndex >= 0)
									effectList[effectIndex].SetEffectActive(false);
							}
						}
					}
				}
				paramRecording.Reset();
			}
			
			//compatible with /set-effect
			else if(address.Equals("/set-brightness")) {
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
			else if(address.Equals("/set-bg-color")) {
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
			else if(address.Equals("/set-video-alpha")) {
				if(count > 0) {
					float alpha = 1;
					if(float.TryParse(dataList[0].ToString(), out alpha)) {
						var color = videoImage.color;
						color.a = alpha;
						videoImage.color = color;
					}
				}
				else return;
			}
			else if(address.Equals("/set-video-speed")) {
				if(count > 0) {
					float factor = 0;
					if(float.TryParse(dataList[0].ToString(), out factor)) {
						SetVideoSpeed(factor);
					}
				}
			}
			
			else if(address.Equals("/set-start-pos")) {
				if(count > 0) {
					float.TryParse(dataList[0].ToString(), out loopingStartPos);
				}
				else return;
			}
			
			//unused cmds
			else if(address.Equals("/cache-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					CacheVideoPlayer(url);
				}
				else return;
			}
			else if(address.Equals("/release-video")) {
				if(count > 0) {
					var url = dataList[0].ToString();
					ReleaseVideoPlayer(url);
				}
				else return;
			}
			else if(address.Equals("/sync-request")) {
				if(count > 0) {
					var ip = dataList[0].ToString();
					OSCClient client = null;
					bool toCloseClient = false;
					if(ip.Equals("server")) {
						client = oscClient;
					}
					else {
						if(count > 1 && int.TryParse(dataList[1].ToString(), out port)) {
							client = CreateOSCClient(ip, port);
							toCloseClient = true;
						}
					}

					if(client != null && curPlayer != null && curPlayer.isPlaying) {
						OSCMessage message = new OSCMessage("/sync-response");
						message.Append(myIP);
						message.Append(curPlayer.url);
						message.Append(curPlayer.frame);
						message.Append(curPlayer.frameRate);
						message.Append(curPlayer.frameCount);
						client.Send(message);
					}

					if(toCloseClient) client.Close();
				}
				else return;
			}
			else if(address.Equals("/sync-response")) {
				if(count > 4) {
					var srcIP = dataList[0].ToString();
					var url = dataList[1].ToString();
					int frame = -1, frameRate = -1, frameCount = -1;
					if(int.TryParse(dataList[2].ToString(), out frame) && 
					int.TryParse(dataList[3].ToString(), out frameRate) && 
					int.TryParse(dataList[4].ToString(), out frameCount)) {
						PlayVideo(url, true, false, (int)(frame + frameRate * 0.1)); //100ms delay
					}
				}
				else return;
			}
		}

		if(isPlayingRecord) {
			var curFrame = curPlayer.frame;
			var seq = paramRecording.seq;
			for(int i = paramRecording.progress; i < seq.Count;i++) {
				var param = seq[i];
				if(param.videoFrame > curFrame) {
					break;
				}
				else if(param.videoFrame == curFrame) {
					//apply param
					int effectIndex = param.effectIndex;
					var effect = effectList[effectIndex];
					for(int k = 0;k < param.paramIndex.Count;k++) {
						effect.SetParameter(param.paramIndex[k],param.paramVal[k]);
					}
					effect.SetEffectActive(true);
				}
			}
		}
	}
	
	public static string GetLocalIPAddress()
	{
		try {
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
		catch(System.Exception e) {
			Debug.Log(e.ToString());
			return null;
		}
		
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

	void StopRecording() {
		paramRecording.state = EffectParamsRecording.State.Playing;
		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/recording-end");
			message.Append(myIP);
			oscClient.Send(message);
		}
	}

	void EndReached(UnityEngine.Video.VideoPlayer vp)
	{
		if(!vp.isLooping) {
			ShowBG();	
		}

		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/end-reach");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}

		switch(paramRecording.state) {
			case EffectParamsRecording.State.Recording:
				StopRecording();
				break;
			case EffectParamsRecording.State.Playing:
				paramRecording.progress = 0;
				break;
		}

		if(vp.canSetTime) {
			var frame = (int)(vp.frameCount * loopingStartPos);
			if(frame > 0) {
				vp.Pause();
				vp.frame = frame;
				vp.Play();
			}
		}
	}

	void OnVideoStarted(UnityEngine.Video.VideoPlayer vp) {
		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/video-start");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}
	}

	void OnVideoPrepared(UnityEngine.Video.VideoPlayer vp) {
		vp.Play();
		bg.enabled = false;
		videoImage.enabled = true;
	}


	void ShowBG() {
		bg.enabled = true;
	}

	//Test functions
	public void onValChanged(System.Single val) {
		DeviceUtils.SetScreenBrightness((int)val);
	}
	
}
