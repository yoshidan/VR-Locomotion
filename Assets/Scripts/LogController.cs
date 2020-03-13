using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

namespace App
{
    
    public class LogController : MonoBehaviour
    {
        public Vector3 consoleDistance = new Vector3(0, 0, 2.5f);
        public Vector2 consoleSize = new Vector2(1.875f, 1.5f);
        public float dynamicPixelsPerUnit = 8000.0f;

        private GameObject _content;

        private Font _font;

        private class Message
        {
            public LogType type;
            public string condition;
            public string stackTrace;
        }

        private readonly ConcurrentQueue<Message> _messsages = new ConcurrentQueue<Message>();

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            _messsages.Enqueue(new Message()
            {
                type = type,
                condition = condition,
                stackTrace = stackTrace
            });
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            transform.localPosition = consoleDistance;

            CreateCanvas();

            var (scrollTransform, scrollRect) = CreateScrollView();

            _content = CreateScrollContent(scrollRect);

            //set parent
            _content.transform.SetParent(scrollTransform);
            scrollTransform.SetParent(transform);
            transform.SetParent(Camera.main.transform);
            scrollTransform.localPosition = Vector3.zero;

        }

        public void Update()
        {

            if (_messsages.TryDequeue(out var message))
            {
                var text = CreateText();
                var shouldOutputStackTrace = false;
                switch (message.type)
                {
                    case LogType.Log:
                    {
                        text.color = Color.white;
                        break;
                    }
                    case LogType.Assert:
                    case LogType.Warning:
                    {
                        text.color = Color.yellow;
                        break;
                    }
                    case LogType.Error:
                    case LogType.Exception:
                    {
                        shouldOutputStackTrace = true;
                        text.color = Color.red;
                        break;
                    }
                }

                var stack = shouldOutputStackTrace ? "\n" + message.stackTrace : "";
                text.text = $"[{message.type.ToString()}] : {message.condition}{stack}";
            }

        }

        private Text CreateText()
        {
            var textObject = new GameObject("Log");
            var textTransform = textObject.transform;
            textTransform.SetParent(_content.transform);
            textTransform.localPosition = Vector3.zero;
            textTransform.localRotation = Quaternion.identity;

            var text = textObject.AddComponent<Text>();
            text.font = _font;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 1;

            var rect = textObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 1.0f);
            rect.sizeDelta = new Vector2(10, 0.5f);

            return text;
        }

        private void CreateCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            gameObject.GetComponent<RectTransform>().sizeDelta = consoleSize;
            gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            gameObject.AddComponent<OVRRaycaster>();

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = dynamicPixelsPerUnit;
            scaler.referencePixelsPerUnit = scaler.dynamicPixelsPerUnit;
        }

        public (Transform, ScrollRect) CreateScrollView()
        {
            var scroll = new GameObject("ScrollView");
            scroll.AddComponent<Mask>();
            scroll.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            scroll.GetComponent<RectTransform>().sizeDelta = consoleSize;

            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            return (scroll.transform, scrollRect);
        }

        private GameObject CreateScrollContent(ScrollRect scrollRect)
        {
            var content = new GameObject("Content");

            var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 0.1f;

            var sizeFilter = content.AddComponent<ContentSizeFitter>();
            sizeFilter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentRectTransform = content.GetComponent<RectTransform>();
            contentRectTransform.pivot = new Vector2(0.5f, 1.0f);
            contentRectTransform.sizeDelta = consoleSize;

            scrollRect.content = contentRectTransform;
            return content;
        }

    }
}