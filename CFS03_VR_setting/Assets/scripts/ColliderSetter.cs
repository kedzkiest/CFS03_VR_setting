using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ColliderSetter : MonoBehaviour
{
    [SerializeField] private GameObject robotArm;
    [SerializeField] private GameObject IKNodes;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(SetCollider());
        }
    }

    private IEnumerator SetCollider()
    {
        robotArm.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        robotArm.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        IKNodes.SetActive(false);
        yield return null;
    }
}
