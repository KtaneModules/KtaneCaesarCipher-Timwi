using System.Collections.Generic;
using System.Linq;
using CaesarCipher;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class CaesarCipherModule : MonoBehaviour
{
    // ReSharper disable once InconsistentNaming
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable[] Buttons;
    public TextMesh[] ButtonLabels;
    public TextMesh DisplayText;

    private string _solution;
    private string _answerSoFar = "";

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        DisplayText.text = "";
        Module.OnActivate += Activate;

        // Generate 12 random characters to display on the buttons
        var pool = Enumerable.Range('A', 26).Select(i => (char) i).ToList();
        var randomChars = new List<char>();
        for (int i = 0; i < 12; i++)
        {
            var ix = Rnd.Range(0, pool.Count);
            var letter = pool[ix];
            var j = i;

            randomChars.Add(letter);
            ButtonLabels[i].text = letter.ToString();
            Buttons[i].OnInteract += delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[j].transform);
                Buttons[j].AddInteractionPunch();
                _answerSoFar += letter;
                Debug.LogFormat("[CaesarCipher #{0}] You pressed {1}; answer now {2}", _moduleId, letter, _answerSoFar);
                if (_answerSoFar.Length == _solution.Length)
                {
                    if (_answerSoFar == _solution)
                    {
                        Debug.LogFormat("[CaesarCipher #{0}] Module solved.", _moduleId);
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[CaesarCipher #{0}] Wrong answer.", _moduleId);
                        Module.HandleStrike();
                    }
                    _answerSoFar = "";
                }
                return false;
            };

            pool.RemoveAt(ix);
        }

        // The solution consists of 5 of those characters
        pool.Clear();
        pool.AddRange(randomChars);
        _solution = "";
        for (int i = 0; i < 5; i++)
        {
            var ix = Rnd.Range(0, pool.Count);
            _solution += pool[ix];
            pool.RemoveAt(ix);
        }
    }

    private void Activate()
    {
        var serial = Bomb.GetSerialNumber();
        bool hasLitNsa, hasParallel, hasCar;
        int numBatteries;

        if (serial == null)
        {
            // Generate random values for testing in Unity
            serial = string.Join("", Enumerable.Range(0, 6).Select(i => Rnd.Range(0, 36)).Select(i => i < 10 ? ((char) ('0' + i)).ToString() : ((char) ('A' + i - 10)).ToString()).ToArray());
            hasLitNsa = Rnd.Range(0, 2) == 0;
            hasParallel = Rnd.Range(0, 2) == 0;
            hasCar = Rnd.Range(0, 2) == 0;
            numBatteries = Rnd.Range(0, 7);
        }
        else
        {
            hasLitNsa = Bomb.IsIndicatorOn(KMBombInfoExtensions.KnownIndicatorLabel.NSA);
            hasParallel = Bomb.IsPortPresent(KMBombInfoExtensions.KnownPortType.Parallel);
            hasCar = Bomb.IsIndicatorPresent(KMBombInfoExtensions.KnownIndicatorLabel.CAR);
            numBatteries = Bomb.GetBatteryCount();
        }

        // Calculate the offset
        var offset = 0;
        if (hasLitNsa && hasParallel)
        {
            // Offset stays at 0
        }
        else
        {
            // Offset by Battery count
            offset += numBatteries;

            // Offset by Serial vowel check
            if (serial.Intersect("AEIOU").Any())
                offset -= 1;

            // Offset by CAR indicator
            if (hasCar)
                offset += 1;

            // Offset by Serial even ending
            if ("02468".Contains(serial.Last()))
                offset += 1;
        }

        DisplayText.text = new string(_solution.Select(ch => (char) ((ch - 'A' - offset + 26) % 26 + 'A')).ToArray());
        Debug.LogFormat("[CaesarCipher #{0}] Puzzle is {1}.", _moduleId, DisplayText.text);
        Debug.LogFormat("[CaesarCipher #{0}] Offset is {1}.", _moduleId, offset);
        Debug.LogFormat("[CaesarCipher #{0}] Solution is {1}.", _moduleId, _solution);
    }

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (!command.StartsWith("PRESS "))
            return null;
        command = command.Substring(6);

        var btns = new List<KMSelectable>();
        foreach (var ch in command)
        {
            if (ch == ' ')
                continue;
            var ix = -1;
            for (int i = 0; i < ButtonLabels.Length; i++)
                if (ButtonLabels[i].text[0] == ch)
                    ix = i;
            if (ix == -1)
                return null;
            btns.Add(Buttons[ix]);
        }
        return btns.ToArray();
    }
}
