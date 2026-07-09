using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局音频管理器，负责背景音乐的随机轮播、跨场景持久化及静音控制
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ---------- 单例 ----------
    public static AudioManager Instance { get; private set; }

    // ---------- 组件引用 ----------
    [Header("音频源")]
    [SerializeField] private AudioSource musicSource;

    // ---------- 播放列表 ----------
    [Header("音乐播放列表")]
    [SerializeField] private List<AudioClip> musicPlaylist;
    [SerializeField] private bool playOnStart = true;

    // ---------- 随机播放状态 ----------
    private List<int> shuffledIndices;      // 打乱后的索引列表
    private int currentPlayIndex = 0;       // 当前播放到第几首（索引）
    private Coroutine musicCoroutine;       // 监听歌曲结束的协程

    // ============================================================
    //  Unity 生命周期
    // ============================================================

    void Awake()
    {
        // 单例模式：确保全局只有一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);   // 跨场景持久化
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 若未手动拖拽 musicSource，自动获取组件
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // 若启用“启动即播放”且列表不为空，则开始随机播放
        if (playOnStart && musicPlaylist.Count > 0)
        {
            PlayNextRandom();
        }
    }

    // ============================================================
    //  公开方法：随机播放控制
    // ============================================================

    /// <summary>
    /// 播放列表中的下一首随机曲目（洗牌算法，一轮播完重新打乱）
    /// </summary>
    public void PlayNextRandom()
    {
        if (musicPlaylist.Count == 0)
        {
            Debug.LogWarning("音乐播放列表为空！");
            return;
        }

        // 如果索引列表为空，或已播完一轮，则重新洗牌
        if (shuffledIndices == null || currentPlayIndex >= shuffledIndices.Count)
        {
            ShufflePlaylist();
            currentPlayIndex = 0;
        }

        // 从洗牌后的列表中取出当前曲目索引
        int clipIndex = shuffledIndices[currentPlayIndex];
        currentPlayIndex++;

        AudioClip clipToPlay = musicPlaylist[clipIndex];
        musicSource.clip = clipToPlay;
        musicSource.loop = false;          // 不由 AudioSource 循环，由协程控制
        musicSource.Play();

        // 启动协程监听歌曲结束
        if (musicCoroutine != null)
            StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(WaitForMusicEnd());
    }

    // ============================================================
    //  私有方法：洗牌与协程
    // ============================================================

    /// <summary>
    /// 监听当前歌曲播放完毕，自动触发下一首
    /// </summary>
    private IEnumerator WaitForMusicEnd()
    {
        yield return new WaitWhile(() => musicSource.isPlaying);
        PlayNextRandom();
    }

    /// <summary>
    /// Fisher-Yates 洗牌算法，打乱播放列表索引
    /// </summary>
    private void ShufflePlaylist()
    {
        shuffledIndices = new List<int>();
        for (int i = 0; i < musicPlaylist.Count; i++)
            shuffledIndices.Add(i);

        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int temp = shuffledIndices[i];
            int randomIndex = Random.Range(i, shuffledIndices.Count);
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }
    }

    // ============================================================
    //  公开方法：手动播放特定音乐（会打断随机循环）
    // ============================================================

    /// <summary>
    /// 强制播放指定音频（可循环），会中断当前的随机播放
    /// </summary>
    /// <param name="clip">要播放的音频片段</param>
    /// <param name="loop">是否循环</param>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;

        // 停止当前正在监听的协程
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
            musicCoroutine = null;
        }

        // 如果正在播放同一首，则不做任何操作
        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    // ============================================================
    //  公开方法：音量控制
    // ============================================================

    /// <summary>
    /// 设置主音量（0~1）
    /// </summary>
    public void SetVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);
    }

    // ============================================================
    //  🎵 新增：静音控制（供 UI 按钮调用）
    // ============================================================

    /// <summary>
    /// 切换静音状态（点击按钮时调用）
    /// </summary>
    public void ToggleMute()
    {
        if (musicSource != null)
            musicSource.mute = !musicSource.mute;
    }

    /// <summary>
    /// 手动设置静音状态（true=静音，false=取消静音）
    /// </summary>
    public void SetMute(bool mute)
    {
        if (musicSource != null)
            musicSource.mute = mute;
    }

    /// <summary>
    /// 查询当前是否处于静音状态（供 UI 显示文字/图标）
    /// </summary>
    public bool IsMuted => musicSource != null && musicSource.mute;
}