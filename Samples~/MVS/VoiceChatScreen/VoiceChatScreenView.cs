using System;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenView : MonoBehaviour
    {
        [SerializeField] private Button muteButton;

        public IObservable<Unit> OnMuteButtonClicked => muteButton.OnClickAsObservable().TakeUntilDestroy(this);

        public void ShowMuteIcon(bool value)
        {
            if (value)
            {

            }
        }
    }
}
