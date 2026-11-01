using System;
using System.Collections;
using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class PvpLanPlayModeTests
    {
        [UnityTest]
        public IEnumerator HostAcceptsClientTeamAndStreamsBattleState()
        {
            var db = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var hostSave = new GameSaveData();
            var guestTeam = new PvpTeam { playerRank=123 };
            for (int i=0;i<5;i++)
            {
                var hostUnit=WildUnitFactory.Create(db.GetById("demo_00"),10,12+i); hostUnit.deployed=true; hostSave.units.Add(hostUnit);
                var guestUnit=WildUnitFactory.Create(db.GetById("demo_00"),10,31+i); guestUnit.deployed=true; guestTeam.units.Add(guestUnit);
            }
            var host = PvpLanSession.Create();
            Assert.IsTrue(host.StartConnection(hostSave, db, true, "127.0.0.1", out var error), error);
            var go = new GameObject("PvP test client"); UnityEngine.Object.DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>(); var client = go.AddComponent<NetworkManager>();
            client.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false };
            transport.SetConnectionData("127.0.0.1", PvpLanSession.Port);
            bool sent = false, receivedOffer = false, receivedStart = false, receivedState = false;
            var packets = new Dictionary<int,string[]>();
            string message = "team\n" + JsonUtility.ToJson(guestTeam);
            int chunks = Mathf.CeilToInt((float)message.Length / 4000);
            client.OnClientConnectedCallback += id => { if (id == client.LocalClientId) sent = true; };
            Assert.IsTrue(client.StartClient());
            client.CustomMessagingManager.RegisterNamedMessageHandler("fam.pvp.v2", (ulong sender, FastBufferReader reader) =>
            {
                reader.ReadValueSafe(out int id); reader.ReadValueSafe(out int index); reader.ReadValueSafe(out int count); reader.ReadValueSafe(out string part);
                if (!packets.TryGetValue(id,out var parts)) packets.Add(id,parts=new string[count]);
                parts[index]=part;
                for(int i=0;i<parts.Length;i++) if(parts[i]==null) return;
                packets.Remove(id); string complete=string.Concat(parts);
                if (complete.StartsWith("offer\n", StringComparison.Ordinal))
                {
                    var hostTeam=JsonUtility.FromJson<PvpTeam>(complete.Substring(6));
                    receivedOffer=hostTeam!=null && hostTeam.Validate(db,out _) && hostTeam.units.Count==5 && hostTeam.playerRank==PlayerRankCalculator.Calculate(hostSave).Total;
                }
                if (complete.StartsWith("start\n", StringComparison.Ordinal)) receivedStart=true;
                if (complete.StartsWith("state\n", StringComparison.Ordinal)) receivedState = true;
            });
            try
            {
                float deadline = Time.realtimeSinceStartup + 15f;
                while (!sent && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(sent, "Client connection timed out.");
                for (int i = 0; i < chunks; i++)
                {
                    string part = message.Substring(i * 4000, Math.Min(4000, message.Length - i * 4000));
                    using (var writer = new FastBufferWriter(128, Allocator.Temp, 16128))
                    {
                        writer.WriteValueSafe(1); writer.WriteValueSafe(i); writer.WriteValueSafe(chunks); writer.WriteValueSafe(part);
                        client.CustomMessagingManager.SendNamedMessage("fam.pvp.v2", NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
                    }
                    yield return new WaitForSecondsRealtime(.03f);
                }
                deadline = Time.realtimeSinceStartup + 25f;
                while(!receivedOffer && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsTrue(receivedOffer,"Client did not receive the host rank and team.");
                Assert.IsFalse(host.MatchActive,"Battle must wait for both players.");
                SendControl(client,2,"offer_ack");
                while(!host.LobbyReady && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsTrue(host.LobbyReady);Assert.AreEqual(123,host.RemotePlayerRank);
                host.ConfirmReady();yield return null;
                Assert.IsFalse(host.MatchActive,"One ready player must not start the match.");
                SendControl(client,3,"ready");
                while ((!receivedStart || !receivedState || !host.MatchActive) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(host.MatchActive, "Host did not accept the team.");
                Assert.IsTrue(receivedStart, "Client did not receive the start message.");
                Assert.IsTrue(receivedState, "Client did not receive a combat snapshot.");
                var battle = UnityEngine.Object.FindAnyObjectByType<BattleDemo>();
                Assert.IsNotNull(battle); Assert.IsTrue(battle.IsPvp);
                Assert.AreEqual(5, battle.Allies.Count); Assert.AreEqual(5, battle.Enemies.Count);
                yield return new WaitForSeconds(4f);
                bool damaged=false;
                foreach(var unit in battle.Allies) if(unit!=null && unit.HealthRatio<.99f) damaged=true;
                foreach(var unit in battle.Enemies) if(unit!=null && unit.HealthRatio<.99f) damaged=true;
                Assert.IsTrue(damaged,"Both teams must find each other and fight automatically.");
            }
            finally { client.Shutdown(); host.Close(); }
            float stopDeadline = Time.realtimeSinceStartup + 5f;
            while (client.ShutdownInProgress && Time.realtimeSinceStartup < stopDeadline) yield return null;
            UnityEngine.Object.Destroy(go);
            yield return null;
        }
        private static void SendControl(NetworkManager client,int id,string kind)
        {
            using(var writer=new FastBufferWriter(256,Allocator.Temp))
            {writer.WriteValueSafe(id);writer.WriteValueSafe(0);writer.WriteValueSafe(1);writer.WriteValueSafe(kind+"\n");client.CustomMessagingManager.SendNamedMessage("fam.pvp.v2",NetworkManager.ServerClientId,writer,NetworkDelivery.ReliableFragmentedSequenced);}
        }
    }
}
