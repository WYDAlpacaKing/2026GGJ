using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Alpaca.Game.Audio
{
    public class MusicMgr : BaseMonoMgr<MusicMgr>
    {
        [Header("音频数据库")]
        [Tooltip("拖拽AudioDatabase资源来配置音频")]
        [SerializeField] private AudioDatabase audioDatabase;

        // --- 内部状态存储 Key ---
        private const string PREF_MUSIC_VOL = "MusicMgr_MusicVol";
        private const string PREF_SOUND_VOL = "MusicMgr_SoundVol";
        private const string PREF_MUSIC_ON = "MusicMgr_MusicOn";
        private const string PREF_SOUND_ON = "MusicMgr_SoundOn";

        [SerializeField, HideInInspector]
        private AudioSource bgMusic = null;
        // 第二路用于交叉淡入淡出
        [SerializeField, HideInInspector]
        private AudioSource bgMusicAlt = null;

        private Coroutine bgmCrossfadeRoutine = null;
        private AudioSource activeBgm = null;    // 当前活动通道
        private AudioSource inactiveBgm = null;  // 备用通道

        // 公开属性供外部（如UI）读取状态
        public float MusicVolume { get; private set; } = 1f;
        public float SoundVolume { get; private set; } = 1f;
        public bool IsMusicOn { get; private set; } = true;
        public bool IsSoundOn { get; private set; } = true;


        private GameObject soundObj = null;
        private List<AudioSource> soundList = new List<AudioSource>();
        private Dictionary<string, List<AudioSource>> soundDict = new Dictionary<string, List<AudioSource>>();// 添加字典来存储音效名字和AudioSource的映射
        private Dictionary<AudioID, List<AudioSource>> soundIDDict = new Dictionary<AudioID, List<AudioSource>>(); // 添加字典来存储AudioID和AudioSource的映射
        private bool isInitialized = false;  // 标记是否已初始化


        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        private void Update()
        {
            // 倒序遍历移除播放结束的音效
            for (int i = soundList.Count - 1; i >= 0; --i)
            {
                if (soundList[i] == null || !soundList[i].isPlaying)
                {
                    if (soundList[i] != null)
                    {
                        RemoveFromDict(soundList[i]);
                        RemoveFromIDDict(soundList[i]);
                        Destroy(soundList[i]); // 销毁组件
                    }
                    soundList.RemoveAt(i);
                }
            }
        }

        private void EnsureBgSources()
        {
            if (bgMusic == null)
            {
                GameObject obj = new GameObject("BgMusic");
                obj.transform.SetParent(this.transform, false);
                bgMusic = obj.AddComponent<AudioSource>();
                bgMusic.loop = true;
                bgMusic.playOnAwake = false;
            }
            if (bgMusicAlt == null)
            {
                GameObject obj2 = new GameObject("BgMusic_Alt");
                obj2.transform.SetParent(this.transform, false);
                bgMusicAlt = obj2.AddComponent<AudioSource>();
                bgMusicAlt.loop = true;
                bgMusicAlt.playOnAwake = false;
            }

            if (activeBgm == null || inactiveBgm == null)
            {
                activeBgm = bgMusic;
                inactiveBgm = bgMusicAlt;
            }

            // 应用当前的静音状态
            bgMusic.mute = !IsMusicOn;
            bgMusicAlt.mute = !IsMusicOn;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (isInitialized) return;

            Debug.Log($"[MusicMgr] 初始化...");

            // 1. 初始化数据库
            if (audioDatabase != null)
            {
                audioDatabase.Initialize();
            }
            else
            {
                Debug.LogError("[MusicMgr] AudioDatabase 未设置！");
            }

            // 2. 加载本地配置 (替代 DataManager)
            LoadLocalSettings();

            // 3. 应用配置到现有组件
            if (bgMusic != null)
            {
                bgMusic.volume = MusicVolume;
                bgMusic.mute = !IsMusicOn;
            }

            isInitialized = true;
        }

        private void LoadLocalSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 1f);
            SoundVolume = PlayerPrefs.GetFloat(PREF_SOUND_VOL, 1f);
            // PlayerPrefs 不支持 bool，用 int 0/1 模拟
            IsMusicOn = PlayerPrefs.GetInt(PREF_MUSIC_ON, 1) == 1;
            IsSoundOn = PlayerPrefs.GetInt(PREF_SOUND_ON, 1) == 1;
        }

        private void SaveLocalSettings()
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
            PlayerPrefs.SetFloat(PREF_SOUND_VOL, SoundVolume);
            PlayerPrefs.SetInt(PREF_MUSIC_ON, IsMusicOn ? 1 : 0);
            PlayerPrefs.SetInt(PREF_SOUND_ON, IsSoundOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 从字符串字典中移除指定的AudioSource
        /// </summary>
        private void RemoveFromDict(AudioSource source)
        {
            foreach (var pair in soundDict)
            {
                if (pair.Value.Contains(source))
                {
                    pair.Value.Remove(source);
                    if (pair.Value.Count == 0)
                    {
                        soundDict.Remove(pair.Key);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 从ID字典中移除指定的AudioSource
        /// </summary>
        private void RemoveFromIDDict(AudioSource source)
        {
            foreach (var pair in soundIDDict)
            {
                if (pair.Value.Contains(source))
                {
                    pair.Value.Remove(source);
                    if (pair.Value.Count == 0)
                    {
                        soundIDDict.Remove(pair.Key);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// ���ű�������
        /// </summary>
        /// <param name="name">��Ƶ����</param>
        /// <summary>
        /// 播放背景音乐（使用AudioID - 推荐方式）
        /// </summary>
        /// <param name="audioID">音频ID</param>
        public void PlayBgMusic(AudioID audioID)
        {
            if (audioDatabase == null) return;
            AudioClip clip = audioDatabase.GetClip(audioID);
            if (clip != null) PlayBgMusicWithClip(clip);
        }

        private void PlayBgMusicWithClip(AudioClip clip)
        {
            if (!isInitialized) Init();
            EnsureBgSources();

            if (bgmCrossfadeRoutine != null)
            {
                StopCoroutine(bgmCrossfadeRoutine);
                bgmCrossfadeRoutine = null;
            }

            // 重置备用通道
            if (inactiveBgm != null)
            {
                inactiveBgm.Stop();
                inactiveBgm.clip = null;
                inactiveBgm.volume = 0f;
            }

            activeBgm.clip = clip;
            activeBgm.volume = MusicVolume; // 使用内部状态
            activeBgm.mute = !IsMusicOn;    // 使用内部状态
            activeBgm.loop = true;
            activeBgm.Play();
        }

        /// <summary>
        /// 交叉淡入淡出到指定 BGM（AudioID）
        /// </summary>
        public void CrossfadeBgMusic(AudioID audioID, float duration = 1f)
        {
            if (audioDatabase == null) return;
            AudioClip clip = audioDatabase.GetClip(audioID);
            if (clip != null) CrossfadeBgMusicWithClip(clip, duration);
        }



        private void CrossfadeBgMusicWithClip(AudioClip newClip, float duration)
        {
            if (!isInitialized) Init();
            EnsureBgSources();

            // 这里的逻辑微调：确保目标音量是当前的 MusicVolume
            inactiveBgm.Stop();
            inactiveBgm.clip = newClip;
            inactiveBgm.volume = 0f;
            inactiveBgm.mute = !IsMusicOn;
            inactiveBgm.loop = true;
            inactiveBgm.Play();

            if (bgmCrossfadeRoutine != null) StopCoroutine(bgmCrossfadeRoutine);
            bgmCrossfadeRoutine = StartCoroutine(CrossfadeRoutine(activeBgm, inactiveBgm, duration));
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float duration)
        {
            if (duration <= 0f) duration = 0.01f;
            float startFrom = (from != null) ? from.volume : 0f;
            float targetTo = MusicVolume; // 目标音量基于当前设置

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                if (from != null) from.volume = Mathf.Lerp(startFrom, 0f, k);
                to.volume = Mathf.Lerp(0f, targetTo, k);
                yield return null;
            }

            if (from != null)
            {
                from.volume = 0f;
                from.Stop();
                from.clip = null;
            }
            to.volume = targetTo;

            var tmp = activeBgm;
            activeBgm = to;
            inactiveBgm = tmp;
            bgmCrossfadeRoutine = null;
        }


        /// <summary>
        /// 改变背景音乐音量（同时更新 DataManager）
        /// </summary>
        public void ChangeBgValue(float v)
        {
            MusicVolume = v;
            SaveLocalSettings(); // 保存

            // 实时应用
            if (bgmCrossfadeRoutine == null)
            {
                if (activeBgm != null) activeBgm.volume = MusicVolume;
            }
            // 如果正在 Crossfade，协程里会读取最新的 MusicVolume，所以不用担心
        }


        /// <summary>
        /// 设置背景音乐开关（同时更新 DataManager）
        /// </summary>
        public void SetMusicOpen(bool isOpen)
        {
            IsMusicOn = isOpen;
            SaveLocalSettings(); // 保存

            if (bgMusic != null) bgMusic.mute = !isOpen;
            if (bgMusicAlt != null) bgMusicAlt.mute = !isOpen;
        }

        public void ChangeSoundValue(float value)
        {
            SoundVolume = value;
            SaveLocalSettings(); // 保存

            // 实时更新所有正在播放的音效
            for (int i = 0; i < soundList.Count; i++)
            {
                if (soundList[i] != null) soundList[i].volume = SoundVolume;
            }
        }

        public void SetSoundOpen(bool isOpen)
        {
            IsSoundOn = isOpen;
            SaveLocalSettings(); // 保存

            for (int i = 0; i < soundList.Count; i++)
            {
                if (soundList[i] != null) soundList[i].mute = !isOpen;
            }
        }

        /// <summary>
        /// ��ͣ��������
        /// </summary>
        public void PauseBgMusic()
        {
            if (bgMusic == null)
                return;
            bgMusic.Pause();
        }

        /// <summary>
        /// ֹͣ��������
        /// </summary>
        /// <param name="name">��Ƶ����</param>
        public void StopBgMusic()
        {
            if (bgmCrossfadeRoutine != null)
            {
                StopCoroutine(bgmCrossfadeRoutine);
                bgmCrossfadeRoutine = null;
            }
            EnsureBgSources();
            if (activeBgm != null)
            {
                activeBgm.Stop();
                activeBgm.clip = null;
                activeBgm.volume = 0f;
            }
            if (inactiveBgm != null)
            {
                inactiveBgm.Stop();
                inactiveBgm.clip = null;
                inactiveBgm.volume = 0f;
            }
        }



        /// <summary>
        /// 播放音效（使用AudioID - 推荐方式）
        /// </summary>
        /// <param name="audioID">音频ID</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="callback">播放完成回调</param>
        public void PlaySound(AudioID audioID, bool isLoop = false, UnityAction<AudioSource> callback = null)
        {
            if (audioDatabase == null)
            {
                Debug.LogError("[MusicMgr] AudioDatabase 未设置！无法使用 AudioID 播放");
                return;
            }

            AudioClip clip = audioDatabase.GetClip(audioID);
            if (clip == null)
            {
                Debug.LogWarning($"[MusicMgr] 无法获取音频ID {audioID} 对应的Clip");
                return;
            }

            PlaySoundWithClip(clip, audioID, isLoop, callback);
        }


        /// <summary>
        /// 使用AudioClip直接播放音效（内部方法）
        /// </summary>
        private void PlaySoundWithClip(AudioClip clip, AudioID audioID, bool isLoop, UnityAction<AudioSource> callback)
        {
            if (soundObj == null) soundObj = new GameObject("Sound_Root");

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = isLoop;
            source.volume = SoundVolume; // 使用属性
            source.mute = !IsSoundOn;    // 使用属性
            source.Play();

            soundList.Add(source);
            if (!soundIDDict.ContainsKey(audioID)) soundIDDict[audioID] = new List<AudioSource>();
            soundIDDict[audioID].Add(source);

            if (callback != null) callback(source);
        }

        /// <summary>
        /// ֹͣ��Ч
        /// </summary>
        public void StopSound(AudioSource source)
        {
            if (soundList.Contains(source))
            {
                soundList.Remove(source);
                RemoveFromDict(source);
                source.Stop();
                GameObject.Destroy(source);
            }
        }

        /// <summary>
        /// ֹͽ��Ч（通过音效名字）- 停止所有该名字的音效
        /// </summary>
        /// <param name="name">音效名字</param>
        public void StopSoundByName(string name)
        {
            if (soundDict.ContainsKey(name))
            {
                List<AudioSource> sources = new List<AudioSource>(soundDict[name]);
                foreach (AudioSource source in sources)
                {
                    if (source != null)
                    {
                        soundList.Remove(source);
                        source.Stop();
                        GameObject.Destroy(source);
                    }
                }
                soundDict.Remove(name);
            }
        }

        /// <summary>
        /// ֹͽ��Ч（通过音效名字）- 只停止第一个该名字的音效
        /// </summary>
        /// <param name="name">音效名字</param>
        /// <summary>
        /// 停止音效（通过AudioID）- 只停止第一个该ID的音效
        /// </summary>
        /// <param name="audioID">音频ID</param>
        public void StopFirstSoundByID(AudioID audioID)
        {
            if (soundIDDict.ContainsKey(audioID) && soundIDDict[audioID].Count > 0)
            {
                AudioSource source = soundIDDict[audioID][0];
                if (source != null)
                {
                    soundList.Remove(source);
                    RemoveFromDict(source);  // 可能同时存在字符串字典中
                    soundIDDict[audioID].Remove(source);
                    if (soundIDDict[audioID].Count == 0)
                    {
                        soundIDDict.Remove(audioID);
                    }
                    source.Stop();
                    GameObject.Destroy(source);
                }
            }
        }

        /// <summary>
        /// ֹͽЧ（通过音效名字）- 只停止第一个该名字的音效
        /// </summary>
        /// <param name="name">音效名字</param>
        public void StopFirstSoundByName(string name)
        {
            if (soundDict.ContainsKey(name) && soundDict[name].Count > 0)
            {
                AudioSource source = soundDict[name][0];
                if (source != null)
                {
                    soundList.Remove(source);
                    RemoveFromIDDict(source);  // 可能同时存在ID字典中
                    soundDict[name].Remove(source);
                    if (soundDict[name].Count == 0)
                    {
                        soundDict.Remove(name);
                    }
                    source.Stop();
                    GameObject.Destroy(source);
                }
            }
        }
    }
}
