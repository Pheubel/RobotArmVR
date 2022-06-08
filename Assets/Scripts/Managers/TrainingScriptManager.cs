using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class TrainingScriptManager : MonoBehaviour
{
    [SerializeField] private TypeWriterText textWriter;
    [SerializeField] private PlayableDirector timeLine;
    [SerializeField] private ControlDirectorTime timeLineController;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject confirmationCanvas;
    [SerializeField] private GameObject fadeVisionCanvas;
    [SerializeField] private PhaseChanger phaseChanger;

    public List<PhaseTransform> phasesPosition;

    private TextAsset trainingScriptJSON;
    private TrainingScript trainingScript;
    private Phase[] phases;
    private bool changeByButton;

    [HideInInspector]
    public Phase currentPhase;

    [HideInInspector]
    public SubPhase currentSubPhase;

    public static TrainingScriptManager Instance {get; private set;}

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        trainingScriptJSON = Resources.Load("JSON/TrainingScript") as TextAsset;
        trainingScript = JsonUtility.FromJson<TrainingScript>(trainingScriptJSON.text);
        phases = trainingScript.phases;
    }

    public void EndPhaseConfirmation()
    {
        confirmationCanvas.SetActive(true);
        Transform backButton = confirmationCanvas.transform.GetChild(0);
        backButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Restart";
        confirmationCanvas.transform.GetChild(0).gameObject.SetActive(true);
        confirmationCanvas.transform.GetChild(1).gameObject.SetActive(true);
        confirmationCanvas.transform.GetChild(2).gameObject.SetActive(false);
    }

    public void EndSubPhaseConfirmation()
    {
        confirmationCanvas.SetActive(true);
        Transform backButton = confirmationCanvas.transform.GetChild(0);
        backButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Back";
        confirmationCanvas.transform.GetChild(0).gameObject.SetActive(true);
        confirmationCanvas.transform.GetChild(1).gameObject.SetActive(true);
        confirmationCanvas.transform.GetChild(2).gameObject.SetActive(false);
    }

    public void ContinueButton()
    {
        confirmationCanvas.SetActive(true);
        confirmationCanvas.transform.GetChild(0).gameObject.SetActive(false);
        confirmationCanvas.transform.GetChild(1).gameObject.SetActive(false);
        confirmationCanvas.transform.GetChild(2).gameObject.SetActive(true);
    }

    public void ActivateTrigger(int trigger)
    {
        switch (trigger)
        {
            //Teleport case
            case 0:
                if (currentSubPhase.subPhaseNumber== 5)
                {
                    Newtime();
                }
                break;
            //Point case
            case 1:
                if (currentSubPhase.subPhaseNumber == 7)
                { 
                    Newtime();
                }
                break;
            //Radio case
            case 2:
                if (currentSubPhase.subPhaseNumber == 8)
                {
                    Newtime();
                }
                break;
            //Safety Button
            case 3:
                if (currentSubPhase.subPhaseNumber == 3)
                {
                    Newtime();
                }
                break;
            //Switch Axis
            case 4:
                if (currentSubPhase.subPhaseNumber == 11)
                {
                    Newtime();
                }
                break;
            //Switch Mode
            case 5:
                if (currentSubPhase.subPhaseNumber == 16)
                {
                    Newtime();
                }
                break;
        }
    }

    public void CloseCanvas()
    {
        confirmationCanvas.SetActive(false);
        textWriter.Clear();
    }

    public void ChangePhase(int phaseNumber)
    {
        currentPhase = phases[phaseNumber];
        currentSubPhase = phases[phaseNumber].subPhases[0];
        CheckTimeLineDifference(currentPhase.startTime);

        CheckPhaseButton();
    }

    public void ChangeSubPhase(int subPhaseNumber)
    {
        textWriter.Clear();
        currentSubPhase = currentPhase.subPhases[subPhaseNumber];

        if (!changeByButton)
        {
            textWriter.Write(currentSubPhase.message);
        }
        changeByButton = false;

        CheckTimeLineDifference(currentSubPhase.startTime);

        CheckPhaseButton();
    }

    public void ChangebyButton(int subPhaseNumber)
    {
        changeByButton = true;
        ChangeSubPhase(subPhaseNumber);

        CheckPhaseButton();
    }

    private void CheckPhaseButton()
    {
        phaseChanger.ActivatePhaseButton(currentPhase.phaseNumber);
    }

    private void CheckTimeLineDifference(int newTime)
    {
        int timeDifference = Math.Abs(Mathf.RoundToInt((float)timeLine.time * 60) - newTime);
        if (timeDifference > 15)
        {
            StartCoroutine(Teleport(newTime));
        }
    }

    private IEnumerator Teleport(int newTime)
    {
        fadeVisionCanvas.SetActive(true);
        fadeVisionCanvas.transform.GetChild(0).GetComponent<Animator>().SetTrigger("FadeVision");
        yield return new WaitForSeconds(1f);
        Vector3 newPosition = phasesPosition[currentPhase.phaseNumber].subPhaseTransforms[currentSubPhase.subPhaseNumber].subPhaseTransform.position;
        player.transform.position = new Vector3(newPosition.x, player.transform.position.y, newPosition.z);
        timeLine.Pause();
        timeLine.time = newTime / 60f;
        timeLine.Resume();
        yield return new WaitForSeconds(1f);

        fadeVisionCanvas.SetActive(false);
    }

    public void PauseTimeLine()
    {
        timeLineController.Pause();
    }

    public void ResumeTimeLine()
    {
        timeLineController.Resume();
    }

    public void Newtime()
    {
        timeLineController.SetTime(currentSubPhase.endTime);
    }

    #region Json reading
    [Serializable]
    public class TrainingScript
    {
        public Phase[] phases;
    }

    [Serializable]
    public class Phase
    {
        public string phaseName;
        public int phaseNumber;
        public SubPhase[] subPhases;
        public int startTime;
        public float endTime;
    }

    [Serializable]
    public class SubPhase
    {
        public int subPhaseNumber;
        public string message;
        public int startTime;
        public float endTime;
    }
    #endregion

    #region Transform saving
    [Serializable]
    public class SubPhaseTransform
    {
        public Transform subPhaseTransform;
    }

    [Serializable]
    public class PhaseTransform
    {
        public Transform phaseTransform;
        public List<SubPhaseTransform> subPhaseTransforms;
    }
    #endregion
}