using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using VIDE_Data; //<--- Import to use easily call VD class

public class TalkUI : MonoBehaviour {
    private enum SpriteSide {
        left = 0, right = 1
    }

    public int talkFontSize = 70;
    public int nameFontSize = 60;

    [SerializeField, Range(0, 1), HeaderAttribute("会話してない立ち絵をどれだけ暗くするか。1で変化なし")]
    private float spriteDisableColor = 0.8f;

    [SerializeField, Space(15)]
    public List<Image> actorSprite = new List<Image>();


    private List<Text> currentOptions = new List<Text>();
    
    private Text talkText, nameText;
    private GameObject uiContainer;

    //We'll use these later
    bool dialoguePaused = false;
    bool animatingText = false;

    IEnumerator npcTextAnimator;

    void Start() {
        VD.LoadDialogues(); //Load all dialogues to memory so that we dont spend time doing so later


    }

        void OnDisable() {
        //If the script gets destroyed, let's make sure we force-end the dialogue to prevent errors
        EndDialogue(null);
    }

    //This begins the conversation (Called by examplePlayer script)
    public void Begin(VIDE_Assign diagToLoad) {
        //Let's clean the NPC text variables
        uiContainer = transform.Find("Text Area").gameObject;
        talkText = uiContainer.transform.Find("Talk Text").GetComponent<Text>();
        nameText = uiContainer.transform.Find("Name/Name Text").GetComponent<Text>();

        talkText.text = "";
        nameText.text = "";
        nameText.fontSize = nameFontSize;

        VD.OnActionNode += ActionHandler;
        VD.OnNodeChange += NodeChangeAction;
        VD.OnEnd += EndDialogue;

        VD.BeginDialogue(diagToLoad);
        uiContainer.SetActive(true);
    }

    void Update() {
        var data = VD.nodeData;

        if (VD.isActive)
        {
            if (!data.pausedAction && data.isPlayer) {
                if (Input.GetKeyDown(KeyCode.S)) {
                    if (data.commentIndex < currentOptions.Count - 1)
                        data.commentIndex++;
                }
                if (Input.GetKeyDown(KeyCode.W)) {
                    if (data.commentIndex > 0)
                        data.commentIndex--;
                }

                //Color the Player options. Blue for the selected one
                for (int i = 0; i < currentOptions.Count; i++) {
                    currentOptions[i].color = Color.white;
                    if (i == data.commentIndex) currentOptions[i].color = Color.yellow;
                }
            }
        }
    }

    public void CallNext() {
       
        if (animatingText) { CutTextAnim(); ; return; }

        if (!dialoguePaused) {
            VD.Next();
            return;
        }
    }

    void ActionHandler(int actionNodeID) {
        Debug.Log("ACTION TRIGGERED: " + actionNodeID.ToString());
    }

    void NodeChangeAction(VD.NodeData data) {
        talkText.text = "";
        talkText.transform.parent.gameObject.SetActive(false);
        

        PostCheckExtraVariables(data);

        if (data.isPlayer) {
        }
        else {
            if (data.sprite != null) {
                
                if (data.extraVars.ContainsKey("side")) {
                    int side = (int)data.extraVars["side"];

                    actorSprite[side].color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                    actorSprite[side].sprite = data.sprite;
                    actorSprite[side].GetComponent<RectTransform>().sizeDelta = new Vector2(data.sprite.textureRect.width, data.sprite.textureRect.height);
                    actorSprite[side].enabled = true;

                    ChangeOthersSprite(side);
                }
            }
            else {
                for (int i = 0; i < actorSprite.Count; i++) {
                    if(actorSprite[i] != null) {
                        actorSprite[i].color = new Color(spriteDisableColor, spriteDisableColor, spriteDisableColor, 1.0f);
                        actorSprite[i].enabled = true;
                    }
                    else {
                        ClearSprite(i);
                    }
                }
            }

            if (data.extraVars.ContainsKey("clear")) {
                ClearSprite((int)data.extraVars["side"]);
            }
            npcTextAnimator = AnimateText(data);
            StartCoroutine(npcTextAnimator);


            if (data.tag.Length > 0)
                nameText.text = data.tag;
            else
                nameText.text = VD.assigned.alias;
            

            talkText.transform.parent.gameObject.SetActive(true);
        }
    }

    public void ClearSprite(int side) {
        actorSprite[side].sprite = null;
        actorSprite[side].enabled = false;
    }

    public void ClearAllSprite() {
        for (int i = 0; i < actorSprite.Count; i++) {
            ClearSprite(i);
        }
    }

    //  omitSide以外のSpriteを暗く、ないしは無効化する。
    void ChangeOthersSprite(int omitSide) {
        for (int i = 0; i < actorSprite.Count; i++) {
            if (i == omitSide)
                continue;
            if(actorSprite[i].sprite == null) {
                actorSprite[i].enabled = false;
                continue;
            }
            actorSprite[i].color = new Color(spriteDisableColor, spriteDisableColor, spriteDisableColor, 1.0f);
        }
    }


    //Check to see if there's Extra Variables and if so, we do stuff
    void PostCheckExtraVariables(VD.NodeData data) {
        //Don't conduct extra variable actions if we are waiting on a paused action
        if (data.pausedAction) return;

        if (!data.isPlayer) //For player nodes
        {
            //Replaces [NAME]
            if (data.extraVars.ContainsKey("nameLookUp"))
                nameLookUp(data);

            //Checks for extraData that concerns font size (CrazyCap node 2)
            if (data.extraData[data.commentIndex].Contains("fs")) {
                string[] fontSize = data.extraData[data.commentIndex].Split(","[0]);
                int fSize = talkFontSize;
                int.TryParse(fontSize[1], out fSize);
                talkText.fontSize = fSize;
            }
            else {
                talkText.fontSize = talkFontSize;
            }
        }
        return;
    }

    //This will replace any "[NAME]" with the name of the gameobject holding the VIDE_Assign
    //Will also replace [WEAPON] with a different variable
    void nameLookUp(VD.NodeData data) {
        if (data.comments[data.commentIndex].Contains("[NAME]"))
            data.comments[data.commentIndex] = data.comments[data.commentIndex].Replace("[NAME]", VD.assigned.gameObject.name);
    }

    //Very simple text animation usin StringBuilder
    public IEnumerator AnimateText(VD.NodeData data) {
        animatingText = true;
        string text = data.comments[data.commentIndex];

        if (!data.isPlayer) {
            StringBuilder builder = new StringBuilder();
            int charIndex = 0;
            while (talkText.text != text) {
                builder.Append(text[charIndex]);
                charIndex++;
                talkText.text = builder.ToString();
                yield return new WaitForSeconds(0.02f);
            }
        }

        talkText.text = data.comments[data.commentIndex]; //Now just copy full text		
        animatingText = false;
    }

    void CutTextAnim() {
        StopCoroutine(npcTextAnimator);
        talkText.text = VD.nodeData.comments[VD.nodeData.commentIndex]; //Now just copy full text		
        animatingText = false;
    }

    //Unsuscribe from everything, disable UI, and end dialogue
    void EndDialogue(VD.NodeData data) {
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= NodeChangeAction;
        VD.OnEnd -= EndDialogue;
        uiContainer.SetActive(false);
        VD.EndDialogue();
    }

}
