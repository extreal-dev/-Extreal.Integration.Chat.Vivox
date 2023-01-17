using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatMonobehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;

        private bool destroyed;

        private void OnDestroy()
            => destroyed = true;

        public void SetText(string message)
        {
            messageText.text = message;
            PassMessageAsync().Forget();
        }

        private async UniTaskVoid PassMessageAsync()
        {
            var ancestor = transform.parent;
            while (ancestor.GetComponent<Canvas>() == null)
            {
                ancestor = ancestor.transform.parent;
            }
            var canvasRectTransform = ancestor.GetComponent<RectTransform>();
            var canvasWidth = canvasRectTransform.rect.width;
            var canvasHeight = canvasRectTransform.rect.height;
            var velocity = Random.Range(0.2f, 0.5f) * canvasWidth;
            var lifetime = (canvasWidth + messageText.preferredWidth) / velocity;

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(messageText.preferredWidth, messageText.preferredHeight);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, messageText.preferredHeight);

            if ((Random.Range(0, 10) & 1) == 0)
            {
                rectTransform.anchoredPosition
                    = new Vector2
                    (
                        -messageText.preferredWidth,
                        Random.Range(0f, canvasHeight - messageText.preferredHeight)
                    );
            }
            else
            {
                rectTransform.anchoredPosition
                    = new Vector2
                    (
                        canvasWidth,
                        Random.Range(0f, canvasHeight - messageText.preferredHeight)
                    );
                velocity = -velocity;
            }

            for (var t = 0f; t < lifetime; t += Time.deltaTime)
            {
                if (destroyed)
                {
                    return;
                }

                rectTransform.anchoredPosition += velocity * Time.deltaTime * Vector2.right;
                await UniTask.Yield();
            }

            Destroy(gameObject);
        }
    }
}
