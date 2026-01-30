using System.Collections.Generic;
using UnityEngine;

namespace Alpaca.Game.Audio
{
    /// <summary>
    /// 音频数据库 - 集中管理所有音频配置
    /// 在Inspector中可以拖拽配置每个AudioID对应的AudioClip
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [Header("音频配置列表")]
        [Tooltip("在此配置所有音频ID对应的AudioClip或路径")]
        public List<AudioConfig> audioConfigs = new List<AudioConfig>();

        private Dictionary<AudioID, AudioConfig> _configDict;
        private bool _isInitialized = false;

        /// <summary>
        /// 初始化字典（延迟初始化，首次访问时自动调用）
        /// </summary>
        public void Initialize()
        {
            // 如果已初始化且字典不为空，跳过
            if (_isInitialized && _configDict != null)
            {
                Debug.Log("[AudioDatabase] 已经初始化过，跳过");
                return;
            }

            // 如果之前初始化失败（_isInitialized 为 true 但 _configDict 为 null），强制重新初始化
            if (_isInitialized && _configDict == null)
            {
                Debug.LogWarning("[AudioDatabase] 检测到初始化状态不一致（_isInitialized=true 但 _configDict=null），强制重新初始化");
                _isInitialized = false;
            }

            _configDict = new Dictionary<AudioID, AudioConfig>();

            // 确保列表非 null；若为空则继续以空列表初始化，避免后续 _configDict 为 null
            if (audioConfigs == null)
            {
                Debug.LogWarning("[AudioDatabase] audioConfigs 列表为 null！将使用空列表继续初始化（请在资源中添加配置）");
                audioConfigs = new List<AudioConfig>();
            }
            else if (audioConfigs.Count == 0)
            {
                Debug.LogWarning("[AudioDatabase] audioConfigs 列表为空！请至少添加一个音频配置");
            }

            int loadedCount = 0;
            foreach (var config in audioConfigs)
            {
                if (config == null)
                {
                    Debug.LogWarning("[AudioDatabase] 发现空的音频配置项，已跳过");
                    continue;
                }

                if (!_configDict.ContainsKey(config.id))
                {
                    
                    _configDict[config.id] = config;
                    loadedCount++;
                }
                else
                {
                    Debug.LogWarning($"[AudioDatabase] 重复的音频ID: {config.id}，将使用第一个配置");
                }
            }

            _isInitialized = true;
            Debug.Log($"[AudioDatabase] 初始化完成，共加载 {loadedCount}/{audioConfigs.Count} 个音频配置，字典中有 {_configDict.Count} 个条目");

            // 最终确认 _configDict 不为 null
            if (_configDict == null)
            {
                Debug.LogError("[AudioDatabase] 严重错误：Initialize() 执行后 _configDict 仍为 null！");
                _configDict = new Dictionary<AudioID, AudioConfig>(); // 强制创建一个空字典
            }
        }

        /// <summary>
        /// 根据ID获取音频配置
        /// </summary>
        public AudioConfig GetConfig(AudioID id)
        {
            // 如果未初始化或字典为 null，执行初始化
            if (!_isInitialized || _configDict == null)
            {
                Initialize();
            }

            // 再次检查（防止初始化失败）
            if (_configDict == null)
            {
                Debug.LogError("[AudioDatabase] _configDict 仍为 null，无法获取配置。这不应该发生！");
                return null;
            }

            if (_configDict.TryGetValue(id, out AudioConfig config))
            {
                return config;
            }

            Debug.LogWarning($"[AudioDatabase] 未找到音频ID: {id}");
            return null;
        }

        /// <summary>
        /// 获取音频Clip（优先使用直接引用，否则使用路径加载）
        /// </summary>
        public AudioClip GetClip(AudioID id)
        {
            var config = GetConfig(id);
            if (config == null) return null;

            return config.GetClip();
        }

        /// <summary>
        /// 检查配置是否完整（编辑器工具）
        /// </summary>
        [ContextMenu("检查配置完整性")]
        public void CheckConfiguration()
        {
            Initialize();

            int missingCount = 0;
            foreach (AudioID audioID in System.Enum.GetValues(typeof(AudioID)))
            {
                var config = GetConfig(audioID);
                if (config == null)
                {
                    Debug.LogWarning($"[AudioDatabase] 缺少音频配置: {audioID}");
                    missingCount++;
                }
                else if (config.GetClip() == null)
                {
                    Debug.LogWarning($"[AudioDatabase] 音频ID {audioID} 的配置无效（Clip和路径都为空）");
                    missingCount++;
                }
            }

            if (missingCount == 0)
            {
                Debug.Log("[AudioDatabase] ✅ 所有配置完整！");
            }
            else
            {
                Debug.LogWarning($"[AudioDatabase] ⚠️ 发现 {missingCount} 个配置问题");
            }
        }
    }
}


