using System.Collections;
using System.Collections.Generic;
using HuggingFace.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIGalleryObject : MonoBehaviour
{
    private const int NO_ACTIVE_IMAGE = -1;
    
    [SerializeField] private Image[] images;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text statusText;

    private string normalColorHex;
    private string errorColorHex;
    private bool isWaitingForResponse;

    private int activeImage = NO_ACTIVE_IMAGE;
    
    private Dictionary<string, int> sideToImageIndex = new Dictionary<string, int>
    {
        {"Side1", 0},
        {"Side2", 1},
        {"Side3", 2},
        {"Side4", 3}
    };
    
    void Awake()
    {
        normalColorHex = ColorUtility.ToHtmlStringRGB(statusText.color);
        errorColorHex = ColorUtility.ToHtmlStringRGB(Color.red);
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        inputField.gameObject.SetActive(false);
    }

    private void OnInputFieldEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendQuery();
        }
    }

    private void SendQuery()
    {
        if (isWaitingForResponse) return;

        string inputText = inputField.text;
        if (string.IsNullOrEmpty(inputText))
        {
            return;
        }

        if (activeImage == NO_ACTIVE_IMAGE)
            return;

        statusText.text = $"<color=#{normalColorHex}>Generating......</color>";
        images[activeImage].color = Color.black;

        isWaitingForResponse = true;
        inputField.interactable = false;
        inputField.text = "";

        HuggingFaceAPI.TextToImage(inputText, texture => {
            images[activeImage].sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            images[activeImage].color = Color.white;
            statusText.text = "";
            isWaitingForResponse = false;
            inputField.interactable = true;
            inputField.gameObject.SetActive(false);
        }, error => {
            statusText.text = $"<color=#{errorColorHex}>Error: {error}</color>";
            isWaitingForResponse = false;
            inputField.interactable = true;
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        statusText.text = other.gameObject.name;
        if (sideToImageIndex.TryGetValue(other.gameObject.name, out int index))
        {
            activeImage = index;
            inputField.gameObject.SetActive(true);
        }
        else
        {
            activeImage = NO_ACTIVE_IMAGE;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        statusText.text = "";
        activeImage = NO_ACTIVE_IMAGE;
        inputField.gameObject.SetActive(false);
    }    
}