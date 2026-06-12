using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    #region Singleton
    private static SoundManager _instance;

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
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        var soundManagers = FindObjectsByType<SoundManager>(FindObjectsSortMode.None);
        for(int i=1; i < soundManagers.Length; i++)
        {
            Destroy(soundManagers[i].gameObject);
        }
        DontDestroyOnLoad(soundManagers[0].gameObject);
    }
    #endregion

    #region Catalogs
    [System.Serializable]
    public struct SoundPair
    {
        public string name;
        public AudioClip clip;
    }
    [SerializeField] private List<SoundPair> BackGroundMusic;
    private string currentBackGroundMusic;
    [SerializeField] private List<SoundPair> SoundEffects;

    public void ChangeBackGroundMusic(string name)
    {
        foreach (var pair in BackGroundMusic)
        {
            if (pair.name == name)
            {
                currentBackGroundMusic = name;
                GetComponent<AudioSource>().clip = pair.clip;
                GetComponent<AudioSource>().Play();
                GetComponent<AudioSource>().loop = true;
                return;
            }
        }
    }

    public void PlaySoundEffect(string name)
    {
        foreach (var pair in SoundEffects)
        {
            if (pair.name == name)
            {
                GetComponent<AudioSource>().PlayOneShot(pair.clip);
                return;
            }
        }
    }
    #endregion
    // Update is called once per frame
    void Update()
    {
        
    }
   
}
