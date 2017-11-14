using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VIDE_Data;

public class StartTalkEvent : MonoBehaviour {
    public TalkUI diagUI;
    private VIDE_Assign assigned;

	// Use this for initialization
	void Start () {
        assigned = GetComponent<VIDE_Assign>();
        diagUI.Begin(assigned);
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.E)) {
            if (VD.isActive) {
                if (assigned.alias != "NonDialogue") {
                    diagUI.CallNext();
                }
            }
        }

    }
}
