using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class BreedingConsumptionPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator SuccessfulBreedingConsumesParentsAndPersistsChildAndFormation()
        { yield return Exercise(false); }

        [UnityTest]
        public IEnumerator FailedSaveRetainsParentsFormationAndSelection()
        { yield return Exercise(true); }

        [UnityTest]
        public IEnumerator SpiritAndMaterialProduceAndSaveAnArcaneOffspring()
        { yield return Exercise(false,SpeciesFamily.Spirit,SpeciesFamily.Material,SpeciesFamily.Arcane); }

        private IEnumerator Exercise(bool failSave,SpeciesFamily firstFamily=SpeciesFamily.Slime,SpeciesFamily secondFamily=SpeciesFamily.Slime,SpeciesFamily expectedFamily=SpeciesFamily.Slime)
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");
            yield return null;
            var controller=Object.FindAnyObjectByType<GameUIController>();
            var saveField=typeof(GameUIController).GetField("save",Flags);
            var original=(GameSaveData)saveField.GetValue(controller);
            byte[] originalFile=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            var navigate=typeof(GameUIController).GetMethod("Navigate",Flags);
            var breed=typeof(GameUIController).GetMethod("BreedSelected",Flags);
            var parentA=typeof(GameUIController).GetField("parentA",Flags);
            var parentB=typeof(GameUIController).GetField("parentB",Flags);
            object previousA=parentA.GetValue(controller),previousB=parentB.GetValue(controller);
            object previousSelected=typeof(GameUIController).GetField("selectedUnitId",Flags).GetValue(controller);
            var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var species=database.FindCandidates(firstFamily,SpeciesRank.F);
            var secondSpecies=database.FindCandidates(secondFamily,SpeciesRank.F)[firstFamily==secondFamily?1:0];
            var a=WildUnitFactory.Create(species[0],1,1,"consumption-parent-a");a.deployed=true;
            var b=WildUnitFactory.Create(secondSpecies,1,2,"consumption-parent-b");b.deployed=true;
            a.equipment=EquipmentCatalog.Roll(701,5);b.equipment=EquipmentCatalog.Roll(702,5);
            var other=WildUnitFactory.Create(species[0],1,3,"unrelated-unit");other.deployed=false;
            var testData=new GameSaveData {units=new List<UnitSaveData>{a,b,other},formationUnitIds=new List<string>{a.id,b.id},schemaVersion=failSave?SaveSystem.CurrentSchemaVersion+1:SaveSystem.CurrentSchemaVersion};
            try
            {
                saveField.SetValue(controller,testData);parentA.SetValue(controller,a.id);parentB.SetValue(controller,b.id);
                navigate.Invoke(controller,new object[]{"BREEDING"});
                Assert.IsNull(GameObject.Find("Button 別の結果を試す"));
                Assert.IsNull(GameObject.Find("Breeding Result"));
                foreach(TMPro.TMP_Text label in controller.GetComponentsInChildren<TMPro.TMP_Text>())
                    foreach(string hidden in new[]{"子どもの予測","子のランク：","種族候補：","抽選番号："})
                        StringAssert.DoesNotContain(hidden,label.text);
                string before=JsonUtility.ToJson(testData);
                if(failSave)LogAssert.Expect(LogType.Warning,"This save was created by a newer game version and is read-only.");
                breed.Invoke(controller,null);
                yield return null;
                var result=(GameSaveData)saveField.GetValue(controller);
                if(failSave)
                {
                    Assert.IsNull(GameObject.Find("Breeding Outcome"),"A failed save must not display a successful offspring result.");
                    Assert.AreSame(testData,result);
                    Assert.AreEqual(before,JsonUtility.ToJson(result));
                    Assert.AreEqual(a.id,parentA.GetValue(controller));Assert.AreEqual(b.id,parentB.GetValue(controller));
                    if(originalFile!=null)CollectionAssert.AreEqual(originalFile,File.ReadAllBytes(SaveSystem.SavePath));
                }
                else
                {
                    Assert.AreEqual(2,result.units.Count);
                    Assert.IsNull(result.units.Find(unit=>unit.id==a.id || unit.id==b.id));
                    Assert.AreEqual(a.displayName,AncestrySystem.Find(result,a.id)?.displayName);
                    Assert.AreEqual(b.displayName,AncestrySystem.Find(result,b.id)?.displayName);
                    Assert.IsTrue(result.equipmentInventory.Exists(item=>item.id==a.equipment.id));
                    Assert.IsTrue(result.equipmentInventory.Exists(item=>item.id==b.equipment.id));
                    UnitSaveData child=result.units.Find(unit=>unit.id!=other.id);
                    Assert.IsNotNull(child);Assert.AreEqual(a.id,child.fatherId);Assert.AreEqual(b.id,child.motherId);
                    Assert.AreEqual(expectedFamily.ToString(),child.family);
                    Assert.IsTrue(child.deployed);
                    CollectionAssert.AreEqual(new[]{child.id},result.formationUnitIds);
                    Assert.IsNull(parentA.GetValue(controller));Assert.IsNull(parentB.GetValue(controller));
                    GameSaveData loaded=SaveSystem.Load();
                    Assert.AreEqual(JsonUtility.ToJson(result),JsonUtility.ToJson(loaded));
                    Assert.IsNotNull(GameObject.Find("Breeding Outcome"));
                    GameObject childIcon=GameObject.Find("Breeding Child Icon "+child.id);
                    Assert.IsNotNull(childIcon);
                    Assert.IsNotNull(childIcon.transform.Find("キャラクターアイコン").GetComponent<UnityEngine.UI.Image>().sprite);
                    childIcon.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    yield return null;
                    Assert.AreEqual(child.id,typeof(GameUIController).GetField("selectedUnitId",Flags).GetValue(controller));
                    Assert.IsNotNull(GameObject.Find("Unit Detail"));
                    Assert.IsNotNull(GameObject.Find("Unit Icon "+child.id),"Opening the result must show the page containing the child.");
                    Assert.IsNotNull(GameObject.Find("Button 固有魔法を見る"));
                    string savedResult=JsonUtility.ToJson(result);
                    breed.Invoke(controller,null);
                    Assert.AreEqual(savedResult,JsonUtility.ToJson(saveField.GetValue(controller)),"The consumed parents must not be reusable.");
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                saveField.SetValue(controller,original);parentA.SetValue(controller,previousA);parentB.SetValue(controller,previousB);
                typeof(GameUIController).GetField("selectedUnitId",Flags).SetValue(controller,previousSelected);
                navigate.Invoke(controller,new object[]{"HOME"});
                if(originalFile!=null)File.WriteAllBytes(SaveSystem.SavePath,originalFile);
                else if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);
            }
        }
    }
}
