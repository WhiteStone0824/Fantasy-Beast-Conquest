using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FusionAnyMonster.Tests
{
    public sealed class SpeciesDiscoveryPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator CatalogueMasksUnknownSpeciesAndKeepsFormerCompanionsVisible()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=Object.FindAnyObjectByType<GameUIController>();
            var saveField=typeof(GameUIController).GetField("save",Flags);
            var original=(GameSaveData)saveField.GetValue(ui);
            var database=(SpeciesDatabase)typeof(GameUIController).GetField("speciesDatabase",Flags).GetValue(ui);
            var ordered=new List<SpeciesDefinition>();
            foreach(var species in database.species)if(species!=null)ordered.Add(species);
            ordered.Sort((a,b)=>{int rank=a.rank.CompareTo(b.rank);if(rank!=0)return rank;int family=a.family.CompareTo(b.family);return family!=0?family:string.CompareOrdinal(a.speciesId,b.speciesId);});
            var known=ordered[0];var unknown=ordered[1];
            var test=new GameSaveData{discoveredSpeciesIds=new List<string>{known.speciesId}};
            try
            {
                saveField.SetValue(ui,test);
                typeof(GameUIController).GetField("speciesPage",Flags).SetValue(ui,0);
                typeof(GameUIController).GetMethod("Navigate",Flags).Invoke(ui,new object[]{"SPECIES"});
                yield return null;
                var hidden=GameObject.Find("Species Icon "+unknown.speciesId).GetComponent<Button>();
                Assert.IsFalse(hidden.interactable);
                var silhouette=hidden.transform.Find("キャラクターアイコン").GetComponent<Image>();
                Assert.AreEqual((Color)new Color32(0x27,0x3e,0x57,0xff),silhouette.color);
                Assert.AreEqual("Fusion Any Monster/UI/Catalogue Silhouette",silhouette.material.shader.name);
                var texts=hidden.GetComponentsInChildren<TMP_Text>();
                Assert.IsTrue(System.Array.Exists(texts,t=>t.text=="???"));
                Assert.IsFalse(System.Array.Exists(texts,t=>t.text==unknown.displayName));
                hidden.onClick.Invoke();yield return null;
                Assert.IsNull(GameObject.Find("Species Detail"));
                typeof(GameUIController).GetMethod("ShowSpeciesDetail",Flags).Invoke(ui,new object[]{unknown});yield return null;
                Assert.IsNull(GameObject.Find("Species Detail"));
                var revealed=GameObject.Find("Species Icon "+known.speciesId).GetComponent<Button>();
                Assert.IsTrue(revealed.interactable);
                Assert.AreEqual(Color.white,revealed.transform.Find("キャラクターアイコン").GetComponent<Image>().color);
                revealed.onClick.Invoke();yield return null;
                Assert.IsNotNull(GameObject.Find("Species Detail"));
                // A newly acquired species becomes visible without revealing the rest of the catalogue.
                test.units.Add(new UnitSaveData{speciesId=unknown.speciesId});
                typeof(GameUIController).GetMethod("ShowSpecies",Flags).Invoke(ui,null);yield return null;
                Assert.IsTrue(GameObject.Find("Species Icon "+unknown.speciesId).GetComponent<Button>().interactable);
                Assert.AreEqual(2,test.discoveredSpeciesIds.Count);
            }
            finally
            {
                saveField.SetValue(ui,original);
                typeof(GameUIController).GetMethod("Navigate",Flags).Invoke(ui,new object[]{"HOME"});
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
