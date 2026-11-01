using UnityEngine;
using UnityEngine.UI;

namespace FusionAnyMonster.UI
{
    // A quiet dotted route behind the stage cards, laid out in the panel's own coordinates.
    public sealed class ExpeditionRouteGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;
            for(int i=0;i<4;i++)
            {
                Vector2 a=Point(i,r),b=Point(i+1,r);
                for(int dot=0;dot<8;dot++)
                {
                    Vector2 p=Vector2.Lerp(a,b,(dot+.5f)/8f);float size=Mathf.Max(1.5f,Mathf.Min(r.width,r.height)*.004f);
                    int start=vh.currentVertCount;var v=UIVertex.simpleVert;v.color=color;
                    v.position=p+new Vector2(-size,-size);vh.AddVert(v);v.position=p+new Vector2(-size,size);vh.AddVert(v);
                    v.position=p+new Vector2(size,size);vh.AddVert(v);v.position=p+new Vector2(size,-size);vh.AddVert(v);
                    vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start+2,start+3,start);
                }
            }
        }
        private static Vector2 Point(int index,Rect rect)
        {float x=index%2==0?.115f:.285f;float y=.82f-index*.175f;return new Vector2(rect.xMin+rect.width*x,rect.yMin+rect.height*y);}
    }
}
