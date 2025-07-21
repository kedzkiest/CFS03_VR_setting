using Mocopi.Receiver.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Mocopi.Receiver
{
    /// <summary>
    /// 任意の MocopiAvatarBase 派生クラスを受信対象にできる汎用レシーバ
    /// </summary>
    public sealed class MocopiGenericReceiver : MonoBehaviour
    {
        #region --Fields--
        /// <summary>Avatar &amp; Port ペア</summary>
        public List<AvatarPortPair> AvatarSettings = new List<AvatarPortPair>();

        /// <summary>OnEnable 時に自動開始するか</summary>
        public bool IsReceivingOnEnable = true;
        #endregion

        #region --Properties--
        private MocopiUdpReceiver[] UdpReceivers { get; set; }
        #endregion

        #region --Unity LifeCycle--
        private void OnEnable ()
        {
            if (IsReceivingOnEnable) UdpStart();
        }
        private void OnDisable ()
        {
            if (IsReceivingOnEnable) UdpStop();
        }
        private void OnDestroy ()
        {
            UnsetUdpDelegate();
        }
        #endregion

        #region --Public API--
        public void StartReceiving() { if (!IsReceivingOnEnable) UdpStart(); }
        public void StopReceiving () { if (!IsReceivingOnEnable) UdpStop();  }

        /// <summary>コードから Avatar を追加</summary>
        public void AddAvatar(MocopiAvatarBase avatar, int port)
            => AvatarSettings.Add(new AvatarPortPair(avatar, port));
        #endregion

        #region --Private--
        void UdpStart()
        {
            UdpStop();

            if (UdpReceivers == null || UdpReceivers.Length != AvatarSettings.Count)
                InitializeUdpReceiver();

            foreach (var r in UdpReceivers) r?.UdpStart();
        }

        void UdpStop()
        {
            if (UdpReceivers == null) return;
            foreach (var r in UdpReceivers) r?.UdpStop();
        }

        void InitializeUdpReceiver()
        {
            UnsetUdpDelegate();
            UdpReceivers = new MocopiUdpReceiver[AvatarSettings.Count];

            for (int i = 0; i < AvatarSettings.Count; i++)
            {
                var setting = AvatarSettings[i];
                if (setting.Avatar == null) continue;

                UdpReceivers[i] = new MocopiUdpReceiver(setting.Port);
                UdpReceivers[i].OnReceiveSkeletonDefinition += setting.Avatar.InitializeSkeleton;
                UdpReceivers[i].OnReceiveFrameData         += setting.Avatar.UpdateSkeleton;
            }
        }

        void UnsetUdpDelegate()
        {
            if (UdpReceivers == null) return;

            for (int i = 0; i < UdpReceivers.Length; i++)
            {
                var setting = AvatarSettings[i];
                if (setting?.Avatar == null || UdpReceivers[i] == null) continue;

                UdpReceivers[i].OnReceiveSkeletonDefinition -= setting.Avatar.InitializeSkeleton;
                UdpReceivers[i].OnReceiveFrameData         -= setting.Avatar.UpdateSkeleton;
                UdpReceivers[i].UdpStop();
            }
            UdpReceivers = null;
        }
        #endregion

        #region --Nested Class--
        /// <summary>Avatar と Port のセット</summary>
        [System.Serializable]
        public sealed class AvatarPortPair
        {
            public MocopiAvatarBase Avatar;
            public int Port = 12351;

            public AvatarPortPair(MocopiAvatarBase avatar, int port)
            { Avatar = avatar; Port = port; }
        }
        #endregion
    }
}
