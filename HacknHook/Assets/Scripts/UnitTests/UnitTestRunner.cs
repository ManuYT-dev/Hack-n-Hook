using Unity.VisualScripting;
using UnityEngine;

public class UnitTestRunner : MonoBehaviour
{
    void Start()
    {
        this.AddComponent<InputUnitTests>();
    }
}
