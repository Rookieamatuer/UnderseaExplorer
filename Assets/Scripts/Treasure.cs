using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Diver"))
        {
            UnderseaReefGenerator.instance.treasureNum--;
            Destroy(gameObject);
        }
    }
}
