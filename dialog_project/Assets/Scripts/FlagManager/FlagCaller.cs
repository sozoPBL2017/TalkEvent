using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagCaller : MonoBehaviour {

    private FlagManager flagManager;

	void Start () {
        flagManager = GetComponent<FlagManager>();

        flagManager.check_1 = true;
        flagManager.check_1_count = 120;
	}
	
	void Update () {
        Debug.Log(flagManager.check_1);
        Debug.Log(flagManager.check_1_count);
	}
}
