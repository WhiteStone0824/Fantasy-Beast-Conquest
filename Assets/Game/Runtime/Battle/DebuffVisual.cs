using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster.Battle
{
    /// <summary>One bounded, reusable mesh per afflicted actor; no particles or objects spawned per frame.</summary>
    public sealed class DebuffVisual : MonoBehaviour
    {
        private Combatant owner;
        private SpriteRenderer body;
        private Mesh mesh;
        private MeshRenderer visualRenderer;
        private static Material material;
        private readonly List<Vector3> vertices=new List<Vector3>(1024);
        private readonly List<Color> colors=new List<Color>(1024);
        private readonly List<int> triangles=new List<int>(2048);
        private readonly float[] opacity=new float[8];
        private float phase,renderClock;
        private bool forceRender;
        public int VisibleMask { get; private set; }

        internal void Initialize(Combatant combatant)
        {
            owner=combatant;body=owner.GetComponent<SpriteRenderer>();
            gameObject.layer=owner.gameObject.layer;
            mesh=new Mesh{name="Debuff effect mesh"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            visualRenderer=gameObject.AddComponent<MeshRenderer>();
            visualRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            visualRenderer.receiveShadows=false;
            if(material==null)material=new Material(Shader.Find("Sprites/Default")){name="Shared debuff effects"};
            visualRenderer.sharedMaterial=material;visualRenderer.enabled=false;
            Wake();
        }
        internal void Wake() { enabled=true;forceRender=true; }
        internal void Clear()
        {
            for(int i=0;i<8;i++)opacity[i]=0;
            VisibleMask=0;if(visualRenderer!=null)visualRenderer.enabled=false;enabled=false;
        }
        private void LateUpdate()
        {
            if(owner==null || !owner.IsAlive){Clear();return;}
            int mask=owner.ActiveDebuffMask;
            float dt=Time.deltaTime;phase+=dt;renderClock+=dt;
            VisibleMask=0;
            for(int i=0;i<8;i++)
            {
                opacity[i]=Mathf.MoveTowards(opacity[i],(mask&(1<<i))!=0?1:0,dt*7);
                if(opacity[i]>.001f)VisibleMask|=1<<i;
            }
            if(VisibleMask==0 && mask==0){Clear();return;}
            if(!forceRender && (dt<=0 || renderClock<1f/30))return;
            forceRender=false;renderClock=0;
            Vector3 scale=owner.transform.lossyScale;
            transform.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1);
            float radius=.5f;
            if(body!=null)
            {
                transform.localPosition=owner.transform.InverseTransformPoint(body.bounds.center);
                radius=Mathf.Clamp(Mathf.Max(body.bounds.extents.x,body.bounds.extents.y),.3f,1.2f);
                visualRenderer.sortingLayerID=body.sortingLayerID;visualRenderer.sortingOrder=body.sortingOrder+3;
            }
            vertices.Clear();colors.Clear();triangles.Clear();
            for(int i=0;i<8;i++)if(opacity[i]>.001f)Draw((DebuffType)i,radius,opacity[i]);
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0,false);
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*8);
            visualRenderer.enabled=vertices.Count>0;
        }
        private void Draw(DebuffType type,float r,float alpha)
        {
            switch(type)
            {
                case DebuffType.Poison:
                    for(int i=0;i<4;i++)
                    {
                        float life=Mathf.Repeat(phase*.7f+i*.25f,1);
                        Vector2 p=new Vector2(-r*.85f+Mathf.Sin(i*2+phase*2)*.15f,-r*.45f+life*r*1.8f);
                        Color c=new Color(.58f,.95f,.20f,alpha*(1-life)*.85f);
                        Ring(p,.045f+life*.065f,.018f,c,12);
                        Disc(p+new Vector2(-.02f,.025f),.017f,new Color(.9f,1,.6f,c.a),8);
                    }
                    break;
                case DebuffType.Burn:
                    for(int i=0;i<5;i++)
                    {
                        float life=Mathf.Repeat(phase*1.3f+i*.2f,1);
                        Vector2 p=new Vector2((i-2)*r*.34f,-r*.7f+life*r*.42f);
                        float width=.07f*(1-life*.5f),height=.16f+(1-life)*.22f;
                        Color c=new Color(1,.24f,.025f,alpha*(1-life*.65f));
                        Triangle(p+Vector2.left*width,p+new Vector2(Mathf.Sin(phase*7+i)*.055f,height),p+Vector2.right*width,c);
                        Triangle(p+Vector2.left*width*.5f,p+Vector2.up*height*.65f,p+Vector2.right*width*.5f,new Color(1,.85f,.15f,c.a));
                    }
                    break;
                case DebuffType.Freeze:
                    Ring(Vector2.zero,r*1.12f,.025f,new Color(.45f,.9f,1,alpha*.9f),6);
                    for(int i=0;i<3;i++)
                    {
                        Vector2 p=new Vector2((i-1)*r*.48f,-r*.82f);
                        Vector2 tip=p+new Vector2((i-1)*.08f,r*(i==1?1.5f:1.1f));
                        Triangle(p+Vector2.left*.13f,tip,p+Vector2.right*.13f,new Color(.3f,.75f,1,alpha*.35f));
                        Line(p+Vector2.left*.13f,tip,.022f,new Color(.7f,.97f,1,alpha));
                        Line(tip,p+Vector2.right*.13f,.022f,new Color(.7f,.97f,1,alpha));
                    }
                    break;
                case DebuffType.Paralysis:
                    for(int i=0;i<2;i++)
                    {
                        float side=i==0?-1:1;
                        Vector2 p=new Vector2(side*r*.96f,r*.65f);
                        Color c=new Color(1,.91f,.1f,alpha*(.65f+.35f*Mathf.Sin(phase*19+i)));
                        Vector2 b=p+new Vector2(-side*.15f,-r*.6f),d=b+new Vector2(side*.2f,.02f),e=d+new Vector2(-side*.16f,-r*.65f);
                        Line(p,b,.04f,c);Line(b,d,.04f,c);Line(d,e,.04f,c);
                    }
                    break;
                case DebuffType.Sleep:
                    for(int i=0;i<3;i++)
                    {
                        float life=Mathf.Repeat(phase*.45f+i/3f,1),size=.065f+life*.055f;
                        Vector2 p=new Vector2(r*.25f+life*.45f,r*.8f+life*.6f);
                        Color c=new Color(.65f,.78f,1,alpha*(1-life));
                        Line(p+new Vector2(-size,size),p+new Vector2(size,size),.025f,c);
                        Line(p+new Vector2(size,size),p+new Vector2(-size,-size),.025f,c);
                        Line(p+new Vector2(-size,-size),p+new Vector2(size,-size),.025f,c);
                    }
                    break;
                case DebuffType.Slow:
                    Ellipse(new Vector2(0,-r*.65f),r*1.15f,r*.32f,.025f,new Color(.34f,.56f,.83f,alpha*.85f));
                    for(int i=0;i<2;i++)
                    {
                        float y=-r*.25f-i*.16f-Mathf.Repeat(phase*.25f,.16f);
                        Vector2 p=new Vector2(-r*.45f,y);Color c=new Color(.6f,.8f,1,alpha*.9f);
                        Line(p+new Vector2(-.10f,.08f),p,.03f,c);Line(p,p+new Vector2(.10f,.08f),.03f,c);
                    }
                    break;
                case DebuffType.Silence:
                    {
                        Vector2 p=new Vector2(-r*.55f,r*1.1f+Mathf.Sin(phase*2)*.04f);
                        Color c=new Color(1,.48f,.67f,alpha);
                        Ring(p,.16f,.023f,c,16);
                        Line(p+new Vector2(-.08f,-.12f),p+new Vector2(-.14f,-.22f),.022f,c);
                        Line(p+new Vector2(-.11f,-.11f),p+new Vector2(.11f,.11f),.04f,c);
                    }
                    break;
                case DebuffType.Curse:
                    Ellipse(new Vector2(0,-r*.68f),r*1.25f,r*.35f,.025f,new Color(.65f,.2f,.9f,alpha*.8f));
                    for(int i=0;i<3;i++)
                    {
                        float angle=phase*1.2f+i*Mathf.PI*2/3;
                        Vector2 p=new Vector2(Mathf.Cos(angle)*r*1.2f,Mathf.Sin(angle)*r*.7f);
                        Color c=new Color(.85f,.35f,1,alpha*.9f);
                        Line(p+Vector2.up*.12f,p+Vector2.right*.07f,.025f,c);Line(p+Vector2.right*.07f,p+Vector2.down*.12f,.025f,c);
                        Line(p+Vector2.down*.12f,p+Vector2.left*.07f,.025f,c);Line(p+Vector2.left*.07f,p+Vector2.up*.12f,.025f,c);
                    }
                    break;
            }
        }
        private void Triangle(Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
            colors.Add(tint);colors.Add(tint);colors.Add(tint);
            triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
        }
        private void Line(Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 delta=b-a;Vector2 normal=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            Triangle(a-normal,a+normal,b+normal,tint);Triangle(a-normal,b+normal,b-normal,tint);
        }
        private void Ring(Vector2 center,float radius,float width,Color tint,int segments)
        {
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,tint);
            }
        }
        private void Disc(Vector2 center,float radius,Color tint,int segments)
        {
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Triangle(center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint);
            }
        }
        private void Ellipse(Vector2 center,float x,float y,float width,Color tint)
        {
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                Line(center+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),center+new Vector2(Mathf.Cos(b)*x,Mathf.Sin(b)*y),width,tint);
            }
        }
        private void OnDisable(){if(visualRenderer!=null)visualRenderer.enabled=false;}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
