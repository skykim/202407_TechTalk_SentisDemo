using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCManager : MonoBehaviour
{
    [SerializeField] private MiniLMModel miniLMObject;
    [SerializeField] private WhisperModel whisperObject;
    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private  JetsModel jetsObject;

    [SerializeField] private float confidence = 0.5f;
    [SerializeField] [TextArea(3, 50)] private string roleText;
    [SerializeField] [TextArea(3, 50)] private string ragText;

    [SerializeField] private GameObject npcTextParent;
    [SerializeField] private GameObject npcTextPrefab;
    [SerializeField] private GameObject userTextPrefab;
    [SerializeField] private ScrollRect dialogueRect;
    
    private RAGPromptBuilder promptBuilder = new RAGPromptBuilder();
    private bool isWaitingForResponse;
    private const int MaxConversationEntries = 10;
    private const string ApiEndpoint = "http://localhost:11434/api/generate";
    private const string ModelName = "llama3:8b";

    private void Awake()
    {
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        InitializePrompt();
    }
    
    private async void OnInputFieldEndEdit(string text)
    {
        await SendQueryAsync();
    }

    private async Task SendQueryAsync()
    {
        if (isWaitingForResponse) return;

        string inputText = inputField.text;
        if (string.IsNullOrEmpty(inputText)) return;
        
        SetUIState(true);
        
        try
        {
            string userQuery = inputField.text; 
            AppendTextPrefab(userTextPrefab, userQuery);
            
            UpdateContext(userQuery);
            promptBuilder.UpdateQuestion("User",userQuery);
            
            string finalPrompt = promptBuilder.BuildPrompt();
            Debug.Log(finalPrompt);
            
            string npcResponse = await SendPromptAsync(finalPrompt);
            AppendTextPrefab(npcTextPrefab, npcResponse);
            
            jetsObject.TextToSpeech(npcResponse);
            
            UpdateConversationHistory(userQuery, npcResponse);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred: {ex.Message}");
            AppendTextPrefab(npcTextPrefab, "Sorry, an error occurred. Please try again.");
        }
        finally
        {
            SetUIState(false);
        }
    }

    private void SetUIState(bool isProcessing)
    {
        isWaitingForResponse = isProcessing;
        inputField.interactable = !isProcessing;
        
        if(!isProcessing)
            inputField.text = "";
    }

    private void UpdateContext(string userQuery)
    {
        promptBuilder.ClearContext();
        string[] lines = ragText.Split("\n");
        foreach (var line in lines)
        {
            float value = miniLMObject.RunMiniLM(userQuery, line);
            if (value > confidence)
            {
                promptBuilder.AddContext(line);
                Debug.Log($"{value} - {line}");
            }            
        }
    }

    private void UpdateConversationHistory(string userQuery, string npcResponse)
    {
        promptBuilder.AddConversationEntry("User", userQuery);
        promptBuilder.AddConversationEntry("NPC", npcResponse);
    }

    private void InitializePrompt()
    {
        promptBuilder.AddSystem("You are an Assistant in a game world. Answer the ### Question ### section by referring to the ### Context ### section. Keep your response in character, very brief, and limited to two short sentences at most. Absolutely avoid mentioning that you're an NPC, and respond as if you're truly the person fitting the given role. Do not use any emojis or emoticons.");
        promptBuilder.SetAssistantRole(roleText);
    }
    
    private async Task<string> SendPromptAsync(string promptText)
    {
        using (var client = new HttpClient())
        {
            var requestObject = new
            {
                model = ModelName,
                prompt = promptText
            };

            var json = JsonConvert.SerializeObject(requestObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(ApiEndpoint, content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return ParseResponseContent(responseContent);
        }
    }

    private string ParseResponseContent(string responseContent)
    {
        var lines = responseContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var resultText = new StringBuilder();
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var jsonObject = JObject.Parse(line);
                if (jsonObject.TryGetValue("response", out JToken responseValue))
                {
                    resultText.Append(responseValue.ToString());
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse JSON: {ex.Message}");
            }
        }
        
        return resultText.ToString();
    }

    private void AppendTextPrefab(GameObject prefab, string say)
    {
        var newObject = Instantiate(prefab, npcTextParent.transform);
        newObject.GetComponent<TextMeshProUGUI>().text = say;
        Canvas.ForceUpdateCanvases();
        dialogueRect.verticalNormalizedPosition = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            whisperObject.StartRecording();
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            ProcessVoiceInput();
        }
    }

    private async void ProcessVoiceInput()
    {
        bool success = whisperObject.StopRecording();
        if (success)
        {
            string result = await whisperObject.RunWhisperAsync();
            inputField.text = result;
            await SendQueryAsync();
        }
    }
}