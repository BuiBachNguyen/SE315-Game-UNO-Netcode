using UnityEngine;

public class SoundPlay : MonoBehaviour
{
    [SerializeField] private string bgmName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.Instance.ChangeBackGroundMusic(bgmName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
