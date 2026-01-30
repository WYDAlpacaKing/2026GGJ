using UnityEngine;

namespace Alpaca.Game.Audio
{
    /// <summary>
    /// 音频配置数据 - 每个音频ID对应的配置信息
    /// </summary>
    [System.Serializable]
    public class AudioConfig
    {
        [Header("音频ID")]
        public AudioID id;

        [Header("音频源")]
        [Tooltip("直接引用AudioClip（推荐，打包后更快）")]
        public AudioClip clip;

        [Tooltip("Resources路径（如果clip为空时使用）")]
        public string resourcePath;

        [Tooltip("是否优先使用直接引用的Clip")]
        public bool useClipDirectly = true;

        /// <summary>
        /// 获取实际的音频Clip
        /// </summary>
        public AudioClip GetClip()
        {
            if (useClipDirectly && clip != null)
            {
                return clip;
            }

            if (!string.IsNullOrEmpty(resourcePath))
            {
                AudioClip loadedClip = Resources.Load<AudioClip>(resourcePath);
                if (loadedClip == null)
                {
                    Debug.LogWarning($"[AudioConfig] 无法从路径加载音频: {resourcePath}");
                }
                return loadedClip;
            }

            return null;
        }

        /// <summary>
        /// 获取音频名称（用于调试）
        /// </summary>
        public string GetName()
        {
            if (clip != null)
            {
                return clip.name;
            }
            if (!string.IsNullOrEmpty(resourcePath))
            {
                string[] parts = resourcePath.Split('/');
                return parts[parts.Length - 1];
            }
            return id.ToString();
        }
    }

}

