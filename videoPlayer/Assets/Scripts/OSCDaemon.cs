//#define EFFECT_TEST
//#define NO_DEBUG_SHOW
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
	public Camera cam;
	public VideoPlayer noCachePlayer;
	public VideoPlayer playerPrefab;
	public Transform playerRoot;
	public RawImage videoImage;
	public Image stillImage;
	public KTEffectBase[] effectList;
	public Canvas txtCanvas;

	public TextManager txtManager;

	public ParamPatternSet patternSet;

	private OSCReciever reciever;
	private string targetServerIP;
	private int port;
	private OSCClient oscClient;
	VideoPlayer curPlayer = null;
	private string myIP;

	private RenderTexture _videoTex;
	private RenderTexture videoTex {
		set {
			_videoTex = value;
			noCachePlayer.targetTexture = _videoTex;
			videoImage.texture = _videoTex;
		}

		get {
			return _videoTex;
		}
	}
	private Dictionary<string, VideoPlayer> playerCache = new Dictionary<string, VideoPlayer>();
	private Dictionary<string, Sprite> imgCache = new Dictionary<string, Sprite>();

	
	private float loopingStartPos = -1;
	private float loopingStopPos = -1;

	private float firstPlayStartPos = 0;
	private float firstPlayStopPos = -1;

	private long stopFrame = -1;

	public TextAsset dancerList;
	private string[] dancerNames;
	
	EffectParamsRecording paramRecording = new EffectParamsRecording();
	
	bool isPlayingRecord {
		get {
			return paramRecording.state == EffectParamsRecording.State.Playing && curPlayer != null && curPlayer.isPlaying;
		}
	}

	void Start()
	{
		dancerNames = dancerList.text.Split('\n');
		videoTex = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
		
		noCachePlayer.loopPointReached += EndReached;
		noCachePlayer.started += OnVideoStarted;
		noCachePlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;

		cam = GetComponent<Camera>();
		if(reciever == null) {
			reciever = new OSCReciever();
			reciever.Open(oscPort);
		}

		info.enabled = false;
		myIP = GetLocalIPAddress();
		StartCoroutine(ShowInfoForFixedDuration("v" + Application.version + ";" + myIP, 10, 60, 1));
		DeviceUtils.SetScreenBrightness(255);

		#if EFFECT_TEST && UNITY_EDITOR
		
		#endif
	}

	IEnumerator ShowInfoForFixedDuration(string msg,float duration,int size, int level = 0) {
		#if NO_DEBUG_SHOW
		if(level == 0) yield break;
		#endif

		info.fontSize = size;
		float timer = 0;
		info.enabled = true;
		info.text = msg;
		while(timer < duration) {
			yield return null;
			timer += Time.deltaTime;
		}

		info.enabled = false;
	}

	IEnumerator DownloadFileToPath(string url, string path, System.Action cb) {
		var interval = new WaitForSeconds(0.1f);
		using(UnityWebRequest req = UnityWebRequest.Get(url)) {
			if(req == null) {
				info.fontSize = 25;
				info.text = "url is invalid";
				yield break;
			}
			req.downloadHandler =  new DownloadHandlerFile(path);
			req.SendWebRequest();
			info.enabled = true;
			while(!req.isDone && !req.isNetworkError && !req.isHttpError) {
				info.fontSize = 25;
				info.text = "Progress:" + ((int)(req.downloadProgress * 100));
				yield return interval;
			}
			info.text = "";
			
			if (req.isNetworkError || req.isHttpError) {
				info.fontSize = 25;
				info.text = req.error + ",http server not available?";	
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
			StartCoroutine(ShowInfoForFixedDuration("no " + fileName, 3, 30));
			return null;
		}

		var imgBytes = File.ReadAllBytes(imgPath);
		Texture2D tex = new Texture2D(1280, 720);
		tex.LoadImage(imgBytes, false);
		if(oscClient != null) {
			var msg = new OSCMessage("/image-loaded");
			msg.Append(myIP);
			msg.Append(fileName);
			oscClient.Send(msg);
		}

		return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
	}

	VideoPlayer CacheVideoPlayer(string url) {
		var newPlayer = LeanPool.Spawn<VideoPlayer>(playerPrefab);
		newPlayer.url = url;
		newPlayer.loopPointReached += EndReached;
		newPlayer.started += OnVideoStarted;
		newPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
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
			player.targetTexture = null;
			player.Stop();
			player.name = "VideoPlayer(Cloned)";
			LeanPool.Despawn(player);	
		}
	}

	void LoadVideo(string url, bool isLooping, bool cache, VideoPlayer.EventHandler cb) {
		var prevPlayer = curPlayer;
		if(!cache) {
			curPlayer = noCachePlayer;
			if(!curPlayer.url.Equals(url))
				curPlayer.url = url;
		}
		else if(playerCache.ContainsKey(url)) {
			curPlayer = playerCache[url];
			curPlayer.targetTexture = videoTex;
		}
		else {
			curPlayer = CacheVideoPlayer(url);
			curPlayer.targetTexture = videoTex;
		}

		if(prevPlayer != null && prevPlayer.isPlaying) {
			prevPlayer.Pause();
			prevPlayer.frame = 0;
		}

		curPlayer.isLooping = isLooping;
		if(!curPlayer.isPrepared) {
			if(cb != null) {
				curPlayer.prepareCompleted += cb;
			}
			curPlayer.Prepare();
		}
		else if(cb != null)
			cb(curPlayer);
	}

	void PlayVideo(string url, bool isLooping, bool cache) {
		LoadVideo(url, isLooping, cache, OnVideoPrepared);
	}

	Coroutine downloadRoutine;

	void StopVideo() {
		if(curPlayer != null) {
			curPlayer.Pause();
			curPlayer.frame = 0;
		}
		videoImage.enabled = false;
	}

	void CloseAll() {
		txtCanvas.enabled = false;
		stillImage.enabled = false;
		info.enabled = false;
		StopVideo();
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

	public class Command {
		public string name;
		public List<System.Object> parameters;
	}

	private List<Command> cmdList = new List<Command>();
	private enum CmdListMode {
		SingleCmdMode = -1,
		OneTime = 0,
		Loop = 1,
	}

	CmdListMode cmdListMode = CmdListMode.SingleCmdMode;
	private int cmdProgress = -1;
	private List<object> dataBuf = new List<object>();

	private void DisableAllEffect() {
		for(int i = 0;i < effectList.Length;i++) {
			effectList[i].SetEffectActive(false);
		}
	}

	private string GetTextFilePath(string fileName) {
		return Path.Combine(Application.persistentDataPath, fileName);
	}

	void ExecuteOSCCmd(string address, List<System.Object> dataList) {
		int count = dataList.Count;

		if(address.Equals("/play-video")) {
			if(count > 0) {
				ResetVideoRelatedParams();
				var url = dataList[0].ToString();	
				if(!url.Contains("http")) {
					if(url.Equals("-1")) {
						StopVideo();
						return;
					}
					var fileName = Path.GetFileName(url);
					var fullPath = Path.Combine(Application.persistentDataPath, fileName);
					url = "file://" + fullPath;
					if(!File.Exists(fullPath)) {
						StartCoroutine(ShowInfoForFixedDuration("no " + fileName, 3, 40));
						return;
					}
				}

				var isLooping = false;
				int val = 0;
				if(count > 1) 
					float.TryParse(dataList[1].ToString(), out firstPlayStartPos);
				
				if(count > 2) 
					float.TryParse(dataList[2].ToString(), out firstPlayStopPos);
				
				if(count > 3 && int.TryParse(dataList[3].ToString(), out val))
					isLooping = val == 1;

				if(count > 4)
					float.TryParse(dataList[4].ToString(), out loopingStartPos);

				if(count > 5)
					float.TryParse(dataList[5].ToString(), out loopingStopPos);
				
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
			ResetVideoRelatedParams();
			if(curPlayer != null && !curPlayer.isPlaying) {
				if(count > 0) {
					float.TryParse(dataList[0].ToString(), out firstPlayStopPos);
					if(firstPlayStopPos > 0) stopFrame = (long)(curPlayer.frameCount * firstPlayStopPos);
				}
				
				if(count > 1)
					float.TryParse(dataList[1].ToString(), out loopingStartPos);

				if(count > 2)
					float.TryParse(dataList[2].ToString(), out loopingStopPos);
				curPlayer.Play();
			}
		}
		else if(address.Equals("/load-video")) {
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
					if(!File.Exists(fullPath)) {
						StartCoroutine(ShowInfoForFixedDuration("no " + fileName, 3, 40));
						return;
					}
				}

				LoadVideo(url, true, false, OnVideoLoaded);
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
					if(!fileExist) {
						StartCoroutine(ShowInfoForFixedDuration("download failed", 3, 40));
						return;
					}

					if(testPlay) {
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
				info.enabled = !info.enabled;
				if(info.enabled) {
					info.fontSize = 90;
					int index = -1;
					int.TryParse(dataList[0].ToString(), out index);

					if(index >= 0 && index < dancerNames.Length) {
						info.text = dancerNames[index];
					}
					else {
						info.text = dataList[0].ToString();
					}
				}
			}
			else
			{
				return;
			}
		}
		else if(address.Equals("/set-effect-param")) {
			if(count > 0) {
				int effectIndex = -1;
				if(int.TryParse(dataList[0].ToString(), out effectIndex)) {
					if(effectIndex >= 0 && effectIndex < effectList.Length) {
						bool toRecord = false;
						int temp = 0;
						if(count > 1) {
							int.TryParse(dataList[1].ToString(), out temp);
							toRecord = temp == 1;
						
							EffectParamsRecording.EffectParams paramFrame = null;
							if(toRecord && paramRecording.state == EffectParamsRecording.State.Recording && curPlayer != null && curPlayer.isPlaying) {
								for(int i = 0;i < paramRecording.seq.Count;i++) {
									var frameData = paramRecording.seq[i];
									if(frameData.effectIndex == effectIndex && frameData.videoFrame == curPlayer.frame) {
										paramFrame = frameData;
										break;
									}
								}

								if(paramFrame == null) {
									paramFrame = new EffectParamsRecording.EffectParams();
									paramRecording.seq.Add(paramFrame);
									paramFrame.effectIndex = effectIndex;
									paramFrame.videoFrame = curPlayer.frame;
								}
							}

							var effect = effectList[effectIndex];
							float val = -1;
							int paramIndex = -1;

							for(int i = 2;i < count;) {
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
								else {
									i++;
								}
							}
							effect.SetEffectActive(true);
						}
					}
					else {
						StartCoroutine(ShowInfoForFixedDuration("effectIndex out of bound", 3, 25));
					}
				}
			}
			else
			{
				return;
			}
		}
		else if(address.Equals("/trigger-effect")) {
			if(count > 0 && patternSet != null) {
				int effectIndex = -1;
				if(int.TryParse(dataList[0].ToString(), out effectIndex)) {
					if(effectIndex >= 0 && effectIndex < effectList.Length) {
						paramRecording.Reset();
						float duration = 1;
						float.TryParse(dataList[1].ToString(), out duration);

						var effect = effectList[effectIndex];
						effect.ResetTriggerData();
						effect.SetEffectRoutineDuration(duration);
						List<object> funcParamBuf = new List<object>();
						for(int i = 2;i < count;) {
							int paramIndex = -1, patternID = -1, funcParamCount = -1;	
							funcParamBuf.Clear();	
							int.TryParse(dataList[i].ToString(), out paramIndex);
							int.TryParse(dataList[i+1].ToString(), out patternID);
							int.TryParse(dataList[i+2].ToString(), out funcParamCount);
							if(paramIndex >= 0 && patternID >= 0 && funcParamCount > 0) {
								for(int k = 0;k < funcParamCount;k++) {
									funcParamBuf.Add(dataList[i+3+k].ToString());
								}
								var paramFunc = ParamPattern.GetFunc(
									(ParamPattern.PatternType)patternID,
									funcParamBuf);
								effect.AddData(paramIndex, paramFunc);
								i = i + 3 + funcParamCount;
							}
							else {
								i += 3;
							}
						}

						effect.TriggerEffect();
					}
					else {
						StartCoroutine(ShowInfoForFixedDuration("effectIndex out of bound", 3, 25));
					}
				}
			}
			else {
				return;
			}
		}
		else if(address.Equals("/record-params")) {
			int policy = 0;
			if(count > 0) {
				int.TryParse(dataList[0].ToString(), out policy);
			}

			paramRecording.Reset();
			paramRecording.state = EffectParamsRecording.State.Recording;
			
			if(curPlayer != null && curPlayer.url != null) {
				switch(policy) {
					case 0: //replay
						if(curPlayer.isPlaying) PlayVideo(curPlayer.url, true, false);
						break;
					case 1: //no replay
						if(!curPlayer.isPlaying) PlayVideo(curPlayer.url, true, false);
						break;
					case 2:
						//no action
						break;
				}
				
			}
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

				if(count > 1) {
					float alpha = 0;
					if(float.TryParse(dataList[1].ToString(), out alpha)) {
						var color = stillImage.color;
						color.a = alpha;
						stillImage.color = color;
						stillImage.enabled = true;
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
					DisableAllEffect();
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
		else if(address.Equals("/close-all")) {
			CloseAll();
		}
		else if(address.Equals("/show-text")) {
			if(count > 5) {
				TextManager.SentenceData sd = null;
				string content = dataList[0].ToString();
				
				if(content.Equals("-1")) {
					sd = txtManager.GetRandSentenceData();
				}
				else {
					sd = new TextManager.SentenceData();
					sd.main = content;
				}
				TextManager.Orientation orient = TextManager.Orientation.Horizontal;
				int fontSize = 28;
				float posX = 0.5f;
				float posY = 0.5f;
				int effectIndex = 0;

				int val = 0;
				if(int.TryParse(dataList[1].ToString(), out val)) {
					orient = (TextManager.Orientation)val;
				}
				if(count > 2)
					int.TryParse(dataList[2].ToString(), out fontSize);
				
				if(count > 3)
					float.TryParse(dataList[3].ToString(), out posX);
				
				if(count > 4)
					float.TryParse(dataList[4].ToString(), out posY);
				
				if(count > 5)
					int.TryParse(dataList[5].ToString(), out effectIndex);

				dataBuf.Clear();
				for(int i = 6;i < count;i++) {
					dataBuf.Add(dataList[i].ToString());
				}

				txtManager.ShowText(sd, orient, fontSize, posX, posY, effectIndex, dataBuf);
			}
			else {
				txtManager.ShowText();
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
		else if(address.Equals("/set-bg-color")) {
			if(count > 0) {
				float val = 0;
				Color color = Color.white;
				int i = 0;
				foreach(var valStr in dataList) {
					if(float.TryParse(valStr.ToString(), out val)) {
						if(i == 0)
							color.r = val;
						else if(i == 1) {
							color.g = val;
						}
						else if(i == 2) {
							color.b = val;
						}
						else {
							color.a = val;
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
				if(loopingStartPos > 1) loopingStartPos = 1;
			}
			else return;
		}
		else if(address.Equals("/get-battery-level")) {
			if(oscClient != null) {
				if(oscClient != null) {
					OSCMessage message = new OSCMessage("/battery-level");
					message.Append(SystemInfo.batteryLevel);
					oscClient.Send(message);
				}
			}
		}
		else if(address.Equals("/set-text-effect-sentences-file")) {
			if(count > 0) {
				string path = GetTextFilePath(dataList[0].ToString());
				if(!File.Exists(path)) {
					StartCoroutine(ShowInfoForFixedDuration("no such text file", 3, 25));
					return;
				}
				else {
					PlayerPrefs.SetString(TextManager.sentenceInputPathKey, path);
					txtManager.ProcessInputData(File.ReadAllText(path));
				}
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
	}

	void Update () {
		if(reciever.hasWaitingMessages()) {
			OSCMessage msg = reciever.getNextMessage();
			var dataList = msg.Data;
			var address = msg.Address;

			//parse
			if(address.Equals("/cmd-list")) {
				
				int val;
				if(int.TryParse(dataList[0].ToString(), out val)) {
					cmdProgress = 0;
					Command cmd = null;
					cmdList.Clear();

					cmdListMode = (CmdListMode)val;
					if(cmdListMode != CmdListMode.SingleCmdMode) {
						int count = dataList.Count;
						for(int i = 1;i < count;i++) {
							var data = dataList[i].ToString();
							if(data.StartsWith("/")) {
								cmd = new Command();
								cmd.parameters = new List<System.Object>();
								cmd.name = data;
								cmdList.Add(cmd);
							}
							else if(cmd.parameters != null) {
								cmd.parameters.Add(data);
							}
						}

						DoNextCmd();
					}
				}
			}
			else {
				cmdListMode = CmdListMode.SingleCmdMode;
				ExecuteOSCCmd(address, dataList);
			}
		}
		
		if(isPlayingRecord) {
			var curFrame = curPlayer.frame;
			var seq = paramRecording.seq;
			for(int i = paramRecording.progress; i < seq.Count;i++) {
				var param = seq[i];
				if(param.videoFrame > curFrame) {
					paramRecording.progress = i;
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

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate()
	{
		if(stopFrame > 0 && curPlayer != null && curPlayer.isPlaying && curPlayer.frame >= stopFrame) {
			EndReached(curPlayer);
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

	void DoNextCmd() {
		if(cmdProgress < cmdList.Count) {
			var cmd = cmdList[cmdProgress];
			ExecuteOSCCmd(cmd.name, cmd.parameters);
			cmdProgress++;
		}
		else {
			if(cmdListMode == CmdListMode.Loop)
				cmdProgress = 0;
		}
	}

	void EndReached(UnityEngine.Video.VideoPlayer vp)
	{
		if(!vp.isLooping) {
			if(cmdListMode != CmdListMode.SingleCmdMode)
				DoNextCmd();
			else //default 
				StopVideo();
		}
		else {
			if(loopingStartPos > 0) {
				long loopingStartFrame = (long)(vp.frameCount * loopingStartPos);
				vp.frame = loopingStartFrame;
			}
			else {
				vp.frame = 0;
			}

			vp.Play();
			stopFrame = loopingStopPos > 0? (long)(loopingStopPos * vp.frameCount) : -1;
		}

		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/end-reach");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}

		//set parameter recorder
		switch(paramRecording.state) {
			case EffectParamsRecording.State.Recording:
				StopRecording();
				break;
			case EffectParamsRecording.State.Playing:
				paramRecording.progress = 0;
				break;
		}

	}

	void ResetVideoRelatedParams() {
		loopingStartPos = -1;
		loopingStopPos = -1;
		firstPlayStartPos = 0;
		firstPlayStopPos = -1;
		stopFrame = -1;
	}

	void OnVideoStarted(UnityEngine.Video.VideoPlayer vp) {
		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/video-start");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}
	}

	void OnVideoLoaded(UnityEngine.Video.VideoPlayer vp) {
		if(oscClient != null) {
			OSCMessage message = new OSCMessage("/video-loaded");
			message.Append(myIP);
			message.Append(Path.GetFileName(vp.url));
			oscClient.Send(message);
		}
	}

	void OnVideoPrepared(UnityEngine.Video.VideoPlayer vp) {
		if(firstPlayStartPos > 0)
			vp.frame = (long)(firstPlayStartPos * vp.frameCount);
		else
			vp.frame = 0;

		if(firstPlayStopPos > 0)
			stopFrame = (long)(firstPlayStopPos * vp.frameCount);
		else
			stopFrame = -1;

		vp.Play();
		videoImage.enabled = true;
	}

	//Test functions
	public void onValChanged(System.Single val) {
		DeviceUtils.SetScreenBrightness((int)val);
	}
	
}
