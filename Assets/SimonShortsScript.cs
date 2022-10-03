using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WireGenerator;
using WG = WireGenerator.MeshGenerator;
using RND = System.Random;
using RNG = UnityEngine.Random;
using System.Linq;
using System.Text.RegularExpressions;

public class SimonShortsScript : MonoBehaviour
{
    public MeshFilter[] Wires, LongWires;
    public KMSelectable[] Buttons;
    public Renderer[] ButtonRenderers;
    public ButtonPressAnim[] PressAnims;
    public Color[] ButtonColors;
    public string[] ColorNameRef;
    public GameObject Spark;
    public KMBombModule Module;
    public KMAudio mAudio;
    public TextMesh[] colorblindTexts;
    public KMColorblindMode colorblindMode;

    private static int _idc;
    private int _id, _pressesEntered, stagesRequired;
    private List<Vector3[]> _paths = new List<Vector3[]>();
    private List<Transform> _pathParents = new List<Transform>();
    private Color[] _usedColors;
    private string[] _usedColorNameRef;
    private List<Dir[]> _animations = new List<Dir[]>();
    private List<Dir> _expectedPresses = new List<Dir>();
    private bool _isSolved, interactable, flasherRunning, colorblindDetected, playSounds;
    private float cooldown;
    IEnumerator flashRunner;

    private void Awake()
    {
        RND rng = new RND(RNG.Range(int.MinValue, int.MaxValue));
        foreach(MeshFilter wire in Wires)
        {
            Point[] be = WG.GenerateWire(rng);
            wire.sharedMesh = WG.GenerateWireMesh(be, Color.gray);
            _paths.Add(be.Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z)).ToArray());
            _pathParents.Add(wire.transform);
        }
        foreach(MeshFilter wire in LongWires)
        {
            Point[] be = WG.GenerateWire(rng, 0.03 * 1.414);
            wire.sharedMesh = WG.GenerateWireMesh(be, Color.gray);
            _paths.Add(be.Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z)).ToArray());
            _pathParents.Add(wire.transform);
        }
        try
        {
            colorblindDetected = colorblindMode.ColorblindModeActive;
        }
        catch
        {
            colorblindDetected = false;
        }

    }

    Dir ToDir(int value)
    {
        switch (value)
        {
            case 0:
            default:
                return Dir.Up;
            case 1:
                return Dir.Left;
            case 2:
                return Dir.Right;
            case 3:
                return Dir.Down;
        }
    }
    int ToIntFromDir(Dir value)
    {
        switch (value)
        {
            case Dir.Up:
            default:
                return 0;
            case Dir.Left:
                return 1;
            case Dir.Right:
                return 2;
            case Dir.Down:
                return 3;
        }
    }
    void Update()
    {
        if (!interactable) return;
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            return;
        }
        if (_pressesEntered > 0)
        {
            Debug.LogFormat("[Simon Shorts #{0}] Inputs have been reset after {1} correct press(es). Please reinput the sequence again.", _id, _pressesEntered);
            _pressesEntered = 0;
        }
        if (!flasherRunning)
        {
            flasherRunning = true;
            flashRunner = RunFlashes();
            StartCoroutine(flashRunner);
        }
    }
    private void Start()
    {
        _id = ++_idc;

        for (var x = 0; x < Buttons.Length; x++)
        {
            var y = x;
            Buttons[x].OnInteract += delegate {
                PressAnims[y].ButtonPush();
                mAudio.PlaySoundAtTransform(ToDir(y).ToString(), Buttons[y].transform);
                Buttons[y].AddInteractionPunch(.3f);
                if (interactable)
                    Press(ToDir(y));
                return false;

            };
        }
        var shuffledIdxes = Enumerable.Range(0, 4).ToArray().Shuffle();

        _usedColors = shuffledIdxes.Select(a => ButtonColors[a]).ToArray();
        _usedColorNameRef = shuffledIdxes.Select(a => ColorNameRef[a]).ToArray();

        Debug.LogFormat("[Simon Shorts #{0}] Buttons' color displayed (in order: Up, Left, Right, Down): {1}", _id, _usedColorNameRef.Join(", "));

        for (int i = 0; i < ButtonRenderers.Length; i++)
            ButtonRenderers[i].material.color = _usedColors[i];

        flashRunner = RunFlashes();
        stagesRequired = RNG.Range(3, 6);
        Debug.LogFormat("[Simon Shorts #{0}] Required stages completed to disarm the module: {1}", _id, stagesRequired);
        Module.OnActivate += () => {AddStage(); StartCoroutine(flashRunner); interactable = true; HandleColorblindMode(); };
    }

    void HandleColorblindMode()
    {
        for (var x = 0; x < colorblindTexts.Length; x++)
        {
            colorblindTexts[x].text = colorblindDetected ? _usedColorNameRef[x] : "";
        }
    }

    private IEnumerator RunFlashes()
    {
        for (var x = 0; x < _animations.Count; x++)
        {
            yield return new WaitForSeconds(0.5f);
            yield return MakeAnimate(_animations[x][0], _animations[x][1]);
        }
        cooldown = 3f;
        flasherRunning = false;
    }

    private void AddStage()
    {
        Dir from = (Dir)RNG.Range(0, 4);
        Dir to = (Dir)(((int)from + RNG.Range(1, 4)) % 4);

        _animations.Add(new Dir[] { from, to });

        Debug.LogFormat("[Simon Shorts #{0}] Stage {5}'s flash is {1} ({2}) to {3} ({4}).", _id, from, _usedColorNameRef[(int)from], to, _usedColorNameRef[(int)to], _animations.Count);

        Color[] c = new int[] { 0, 3, 1, 2 }.Select(i => ButtonColors[i]).ToArray();
        if (from == Dir.Right && to == Dir.Left || from == Dir.Up && to == Dir.Right || from == Dir.Left && to == Dir.Down)
        {
            _expectedPresses.Add((Dir)Array.IndexOf(c, _usedColors[(int)from]));
            Debug.LogFormat("[Simon Shorts #{0}] Using the diagram, the flashing color is used to obtain this press, which corresponds to {1}", _id, (Dir)Array.IndexOf(c, _usedColors[(int)from]));
        }
        else if (from == Dir.Left && to == Dir.Right || from == Dir.Right && to == Dir.Up || from == Dir.Down && to == Dir.Left)
        {
            _expectedPresses.Add((Dir)Array.IndexOf(c, _usedColors[(int)to]));
            Debug.LogFormat("[Simon Shorts #{0}] Using the diagram, the flashing button is used to obtain this press, which corresponds to  {1}", _id, (Dir)Array.IndexOf(c, _usedColors[(int)to]));
        }
        else if (from == Dir.Up && to == Dir.Down || from == Dir.Left && to == Dir.Up || from == Dir.Down && to == Dir.Right)
        {
            _expectedPresses.Add(to);
            Debug.LogFormat("[Simon Shorts #{0}] Using the diagram, the position of the flash is used to obtain this press, which is {1}", _id, to);
        }
        else if (from == Dir.Down && to == Dir.Up || from == Dir.Up && to == Dir.Left || from == Dir.Right && to == Dir.Down)
        {
            _expectedPresses.Add(from);
            Debug.LogFormat("[Simon Shorts #{0}] Using the diagram, the position of the flashing color is used to obtain this press, which is {1}", _id, from);
        }
        else
            throw new InvalidOperationException("Something went wrong in AddStage().");

        Debug.LogFormat("[Simon Shorts #{0}] The full sequence of inputs expected is now: {1}", _id, _expectedPresses.Join(", "));
        _pressesEntered = 0;
    }

    private void Press(Dir d)
    {
        if(_isSolved || _pressesEntered >= _expectedPresses.Count)
            return;
        flasherRunning = false;
        cooldown = 3f;
        if (d == _expectedPresses[_pressesEntered])
        {
            Debug.LogFormat("[Simon Shorts #{0}] You correctly pressed {1} for press #{2} on stage {3}.", _id, d, _pressesEntered + 1, _animations.Count);
            StopCoroutine(flashRunner);
            if (++_pressesEntered >= _expectedPresses.Count)
            {
                
                if(_animations.Count >= stagesRequired)
                {
                    Debug.LogFormat("[Simon Shorts #{0}] You cleared enough stages. Module solved!", _id);
                    Module.HandlePass();
                    _isSolved = true;
                    interactable = false;
                    StartCoroutine(PlaySolveAnim());
                }
                else
                {
                    AddStage();
                    flashRunner = RunFlashes();
                }
            }
        }
        else
        {
            Debug.LogFormat("[Simon Shorts #{0}] You pressed {1}, but I expected {2} for press #{3} on stage {4}. Strike! Resetting inputs...", _id, d, _expectedPresses[_pressesEntered], _pressesEntered + 1, _animations.Count);

            Module.HandleStrike();

            _pressesEntered = 0;
        }
    }

    IEnumerator PlaySolveAnim()
    {
        var stringRefs = new[] { "Up", "Down", "Left", "Right" };
        for (var t = 0; t < 1; t++)
        {
            for (var x = 0; x < 4; x++)
            {
                yield return new WaitForSeconds(0.2f);
                mAudio.PlaySoundAtTransform(stringRefs[x], transform);
            }
        }
    }

    private Coroutine MakeAnimate(Dir from, Dir to)
    {
        if(from == to)
            throw new ArgumentException("Cannot MakeAnimate() from somewhere to itself.");

        int pathID = -1;
        bool reverse = false;

        switch(from)
        {
            case Dir.Up:
                switch(to)
                {
                    case Dir.Left:
                        pathID = 2;
                        break;
                    case Dir.Right:
                        pathID = 1;
                        break;
                    case Dir.Down:
                        pathID = 5;
                        break;
                }
                break;
            case Dir.Left:
                switch(to)
                {
                    case Dir.Up:
                        pathID = 2;
                        reverse = true;
                        break;
                    case Dir.Right:
                        pathID = 4;
                        break;
                    case Dir.Down:
                        pathID = 0;
                        break;
                }
                break;
            case Dir.Right:
                switch(to)
                {
                    case Dir.Up:
                        pathID = 1;
                        reverse = true;
                        break;
                    case Dir.Left:
                        pathID = 4;
                        reverse = true;
                        break;
                    case Dir.Down:
                        pathID = 3;
                        break;
                }
                break;
            case Dir.Down:
                switch(to)
                {
                    case Dir.Up:
                        pathID = 5;
                        reverse = true;
                        break;
                    case Dir.Left:
                        reverse = true;
                        pathID = 0;
                        break;
                    case Dir.Right:
                        reverse = true;
                        pathID = 3;
                        break;
                }
                break;
        }

        if(pathID == -1)
            throw new InvalidOperationException("Something went wrong in MakeAnimate().");
        return StartCoroutine(Animate(pathID, reverse));
    }

    private IEnumerator Animate(int pathID, bool reverse)
    {
        const float dur = 0.75f;
        float time = Time.time;
        Vector3[] path = _paths[pathID];

        Spark.transform.parent = _pathParents[pathID];
        Spark.SetActive(true);

        while(Time.time - time < dur)
        {
            float lerp = (Time.time - time) / dur * (path.Length - 1);
            lerp = reverse ? path.Length - 1.01f - lerp : lerp;
            if((int)lerp < 0 || (int)lerp + 2 > path.Length)
                break;
            Vector3 pos = Vector3.Lerp(path[(int)lerp], path[(int)lerp + 1], lerp % 1f);
            Spark.transform.localPosition = pos;
            //Spark.transform.localScale = Vector3.one * (1f - (Time.time - time) / dur);
            yield return null;
        }

        Spark.SetActive(false);
    }

    private enum Dir
    {
        Up,
        Left,
        Right,
        Down
    }



#pragma warning disable 414
    private readonly string TwitchHelpMessage = "\"!{0} press ULRD\" [presses the button corresponding to the direction up, left, right, and down, respectively, \"press\" is optional] \"!{0} colourblind/colorblind\" [Toggles colorblind mode.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            colorblindDetected ^= true;
            HandleColorblindMode();
        }
        else if (Regex.IsMatch(command, @"^(press\s)?[ULRD\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var cmdModified = command.ToLowerInvariant().Replace("press", "").Trim().Split();
            var buttonsToPress = new List<KMSelectable>();
            foreach (string cmdPart in cmdModified)
            {
                var breakLoop = false;
                foreach (char chrDir in cmdPart)
                {
                    var idx = "ulrd".IndexOf(chrDir);
                    if (idx == -1)
                    {
                        yield return string.Format("sendtochaterror The given direction \"{0}\" is not a valid direction!", chrDir);
                        yield break;
                    }
                    if (_expectedPresses[buttonsToPress.Count] != ToDir(idx))
                    {
                        if (buttonsToPress.Any())
                            yield return string.Format("strikemessage by pressing {0} after {1} correct press(es) in the command provided!", ToDir(idx).ToString(), buttonsToPress.Count);
                        breakLoop = true;
                    }
                    buttonsToPress.Add(Buttons[idx]);
                    if (breakLoop)
                        break;
                }
                if (breakLoop)
                    break;
            }

            yield return null;
            foreach (KMSelectable button in buttonsToPress)
            {
                button.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            if (_isSolved)
                yield return "solve";
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while(!_isSolved)
        {
            Buttons[ToIntFromDir(_expectedPresses[_pressesEntered])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
