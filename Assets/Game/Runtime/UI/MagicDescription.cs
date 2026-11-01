namespace FusionAnyMonster.UI
{
    public static class MagicDescription
    {
        public static string EffectName(BuffType buff)
        {return buff==BuffType.AttackBoost?"攻撃強化":buff==BuffType.DefenseBoost?"防御強化":"加速";}
        public static string Effects(BuiltMagic magic,int maximum=int.MaxValue)
        {return magic.program==null?"使用できない魔法":Spells.SpellPlayerDescription.Effects(magic.program,maximum);}
        public static string Behavior(BuiltMagic magic)
        {return magic.program==null?"使用できない魔法":Spells.SpellPlayerDescription.Behavior(magic.program);}
        public static string EffectDetails(BuiltMagic magic)
        {return magic.program==null?"使用できない魔法":Spells.SpellPlayerDescription.Details(magic.program);}
    }
}
