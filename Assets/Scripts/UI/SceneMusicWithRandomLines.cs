using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景自动播放音乐（一次）+ 定时随机台词
/// 挂载到场景任意物体即可
/// </summary>
public class SceneMusicWithRandomLines : MonoBehaviour
{
    [Header("进入场景播放的音乐（一次）")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.8f;

    [Header("台词配置")]
    [SerializeField] private List<AudioClip> lineClips;   // 可添加多条台词
    [SerializeField][Range(0f, 1f)] private float lineVolume = 0.9f;
    [SerializeField] private float intervalSeconds = 15f; // 间隔时间（秒）

    [Header("调试")]
    [SerializeField] private bool logDebug = true;

    private AudioSource audioSource;

    private void Awake()
    {
        // 获取或添加 AudioSource 组件（独立播放器，不影响背景音乐）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D 音效
    }

    private void Start()
    {
        // 播放开场音乐（一次）
        if (introMusic != null)
        {
            audioSource.clip = introMusic;
            audioSource.volume = musicVolume;
            audioSource.Play();
            if (logDebug) Debug.Log($"[SceneMusic] 播放开场音乐：{introMusic.name}");
        }
        else
        {
            if (logDebug) Debug.LogWarning("[SceneMusic] 未设置开场音乐，跳过。");
        }

        // 启动定时器，开始循环播放随机台词
        if (lineClips != null && lineClips.Count > 0)
        {
            // 先等待开场音乐播完，再开始定时（可选）。这里简单起见，直接开始计时，
            // 第一次台词会在 intervalSeconds 秒后播放。
            StartCoroutine(LineTimerRoutine());
        }
        else
        {
            if (logDebug) Debug.LogWarning("[SceneMusic] 台词列表为空，不播放台词。");
        }
    }

    private IEnumerator LineTimerRoutine()
    {
        while (true)
        {
            // 等待指定间隔
            yield return new WaitForSeconds(intervalSeconds);

            // 随机选一句台词播放
            if (lineClips.Count > 0)
            {
                int randomIndex = Random.Range(0, lineClips.Count);
                AudioClip randomLine = lineClips[randomIndex];

                // 用同一个 audioSource 播放台词（会打断当前播放，但一般台词短，没问题）
                audioSource.clip = randomLine;
                audioSource.volume = lineVolume;
                audioSource.Play();
                if (logDebug) Debug.Log($"[SceneMusic] 播放随机台词：{randomLine.name}");
            }
        }
    }

    /// <summary>
    /// 手动停止播放（可选）
    /// </summary>
    public void StopAllAudio()
    {
        if (audioSource != null)
            audioSource.Stop();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllAudio();
    }
}