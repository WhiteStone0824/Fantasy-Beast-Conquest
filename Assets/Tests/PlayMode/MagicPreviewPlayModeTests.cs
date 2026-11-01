using System.Collections;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using FusionAnyMonster.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FusionAnyMonster.Tests
{
    public sealed class MagicPreviewPlayModeTests
    {
        [UnityTest]
        public IEnumerator SupportPreviewShowsInjuredAlliesHealingAndBuffAuras()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            yield return null;
            var source=new UnitSaveData { id="preview-support",displayName="支援の仲間",rank="S",personality="Gentle",baseStats=new float[]{100,30,10,10,0,0,2,1,8,1} };
            string original=JsonUtility.ToJson(source);
            var genes=SpellProgramBuilder.Projectile(effect:SpellNodeKind.Heal,target:SpellTarget.LowestHealthAlly,trajectory:SpellTrajectory.Homing);
            foreach(var p in new[]{genes.chromosomeA,genes.chromosomeB}){var buff=SpellProgramBuilder.Add(p,SpellNodeKind.ApplyBuff,SpellContext.Hit);buff.parameters.buff=BuffType.DefenseBoost;buff.parameters.multiplier=.2f;p.nodes[2].edges.Add(new SpellEdge(SpellPort.Next,buff.id));}
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=genes},0);
            var host=new GameObject("Support Preview Test",typeof(RectTransform),typeof(RawImage));
            var preview=host.AddComponent<MagicPreviewController>();
            try
            {
                preview.Initialize(source,null,null,genome,null,host.GetComponent<RawImage>());
                Combatant recipient=System.Array.Find(Object.FindObjectsByType<Combatant>(),unit=>unit.CharacterName=="練習用の味方" && unit.transform.position.x>10002);
                Assert.IsNotNull(recipient);
                float before=recipient.HealthRatio;
                Assert.That(before,Is.EqualTo(.3f).Within(.001f));
                foreach(Combatant unit in Object.FindObjectsByType<Combatant>())Assert.IsTrue(unit.IsFriendly);
                yield return new WaitForSeconds(.6f);
                Assert.Greater(recipient.HealthRatio,before);
                Assert.IsNotNull(recipient.transform.Find("Support aura"));
                Assert.IsNotNull(GameObject.Find("Preview ally health"));
                Assert.AreEqual(original,JsonUtility.ToJson(source));
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.Destroy(host); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllSpawnAndTrajectoryPreviewsCanBeSwitchedWithoutLeakingSimulations()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            yield return null;
            var source=new UnitSaveData { id="preview-switch", displayName="試射の仲間", rank="S", personality="Brave", baseStats=new float[]{100,30,10,10,0,0,2,1,8,1} };
            var host=new GameObject("Preview Switch Test",typeof(RectTransform),typeof(RawImage));
            var preview=host.AddComponent<MagicPreviewController>();
            try
            {
                foreach(SpellTrajectory trajectory in System.Enum.GetValues(typeof(SpellTrajectory)))
                {
                    var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(count:5,trajectory:trajectory)},0);
                    preview.Initialize(source,null,null,genome,null,host.GetComponent<RawImage>());
                    yield return new WaitForSeconds(.3f);
                    Assert.AreEqual(1,Object.FindObjectsByType<BattleDemo>().Length,"Switching must discard the previous simulation.");
                    Assert.AreEqual(8,Object.FindObjectsByType<Combatant>().Length,"Every preview must contain only its caster and training targets.");
                    Assert.IsNotNull(preview.OutputTexture);
                    Camera camera=GameObject.Find("Magic Preview Camera").GetComponent<Camera>();
                    Combatant caster=System.Array.Find(Object.FindObjectsByType<Combatant>(),unit=>unit.IsFriendly);
                    Vector3 center=camera.WorldToViewportPoint(caster.transform.position);
                    Assert.That(center.x,Is.EqualTo(.5f).Within(.001f),"The caster must remain horizontally centered for every trajectory.");
                    Assert.That(center.y,Is.EqualTo(.5f).Within(.001f),"The caster must remain vertically centered for every trajectory.");
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.Destroy(host); }
            yield return null;
            Assert.AreEqual(0,Object.FindObjectsByType<Combatant>().Length);
            Assert.AreEqual(0,Object.FindObjectsByType<MagicImpactVisual>().Length,"Preview effects must be discarded even when pooled.");
        }

        [UnityTest]
        public IEnumerator PreviewRendersReplaysAndDisposesWithoutChangingProgress()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            yield return null;
            var genome = SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile(count:5)},0);
            var source = new UnitSaveData {
                id="preview-only", displayName="試射の仲間", rank="S", personality="Brave",
                baseStats=new float[]{100,30,10,10,0,0,2,1,8,1},
                magicGenomeJson=new[]{JsonUtility.ToJson(genome)}
            };
            string before=JsonUtility.ToJson(source);
            string saved=System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllText(SaveSystem.SavePath):null;
            var host=new GameObject("Preview Test",typeof(RectTransform),typeof(RawImage));
            var preview=host.AddComponent<MagicPreviewController>();
            Texture2D sample=null;
            int saves=0;
            System.Action<GameSaveData> savedHandler=data=>saves++;
            SaveSystem.Saved+=savedHandler;
            try
            {
                var image=host.GetComponent<RawImage>();
                preview.Initialize(source,null,null,genome,null,image);
                Assert.AreEqual(1,preview.PlaybackCount);
                yield return null;
                Assert.Greater(Object.FindObjectsByType<SpellProjectile>().Length,0,"The actual battle projectile implementation must run in the preview.");
                for(int i=0;i<4;i++)yield return null;
                Assert.IsTrue(preview.OutputTexture.IsCreated());
                sample=new Texture2D(32,32,TextureFormat.RGB24,false);
                RenderTexture previous=RenderTexture.active;
                try
                {
                    RenderTexture.active=preview.OutputTexture;
                    sample.ReadPixels(new Rect(464,254,32,32),0,0);sample.Apply();
                }
                finally { RenderTexture.active=previous; }
                bool rendered=false;
                foreach(Color pixel in sample.GetPixels())if(pixel.g>.04f && pixel.b>.06f){rendered=true;break;}
                Assert.IsTrue(rendered,"The preview camera must render the practice ground into its texture.");
                RenderTexture texture=preview.OutputTexture;
                preview.Replay();
                Assert.AreEqual(2,preview.PlaybackCount);
                Assert.AreSame(texture,preview.OutputTexture,"Replay must reuse the display texture.");
                yield return null;
                Assert.AreEqual(1,Object.FindObjectsByType<BattleDemo>().Length,"Replay must retire the previous simulation.");
                Assert.AreEqual(before,JsonUtility.ToJson(source));
                Assert.AreEqual(0,saves,"Preview combat must never save progression.");
                Assert.AreEqual(saved,System.IO.File.Exists(SaveSystem.SavePath)?System.IO.File.ReadAllText(SaveSystem.SavePath):null);
                host.SetActive(false);
                Assert.IsNull(preview.OutputTexture);
                Assert.IsNull(image.texture);
                Assert.IsNull(GameObject.Find("Magic Preview World"));
                yield return null;
                Assert.AreEqual(0,Object.FindObjectsByType<SpellProjectile>().Length);
                Assert.AreEqual(0,Object.FindObjectsByType<BattleDemo>().Length);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SaveSystem.Saved-=savedHandler;
                Object.Destroy(host);
                if(sample!=null)Object.Destroy(sample);
            }
        }
    }
}
