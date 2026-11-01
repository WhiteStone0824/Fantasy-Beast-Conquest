using System.Collections;
using System.Reflection;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FusionAnyMonster.Tests
{
    public sealed class StatBarComparisonPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityTest]
        public IEnumerator EqualAttackValuesKeepTheirBarLengthWhenCharactersHaveDifferentHp()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Game/Scenes/Title.unity");yield return null;
            var ui=Object.FindAnyObjectByType<GameUIController>();
            var save=(GameSaveData)typeof(GameUIController).GetField("save",Flags).GetValue(ui);
            var first=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(save.units[0]));
            var second=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(first));
            first.id="scale-first";second.id="scale-second";
            first.individualValues[0]=0;second.individualValues[0]=100;
            string original=JsonUtility.ToJson(save);
            var page=typeof(GameUIController).GetField("unitDetailPage",Flags);object previous=page.GetValue(ui);page.SetValue(ui,0);
            try
            {
                Show(ui,first);yield return null;
                float attack=Width(2),hp=Width(0);
                Show(ui,second);yield return null;
                Assert.Greater(Width(0),hp,"A higher HP value must make only the HP bar longer.");
                Assert.AreEqual(attack,Width(2),.0001f,"The same attack value must have identical width even when another stat changes.");
                Show(ui,first);yield return null;
                Assert.AreEqual(attack,Width(2),.0001f,"Switching order must not change the common scale.");
                Assert.AreEqual(original,JsonUtility.ToJson(save));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                page.SetValue(ui,previous);
                typeof(GameUIController).GetMethod("Navigate",Flags).Invoke(ui,new object[]{"HOME"});
            }
        }
        private static void Show(GameUIController ui,UnitSaveData unit)
        {
            typeof(GameUIController).GetMethod("ClearContent",Flags).Invoke(ui,null);
            typeof(GameUIController).GetMethod("ShowUnitDetail",Flags).Invoke(ui,new object[]{unit});
        }
        private static float Width(int index) { return GameObject.Find("Stat Fill "+index).GetComponent<RectTransform>().anchorMax.x; }
    }
}
