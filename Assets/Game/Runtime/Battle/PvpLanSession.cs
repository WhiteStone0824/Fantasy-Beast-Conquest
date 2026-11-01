using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using FusionAnyMonster.Save;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionAnyMonster.Battle
{
    // The host runs the only combat simulation. Clients receive presentation snapshots.
    public sealed class PvpLanSession : MonoBehaviour
    {
        public const ushort Port = 55555;
        private const string MessageName = "fam.pvp.v2";
        private const int ChunkSize = 4000, MaximumChunks = 512;
        private sealed class Incoming { public string[] parts; public int received; public float time; }
        private readonly Dictionary<string, Incoming> incoming = new Dictionary<string, Incoming>();
        private NetworkManager manager;
        private bool closing;
        private PvpTeam localTeam;
        private int nextMessage;
        private ulong remoteId;
        public static PvpLanSession Instance { get; private set; }
        public PvpTeam HostTeam { get; private set; }
        public PvpTeam GuestTeam { get; private set; }
        public bool MatchActive { get; private set; }
        public bool LobbyReady { get; private set; }
        public bool LocalReady { get; private set; }
        public bool RemoteReady { get; private set; }
        public int? LocalPlayerRank { get { return localTeam==null?(int?)null:localTeam.playerRank; } }
        public int? RemotePlayerRank { get { var team=IsHost?GuestTeam:HostTeam;return team==null?(int?)null:team.playerRank; } }
        public bool IsHost { get { return manager != null && manager.IsHost; } }
        public string Status { get; private set; } = "接続していません";
        public event Action Changed;
        public event Action<string> SnapshotReceived;

        public static string[] GetLocalIPv4Addresses()
        {
            var addresses=new List<string>();
            try
            {
                foreach(var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if(adapter.OperationalStatus!=System.Net.NetworkInformation.OperationalStatus.Up || adapter.NetworkInterfaceType==System.Net.NetworkInformation.NetworkInterfaceType.Loopback)continue;
                    foreach(var unicast in adapter.GetIPProperties().UnicastAddresses)
                    {
                        var ip=unicast.Address;
                        if(ip.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork || System.Net.IPAddress.IsLoopback(ip))continue;
                        string value=ip.ToString();if(!addresses.Contains(value))addresses.Add(value);
                    }
                }
            }
            catch(System.Net.NetworkInformation.NetworkInformationException e){Debug.LogWarning("IPアドレスを取得できません: "+e.Message);}
            addresses.Sort(StringComparer.Ordinal);return addresses.ToArray();
        }

        public static PvpLanSession Create()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("LAN PvP Session");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PvpLanSession>();
            return Instance;
        }

        public bool StartConnection(GameSaveData save, SpeciesDatabase database, bool host, string address, out string error)
        {
            error = null;
            if (manager != null || closing) { error = "既に接続中、または切断処理中です。"; return false; }
            if (!PvpTeam.TryCreate(save, database, out var candidateTeam, out error)) return false;
            address=address==null?"":address.Trim();
            if (!host && (!System.Net.IPAddress.TryParse(address, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || ip.Equals(System.Net.IPAddress.Any) || ip.Equals(System.Net.IPAddress.Broadcast))) { error = "ホストのIPv4アドレスを入力してください。"; return false; }
            localTeam=candidateTeam;
            foreach (var unit in localTeam.units) unit.generatedSpritePath = "";
            var transport = gameObject.AddComponent<UnityTransport>();
            manager = gameObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false };
            transport.SetConnectionData(host ? "127.0.0.1" : address, Port, host ? "0.0.0.0" : null);
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            bool started = host ? manager.StartHost() : manager.StartClient();
            if (!started) { error = "接続を開始できませんでした。"; Close(); return false; }
            manager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, ReceiveChunk);
            SetStatus(host ? "対戦相手を待っています（ポート " + Port + "）" : "ホストへ接続しています…");
            return true;
        }

        private void Connected(ulong client)
        {
            if (manager == null) return;
            if (manager.IsHost)
            {
                if (client == manager.LocalClientId) return;
                if (remoteId != 0 || MatchActive) { manager.DisconnectClient(client); return; }
                remoteId = client; SetStatus("対戦相手の編成を受信中…");
            }
            else if (client == manager.LocalClientId)
            {
                SetStatus("接続しました。編成を送信中…");
                Send(NetworkManager.ServerClientId, "team", JsonUtility.ToJson(localTeam));
            }
        }

        private void Disconnected(ulong client)
        {
            if (manager == null || (manager.IsHost && client != remoteId)) return;
            SetStatus("対戦相手との接続が切れました。");
            if (MatchActive) { var battle = FindAnyObjectByType<BattleDemo>(); if (battle != null) battle.AbortPvp(); }
            else { LobbyReady=LocalReady=RemoteReady=false;HostTeam=GuestTeam=null;incoming.Clear(); }
            remoteId = 0;
        }

        private void Send(ulong to, string kind, string payload)
        {
            string message = kind + "\n" + payload;
            int count = Mathf.CeilToInt((float)message.Length / ChunkSize);
            if (count < 1 || count > MaximumChunks) throw new InvalidOperationException("対戦データが大きすぎます。");
            int id = ++nextMessage;
            StartCoroutine(SendChunks(to, message, id, count,kind=="state"));
        }
        private IEnumerator SendChunks(ulong to, string message, int id, int count,bool realtimeState)
        {
            for (int i = 0; i < count; i++)
            {
                if (manager == null || !manager.IsListening) yield break;
                string chunk = message.Substring(i * ChunkSize, Math.Min(ChunkSize, message.Length - i * ChunkSize));
                using (var writer = new FastBufferWriter(128, Allocator.Temp, ChunkSize * 4 + 128))
                {
                    writer.WriteValueSafe(id); writer.WriteValueSafe(i); writer.WriteValueSafe(count); writer.WriteValueSafe(chunk);
                    manager.CustomMessagingManager.SendNamedMessage(MessageName, to, writer, NetworkDelivery.ReliableFragmentedSequenced);
                }
                if (count > 1 && !realtimeState) yield return new WaitForSecondsRealtime(.03f);
            }
        }

        private void ReceiveChunk(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out int id); reader.ReadValueSafe(out int index); reader.ReadValueSafe(out int count); reader.ReadValueSafe(out string part);
                if (count < 1 || count > MaximumChunks || index < 0 || index >= count || part == null || part.Length > ChunkSize) return;
                if (manager.IsHost ? sender != remoteId : sender != NetworkManager.ServerClientId) return;
                string key = sender + ":" + id;
                if (!incoming.TryGetValue(key, out var message))
                {
                    if (incoming.Count >= 8) return;
                    message = new Incoming { parts = new string[count], time = Time.realtimeSinceStartup }; incoming.Add(key, message);
                }
                if (message.parts.Length != count || message.parts[index] != null) return;
                message.parts[index] = part; message.received++;
                if (message.received != count) return;
                incoming.Remove(key);
                string data = string.Concat(message.parts); int split = data.IndexOf('\n'); if (split < 0) return;
                Handle(sender, data.Substring(0, split), data.Substring(split + 1));
            }
            catch (Exception exception) { Debug.LogWarning("PvP message ignored: " + exception.Message); }
        }

        private void Handle(ulong sender, string kind, string payload)
        {
            if (kind == "team" && manager.IsHost && !MatchActive)
            {
                var other = JsonUtility.FromJson<PvpTeam>(payload);
                var db = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                if (other == null || !other.Validate(db, out _)) { SetStatus("相手の編成データが不正です。"); manager.DisconnectClient(sender); return; }
                foreach (var unit in other.units) unit.generatedSpritePath = "";
                HostTeam = localTeam; GuestTeam = other;
                Send(sender, "offer", JsonUtility.ToJson(HostTeam));
                SetStatus("相手に編成を送信中…");
            }
            else if (kind == "offer" && !manager.IsHost && !MatchActive && HostTeam==null)
            {
                var hostTeam = JsonUtility.FromJson<PvpTeam>(payload);
                if (hostTeam == null || !hostTeam.Validate(Resources.Load<SpeciesDatabase>("SpeciesDatabase"), out _)) { SetStatus("ホストの編成データが不正です。"); Close(); return; }
                foreach (var unit in hostTeam.units) unit.generatedSpritePath = "";
                HostTeam = hostTeam; GuestTeam = localTeam; LobbyReady=true;
                Send(NetworkManager.ServerClientId,"offer_ack","");
                SetStatus("相手のランクを確認して準備完了を押してください。");
            }
            else if(kind=="offer_ack" && manager.IsHost && GuestTeam!=null && !MatchActive)
            { LobbyReady=true;SetStatus("相手のランクを確認して準備完了を押してください。"); }
            else if(kind=="ready" && LobbyReady && !MatchActive)
            { RemoteReady=true;SetStatus("相手が準備完了しました。");if(manager.IsHost)TryStartMatch(); }
            else if (kind == "start" && !manager.IsHost && !MatchActive && LobbyReady && LocalReady && HostTeam!=null)
            {
                MatchActive=true;
                SceneManager.LoadScene("Battle");
            }
            else if (kind == "state" && !manager.IsHost && MatchActive) SnapshotReceived?.Invoke(payload);
        }

        public void ConfirmReady()
        {
            if(manager==null || !manager.IsListening || !LobbyReady || LocalReady || MatchActive)return;
            LocalReady=true;
            Send(manager.IsHost?remoteId:NetworkManager.ServerClientId,"ready","");
            SetStatus(RemoteReady?"対戦を開始します…":"相手の準備を待っています…");
            if(manager.IsHost)TryStartMatch();
        }
        private void TryStartMatch()
        {
            if(!IsHost || !LobbyReady || !LocalReady || !RemoteReady || MatchActive || remoteId==0)return;
            MatchActive=true;Send(remoteId,"start","");SceneManager.LoadScene("Battle");
        }

        public void SendSnapshot(string json) { if (IsHost && MatchActive && remoteId != 0) Send(remoteId, "state", json); }
        private void Update()
        {
            if (incoming.Count == 0) return;
            var expired = new List<string>(); foreach (var item in incoming) if (Time.realtimeSinceStartup - item.Value.time > 15) expired.Add(item.Key);
            foreach (var key in expired) incoming.Remove(key);
        }
        private void SetStatus(string message) { Status = message; Changed?.Invoke(); }
        public void Close()
        {
            MatchActive = LobbyReady = LocalReady = RemoteReady = false; HostTeam = GuestTeam = localTeam = null; incoming.Clear(); remoteId = 0;
            if (manager != null && !closing)
            {
                closing = true;
                manager.OnClientConnectedCallback -= Connected; manager.OnClientDisconnectCallback -= Disconnected;
                manager.Shutdown(); StartCoroutine(FinishShutdown(manager, GetComponent<UnityTransport>()));
                SetStatus("切断しています…");
            }
            else if (!closing) SetStatus("接続していません");
        }
        private IEnumerator FinishShutdown(NetworkManager closingManager, UnityTransport transport)
        {
            while (closingManager != null && closingManager.ShutdownInProgress) yield return null;
            if (closingManager != null) Destroy(closingManager);
            if (transport != null) Destroy(transport);
            manager = null; closing = false; SetStatus("接続していません");
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
