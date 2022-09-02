﻿using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

using rng = UnityEngine.Random;

public class ZScript : MonoBehaviour
{
    public KMSelectable _button;
    public KMBombModule _module;
    public KMAudio _audio;
    public KMBombInfo _info;
    public Renderer _blinkerL, _blinkerR;
    public Transform _hinge, _startPos, _targetPos;

    private bool _hasOpened, _isActive, _isSolved, _isPlaying, _tpActiveOverride;
    private static bool _tpZOn, _tpZOff;
    private static int _idc;
    private int _wordIx, _id, _clicks, _submitStatus;
    private KMAudio.KMAudioRef _ref;
    private Coroutine _routine;

    private static readonly string[] ZWORDS = new string[] { "ealots", "ealous", "ipping", "odiacs", "ombies", "oology", "ygotes" };
    private static readonly string[] ZWORDSTX = new string[] { ".   . ...   . ... . .   ... ... ...   ...   . . .   ", ".   . ...   . ... . .   ... ... ...   . . ...   . . .   ", ". .   . ... ... .   . ... ... .   . .   ... .   ... ... .   ", "... ... ...   ... . .   . .   . ...   ... . ... .   . . .   ", "... ... ...   ... ...   ... . . .   . .   .   . . .   ", "... ... ...   ... ... ...   . ... . .   ... ... ...   ... ... .   ... . ... ...   ", "... . ... ...   ... ... .   ... ... ...   ...   .   . . .   " };

    private void Start()
    {
        _id = ++_idc;
        _wordIx = rng.Range(0, ZWORDS.Length);

        Debug.LogFormat("[Z #{0}] Chose word #{1} (Z{3}).", _id, _wordIx + 1, null, ZWORDS[_wordIx]);

        _button.Children[0].OnInteract += () => { _button.Children[0].AddInteractionPunch(); return false; };
        _button.OnFocus += () => { _isActive = true; };
        _button.OnDefocus += () => { _isActive = false; };
        StartCoroutine(Flash());
    }

    private void Update()
    {
        if(!_hasOpened && (Input.GetKeyDown(KeyCode.Z) || _tpZOn))
            StartCoroutine(Open());
        if(_submitStatus == 0 && (_isActive || _tpActiveOverride) && !_isSolved && !_isPlaying && (Input.GetKeyDown(KeyCode.Z) || _tpZOn))
        {
            _isPlaying = true;
            _routine = StartCoroutine(PlayMusic());
        }
        if(_submitStatus == 0 && _isPlaying && (Input.GetKeyUp(KeyCode.Z) || _tpZOff))
        {
            if(_clicks == 0)
            {
                StopCoroutine(_routine);
                if(_ref != null)
                    _ref.StopSound();
                _isPlaying = false;
            }
            _submitStatus = _clicks;
        }
    }

    private IEnumerator PlayMusic()
    {
        _clicks = 0;
        float time = Time.time;
        while(Time.time - time < 2.5f)
            yield return null;
        time = Time.time;
        _ref = _audio.PlaySoundAtTransformWithRef("music", transform);
        while(Time.time - time < 15f)
        {
            if(Time.time - time < 1.919f)
                _clicks = 8;
            else if(Time.time - time < 1.919f + 1.333f)
                _clicks = 1;
            else if(Time.time - time < 1.919f + 2f * 1.333f)
                _clicks = 2;
            else if(Time.time - time < 1.919f + 3f * 1.333f)
                _clicks = 3;
            else if(Time.time - time < 1.919f + 4f * 1.333f)
                _clicks = 4;
            else if(Time.time - time < 1.919f + 5f * 1.333f)
                _clicks = 5;
            else if(Time.time - time < 1.919f + 6f * 1.333f)
                _clicks = 6;
            else if(Time.time - time < 1.919f + 7f * 1.333f)
                _clicks = 7;
            else
                _clicks = 8;
            yield return null;
        }
        _ref.StopSound();
        SubmitCheck();
        _isPlaying = false;
    }

    private void SubmitCheck()
    {
        if(_submitStatus == 0 || _submitStatus == 8)
        { }
        else if(_submitStatus == _wordIx + 1)
        {
            _isSolved = true;
            Debug.LogFormat("[Z #{0}] Solved!", _id);
            _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            _blinkerL.material.color = Color.green;
            _blinkerR.material.color = Color.green;
            _module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[Z #{0}] Strike! You submitted {1}, but I expected {2}.", _id, _submitStatus, _wordIx + 1);
            _module.HandleStrike();
        }

        _submitStatus = 0;
    }

    private IEnumerator Open()
    {
        if(_hasOpened)
            yield break;
        _hasOpened = true;
        float time = Time.time;
        while(Time.time - time < 2f)
        {
            _hinge.localRotation = Quaternion.Slerp(_startPos.localRotation, _targetPos.localRotation, (Time.time - time) / 2f);
            yield return null;
        }
        _hinge.localRotation = _targetPos.localRotation;
    }

    private IEnumerator Flash()
    {
        int prevTime = (int)Mathf.Floor(_info.GetTime());
        int i = 0, mod = ZWORDSTX[_wordIx].Length, o = rng.Range(0, 5);
        while(true)
        {
            _blinkerL.material.color = _isSolved ? Color.green : Random.Range(0, 2) == 0 ? Color.white : Color.black;
            _blinkerR.material.color = _isSolved ? Color.green : ZWORDSTX[_wordIx][(i + o) % ZWORDSTX[_wordIx].Length] == '.' ^ _blinkerL.material.color == Color.white ? Color.white : Color.black;
            yield return new WaitWhile(() => Mathf.Abs(Mathf.Floor(_info.GetTime()) - prevTime) < 1);
            prevTime = (int)Mathf.Floor(_info.GetTime());
            i++;
            i %= mod;
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = @"Use ""!{0} Z"" to press Z once. Use ""!{0} Z 6"" to hold Z for that many beats.";
#pragma warning restore 0414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        Match m;

        if(command == "z")
        {
            yield return null;
            _tpZOn = true;
            yield return null;
            yield return null;
            _tpZOn = false;
            _tpZOff = true;
            yield return null;
            yield return null;
            _tpZOff = false;
        }
        else if((m = Regex.Match(command, @"^\s*z\s+([1-7])\s*$")).Success)
        {
            int c = int.Parse(m.Groups[1].Value);
            yield return null;
            _tpZOn = true;
            yield return null;
            yield return null;
            _tpZOn = false;
            yield return new WaitUntil(() => _clicks == c);
            _tpZOff = true;
            yield return null;
            yield return null;
            _tpZOff = false;
        }

        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        _tpActiveOverride = true;
        _tpZOn = true;
        yield return null;
        yield return null;
        _tpZOn = false;
        yield return new WaitUntil(() => _clicks == _wordIx + 1);
        _tpZOff = true;
        yield return null;
        yield return null;
        _tpZOff = false;
        yield break;
    }
}
