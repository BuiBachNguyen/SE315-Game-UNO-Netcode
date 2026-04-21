using UnityEngine;

public class TestCaller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CardBuilder.GetCard(CardReference.Instance.GetCardByName("TestCard"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
