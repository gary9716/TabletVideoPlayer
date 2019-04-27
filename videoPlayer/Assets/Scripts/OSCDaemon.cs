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
using Kino;
using Lean.Pool;

class OSCDaemon : MonoBehaviour
{
	public int oscPort = 9000;
	private OSCReciever reciever;
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

	private string targetServerIP;
	private int port;
	private OSCClient oscClient;

	VideoPlayer curPlayer = null;
	private string myIP;
	private RenderTexture videoTex;
	private Dictionary<string, VideoPlayer> playerCache = new Dictionary<string, VideoPlayer>();
	private Dictionary<string, Sprite> imgCache = new Dictionary<string, Sprite>();

	void Start()
	{
		videoTex = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
		noCachePlayer.targetTexture = videoTex;
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
		if(playerCache.ContainsKey(url)) {
			curPlayer = playerCache[url];
		}
		else {
			if(cache) {
				curPlayer = CacheVideoPlayer(url);
			}
			else {
				curPlayer = noCachePlayer;
				curPlayer.url = url;
			}
		}

		if(prevPlayer != null && prevPlayer.isPlaying) {
			prevPlayer.Pause();
			prevPlayer.frame = 0;
		}

		curPlayer.isLooping = isLooping;
		if(frame >= 0) curPlayer.frame = frame;
		curPlayer.Play();
		bg.enabled = false;
		videoImage.enabled = true;
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

	Coroutine animateImgRoutine;
	void ShowImage(string fileName, float duration, float minA, float maxA, int attackFuncIndex, int decayFuncIndex) {
		if(imgCache.ContainsKey(fileName)) {
			var img = imgCache[fileName];
			stillImage.sprite = img;
			if(animateImgRoutine != null) {
				StopCoroutine(animateImgRoutine);
				var color = stillImage.color;
				color.a = minA;
				stillImage.color = color;
			}
			animateImgRoutine = StartCoroutine(AnimateAlpha(stillImage, duration, minA, maxA, attackFuncIndex, decayFuncIndex));
		}
	}

	float stepFunc(float s, float e, float val) {
		if(val > 0) 
			return e;
		else 
			return s;
	}

	IEnumerator AnimateAlpha(Image img, float duration, float minA, float maxA, int attackFuncIndex, int decayFuncIndex = -1) {
		float halfDuration = duration / 2f;
		EasingFunction.Function easeFunc;

		var attackFunc = attackFuncIndex < 0? stepFunc:EasingFunction.GetEasingFunction((EasingFunction.Ease)attackFuncIndex);
		var decayFunc = decayFuncIndex < 0? stepFunc:EasingFunction.GetEasingFunction((EasingFunction.Ease)decayFuncIndex);
		float start = 0, end = 0;

		for(int i = 0;i < 2;i++) {
			if(i == 0) {
				easeFunc = attackFunc;
				start = minA;
				end = maxA;
			}
			else {
				easeFunc = decayFunc;
				start = maxA;
				end = minA;
			}
			float timer = 0;
			while(timer < halfDuration) {
				var progress = timer / halfDuration;
				var val = easeFunc(start, end, progress);
				var color = img.color;
				color.a = val;
				img.color = color;

				timer += Time.deltaTime;
				yield return null;
			}
		}
		
	}

	OSCClient CreateOSCClient(string ip, int port) {
		var client = new OSCClient(System.Net.IPAddress.Parse(ip), port);
		client.Connect();
		return client;
	}

	bool isRecording = false;
	EffectParamsRecording paramRecording;
	
	bool isPlayingRecord {
		get {
			return paramRecording != null;
		}
	}

	void Update () {
		//for(int k = 0;k < 5;k++)
			if(reciever.hasWaitingMessages()) {
				OSCMessage msg = reciever.getNextMessage();
				var dataList = msg.Data;
				int count = dataList.Count;
				var address = msg.Address;
				if(address.Equals("/play-video")) {
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
							PlayVideo(url, val == 1, false);
						else 
							PlayVideo(url, false, false);
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
								StopVideo();
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
					if(isPlayingRecord) return;
					if(count > 0) {
						int effectIndex = -1;
						if(int.TryParse(dataList[0].ToString(), out effectIndex)) {
							if(effectIndex >= 0 && effectIndex < effectList.Length) {
								var effect = effectList[effectIndex];
								float val = -1;
								for(int i = 1;i < count;) {
									var data = dataList[i].ToString();
									if(data.Equals("i") && (i + 2) < count) {
										int paramIndex = -1;
										if(int.TryParse(dataList[i + 1].ToString(), out paramIndex) && float.TryParse(dataList[i + 2].ToString(), out val)) {
											effect.SetParameter(paramIndex, val);
										}
										i+=3;
									} 
									else if(float.TryParse(data, out val)) {
										effect.SetParameter(i - 1, val);
										i++;
									}
								}
								effect.SetEffectVisibility(true);
							}
						}
					}
					else
					{
						return;
					}
				}
				else if(address.Equals("/record-params")) {
					isRecording = true;
					paramRecording = new EffectParamsRecording();
					if(curPlayer != null && curPlayer.url != null)
						PlayVideo(curPlayer.url, true, true);
				}
				else if(address.Equals("/show-image")) {
					if(count > 1) {
						var imgName = dataList[0].ToString();
						float duration = 0;
						int attackFuncIndex = (int)EasingFunction.Ease.EaseInQuint;
						int decayFuncIndex = -1;
						float minA = 0;
						float maxA = 1;
						if(float.TryParse(dataList[1].ToString(), out duration)) {
							if(count > 2)
								float.TryParse(dataList[2].ToString(), out minA);
							if(count > 3)
								float.TryParse(dataList[3].ToString(), out maxA);
							
							if(count > 4)
								int.TryParse(dataList[4].ToString(), out attackFuncIndex);
							if(count > 5)
								int.TryParse(dataList[5].ToString(), out decayFuncIndex);
							ShowImage(imgName, duration, minA, maxA, attackFuncIndex, decayFuncIndex);
						}
					}
					else return;
				}
				else if(address.Equals("/load-image")) {
					if(count > 0) {
						var imgName = dataList[0].ToString();
						var img = LoadSprite(imgName);
						if(img != null) Debug.Log("load image:" + imgName + " succeed");
						else {
							Debug.Log("load image:" + imgName + " failed");
							return;
						}
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
					for(int i = 0;i < count;i++) {
						int effectIndex = -1;
						if(int.TryParse(dataList[i].ToString(), out effectIndex)) {
							effectList[effectIndex].SetEffectVisibility(false);
						}
					}
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
				else if(address.Equals("/play-effect-anim")) {
					if(count > 0) {
						var jsonData = dataList[0].ToString();
						//parse jsonData
						var paramObj = JsonUtility.FromJson<EffectParamSeq>(jsonData);
						if(paramObj != null) {
							var index = paramObj.index;
							if(index >= 0 && index < effectList.Length) {
								effectList[index].Play(paramObj);
							}
						}
					}
					else return;
				}
				else if(address.Equals("/stop-effect-anim")) {
					if(count > 0) {
						var index = -1;
						if(int.TryParse(dataList[0].ToString(), out index)) {
							if(index >= 0 && index < effectList.Length) {
								effectList[index].Stop();
							}
						}
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
							PlayVideo(url, true, true, (int)(frame + frameRate * 0.1)); //100ms delay
						}
					}
					else return;
				}
			}
	}
	
	
	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate()
	{
		if(isRecording && curPlayer != null && curPlayer.isPlaying) {
			int index = 0;
			foreach(var effect in effectList) {
				if (effect.isEffectActive) {
					var effectParam = effect.GetCurrentParams();
					if(effectParam != null) {
						effectParam.videoFrame = curPlayer.frame;
						effectParam.index = index;
						paramRecording.seq.Add(effectParam);
					}
				}
				index++;
			}
		}
		else if(isPlayingRecord) {
			var curFrame = curPlayer.frame;
			var seq = paramRecording.seq;
			for(int i = paramRecording.progress; i < seq.Count;i++) {
				var param = seq[i];
				if(param.videoFrame > curFrame) {
					break;
				}
				else if(param.videoFrame == curFrame) {
					//apply param
					int effectIndex = param.index;
					effectList[effectIndex].SetParameter(param);
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
		isRecording = false;
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

		if(isRecording) {
			StopRecording();
		}

		if(paramRecording != null) {
			paramRecording.progress = 0;
		}
	}

	void ShowBG() {
		bg.enabled = true;
	}

	//Test functions
	public void onValChanged(System.Single val) {
		DeviceUtils.SetScreenBrightness((int)val);
	}

	/*
	IEnumerator DownloadAssetBundle(string url, System.Action<AssetBundle> cb) {
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url);
		www.downloadHandler = new DownloadHandlerAssetBundle(url, 0);
        yield return www.SendWebRequest();

        if(www.isNetworkError || www.isHttpError) {
			cb(null);
        }
        else {
			AssetBundle bundle = (www.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
			//SaveBundleToStorage(www.downloadHandler.data, Path.GetFileNameWithoutExtension(www.url));
			cb(bundle);
        }
    }

	public void TestBundleRetrieve() {
		var serverIP = "127.0.0.1";
		var bundleName = "glitchon";
		var url = string.Format("http://{0}/assets/Android/{1}", serverIP, bundleName);
		StartCoroutine(DownloadAssetBundle(url, (bundle) => {
			if(bundle != null) {
				StartCoroutine(PlayAnimInBundle(bundle, "testGlitch"));
			}
		}));
	}
	
	void StopAnim() {
		animPlayer.Stop();
	}

	IEnumerator PlayAnimInBundle(AssetBundle bundle, string clipName) {
		var loadAsset = bundle.LoadAssetAsync<AnimationClip>("Assets/Animations/GlitchOn.anim");
		yield return loadAsset;
		animPlayer.AddClip((AnimationClip)loadAsset.asset, clipName);
		animPlayer.Play(clipName);
		
		yield return new WaitForSeconds(5);
		StopAnim();
		//ResetAGlitch(0);

		yield return new WaitForSeconds(5);
		animPlayer.Play(clipName);
	}
	*/
	
}
