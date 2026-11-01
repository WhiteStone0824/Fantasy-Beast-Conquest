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
    public sealed class EquipmentPlayModeTests
    {
        [UnityTest] public IEnumerator EquipSwapAndUnequipPersistWithoutDuplicatingItems()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=Object.FindAnyObjectByType<GameUIController>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var saveField=typeof(GameUIController).GetField("save",flags);
            var original=(GameSaveData)saveField.GetValue(ui);
            byte[] originalFile=File.Exists(SaveSystem.SavePath)?File.ReadAllBytes(SaveSystem.SavePath):null;
            var equip=typeof(GameUIController).GetMethod("SetEquipment",flags);
            var one=EquipmentCatalog.Roll(11,5);var two=EquipmentCatalog.Roll(12,5);
            var unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(original.units[0]));unit.id="equipment-test";unit.equipment=null;
            var test=new GameSaveData {equipmentInitialized=true,units=new List<UnitSaveData>{unit},equipmentInventory=new List<EquipmentItem>{one,two}};
            try
            {
                saveField.SetValue(ui,test);
                equip.Invoke(ui,new object[]{unit.id,one.id});
                var saved=SaveSystem.Load();Assert.AreEqual(one.id,saved.units[0].equipment.id);Assert.AreEqual(1,saved.equipmentInventory.Count);
                equip.Invoke(ui,new object[]{unit.id,two.id});
                saved=SaveSystem.Load();Assert.AreEqual(two.id,saved.units[0].equipment.id);Assert.AreEqual(one.id,saved.equipmentInventory[0].id);
                equip.Invoke(ui,new object[]{unit.id,null});
                saved=SaveSystem.Load();Assert.IsNull(saved.units[0].equipment);Assert.AreEqual(2,saved.equipmentInventory.Count);
            }
            finally
            {
                if(originalFile==null){if(File.Exists(SaveSystem.SavePath))File.Delete(SaveSystem.SavePath);}
                else File.WriteAllBytes(SaveSystem.SavePath,originalFile);
                saveField.SetValue(ui,original);
                typeof(GameUIController).GetMethod("Navigate",flags).Invoke(ui,new object[]{"HOME"});
            }
        }
    }
}
