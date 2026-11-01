using System;
using System.Collections.Generic;
using System.Text;
namespace FusionAnyMonster.Spells
{
    public static class SpellPlayerDescription
    {
        private static string Number(float v){return Math.Ceiling(Math.Round(v,4)).ToString("0");}
        private static readonly string[] Statuses={"毒","火傷","凍結","麻痺","睡眠","鈍足","沈黙","呪い"};
        private static string Effect(SpellInstruction n)
        {
            switch(n.kind)
            {
                case SpellNodeKind.ApplyStatus:return Statuses[(int)n.parameters.status];case SpellNodeKind.Heal:return "HP回復";
                case SpellNodeKind.ApplyBuff:return UI.MagicDescription.EffectName(n.parameters.buff);case SpellNodeKind.Explosion:return "爆発";
                case SpellNodeKind.SplitProjectile:return "分裂・連鎖";case SpellNodeKind.LifeSteal:return "HP吸収";
                case SpellNodeKind.ManaSteal:return "MP吸収";case SpellNodeKind.Knockback:return "ノックバック";default:return null;
            }
        }
        public static string Effects(CompiledSpell spell,int maximum)
        {
            var names=new List<string>();foreach(var n in spell.instructions){string name=Effect(n);if(name!=null && !names.Contains(name))names.Add(name);}
            if(names.Count==0)return "追加効果なし";
            string text=string.Join("・",names.GetRange(0,Math.Min(names.Count,maximum)));
            return names.Count>maximum?text+" ほか"+(names.Count-maximum)+"種":text;
        }
        public static string Behavior(CompiledSpell spell)
        {
            var root=spell.instructions[spell.root];string trigger=root.kind==SpellNodeKind.EnemyNearTrigger?"敵が"+Number(root.parameters.radius)+"以内に接近":root.kind==SpellNodeKind.HPThresholdTrigger?"HPが"+Number(root.parameters.threshold*100)+"%以下":"使用可能になり次第、自動発動";
            string target=SpellExecutor.IsSupportTarget(spell.target)?"味方（自分を含む）":"敵";
            return "対象："+target+"\n発動："+trigger+"\n再使用："+Number(spell.cooldown)+"秒\n射程："+Number(spell.range)+"\n途中の分岐・命中・消滅によって次の効果が変化します。実戦ではMPと有効な対象も必要です。";
        }
        public static string Details(CompiledSpell spell)
        {
            var text=new StringBuilder();Describe(spell,spell.root,0,text);text.AppendLine().Append("状態異常の付与率は相手の属性抵抗・デバフ抵抗で低下します。各弾の寿命は最大").Append(Number(spell.maxLifetime)).Append("秒です。");return text.ToString();
        }
        private static void Describe(CompiledSpell s,int index,int depth,StringBuilder text)
        {
            var n=s.instructions[index];var p=n.parameters;string description=null;
            switch(n.kind)
            {
                case SpellNodeKind.SpawnProjectile:case SpellNodeKind.SplitProjectile:
                    string[] paths={"直進","追尾","螺旋","波状","周回"};description=(n.kind==SpellNodeKind.SplitProjectile?"新たに":"")+p.count+"発の"+paths[(int)p.trajectory]+"弾を生成";break;
                case SpellNodeKind.SpawnArea:description="半径"+Number(p.radius)+"の範囲を"+Number(p.seconds)+"秒間展開。0.5秒ごとに作用";break;
                case SpellNodeKind.Damage:description="威力と魔法攻撃力に応じたダメージ（倍率"+p.multiplier.ToString("0.##")+"）";break;
                case SpellNodeKind.Heal:description="味方のHPを回復（威力と魔法攻撃力に応じる）";break;
                case SpellNodeKind.Explosion:description="確率"+Number(p.probability*100)+"%で半径"+Number(p.radius)+"の爆発ダメージ";break;
                case SpellNodeKind.ApplyStatus:
                    description=Statuses[(int)p.status]+"：基礎確率"+Number(p.probability*100)+"%、持続"+(p.status==DebuffType.Poison?"10":p.status==DebuffType.Burn?"4":Number(p.seconds))+"秒。";
                    description+=p.status==DebuffType.Poison?"毎秒、最大HPの2%ダメージ":p.status==DebuffType.Burn?"毎秒、最大HPの5%ダメージ":p.status==DebuffType.Slow?"移動速度45%低下":p.status==DebuffType.Silence?"魔法を封じる":p.status==DebuffType.Curse?"与ダメージ30%低下":"移動と攻撃を停止";break;
                case SpellNodeKind.Knockback:description="確率"+Number(p.probability*100)+"%で押し戻す";break;
                case SpellNodeKind.LifeSteal:description="確率"+Number(p.probability*100)+"%で直前に与えた実ダメージの"+Number(p.multiplier*100)+"%をHP吸収";break;
                case SpellNodeKind.ManaSteal:description="確率"+Number(p.probability*100)+"%で敵のMPを吸収";break;
                case SpellNodeKind.Delay:description=Number(p.seconds)+"秒待って続ける";break;
                case SpellNodeKind.RandomBranch:description="確率"+Number(p.probability*100)+"%で成功側、残りは失敗側へ";break;
                case SpellNodeKind.Conditional:description=p.condition==SpellCondition.CasterLowHealth?"自分のHPで分岐":p.condition==SpellCondition.TargetLowHealth?"対象のHPで分岐":p.condition==SpellCondition.TargetAlive?"対象の生存で分岐":"近くの敵の有無で分岐";break;
                case SpellNodeKind.ApplyBuff:description=UI.MagicDescription.EffectName(p.buff)+" "+Number(p.multiplier*100)+"%、"+Number(p.seconds)+"秒";break;
            }
            if(description!=null)text.Append(' ',Math.Min(depth,4)*2).Append("・").AppendLine(description);
            for(int port=0;port<7;port++)
            {
                int child=n.outputs[port];if(child<0)continue;
                if(port>0)text.Append(' ',Math.Min(depth,4)*2).AppendLine(n.kind==SpellNodeKind.Sequence?"追加の処理：":port==(int)SpellPort.OnHit?"命中時：":port==(int)SpellPort.OnExpire?"消滅時：":port==(int)SpellPort.OnKill?"撃破時：":port==(int)SpellPort.True?"成功側：":"失敗側：");
                Describe(s,child,depth+1,text);
            }
        }
    }
}
