# 202407_TechTalk_SentisDemo
Sentis Demo for Monthly Tech Talk

## Requirements ##
- Unity 2023.2.20f1
- Sentis 1.5.0-pre.3
- [Ollama](https://github.com/ollama/ollama)

## Setup ##

### 1. Download and unzip StreamingAssets.zip to /Assets/StreamingAssets folder ###
- Download [StreamingAssets.zip](https://drive.google.com/file/d/1Bl4vNthQ9zQvw5SbgpgSmErCBWIFTWG9/view?usp=sharing)

### 2. Install Ollama and download LLMs ###
- Download and install Ollama
- Run and chat with LLaMa3:8b

### 3. Get Huggingface API Key ###
- [Create new token](https://huggingface.co/settings/tokens)
- Paste into Unity > Window > Hugging Face API Wizard > API Key
- Test API Key

## Export a ONNX ##

### 1. Pre-trained model ###

| Model  | Colab |
| ------------- | ------------- |
| Hand Landmark (MediaPipe)  | <a href="https://colab.research.google.com/drive/1zWyOR1wk-idryt4xiWGxP3P6HsLZE3v4?usp=sharing"><img alt="colab link" src="https://colab.research.google.com/assets/colab-badge.svg" /></a>  |
| LogMel Spectrogram   | <a href="https://colab.research.google.com/drive/1AIH37wtF1WSU6AeZtFy_nG923cSAavmG?usp=sharing"><img alt="colab link" src="https://colab.research.google.com/assets/colab-badge.svg" /></a>  |
| Whisper (Encoder, Decoder)   | <a href="https://colab.research.google.com/drive/1byrBznenpFbIn4hRNHRFLIHXGhXq3nEU?usp=sharing"><img alt="colab link" src="https://colab.research.google.com/assets/colab-badge.svg" /></a>  |
| MiniLM-L12-v2   | <a href="https://colab.research.google.com/drive/1zjKi_6rzW-nGCfcvslKYSzSC-3QwJEw9?usp=sharing"><img alt="colab link" src="https://colab.research.google.com/assets/colab-badge.svg" /></a>  |
| Text To Speech (Jets) | [Link](https://github.com/Masao-Someki/espnet_onnx/) |

### 2. Training model  ###

| Model  | Colab |
| ------------- | ------------- |
| MLP Classifier  | <a href="https://colab.research.google.com/drive/1e525r8m5fQ2ZbR0jNfJsX8ei-OdMqSQi?usp=sharing"><img alt="colab link" src="https://colab.research.google.com/assets/colab-badge.svg" /></a>  |


## Scene ##

- Gesture Recognition Task: /Scene/HandRecognitionScene.unity

[![Gesture Recognition Task](https://img.youtube.com/vi/ZUtc2hDuwk8/0.jpg)](https://www.youtube.com/watch?v=ZUtc2hDuwk8)
<!--![demo1-resize](https://github.com/user-attachments/assets/911687a2-f1f0-4956-950c-99c52997ac0f)-->

- AI NPC Dialogue Task ###: /Scene/AINPCScene.unity

[![AI NPC Dialogue Task](https://img.youtube.com/vi/7UCuDyzBuOM/0.jpg)](https://www.youtube.com/watch?v=7UCuDyzBuOM)
<!--![demo2-resize](https://github.com/user-attachments/assets/0c3fd6f3-e69c-455f-aad8-d20ed34b6d65)-->

- AI Gallery: /Scene/AIGalleryScene.unity

[![AI NPC Dialogue Task](https://img.youtube.com/vi/i4JvkoHogRo/0.jpg)](https://www.youtube.com/watch?v=i4JvkoHogRo)
<!--![demo3-resize](https://github.com/user-attachments/assets/c5580f7d-bad9-4325-8e54-ed4b56b54e11)-->

## Contact ##

If you have any questions, feel free to ask me at sky.kim@unity3d.com.
