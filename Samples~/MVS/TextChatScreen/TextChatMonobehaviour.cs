using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatMonobehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;

        public void SetText(string message)
        {
            messageText.text = message;
            PassMessageAsync().Forget();
            Destroy(gameObject, 10);
        }

        private async UniTaskVoid PassMessageAsync()
        {
            var rectTransform = GetComponent<RectTransform>();
            var left2right = (Random.Range(0, 10) & 1) == 0;
            var velocity = Random.Range(1f, 3f);

            if (left2right)
            {

            }

            await UniTask.Yield();
        }
    }
}
