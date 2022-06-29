using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 
/// Created by Phillip Kollwitz on June 28th, 2022
/// Demo Project is provided by GateGames.
/// We don't own the 'Wordle' property, this is only used for demonstration purposes
/// For more information, visit 'nytimes.com/games/wordle'
/// 
/// </summary>

/* class for better visualization in the inspector */
[CustomEditor(typeof(WordleGame))]
public class WordleEditor : Editor
{
    bool showReferences = false;
    bool showOptions = false;

    public override void OnInspectorGUI()
    {
        WordleGame game = (WordleGame)target;

        /* Important References */
        showReferences = EditorGUILayout.Foldout(showReferences, "Script References");

        if (showReferences)
        {
            game.wordleData = (TextAsset)EditorGUILayout.ObjectField("Dictionary Data", game.wordleData, typeof(TextAsset), false);
            game.wordleBox = (GameObject)EditorGUILayout.ObjectField("WordleBox Asset", game.wordleBox, typeof(GameObject), false);
        }

        /* General Options */
        showOptions = EditorGUILayout.Foldout(showOptions, "Game Options");

        if (showOptions)
        {
            game.wordLength = EditorGUILayout.IntSlider("Length of words to guess", game.wordLength, 3, 9);
            game.amountOfGames = (byte)EditorGUILayout.IntSlider("Number of Games", game.amountOfGames, 0, 3);
            game.maxTryCount = EditorGUILayout.IntSlider("Max Amount of Guesses", game.maxTryCount, game.amountOfGames, 8);
        }
    }
}

/* class for handling setup and UI implementation */
public class WordleGame : MonoBehaviour
{
    public TextAsset wordleData;        //raw dictionary data
    public GameObject wordleBox;        //prefab for ui elements

    public string[] words;
    public int maxTryCount = 8;         //max amount of valid trys
    public int wordLength = 5;          //length of valid word
    public byte amountOfGames = 2;      //amount of wordles playing simultaneously

    public List<Wordle> games;

    public string input;                //user input

    //creates the individual instances of the wordle games
    void CreateInstances(byte amount)
    {
        for (byte i = 0; i < amount; i++)
        {
            Wordle wordle = new Wordle(this);
            games.Add(wordle);

            //create parent for the specific wordle ui elements
            RectTransform wordleParent = new GameObject("Wordle", typeof(RectTransform)).GetComponent<RectTransform>();
            wordleParent.SetParent(transform);
            wordleParent.localScale = Vector3.one;

            //add vertical layout group component, keep spacing consistent
            VerticalLayoutGroup layout = wordleParent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 20;

            CreateUIElement(i);
        }
    }

    //spawns a new wordle box element
    void CreateUIElement(byte number)
    {
        Instantiate(wordleBox, transform.GetChild(number));
    }

    void UpdateUITextElements()
    {
        //update characters in the UI
        for (int i = 0; i < games.Count; i++)
        {
            for (int l = 0; l < wordLength; l++)
            {
                Wordle w = games[i];

                //if wordle has been won or lost, ignore it
                if (!w.active)
                {
                    continue;
                }

                //get wordle parent with child (i)
                //get word parent with child (w.tryCount)
                //get letterbox image with child (l)
                //get text with child (0)
                TextMeshProUGUI textElement = transform.GetChild(i).GetChild(w.tryCount).GetChild(l).GetChild(0).GetComponent<TextMeshProUGUI>();

                //set characters according to input
                if (l >= input.Length)
                {
                    textElement.text = "";
                    continue;
                }

                textElement.text = input[l].ToString().ToUpper();
            }
        }
    }

    void Awake()
    {
        //load data into words array
        words = wordleData.text.Split('\n');
        //IMPORTANT: remove last char, because unity loads backspaces for some reason
        for (int i = 0; i < words.Length; i++) { words[i] = words[i].Remove(words[i].Length - 1); }

        //init list of wordle instances
        games = new List<Wordle>();

        //init input string
        input = "";
    }

    void Start()
    {
        CreateInstances(amountOfGames);
    }

    //OnGUI for listening to user input, not needing UI elements to do so
    void OnGUI()
    {
        /* main game loop */

        //get input event
        Event e = Event.current;

        //receiving user input
        if (e.type == EventType.KeyDown)
        {
            //a letter has been entered, don't accept whitespaces; length limit applys
            if (char.IsLetter(e.character) && !char.IsWhiteSpace(e.character) && input.Length < wordLength)
            {
                input += e.character;
                UpdateUITextElements();

                return;
            }

            //backspace has been pressed, remove last letter
            if (e.keyCode == KeyCode.Backspace && input.Length > 0)
            {
                input = input.Remove(input.Length - 1);
                UpdateUITextElements();

                return;
            }

            //enter has been pressed, try guess word
            if (e.keyCode == KeyCode.Return)
            {
                //apply input for all instances of the game
                for (int i = 0; i < games.Count; i++)
                {
                    Wordle w = games[i];

                    //if wordle has been won or lost, ignore it
                    if (!w.active)
                    {
                        continue;
                    }

                    bool guess = w.Guess(input);

                    //if guess is valid, color ui and continue
                    if (guess)
                    {
                        for (int c = 0; c < w.guessCode.Length; c++)
                        {
                            //get wordle parent with child (i)
                            //get word parent with child (w.tryCount); -1, because it's the previous one
                            //get letterbox image with child (c)
                            Image box = transform.GetChild(i).GetChild(w.tryCount - 1).GetChild(c).GetComponent<Image>();
                            
                            //color box image according to the Guesscode
                            switch (w.guessCode[c])
                            {
                                case 0:
                                    box.color = Color.grey;     //letter is wrong
                                    break;
                                case 1:
                                    box.color = Color.yellow;   //letter is in wrong position
                                    break;
                                case 2:
                                    box.color = Color.green;    //letter is in right position
                                    break;
                                default:                        //invalid, ignore
                                    break;
                            }
                        }

                        //create a new wordle box if active
                        if (w.active)
                        {
                            CreateUIElement((byte)i);
                        }
                    }
                }

                //reset input
                input = "";
                UpdateUITextElements();
            }
        }
    }
}

/* standalone class for game logic */
public class Wordle
{
    WordleGame game;            //reference to main script

    public string word;         //word to guess
    public bool solved;         //not used, but helpful for determining success
    public bool active;         //if game is active

    public int tryCount;        //amount of guesses

    //Guesscode used for UI
    public byte[] guessCode;    //0 - wrong letter; 1 - wrong position; 2 - right position

    public Wordle(WordleGame game)
    {
        this.game = game;

        //get a random word from the loaded dictionary
        word = game.words[Random.Range(0, game.words.Length)];

        //init the Guesscode
        guessCode = new byte[word.Length];

        solved = false;
        active = true;
        tryCount = 0;
    }

    public bool WordExists(string word)
    {
        foreach (string w in game.words)
        {
            if (word == w)
            {
                return true;
            }
        }

        //word is not part of dictionary
        return false;
    }

    public bool Guess(string givenWord)
    {
        //ignore upper case letters
        givenWord = givenWord.ToLower();

        //check if given word is part of dictionary, if not - return
        if (!WordExists(givenWord))
        {
            Debug.Log("Word does not exist.");
            return false;
        }

        //check if given word has the same length, if not - return
        if (givenWord.Length != word.Length)
        {
            Debug.Log("Word length does not match up.");
            return false;
        }

        //try is legit, add to count
        tryCount++;

        //begin checking individual letters
        for (int l = 0; l < word.Length; l++)
        {
            //letter is in the right position
            if (word[l] == givenWord[l])
            {
                guessCode[l] = 2;
                continue;
            }

            //letter is in the wrong position
            if (word.Contains(givenWord[l]))
            {
                guessCode[l] = 1;
                continue;
            }

            //letter is wrong
            guessCode[l] = 0;
        }

        //check if player guessed right, if yes - wordle is solved
        if (givenWord == word)
        {
            Debug.Log("You solved the Wordle! Congrats!");

            active = false;
            solved = true;
            return true;
        }

        //check if trys exceeded max amount, if yes - wordle is lost
        if (tryCount >= game.maxTryCount)
        {
            Debug.Log($"Wordle was not solved. The answer was: {word}");

            active = false;
            return true;
        }

        //game loop continues until player guessed right or loses
        return true;
    }
}
