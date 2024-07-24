using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using TMPro;
using FF = Unity.Sentis.Functional;

public class HandLandmarkModel : MonoBehaviour
{
    //Draw the *.sentis or *.onnx model asset here:
    //public ModelAsset asset;
    [SerializeField] private string handLandmarkModelName = "hand_landmark_full.sentis";
    [SerializeField] private string classificationModelName = "3d_keypoint_classifier.sentis";
    
    //Drag a link to a raw image here:
    [SerializeField] private RawImage previewUI = null;

    // Put your bounding box sprite image here
    [SerializeField] private Sprite boxSprite;

    //Resolution of preview image or video
    Vector2Int resolution = new Vector2Int(640, 640);
    WebCamTexture webcam;

    const BackendType backend = BackendType.GPUCompute;
    RenderTexture targetTexture;
    IWorker worker1;
    IWorker worker2;

    //Holds image size
    const int size = 224;
    const int halfSize = size / 2;

    Model handLandmarkModel;
    Model classificationModel;
    Model classificationModelWithArgMax;

    //webcam device name:
    const string deviceName = "";
    
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
    }

    List<GameObject> boxPool = new();

    [SerializeField] private TMP_Text handnessText;
    [SerializeField] private TMP_Text probText;
    [SerializeField] private Slider sliderRock;
    [SerializeField] private Slider sliderScissors;
    [SerializeField] private Slider sliderPaper;
    
    private bool _isTraining = false;
    List<string> _traingData = new List<string>();

    private HandShape handShapeIndex = HandShape.Rock;

    public enum HandShape
    {
        Rock = 0,
        Scissors = 1,
        Paper = 2
    }
    
    void Start()
    {
        targetTexture = new RenderTexture(resolution.x, resolution.y, 0);
        previewUI.texture = targetTexture;

        SetupInput();
        SetupModel();
        SetupEngine();
    }

    void SetupModel()
    {
        handLandmarkModel = ModelLoader.Load(Application.streamingAssetsPath + "/" + handLandmarkModelName);
        classificationModel = ModelLoader.Load(Application.streamingAssetsPath + "/" + classificationModelName);
        
        classificationModelWithArgMax = Functional.Compile(
            (landmark) =>
            {
                var prob = classificationModel.Forward(landmark)[0];
                return FF.Softmax(prob);
            },
            (classificationModel.inputs[0])
        );
    }

    public void SetupEngine()
    {
        worker1 = WorkerFactory.CreateWorker(backend, handLandmarkModel);
        worker2 = WorkerFactory.CreateWorker(backend, classificationModelWithArgMax);
    }

    void SetupInput()
    {
        webcam = new WebCamTexture(deviceName, resolution.x, resolution.y);
        webcam.requestedFPS = 30;
        webcam.Play();
    }

    void Update()
    {
        // Format video input
        if (!webcam.didUpdateThisFrame) return;

        var aspect1 = (float)webcam.width / webcam.height;
        var aspect2 = (float)resolution.x / resolution.y;
        var gap = aspect2 / aspect1;

        var vflip = webcam.videoVerticallyMirrored;
        var scale = new Vector2(gap, vflip ? -1 : 1);
        var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

        Graphics.Blit(webcam, targetTexture, scale, offset);
    }

    void LateUpdate()
    {
        RunInference(targetTexture);
    }

    void DrawLandmarks(TensorFloat landmarks, Vector2 scale)
    {
        //Draw the landmarks on the hand
        for (int j = 0; j < 21; j++)
        {
            var marker = new BoundingBox
            {
                centerX = landmarks[0, j * 3] * scale.x  - (size / 2) * scale.x,
                centerY = landmarks[0, j * 3 + 1] * scale.y  - (size/2) * scale.y,
                width = 8f * scale.x,
                height = 8f * scale.y,
            };
            DrawBox(marker, boxSprite, j);
        }
    }

    void RunInference(Texture source)
    {
        // Prepare input image
        var transform = new TextureTransform();
        transform.SetDimensions(size, size, 3);
        transform.SetTensorLayout(0, 3, 1, 2);
        using var image = TextureConverter.ToTensor(source, transform);

        // Execute inference
        worker1.Execute(image);
        
        using var landmarks = worker1.PeekOutput("Identity") as TensorFloat;
        
        ClearAnnotations();

        Vector2 markerScale = previewUI.rectTransform.rect.size / size;
        landmarks.CompleteOperationsAndDownload();

        bool showExtraInformation = true;
        if (showExtraInformation)
        {
            using var prob = worker1.PeekOutput("Identity_1") as TensorFloat;
            using var handness = worker1.PeekOutput("Identity_2") as TensorFloat;
            prob.CompleteOperationsAndDownload();
            handness.CompleteOperationsAndDownload();
            
            probText.text = prob[0].ToString();
            
            if (prob[0] > 0.95f)
            {
                // Determine hand orientation
                if (handness[0] < 0.2f)
                    handnessText.text = "Left";
                else if (handness[0] > 0.8f)
                    handnessText.text = "Right";
                else
                    handnessText.text = "";
            
                // Normalize landmark data
                float[] outputData = landmarks.ToReadOnlyArray();
                for (int index = 0; index < outputData.Length; index++)
                {
                    outputData[index] = (outputData[index] - halfSize) / halfSize;
                }

                // Execute secondary inference
                TensorShape shape = new TensorShape(1, 63);
                using var tensor = new TensorFloat(shape, outputData);
                worker2.Execute(tensor);
                using var classIndex = worker2.PeekOutput() as TensorFloat;

                if (classIndex != null)
                {
                    classIndex.CompleteOperationsAndDownload();

                    // Update slider values
                    sliderRock.value = classIndex[0];
                    sliderScissors.value = classIndex[1];
                    sliderPaper.value = classIndex[2];
                }
                
                DrawLandmarks(landmarks, markerScale);

                if (_isTraining)
                {
                    AddTrainingData((int)handShapeIndex, landmarks);
                }
            }
        }
    }
    
    private void AddTrainingData(int classIndex, TensorFloat landmarks)
    {
        string data = classIndex.ToString() + ",";
        for (int index = 0; index < 21; index++)
        {
            data += ((landmarks[index*3+0]-halfSize)/halfSize).ToString() + ",";
            data += ((landmarks[index*3+1]-halfSize)/halfSize).ToString() + ",";
            data += ((landmarks[index*3+2]-halfSize)/halfSize).ToString() + ",";
        }
        _traingData.Add(data);
    }

    public void DrawBox(BoundingBox box, Sprite sprite, int ID)
    {
        GameObject panel = null;
        if (ID >= boxPool.Count)
        {
            panel = new GameObject("landmark");
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<Image>();
            panel.transform.SetParent(previewUI.transform, false);
            boxPool.Add(panel);
        }
        else
        {
            panel = boxPool[ID];
            panel.SetActive(true);
        }

        var img = panel.GetComponent<Image>();
        img.color = Color.white;
        img.sprite = sprite;
        img.type = Image.Type.Sliced;

        panel.transform.localPosition = new Vector3(box.centerX, -box.centerY);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(box.width, box.height);
    }
    
    public void ClearAnnotations()
    {
        for (int i = 0; i < boxPool.Count; i++)
        {
            boxPool[i].SetActive(false);
        }
    }
    
    void CleanUp()
    {
        if (webcam) Destroy(webcam);
        RenderTexture.active = null;
        targetTexture.Release();

        worker1?.Dispose();
        worker1 = null;
        worker2?.Dispose();
        worker2 = null;
    }

    void OnDestroy()
    {
        CleanUp();
    }
    
    // Traninig
    [ContextMenu("Record_Rock")]
    private void EnableTrainingRock()
    {
        _traingData.Clear();
        handShapeIndex = HandShape.Rock;
        _isTraining = true;
        Debug.Log("EnableTraining_Rock");
    }
    
    [ContextMenu("Record_Scissors")]
    private void EnableTrainingScissors()
    {
        _traingData.Clear();
        handShapeIndex = HandShape.Scissors;
        _isTraining = true;
        Debug.Log("EnableTraining_Scissors");
    }
    
    [ContextMenu("Record_Paper")]
    private void EnableTrainingPaper()
    {
        _traingData.Clear();
        handShapeIndex = HandShape.Paper;
        _isTraining = true;
        Debug.Log("EnableTraining_Paper");
    }    
    
    [ContextMenu("Stop")]
    private void DisableTraining()
    {
        _isTraining = false;
        
        using (StreamWriter writer = new StreamWriter("points.csv", true))
        {
            foreach (string data in _traingData)
            {
                writer.WriteLine(data);
            }
        }
        
        Debug.Log("DisableTraining");
    }
}