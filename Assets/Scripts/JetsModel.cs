using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using System.Threading.Tasks;
using ReadyPlayerMe.Core;

//                      Jets Text-To-Speech Inference
//                      =============================
//
// This file implements the Jets Text-to-speech model in Unity Sentis
// The model uses phenomes instead of raw text so you have to convert it first.
// Place this file on the Main Camera
// Add an audio source
// Change the inputText
// When running you can press space bar to play it again

public class JetsModel : MonoBehaviour
{
    public VoiceHandler handler;
    //public string inputText = "Once upon a time, there lived a girl called Alice. She lived in a house in the woods.";
    //string inputText = "The quick brown fox jumped over the lazy dog";
    //string inputText = "There are many uses of the things she uses!";

    //Set to true if we have put the phoneme_dict.txt in the Assets/StreamingAssets folder
    bool hasPhenomeDictionary = true;

    readonly string[] phonemes = new string[] { 
        "<blank>", "<unk>", "AH0", "N", "T", "D", "S", "R", "L", "DH", "K", "Z", "IH1", 
        "IH0", "M", "EH1", "W", "P", "AE1", "AH1", "V", "ER0", "F", ",", "AA1", "B", 
        "HH", "IY1", "UW1", "IY0", "AO1", "EY1", "AY1", ".", "OW1", "SH", "NG", "G", 
        "ER1", "CH", "JH", "Y", "AW1", "TH", "UH1", "EH2", "OW0", "EY2", "AO0", "IH2", 
        "AE2", "AY2", "AA2", "UW0", "EH0", "OY1", "EY0", "AO2", "ZH", "OW2", "AE0", "UW2", 
        "AH2", "AY0", "IY2", "AW2", "AA0", "\"", "ER2", "UH2", "?", "OY2", "!", "AW0", 
        "UH0", "OY0", "..", "<sos/eos>" };

    readonly string[] alphabet = "AE1 B K D EH1 F G HH IH1 JH K L M N AA1 P K R S T AH1 V W K Y Z".Split(' ');

    //Can change pitch and speed with this for a slightly different voice:
    const int samplerate = 22050;

    Dictionary<string, string> dict = new ();

    IWorker engine;

    AudioClip clip;

    void Start()
    {
        LoadModel();
        ReadDictionary();
    }

    void LoadModel()
    {
        var model = ModelLoader.Load(Path.Join(Application.streamingAssetsPath ,"jets-text-to-speech.sentis"));
        engine = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);
    }

    public void TextToSpeech(string inputText)
    {
        string ptext;
        if (hasPhenomeDictionary)
        {
            ptext = TextToPhonemes(inputText);
            //Debug.Log(ptext);
        }
        else
        {
            //If we have no phenome dictionary we can use one of these examples:
            ptext = "DH AH0 K W IH1 K B R AW1 N F AA1 K S JH AH1 M P S OW1 V ER0 DH AH0 L EY1 Z IY0 D AO1 G .";
            //ptext = "W AH1 N S AH0 P AA1 N AH0 T AY1 M , AH0 F R AA1 G M EH1 T AH0 P R IH1 N S EH0 S . DH AH0 F R AA1 G K IH1 S T DH AH0 P R IH1 N S EH0 S AH0 N D B IH0 K EY1 M AH0 P R IH1 N S .";
            //ptext = "D UW1 P L AH0 K EY2 T";
        }
        DoInference(ptext);
    }

    void ReadDictionary()
    {
        if (!hasPhenomeDictionary) return;
        string[] words = File.ReadAllLines(Path.Join(Application.streamingAssetsPath,"phoneme_dict.txt"));
        for (int i = 0; i < words.Length; i++)
        {
            string s = words[i];
            string[] parts = s.Split();
            if (parts[0] != ";;;") //ignore comments in file
            {
                string key = parts[0];
                dict.Add(key, s.Substring(key.Length + 2));
            }
        }
        // Add codes for punctuation to the dictionary
        dict.Add(",", ",");
        dict.Add(".", ".");
        dict.Add("!", "!");
        dict.Add("?", "?");
        dict.Add("\"", "\"");
        // You could add extra word pronounciations here e.g.
        //dict.Add("somenewword","[phonemes]");
    }

    public string ExpandNumbers(string text)
    {
        return text
            .Replace("0", " ZERO ")
            .Replace("1", " ONE ")
            .Replace("2", " TWO ")
            .Replace("3", " THREE ")
            .Replace("4", " FOUR ")
            .Replace("5", " FIVE ")
            .Replace("6", " SIX ")
            .Replace("7", " SEVEN ")
            .Replace("8", " EIGHT ")
            .Replace("9", " NINE ");
    }

    public string TextToPhonemes(string text)
    {
        string output = "";
        text = ExpandNumbers(text).ToUpper();

        string[] words = text.Split();
        for (int i = 0; i < words.Length; i++)
        {
            output += DecodeWord(words[i]);
        }
        return output;
    }

    //Decode the word into phenomes by looking for the longest word in the dictionary that matches
    //the first part of the word and so on. 
    //This works fairly well but could be improved. The original paper had a model that
    //dealt with guessing the phonemes of words
    public string DecodeWord(string word)
    {
        string output = "";
        int start = 0;
        for (int end = word.Length; end >= 0 && start < word.Length ; end--)
        { 
            if (end <= start) //no matches
            {
                start++;
                end = word.Length + 1;
                continue;
            }
            string subword = word.Substring(start, end - start);
            if (dict.TryGetValue(subword, out string value))
            {
                output += value + " ";
                start = end;
                end = word.Length + 1;
            }
        }
        return output;
    }
   
    int[] GetTokens(string ptext)
    {
        string[] p = ptext.Split();
        var tokens = new int[p.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            tokens[i] = Mathf.Max(0, System.Array.IndexOf(phonemes, p[i])); 
        }
        return tokens;
    }

    public void DoInference(string ptext)
    {      
        int[] tokens = GetTokens(ptext);

        using var input = new TensorInt(new TensorShape(tokens.Length), tokens);
        var result = engine.Execute(input);

        var output = result.PeekOutput("wav") as TensorFloat;
        output.CompleteOperationsAndDownload();
        var samples = output.ToReadOnlyArray();

        Debug.Log($"Audio size = {samples.Length / samplerate} seconds");

        clip = AudioClip.Create("voice audio", samples.Length, 1, samplerate, false);
        clip.SetData(samples, 0);

        Speak();
    }
    private void Speak()
    {
        handler.PlayAudioClip(clip);
    }

    private void OnDestroy()
    {
        engine?.Dispose();
    }
}