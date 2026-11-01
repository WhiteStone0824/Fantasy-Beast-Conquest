using UnityEngine;
using UnityEngine.UI;

namespace FusionAnyMonster.UI
{
    /// <summary>Shared visual tokens for the expedition UI. See Docs/UIDesignDirection.md.</summary>
    public static class GameUiTheme
    {
        public static readonly Color Text = new Color(.93f,.96f,.98f);
        public static readonly Color Muted = new Color(.73f,.81f,.85f);
        public static readonly Color Surface = new Color(.035f,.065f,.10f,.97f);
        public static readonly Color Accent = new Color(.22f,.77f,.75f);
        public static readonly Color PrimaryButton = new Color(.08f,.35f,.36f,.98f);
        public static readonly Color Gold = new Color(.96f,.73f,.38f);
        public static readonly Color Button = new Color(.10f,.20f,.25f,.98f);
        public static readonly Color Border = new Color(.35f,.57f,.58f,.55f);
        private static Sprite roundedSurface;

        public static void ApplySurface(Image image)
        {
            if (image == null) return;
            if (roundedSurface == null)
            {
                const int size=48, radius=10;
                var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                texture.name="UI surface";texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color32[size*size];
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float dx=Mathf.Max(radius-x-.5f,0,x-(size-radius)+.5f);
                    float dy=Mathf.Max(radius-y-.5f,0,y-(size-radius)+.5f);
                    byte alpha=(byte)Mathf.RoundToInt(255f*Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy)));
                    pixels[y*size+x]=new Color32(255,255,255,alpha);
                }
                texture.SetPixels32(pixels);texture.Apply(false,true);
                roundedSurface=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
                roundedSurface.name="UI rounded surface";
            }
            image.sprite=roundedSurface;image.type=Image.Type.Sliced;
        }
    }
}
