using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillRangeData
    {
        public string RangeId;
        public string Name;
        public bool IncludeSelf;

        // 엑셀에서 Range1~Range30 문자열로 받아옴
        public List<string> RangeRaw = new();

        // 런타임용 (파싱된 좌표)
        public List<(int x, int y)> Positions = new();
    }
}