using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    private const string DrawSoundEffectName = "Draw";

    #region Singleton
    private static SoundManager _instance;
    private AudioSource audioSource;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SoundManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    _instance = go.AddComponent<SoundManager>();
                    Debug.LogWarning("[SoundManager] Created a runtime SoundManager because no prefab instance was found.");
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Catalogs
    [System.Serializable]
    public struct SoundPair
    {
        public string name;
        public AudioClip clip;
    }
    [SerializeField] private List<SoundPair> BackGroundMusic = new List<SoundPair>();
    private string currentBackGroundMusic;
    [SerializeField] private List<SoundPair> SoundEffects = new List<SoundPair>();

    public void ChangeBackGroundMusic(string name)
    {
        if (BackGroundMusic == null)
        {
            Debug.LogWarning("[SoundManager] Background music catalog is missing.");
            return;
        }

        foreach (var pair in BackGroundMusic)
        {
            if (pair.name == name)
            {
                if (pair.clip == null)
                {
                    Debug.LogWarning($"[SoundManager] Background music '{name}' has no AudioClip.");
                    return;
                }

                if (currentBackGroundMusic == name && audioSource.isPlaying)
                {
                    return;
                }

                currentBackGroundMusic = name;
                audioSource.clip = pair.clip;
                audioSource.loop = true;
                audioSource.Play();
                return;
            }
        }

        Debug.LogWarning($"[SoundManager] Background music '{name}' was not found.");
    }

    public void PlaySoundEffect(string name)
    {
        if (SoundEffects == null)
        {
            Debug.LogWarning("[SoundManager] Sound effect catalog is missing.");
            return;
        }

        foreach (var pair in SoundEffects)
        {
            if (pair.name == name)
            {
                if (pair.clip == null)
                {
                    Debug.LogWarning($"[SoundManager] Sound effect '{name}' has no AudioClip.");
                    return;
                }

                audioSource.PlayOneShot(pair.clip);
                return;
            }
        }

        Debug.LogWarning($"[SoundManager] Sound effect '{name}' was not found.");
    }

    public void PlayDrawVfx()
    {
        PlaySoundEffect(DrawSoundEffectName);
    }
    #endregion
}
