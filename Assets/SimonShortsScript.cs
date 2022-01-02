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
    public Color[] ButtonColors;
    public GameObject Spark;
    public KMBombModule Module;
    public KMAudio Audio;

    private static int _idc;
    private int _id = ++_idc, _stage, _pressesEntered;
    private List<Vector3[]> _paths = new List<Vector3[]>();
    private List<Transform> _pathParents = new List<Transform>();
    private Color[] _usedColors;
    private List<Dir[]> _animations = new List<Dir[]>();
    private List<Dir> _expectedPresses = new List<Dir>();
    private bool _isSolved;

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
    }

    private void Start()
    {
        Buttons[0].OnInteract += () => { Audio.PlaySoundAtTransform("Up", Buttons[0].transform); Buttons[0].AddInteractionPunch(.3f); Press(Dir.Up); return false; };
        Buttons[1].OnInteract += () => { Audio.PlaySoundAtTransform("Down", Buttons[1].transform); Buttons[1].AddInteractionPunch(.3f); Press(Dir.Left); return false; };
        Buttons[2].OnInteract += () => { Audio.PlaySoundAtTransform("Left", Buttons[2].transform); Buttons[2].AddInteractionPunch(.3f); Press(Dir.Right); return false; };
        Buttons[3].OnInteract += () => { Audio.PlaySoundAtTransform("Right", Buttons[3].transform); Buttons[3].AddInteractionPunch(.3f); Press(Dir.Down); return false; };

        _usedColors = new Color[ButtonColors.Length];
        ButtonColors.CopyTo(_usedColors, 0);
        _usedColors.Shuffle();

        for(int i = 0; i < ButtonRenderers.Length; i++)
            ButtonRenderers[i].material.color = _usedColors[i];

        StartCoroutine(RunFlashes());

        Module.OnActivate += () => { _stage = 1; };
    }

    private IEnumerator RunFlashes()
    {
        while(_stage < 1)
            yield return null;

        AddStage();

        while(_stage < 2)
        {
            yield return MakeAnimate(_animations[0][0], _animations[0][1]);
            yield return new WaitForSeconds(1f);
        }

        AddStage();

        while(_stage < 3)
        {
            yield return MakeAnimate(_animations[0][0], _animations[0][1]);
            yield return MakeAnimate(_animations[1][0], _animations[1][1]);
            yield return new WaitForSeconds(1f);
        }

        AddStage();

        while(_stage < 4)
        {
            yield return MakeAnimate(_animations[0][0], _animations[0][1]);
            yield return MakeAnimate(_animations[1][0], _animations[1][1]);
            yield return MakeAnimate(_animations[2][0], _animations[2][1]);
            yield return new WaitForSeconds(1f);
        }

        AddStage();

        while(_stage < 5)
        {
            yield return MakeAnimate(_animations[0][0], _animations[0][1]);
            yield return MakeAnimate(_animations[1][0], _animations[1][1]);
            yield return MakeAnimate(_animations[2][0], _animations[2][1]);
            yield return MakeAnimate(_animations[3][0], _animations[3][1]);
            yield return new WaitForSeconds(1f);
        }
    }

    private void AddStage()
    {
        Dir from = (Dir)RNG.Range(0, 4);
        Dir to = (Dir)(((int)from + RNG.Range(1, 4)) % 4);

        _animations.Add(new Dir[] { from, to });

        Debug.LogFormat("[Simon Shorts #{0}] The next flash is {1} ({2}) to {3} ({4}).", _id, from, _usedColors[(int)from], to, _usedColors[(int)to]);

        Color[] c = new int[] { 0, 3, 1, 2 }.Select(i => ButtonColors[i]).ToArray();
        if(from == Dir.Right && to == Dir.Left || from == Dir.Up && to == Dir.Right || from == Dir.Left && to == Dir.Down)
            _expectedPresses.Add((Dir)Array.IndexOf(c, _usedColors[(int)from]));
        else if(from == Dir.Left && to == Dir.Right || from == Dir.Right && to == Dir.Up || from == Dir.Down && to == Dir.Left)
            _expectedPresses.Add((Dir)Array.IndexOf(c, _usedColors[(int)to]));
        else if(from == Dir.Up && to == Dir.Down || from == Dir.Left && to == Dir.Up || from == Dir.Down && to == Dir.Right)
            _expectedPresses.Add(to);
        else if(from == Dir.Down && to == Dir.Up || from == Dir.Up && to == Dir.Left || from == Dir.Right && to == Dir.Down)
            _expectedPresses.Add(from);
        else
            throw new InvalidOperationException("Something went wrong in AddStage().");

        Debug.LogFormat("[Simon Shorts #{0}] I expect the following inputs: {1}", _id, _expectedPresses.Join(", "));
        _pressesEntered = 0;
    }

    private void Press(Dir d)
    {
        if(_isSolved || _pressesEntered >= _expectedPresses.Count)
            return;

        if(d == _expectedPresses[_pressesEntered])
        {
            Debug.LogFormat("[Simon Shorts #{0}] You correctly pressed {1}.", _id, d);
            if(++_pressesEntered == _expectedPresses.Count)
            {
                _stage++;

                if(_stage >= 5)
                {
                    Debug.LogFormat("[Simon Shorts #{0}] Module solved!", _id);
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    Module.HandlePass();
                    _isSolved = true;
                }
            }
        }
        else
        {
            Debug.LogFormat("[Simon Shorts #{0}] You pressed {1}, but I expected {2}. Strike! Resetting inputs...", _id, d, _expectedPresses[_pressesEntered]);

            Module.HandleStrike();

            _pressesEntered = 0;
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
    private const string TwitchHelpMessage = "Use \"!{0} ULRD\" to press the up, left, right, and down button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if(Regex.IsMatch(command, @"^[ULRD\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            foreach(char c in command.ToLowerInvariant())
            {
                if(c == 'u')
                    Buttons[0].OnInteract();
                else if(c == 'l')
                    Buttons[1].OnInteract();
                else if(c == 'r')
                    Buttons[2].OnInteract();
                else if(c == 'd')
                    Buttons[3].OnInteract();
                else
                    continue;
                yield return new WaitForSeconds(0.1f);
            }
            yield return "solve";
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while(!_isSolved)
        {
            if(_pressesEntered >= _expectedPresses.Count)
                yield return true;
            else
                yield return ProcessTwitchCommand(_expectedPresses[_pressesEntered].ToString()[0].ToString());
        }
    }
}
