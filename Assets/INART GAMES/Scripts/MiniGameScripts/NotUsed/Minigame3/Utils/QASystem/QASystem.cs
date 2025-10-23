
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class QASystem : MonoBehaviour
{
    enum QAStates{
        LOAD,
        DISPLAY,
        WAITING,
        OUTPUT,
        RESTART
    }

    [Header("Actual Question")]
    public string actualQuestion;

    public List<Question> questions = new List<Question>();
    public List<GameObject> buttons;

    public static QASystem instance;
    
    private QAStates qAStates;

    //Total answers to play
    private int playTimes;
    //actual answer
    private int countPlay;

    private float waitingToRestart;

    //actual correctAnswer loaded from list
    private int actualCorrectAnswer;

    //correct true -incorrect false
    private bool verifyStatus;
    private bool firstTime;

    void Awake() {
        instance = this;
    }
    void Start()
    {
        firstTime = true;
        qAStates = QAStates.LOAD;
        actualQuestion = "";
        actualCorrectAnswer = 0;
        playTimes = 4;
        countPlay = 0;
        waitingToRestart = 0f;
        //questions
        string[] auxAnswers1 = { "respuesta1", "respuesta2", "respuesta3", "respuesta4" };
        questions.Add(new Question("Pregunta1",auxAnswers1, 0));

        string[] auxAnswers2 = { "respuesta2", "respuesta2", "respuesta3", "respuesta4" };
        questions.Add(new Question("Pregunta1", auxAnswers1, 3));

        string[] auxAnswers3 = { "respuesta3", "respuesta2", "respuesta3", "respuesta4" };
        questions.Add(new Question("Pregunta1", auxAnswers1, 2));

        string[] auxAnswers4 = { "respuesta4", "respuesta2", "respuesta3", "respuesta4" };
        questions.Add(new Question("Pregunta1", auxAnswers1, 1));
    }

    // Update is called once per frame
    void Update()
    {
        switch(qAStates){
            case QAStates.LOAD:
                actualCorrectAnswer = questions[countPlay].correctAnswer;
                questions[countPlay].LoadAnswers(buttons);
                qAStates = QAStates.DISPLAY;
            break;
            case QAStates.DISPLAY:
                //call the text in screen to display de Question
                actualQuestion = questions[countPlay].question;
                qAStates = QAStates.WAITING;
            break;
            case QAStates.WAITING:

            break;
            case QAStates.OUTPUT:
                //reset o loop game
                countPlay++;
                if(countPlay < playTimes)
                {
                    qAStates = QAStates.RESTART;
                    
                }
                else
                {
                    //pass to next scene
                } 
            break;
            case QAStates.RESTART:
                waitingToRestart += Time.deltaTime;
                if(waitingToRestart >= 3f)
                {
                    qAStates = QAStates.LOAD;
                    CleanButtons();
                    waitingToRestart = 0f;
                }
            break;
        }
    }
    public void VerifyCorrectAnswer(int btnID){
        if(btnID == actualCorrectAnswer){
            verifyStatus = true;

            //StartCoroutine(ProgressionCanvas._instance.FillBar(countPlay));
            if (firstTime)
            {
                //StartCoroutine(ProgressionCanvas._instance.FillBar(countPlay));
                firstTime = false;
            }
            //StartCoroutine(ProgressionCanvas._instance.UnlockStuff(countPlay));
            buttons[btnID].GetComponent<Image>().color = Color.green;
            qAStates = QAStates.OUTPUT;
        }
        else{
            verifyStatus = false;
            buttons[btnID].GetComponent<Image>().color = Color.red;
        }
        
    }
    public void CleanButtons()
    {
        foreach (GameObject btn in buttons)
        {
            btn.GetComponent<Image>().color = Color.white;
        }
    }
}
public class Question
{
    public string question;

    public List<string> answers = new List<string>();

    public int correctAnswer;

    public Question (string question, string [] answers, int correctAnswer)
    {
        this.question = question;
        this.answers.AddRange(answers);
        this.correctAnswer = correctAnswer;
    }
    public void LoadAnswers(List<GameObject> buttons)
    {
        //check if the number of answers its equal to the number of buttons
        if (answers.Count == buttons.Count)
        {
            for (int i = 0; i < answers.Count; i++)
            {
                buttons[i].GetComponentInChildren<Text>().text = answers[i];
            }
        }
    }
}
