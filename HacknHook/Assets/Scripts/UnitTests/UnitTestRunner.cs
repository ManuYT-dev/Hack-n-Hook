using Unity.VisualScripting;
using UnityEngine;

public class UnitTestRunner : MonoBehaviour
{
    #region Unity Funcs
    void Start()
    {
        this.AddComponent<InputUnitTests>();
    }
    #endregion
}
