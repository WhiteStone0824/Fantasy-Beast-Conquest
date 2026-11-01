using UnityEngine;
using UnityEngine.UI;

namespace FusionAnyMonster.UI
{
    /// <summary>Ten axes with a fixed 0-100 scale; a closed polygon represents this individual's values.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IndividualValueRadar : MaskableGraphic
    {
        public const int AxisCount=10;
        private readonly float[] normalized=new float[AxisCount];
        public float GetNormalizedValue(int index) { return index<0 || index>=AxisCount?0:normalized[index]; }
        public void SetValues(int[] values)
        {
            for(int i=0;i<AxisCount;i++)normalized[i]=IndividualValues.Clamp(values!=null && i<values.Length?values[i]:0)/(float)IndividualValues.MaxValue;
            raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect area=rectTransform.rect;Vector2 center=area.center;
            float radius=Mathf.Max(0,Mathf.Min(area.width,area.height)*.48f);
            Color grid=new Color(.30f,.52f,.63f,.5f),tint=color;
            for(int ring=1;ring<=5;ring++)for(int axis=0;axis<AxisCount;axis++)
                Line(mesh,center+Direction(axis)*radius*ring/5f,center+Direction(axis+1)*radius*ring/5f,.75f,grid);
            for(int axis=0;axis<AxisCount;axis++)Line(mesh,center,center+Direction(axis)*radius,.65f,grid);
            Color fill=tint;fill.a=.20f;
            for(int axis=0;axis<AxisCount;axis++)
            {
                int next=(axis+1)%AxisCount;
                Vector2 point=center+Direction(axis)*radius*normalized[axis],other=center+Direction(next)*radius*normalized[next];
                int start=mesh.currentVertCount;mesh.AddVert(center,fill,Vector2.zero);mesh.AddVert(point,fill,Vector2.zero);mesh.AddVert(other,fill,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);
                Line(mesh,point,other,2f,tint);Line(mesh,point-Vector2.right*2,point+Vector2.right*2,4f,tint);
            }
        }
        private static Vector2 Direction(int axis) { float angle=Mathf.PI*.5f-axis*Mathf.PI*2/AxisCount;return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)); }
        private static void Line(VertexHelper mesh,Vector2 start,Vector2 end,float width,Color tint)
        {
            Vector2 delta=end-start;if(delta.sqrMagnitude<.0001f)return;
            Vector2 normal=new Vector2(-delta.y,delta.x).normalized*width*.5f;int index=mesh.currentVertCount;
            mesh.AddVert(start-normal,tint,Vector2.zero);mesh.AddVert(start+normal,tint,Vector2.zero);mesh.AddVert(end+normal,tint,Vector2.zero);mesh.AddVert(end-normal,tint,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
        }
    }
}
