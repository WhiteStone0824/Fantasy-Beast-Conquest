using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster
{
    /// <summary>Deterministic sample master data used by editor setup and runtime fallback.</summary>
    public static class DemoDataFactory
    {
        public const int SpeciesPerFamilyRank = 2;
        public const int ExpectedSpeciesCount = 30 * 7 * SpeciesPerFamilyRank;

        // Names are species names in their own right. Rank is metadata and never becomes a name prefix.
        // Each family lists two distinct species for each of the seven ranks.
        private static readonly string[][] SpeciesNames =
        {
            new[] { "\u30b9\u30e9\u30a4\u30e0", "\u30a2\u30ef\u30b9\u30e9\u30a4\u30e0", "\u3057\u305a\u304f\u30bc\u30ea\u30fc", "\u306c\u307e\u3077\u308b", "\u30df\u30ba\u30e2\u30c1", "\u3079\u305f\u3079\u305f", "\u96e8\u3064\u3076", "\u6ce1\u307e\u3058\u3093", "\u7460\u7483\u30bc\u30ea\u30fc", "\u7425\u73c0\u30bc\u30ea\u30fc", "\u9727\u307e\u308b", "\u6c34\u93e1\u30bc\u30ea\u30fc", "\u96f2\u308f\u305f\u30bc\u30ea\u30fc", "\u661f\u96eb\u30bc\u30ea\u30fc" },
            new[] { "\u68ee\u72ac", "\u91ce\u306d\u305a\u307f", "\u7259\u30a6\u30b5\u30ae", "\u5c71\u8352\u3089\u3057", "\u8d64\u5c3e\u30aa\u30aa\u30ab\u30df", "\u82d4\u30c4\u30ce", "\u9ed2\u6591\u30d2\u30e7\u30a6", "\u5ca9\u99c6\u3051", "\u6708\u8f2a\u30b0\u30de", "\u706b\u55b0\u3044\u30c6\u30f3", "\u767d\u9280\u30e9\u30a4\u30ac\u30fc", "\u96f7\u89d2\u30d0\u30a4\u30bd\u30f3", "\u5929\u7fd4\u3051\u308b\u7345\u5b50", "\u53e4\u68ee\u306e\u738b\u7363" },
            new[] { "\u3053\u3060\u307e\u9ce5", "\u91ce\u539f\u30b9\u30ba\u30e1", "\u98a8\u5207\u308a\u30c4\u30d0\u30e1", "\u706b\u7fbd\u30ac\u30e9\u30b9", "\u9752\u7fbd\u30d5\u30af\u30ed\u30a6", "\u7802\u6f20\u30d2\u30d0\u30ea", "\u591c\u6e21\u308a\u30b3\u30a6\u30e2\u30ea", "\u96f7\u9cf4\u30ef\u30b7", "\u96ea\u51a0\u30c4\u30eb", "\u7d05\u84ee\u30af\u30b8\u30e3\u30af", "\u6708\u8aad\u30df\u30df\u30ba\u30af", "\u5d50\u55b0\u3044\u30b0\u30ea\u30d5", "\u6681\u306e\u9cf3\u9ce5", "\u5929\u7a79\u306e\u5927\u9df2" },
            new[] { "\u77f3\u30c8\u30ab\u30b2", "\u8349\u5c3e\u30d8\u30d3", "\u7802\u6f5c\u308a\u30e4\u30e2\u30ea", "\u6cbc\u30ef\u30cb", "\u5ca9\u80cc\u30c8\u30ab\u30b2", "\u6bd2\u7259\u30de\u30e0\u30b7", "\u9752\u9c57\u30d0\u30b7\u30ea\u30b9\u30af", "\u706b\u53e3\u30b5\u30e9\u30de\u30f3\u30c0\u30fc", "\u93a7\u9996\u30ab\u30e1\u30ec\u30aa\u30f3", "\u9ed2\u66dc\u30b3\u30d6\u30e9", "\u971c\u80cc\u30ef\u30cb", "\u96f7\u9c57\u30c9\u30ec\u30a4\u30af", "\u53e4\u9c57\u306e\u86c7\u738b", "\u5929\u88c2\u304d\u5927\u86c7" },
            new[] { "\u96e8\u86d9", "\u304a\u305f\u307e\u574a", "\u82d4\u30d2\u30ad\u30ac\u30a8\u30eb", "\u6c34\u8fba\u30a4\u30e2\u30ea", "\u6cbc\u306c\u3057\u30ac\u30a8\u30eb", "\u6ce1\u5439\u304d\u30a6\u30b7\u30ac\u30a8\u30eb", "\u9752\u6591\u30a2\u30db\u30ed\u30fc\u30c8\u30eb", "\u6bd2\u6cbc\u30e4\u30c9\u30af\u30ac\u30a8\u30eb", "\u6708\u5f71\u30aa\u30aa\u30b5\u30f3\u30b7\u30e7\u30a6", "\u5ca9\u7815\u304d\u30ac\u30de", "\u9727\u7b1b\u30ab\u30a8\u30eb", "\u6c37\u6cbc\u30a4\u30e2\u30ea", "\u53e4\u6c60\u306e\u4e3b", "\u5929\u96e8\u306e\u5927\u86d9" },
            new[] { "\u5c0f\u5ddd\u30cf\u30bc", "\u6d5c\u8fba\u30a4\u30ef\u30b7", "\u6ce1\u3075\u3050", "\u73ca\u745a\u30bf\u30a4", "\u6df1\u6d77\u30a2\u30f3\u30b3\u30a6", "\u6f6e\u5439\u304d\u30af\u30b8\u30e9", "\u9752\u92fc\u30b5\u30e1", "\u6cbc\u5e95\u30ca\u30de\u30ba", "\u6708\u5149\u30af\u30e9\u30b2", "\u706b\u5c71\u3046\u306a\u304e", "\u6e26\u6f6e\u30de\u30f3\u30bf", "\u6c37\u6d77\u30b7\u30fc\u30e9\u30ab\u30f3\u30b9", "\u6d77\u9cf4\u308a\u306e\u4e3b", "\u84bc\u6d77\u30ea\u30f4\u30a1\u30a4\u30a2" },
            new[] { "\u3053\u304c\u306d\u30d0\u30c1", "\u8449\u3063\u3071\u30c6\u30f3\u30c8\u30a6", "\u7cf8\u5410\u304d\u30af\u30e2", "\u591c\u86cd", "\u938c\u89d2\u30ab\u30de\u30ad\u30ea", "\u6bd2\u91dd\u30b5\u30bd\u30ea", "\u82b1\u7c89\u30e2\u30b9", "\u7532\u5191\u30ab\u30d6\u30c8", "\u7d05\u84ee\u30a2\u30b2\u30cf", "\u93e1\u7fc5\u30c8\u30f3\u30dc", "\u5893\u5b88\u30b3\u30ac\u30cd", "\u96f7\u89d2\u30af\u30ef\u30ac\u30bf", "\u5973\u738b\u30b9\u30ba\u30e1\u30d0\u30c1", "\u5343\u773c\u306e\u53e4\u866b" },
            new[] { "\u78ef\u30ac\u30cb", "\u7802\u6d5c\u30e4\u30c9\u30ab\u30ea", "\u6ce1\u5439\u304d\u30b7\u30e3\u30b3", "\u77f3\u80cc\u30b6\u30ea\u30ac\u30cb", "\u8d64\u722a\u30ac\u30b6\u30df", "\u5ca9\u7815\u304d\u30bf\u30e9\u30d0", "\u771f\u73e0\u30e4\u30b7\u30ac\u30cb", "\u6df1\u6df5\u30ed\u30d6\u30b9\u30bf\u30fc", "\u706b\u5c71\u30c0\u30f3\u30b4\u30e0\u30b7", "\u7460\u7483\u30b6\u30ea\u30ac\u30cb", "\u53cc\u92cf\u30b7\u30aa\u30de\u30cd\u30ad", "\u6c37\u6bbb\u30ad\u30f3\u30b0\u30af\u30e9\u30d6", "\u9244\u7532\u306e\u5927\u87f9", "\u6d77\u5e95\u57ce\u306e\u5b88\u308a\u624b" },
            new[] { "\u308f\u304b\u3070\u30c8\u30b2", "\u3064\u308b\u8349\u3046\u3055\u304e", "\u82b1\u304b\u3076\u308a", "\u7720\u308a\u30ad\u30ce\u30b3\u8349", "\u4eba\u55b0\u3044\u30a2\u30b6\u30df", "\u8526\u5dfb\u304d\u30ed\u30fc\u30d1\u30fc", "\u6b4c\u3046\u82b1\u6a39", "\u706b\u82b1\u30b5\u30dc\u30c6\u30f3", "\u767d\u6a3a\u30c9\u30e9\u30a4\u30a2\u30c9", "\u6bd2\u871c\u30e9\u30d5\u30ec\u30b7\u30a2", "\u6708\u5149\u30d2\u30a4\u30e9\u30ae", "\u7d61\u307f\u68ee\u306e\u5927\u6a39", "\u82b1\u51a0\u30de\u30f3\u30c9\u30ec\u30a4\u30af", "\u4e16\u754c\u6a39\u306e\u82e5\u679d" },
            new[] { "\u3053\u3082\u308c\u3073\u8338", "\u80de\u5b50\u307c\u3046\u3084", "\u7720\u308a\u30de\u30c3\u30b7\u30e5", "\u6cbc\u5730\u30b7\u30e1\u30b8", "\u6bd2\u5098\u30bf\u30de\u30b4\u30bf\u30b1", "\u7b11\u3044\u30a8\u30ea\u30f3\u30ae", "\u9752\u82d4\u30d2\u30e9\u30bf\u30b1", "\u591c\u5149\u30ad\u30af\u30e9\u30b2", "\u706b\u5439\u304d\u30d9\u30cb\u30c6\u30f3\u30b0", "\u77f3\u8338\u30ac\u30f3\u30b3", "\u5922\u898b\u30dd\u30eb\u30c1\u30fc\u30cb", "\u8150\u6d77\u306e\u30cc\u30e1\u30ea\u30bf\u30b1", "\u68ee\u5b88\u308a\u306e\u8001\u8338", "\u661f\u964d\u308b\u51ac\u866b\u590f\u8349" },
            new[] { "\u68ee\u756a\u306e\u5b50", "\u77f3\u7b1b\u306e\u6c11", "\u5ddd\u8fba\u306e\u72e9\u4eba", "\u91ce\u935b\u51b6", "\u89d2\u7b1b\u306e\u6226\u58eb", "\u9727\u96a0\u308c\u306e\u65a5\u5019", "\u9244\u9774\u306e\u5c71\u5175", "\u7d05\u84ee\u306e\u821e\u3044\u624b", "\u6708\u4e0b\u306e\u5263\u58eb", "\u9df9\u76ee\u306e\u5f13\u624b", "\u767d\u6a3a\u306e\u8ce2\u8005", "\u96f7\u69cc\u306e\u8c6a\u5091", "\u53e4\u738b\u306e\u8fd1\u885b", "\u9ece\u660e\u306e\u52c7\u8005" },
            new[] { "\u5ca9\u3053\u3073\u3068", "\u4e18\u306e\u5de8\u4eba", "\u82d4\u5ca9\u30b4\u30fc\u30ec\u30e0", "\u96ea\u539f\u30c8\u30ed\u30eb", "\u5c71\u7815\u304d\u30aa\u30fc\u30ac", "\u96f7\u9cf4\u306e\u5de8\u5175", "\u9727\u5cf0\u306e\u5927\u7537", "\u9244\u9aa8\u30b8\u30e3\u30a4\u30a2\u30f3\u30c8", "\u6eb6\u5ca9\u30bf\u30a4\u30bf\u30f3", "\u6c37\u58c1\u306e\u5b88\u4eba", "\u6df1\u68ee\u306e\u53e4\u5de8\u4eba", "\u661f\u55b0\u3044\u5de8\u4eba", "\u5927\u5730\u3092\u62c5\u3046\u8005", "\u5929\u84cb\u3092\u652f\u3048\u308b\u5de8\u4eba" },
            new[] { "\u571f\u304f\u308c\u4eba\u5f62", "\u85c1\u675f\u30c9\u30fc\u30eb", "\u77f3\u81fc\u30b4\u30fc\u30ec\u30e0", "\u6ce5\u4eba\u5f62", "\u7802\u6642\u8a08\u5175", "\u78c1\u9244\u30b4\u30fc\u30ec\u30e0", "\u9752\u9285\u306e\u756a\u5175", "\u9676\u5668\u306e\u8e0a\u308a\u5b50", "\u9ed2\u66dc\u306e\u9a0e\u58eb\u50cf", "\u7425\u73c0\u306e\u5b88\u308a\u4eba", "\u767d\u78c1\u306e\u5973\u795e\u50cf", "\u5343\u5e74\u77f3\u50cf", "\u53e4\u4ee3\u907a\u8de1\u306e\u756a\u4eba", "\u661f\u92fc\u306e\u5de8\u50cf" },
            new[] { "\u5c0f\u77f3\u3053\u308d", "\u7802\u91d1\u30e2\u30b0\u30e9", "\u6c34\u6676\u30e4\u30e2\u30ea", "\u746a\u7459\u30c8\u30ab\u30b2", "\u7fe1\u7fe0\u30ab\u30e1", "\u7d05\u7389\u30b3\u30a6\u30e2\u30ea", "\u9ed2\u66dc\u30aa\u30aa\u30ab\u30df", "\u7425\u73c0\u30e0\u30ab\u30c7", "\u84bc\u7389\u30d5\u30af\u30ed\u30a6", "\u9280\u6676\u30ab\u30cb", "\u9ec4\u7389\u30e9\u30a4\u30aa\u30f3", "\u661f\u9244\u306e\u7363", "\u8679\u6676\u30c9\u30e9\u30b4\u30f3", "\u5929\u6676\u306e\u5b88\u8b77\u7363" },
            new[] { "\u6b6f\u8eca\u30cd\u30ba\u30df", "\u30d6\u30ea\u30ad\u5175", "\u304b\u3089\u304f\u308a\u72ac", "\u84b8\u6c17\u30d0\u30c3\u30bf", "\u92fc\u9244\u30ab\u30e9\u30b9", "\u30bc\u30f3\u30de\u30a4\u5175", "\u771f\u936e\u30d5\u30af\u30ed\u30a6", "\u78c1\u6c17\u30b4\u30fc\u30ec\u30e0", "\u96f7\u7ba1\u30bf\u30f3\u30af", "\u81ea\u8d70\u5927\u7832", "\u767d\u9280\u30aa\u30fc\u30c8\u30de\u30bf", "\u9ed2\u9244\u306e\u9a0e\u58eb", "\u53e4\u4ee3\u6a5f\u95a2\u5175", "\u661f\u6e21\u308a\u6a5f\u5de7\u7adc" },
            new[] { "\u306d\u3080\u308a\u4eba\u5f62", "\u3064\u304e\u306f\u304e\u541b", "\u57f9\u990a\u30b9\u30e9\u30a4\u30e0", "\u3046\u3064\u308d\u306a\u4eba\u5f62", "\u5408\u6210\u7363\u30ad\u30de\u30a4\u30e9", "\u785d\u5b50\u306e\u5b50", "\u7e2b\u3044\u76ee\u306e\u5de8\u4eba", "\u57f9\u990a\u69fd\u306e\u756a\u4eba", "\u84bc\u767d\u306e\u4eba\u9020\u4eba", "\u7363\u5fc3\u30db\u30e0\u30f3\u30af\u30eb\u30b9", "\u8a18\u61b6\u3092\u6301\u3064\u4eba\u5f62", "\u4e0d\u6b7b\u8eab\u306e\u5408\u6210\u4f53", "\u539f\u521d\u306e\u30db\u30e0\u30f3\u30af\u30eb\u30b9", "\u547d\u3092\u7de8\u3080\u3082\u306e" },
            new[] { "\u9b54\u5c0e\u66f8\u306e\u866b", "\u706b\u82b1\u30a4\u30f3\u30af", "\u307e\u3058\u306a\u3044\u30d5\u30af\u30ed\u30a6", "\u6d6e\u904a\u3059\u308b\u5dfb\u7269", "\u546a\u6587\u55b0\u3089\u3044", "\u9752\u71d0\u306e\u30e9\u30f3\u30bf\u30f3", "\u9b54\u6cd5\u9663\u306e\u756a\u4eba", "\u7981\u66f8\u306e\u756a\u72ac", "\u661f\u8aad\u307f\u306e\u6c34\u6676", "\u96f7\u6587\u306e\u30b4\u30fc\u30ec\u30e0", "\u6642\u8a60\u307f\u306e\u53f8\u66f8", "\u7d05\u84ee\u306e\u9b54\u5c0e\u5668", "\u4e07\u8c61\u306e\u5927\u9b54\u5c0e\u66f8", "\u5929\u5730\u5275\u4e16\u306e\u66f8" },
            new[] { "\u706b\u8725\u8734", "\u82e5\u89d2\u30ef\u30a4\u30d0\u30fc\u30f3", "\u5ca9\u7ffc\u7adc", "\u6cbc\u6bd2\u30c9\u30e9\u30b4\u30f3", "\u7d05\u84ee\u306e\u98db\u7adc", "\u9752\u5d50\u30ef\u30a4\u30d0\u30fc\u30f3", "\u93a7\u9c57\u30c9\u30ec\u30a4\u30af", "\u6c37\u7259\u30c9\u30e9\u30b4\u30f3", "\u96f7\u89d2\u30ea\u30f3\u30c9\u30d6\u30eb\u30e0", "\u6df1\u7dd1\u306e\u53e4\u7adc", "\u9ec4\u91d1\u306e\u30b0\u30ea\u30d5\u30a9\u30f3\u7adc", "\u9ed2\u66dc\u30d0\u30cf\u30e0\u30fc\u30c8", "\u5929\u7fd4\u3051\u308b\u9f8d\u795e", "\u4e16\u754c\u55b0\u3089\u3044\u306e\u7adc" },
            new[] { "\u4e00\u89d2\u3046\u307e", "\u68ee\u306e\u767d\u9e7f", "\u706b\u5c3e\u304d\u3064\u306d", "\u7fbd\u6839\u7345\u5b50", "\u6708\u5f71\u30e6\u30cb\u30b3\u30fc\u30f3", "\u9727\u89d2\u30ab\u30e2\u30b7\u30ab", "\u7d05\u7ffc\u30da\u30ac\u30b5\u30b9", "\u96f7\u722a\u30d2\u30dd\u30b0\u30ea\u30d5", "\u5922\u55b0\u3044\u30d0\u30af", "\u96ea\u9b23\u30d5\u30a7\u30f3\u30ea\u30eb", "\u8679\u5c3e\u306e\u9e92\u9e9f", "\u5929\u706b\u306e\u9cf3\u51f0", "\u795e\u57df\u306e\u767d\u864e", "\u6681\u3092\u904b\u3076\u970a\u7363" },
            new[] { "\u82b1\u3073\u3089\u5996\u7cbe", "\u6728\u306e\u5b9f\u30d4\u30af\u30b7\u30fc", "\u9234\u9cf4\u308a\u30b3\u30ed\u30dc\u30c3\u30af\u30eb", "\u6c34\u7389\u30cb\u30f3\u30d5", "\u3044\u305f\u305a\u3089\u30b7\u30eb\u30d5", "\u871c\u96c6\u3081\u306e\u7fbd\u866b", "\u8ff7\u3044\u68ee\u306e\u5c0f\u4eba", "\u6708\u706f\u308a\u30a6\u30a3\u30b9\u30d7", "\u82b1\u51a0\u306e\u8e0a\u308a\u5b50", "\u6728\u6f0f\u308c\u65e5\u30a8\u30eb\u30d5", "\u5922\u7d21\u304e\u306e\u5996\u7cbe", "\u4e03\u8272\u306e\u7fbd\u6839\u59eb", "\u6642\u6e21\u308a\u306e\u30d5\u30a7\u30a4", "\u5e38\u82e5\u306e\u5973\u738b" },
            new[] { "\u98a8\u306e\u3053\u3060\u307e", "\u5ddd\u9727\u306e\u5b50", "\u706b\u306e\u7c89\u306e\u7cbe", "\u77f3\u3053\u308d\u306e\u7cbe", "\u6728\u970a", "\u6c34\u93e1\u306e\u7cbe", "\u3064\u3080\u3058\u98a8", "\u706f\u706b\u306e\u7cbe\u970a", "\u96f7\u96f2\u306e\u5b50", "\u6c37\u6cb3\u306e\u7cbe", "\u68ee\u7f85\u306e\u5927\u7cbe", "\u671d\u713c\u3051\u306e\u7cbe\u970a", "\u56db\u5b63\u3092\u5de1\u308b\u3082\u306e", "\u5929\u5730\u306e\u606f\u5439" },
            new[] { "\u767d\u7fbd\u306e\u4f7f\u3044", "\u9234\u97f3\u306e\u5929\u4f7f", "\u9053\u3057\u308b\u3079\u306e\u7ffc", "\u7948\u308a\u306e\u5b88\u4eba", "\u9ece\u660e\u306e\u9a0e\u58eb\u5929\u4f7f", "\u9752\u7a7a\u306e\u5927\u7ffc", "\u8056\u706b\u306e\u5fa1\u4f7f\u3044", "\u91d1\u74b0\u306e\u5b88\u8b77\u8005", "\u795e\u8a17\u306e\u7ffc\u4eba", "\u6681\u5149\u306e\u5927\u5929\u4f7f", "\u767d\u9280\u306e\u71be\u5929\u4f7f", "\u5929\u9580\u306e\u756a\u4eba", "\u4e03\u5149\u306e\u30bb\u30e9\u30d5", "\u661f\u754c\u3092\u7167\u3089\u3059\u5fa1\u4f7f\u3044" },
            new[] { "\u5c0f\u9b3c\u306e\u5b50", "\u3044\u305f\u305a\u3089\u30a4\u30f3\u30d7", "\u91dd\u5c3e\u30ac\u30fc\u30b4\u30a4\u30eb", "\u7159\u5e55\u306e\u30b4\u30d6\u30ea\u30f3", "\u591c\u76d7\u306e\u30d0\u30b0\u30d9\u30a2", "\u706b\u904a\u3073\u30b5\u30e9\u30de\u30f3\u30c0\u30fc", "\u5922\u9b54\u30a4\u30f3\u30ad\u30e5\u30d0\u30b9", "\u6bd2\u89d2\u30aa\u30fc\u30ac", "\u9ed2\u7ffc\u30ac\u30fc\u30b4\u30a4\u30eb", "\u51a5\u5e9c\u306e\u30b1\u30eb\u30d9\u30ed\u30b9", "\u7d05\u84ee\u306e\u30d0\u30d5\u30a9\u30e1\u30c3\u30c8", "\u5948\u843d\u306e\u30d9\u30eb\u30bc\u30d6", "\u9b54\u754c\u306e\u5927\u516c", "\u7d42\u7109\u3092\u544a\u3052\u308b\u9b54\u795e" },
            new[] { "\u9aa8\u306d\u305a\u307f", "\u5893\u5834\u306e\u30ab\u30e9\u30b9", "\u6ce5\u307e\u307f\u308c\u30be\u30f3\u30d3", "\u93a7\u9aa8\u5175", "\u9752\u706b\u306e\u9ab8\u9aa8", "\u9996\u306a\u3057\u9a0e\u58eb", "\u5c4d\u55b0\u3044\u30b0\u30fc\u30eb", "\u8150\u6cbc\u306e\u30ea\u30c3\u30c1", "\u5893\u5b88\u30c7\u30e5\u30e9\u30cf\u30f3", "\u767d\u9aa8\u30ef\u30a4\u30d0\u30fc\u30f3", "\u6b7b\u8005\u306e\u5927\u884c\u9032", "\u51a5\u754c\u306e\u5c06\u8ecd", "\u4e0d\u6ec5\u306e\u53e4\u738b", "\u9ec4\u6cc9\u8def\u3092\u7d71\u3079\u308b\u8005" },
            new[] { "\u3075\u308f\u308a\u706b", "\u7a93\u8fba\u306e\u3086\u3046\u308c\u3044", "\u8ff7\u5b50\u306e\u970a", "\u3059\u3059\u308a\u6ce3\u304d", "\u9752\u767d\u3044\u4eba\u9b42", "\u53e4\u4e95\u6238\u306e\u5f71", "\u9234\u9cf4\u308a\u5e7d\u970a", "\u591c\u6c7d\u8eca\u306e\u4ea1\u970a", "\u9022\u9b54\u304c\u6642\u306e\u5973", "\u767d\u7121\u57a2\u306e\u82b1\u5ac1\u970a", "\u767e\u9b3c\u306e\u9577", "\u5343\u5e74\u306e\u6028\u970a", "\u5e7d\u4e16\u306e\u6e21\u3057\u5b88", "\u5f7c\u5cb8\u3092\u9589\u3056\u3059\u3082\u306e" },
            new[] { "\u546a\u3044\u306e\u91d8", "\u3072\u3068\u308a\u3067\u52d5\u304f\u9774", "\u6ce3\u304d\u9762\u306e\u80fd\u9762", "\u558b\u308b\u53e4\u93e1", "\u8840\u5857\u308c\u306e\u4eba\u5f62", "\u7b11\u3046\u63d0\u706f", "\u5272\u308c\u305f\u9b54\u93e1", "\u304b\u3089\u304f\u308a\u751f\u9996", "\u9b3c\u706b\u306e\u7c2a", "\u767e\u76ee\u306e\u58fa", "\u5c01\u3058\u3089\u308c\u305f\u7532\u5191", "\u6ce3\u304d\u53eb\u3076\u5927\u9418", "\u5343\u5e74\u546a\u3044\u306e\u9762", "\u5384\u707d\u3092\u5bbf\u3059\u5b9d\u7389" },
            new[] { "\u8db3\u5143\u306e\u5f71", "\u9ed2\u732b\u306e\u5f71\u6cd5\u5e2b", "\u304b\u3052\u307c\u3046\u3057", "\u5f71\u7e2b\u3044\u306e\u72ac", "\u5bb5\u95c7\u306e\u30d5\u30fc\u30c9", "\u5f71\u8e0f\u307f\u306e\u9b3c", "\u9ed2\u7ffc\u306e\u72e9\u4eba", "\u5f71\u55b0\u3044\u30ef\u30fc\u30b0", "\u6708\u8755\u306e\u7363", "\u5f71\u7d75\u306e\u9a0e\u58eb", "\u591c\u5e33\u306e\u5927\u72fc", "\u7121\u8c8c\u306e\u5f71\u6b66\u8005", "\u5e38\u95c7\u306e\u4e3b", "\u5149\u3092\u5451\u3080\u3082\u306e" },
            new[] { "\u661f\u5c51\u306e\u5b50", "\u6708\u898b\u3046\u3055\u304e", "\u6d41\u308c\u661f\u306e\u9b5a", "\u5f57\u5c3e\u306e\u72d0", "\u6708\u8f2a\u30d5\u30af\u30ed\u30a6", "\u661f\u6e21\u308a\u306e\u65c5\u4eba", "\u5bb5\u661f\u306e\u9e7f", "\u5929\u7403\u30af\u30b8\u30e9", "\u9280\u6cb3\u30d2\u30c4\u30b8", "\u6975\u5149\u306e\u5927\u9ce5", "\u661f\u5ea7\u3092\u7d50\u3076\u7363", "\u6708\u55b0\u3044\u306e\u7adc", "\u5929\u72fc\u30b7\u30ea\u30a6\u30b9", "\u9280\u6cb3\u3092\u6cf3\u3050\u3082\u306e" },
            new[] { "\u5e95\u306a\u3057\u6cbc\u306e\u5b50", "\u6df1\u6d77\u306e\u89e6\u624b", "\u6697\u7901\u306e\u7259", "\u865a\u308d\u8c9d", "\u5948\u843d\u306e\u9b5a\u5f71", "\u9ed2\u6f6e\u30af\u30e9\u30fc\u30b1\u30f3", "\u6df1\u6df5\u30a6\u30c4\u30dc", "\u5e95\u7121\u3057\u306e\u6355\u98df\u8005", "\u6697\u9ed2\u73ca\u745a\u306e\u4e3b", "\u7121\u97f3\u306e\u6df1\u6d77\u7adc", "\u5fd8\u5374\u3092\u98fc\u3046\u3082\u306e", "\u9ed2\u6d77\u306e\u5927\u738b", "\u5948\u843d\u3092\u6e21\u308b\u53e4\u9be8", "\u4e16\u754c\u306e\u5e95\u306b\u68f2\u3080\u3082\u306e" },
            new[] { "\u3050\u306b\u3083\u308a\u7389", "\u3042\u3079\u3053\u3079\u9ce5", "\u307e\u305c\u3053\u305c\u7363", "\u88cf\u8fd4\u308a\u30c8\u30ab\u30b2", "\u8679\u8272\u30d0\u30b1\u30e2\u30ce", "\u6b6a\u307f\u89d2\u306e\u30b7\u30ab", "\u5922\u55b0\u3044\u306e\u732b", "\u53cd\u8ee2\u3059\u308b\u4eba\u5f62", "\u4e03\u8272\u306e\u5927\u86c7", "\u59ff\u306a\u304d\u5408\u6210\u7363", "\u4e07\u83ef\u306e\u30ad\u30de\u30a4\u30e9", "\u5f62\u306a\u304d\u6df7\u6c8c", "\u59cb\u307e\u308a\u306e\u7570\u5f62", "\u540d\u3082\u306a\u304d\u4e07\u8c61" },
        };

        private static readonly string[] LegacyNames =
        {
            "\u30b9\u30e9\u30a4\u30e0", "\u7259\u30a6\u30b5\u30ae", "\u9752\u7fbd\u30d5\u30af\u30ed\u30a6", "\u771f\u73e0\u30e4\u30b7\u30ac\u30cb", "\u6cbc\u306c\u3057\u30ac\u30a8\u30eb", "\u771f\u936e\u30d5\u30af\u30ed\u30a6", "\u96f7\u96f2\u306e\u5b50", "\u6b4c\u3046\u82b1\u6a39", "\u9ed2\u7ffc\u30ac\u30fc\u30b4\u30a4\u30eb", "\u9ec4\u91d1\u306e\u30b0\u30ea\u30d5\u30a9\u30f3\u7adc", "\u7cf8\u5410\u304d\u30af\u30e2", "\u3053\u3082\u308c\u3073\u8338"
        };
        private static readonly SpeciesFamily[] LegacyFamilies =
        {
            SpeciesFamily.Slime, SpeciesFamily.Beast, SpeciesFamily.Bird, SpeciesFamily.Crustacean,
            SpeciesFamily.Amphibian, SpeciesFamily.Machine, SpeciesFamily.Spirit, SpeciesFamily.Plant,
            SpeciesFamily.Demon, SpeciesFamily.Dragon, SpeciesFamily.Insect, SpeciesFamily.Fungi
        };
        private static readonly SpeciesRank[] LegacyRanks =
        {
            SpeciesRank.F, SpeciesRank.E, SpeciesRank.D, SpeciesRank.C, SpeciesRank.D, SpeciesRank.C,
            SpeciesRank.B, SpeciesRank.C, SpeciesRank.B, SpeciesRank.A, SpeciesRank.E, SpeciesRank.F
        };
        private static readonly PersonalityType[] LegacyTendencies =
        {
            PersonalityType.Gentle, PersonalityType.Brave, PersonalityType.Aggressive, PersonalityType.Cautious,
            PersonalityType.Calm, PersonalityType.Calm, PersonalityType.Timid, PersonalityType.Gentle,
            PersonalityType.Aggressive, PersonalityType.Brave, PersonalityType.Timid, PersonalityType.Cautious
        };

        public static SpeciesDefinition[] CreateSpecies()
        {
            var result = new List<SpeciesDefinition>(ExpectedSpeciesCount);
            var legacySlots = new HashSet<string>();

            for (int i = 0; i < LegacyNames.Length; i++)
            {
                SpeciesFamily family = LegacyFamilies[i];
                SpeciesRank rank = LegacyRanks[i];
                result.Add(CreateLegacySpecies(i));
                legacySlots.Add(SlotKey(family, rank, 0));
            }

            for (int family = 0; family < 30; family++)
            for (int rank = 0; rank < 7; rank++)
            for (int variant = 0; variant < SpeciesPerFamilyRank; variant++)
            {
                if (legacySlots.Contains(SlotKey((SpeciesFamily)family, (SpeciesRank)rank, variant))) continue;
                result.Add(CreateMatrixSpecies(family, rank, variant));
            }

            return result.ToArray();
        }

        private static string SlotKey(SpeciesFamily family, SpeciesRank rank, int variant)
        {
            return ((int)family) + ":" + ((int)rank) + ":" + variant;
        }

        public static string SpriteSourceId(string speciesId)
        {
            return speciesId;
        }

        private static SpeciesDefinition CreateLegacySpecies(int index)
        {
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.speciesId = "demo_" + index.ToString("00");
            species.displayName = LegacyNames[index];
            species.description = "\u81ea\u52d5\u6226\u95d8\u3068\u914d\u5408\u306e\u691c\u8a3c\u7528\u306b\u7528\u610f\u3055\u308c\u305f\u7a2e\u65cf\u3067\u3059\u3002";
            species.family = LegacyFamilies[index];
            species.rank = LegacyRanks[index];
            species.defaultTendency = LegacyTendencies[index];
            species.baseStats = SpeciesBaseStatsCatalog.Create(species.family,species.rank,species.speciesId);
            SetResistances(species, index, 0);
            SpeciesFamilyTraits.Apply(species);
            species.passive=SpeciesPassiveCatalog.Create(species);
            return species;
        }

        private static SpeciesDefinition CreateMatrixSpecies(int familyIndex, int rankIndex, int variant)
        {
            var family = (SpeciesFamily)familyIndex;
            var rank = (SpeciesRank)rankIndex;
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.speciesId = string.Format("species_F{0:00}_R{1}_{2}", familyIndex, rankIndex, variant == 0 ? "A" : "B");
            species.displayName = SpeciesNames[familyIndex][rankIndex * SpeciesPerFamilyRank + variant];
            species.description = string.Format("{0}\u306e\u500b\u4f53\u3002\u7a2e\u65cf\u56fa\u6709\u306e\u80fd\u529b\u3068\u8010\u6027\u306b\u3001\u500b\u4f53\u5024\u30fb\u6027\u683c\u30fb\u30ec\u30d9\u30eb\u304c\u52a0\u308f\u308a\u307e\u3059\u3002", FamilyJapanese(family));
            species.family = family;
            species.rank = rank;
            species.defaultTendency = (PersonalityType)((familyIndex * 3 + rankIndex + variant) % Enum.GetValues(typeof(PersonalityType)).Length);
            species.baseStats = SpeciesBaseStatsCatalog.Create(species.family,species.rank,species.speciesId);
            SetResistances(species, familyIndex * 17 + rankIndex * 5, variant + 1);
            SpeciesFamilyTraits.Apply(species);
            species.passive=SpeciesPassiveCatalog.Create(species);
            return species;
        }

        private static string FamilyJapanese(SpeciesFamily family)
        {
            string[] names = { "スライム系", "獣系", "鳥系", "爬虫系", "両生系", "水棲系", "虫系", "甲殻系", "植物系", "菌類系", "亜人系", "巨人系", "物質系", "鉱物系", "機械系", "人工生命系", "魔導系", "竜系", "幻獣系", "妖精系", "精霊系", "天使系", "悪魔系", "死霊系", "霊体系", "呪物系", "影系", "星界系", "深淵系", "混沌系" };
            int index = (int)family;
            return index >= 0 && index < names.Length ? names[index] : "未知の系統";
        }
        private static void SetResistances(SpeciesDefinition species, int seed, int offset)
        {
            species.elementResistances = new ElementResistance[7];
            for (int element = 0; element < species.elementResistances.Length; element++)
            {
                int affinityBonus = element == (seed % 7) ? 12 : 0;
                float damageReduction = offset == 0 ? ((seed * 3 + element * 2) % 30) / 100f : ((seed * 3 + element * 7 + offset * 5 + affinityBonus) % 36) / 100f;
                float applicationResistance = offset == 0 ? ((seed * 5 + element * 3) % 35) / 100f : ((seed * 5 + element * 3 + offset * 7 + affinityBonus / 2) % 42) / 100f;
                species.elementResistances[element] = new ElementResistance
                {
                    element = (AttributeElement)element,
                    value = new ResistanceValue
                    {
                        damageReduction = damageReduction,
                        applicationResistance = applicationResistance
                    }
                };
            }

            species.debuffResistances = new DebuffResistance[8];
            for (int debuff = 0; debuff < species.debuffResistances.Length; debuff++)
            {
                species.debuffResistances[debuff] = new DebuffResistance
                {
                    debuff = (DebuffType)debuff,
                    value = new ResistanceValue
                    {
                        damageReduction = 0f,
                        applicationResistance = offset == 0 ? ((seed * 7 + debuff * 4) % 40) / 100f : ((seed * 7 + debuff * 5 + offset * 3) % 36) / 100f
                    }
                };
            }
        }

        public static SpeciesDatabase CreateSpeciesDatabase()
        {
            var database = ScriptableObject.CreateInstance<SpeciesDatabase>();
            database.species = CreateSpecies();
            return database;
        }

        public static MagicGenome DemoGenome(int index)
        {
            return Spells.SpellSlotStorage.Prepare(new MagicGenome{programGenes=Spells.SpellProgramBuilder.Random(index)},index);
        }
    }
}

