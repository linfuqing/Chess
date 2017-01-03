using UnityEngine;
using UnityEngine.UI;

public class MahjongClientRoom : MonoBehaviour
{
    public static MahjongClientRoom instance;

    public float width;
    public float height;
    public float length;
    public float size;
    public float offset;
    public float throwTime;
    public float moveTime;
    public float smoothTime;
    public float maxSpeed;
    public Vector3 handPosition;
    public Vector3 discardPosition;
    public Vector3 scorePosition;
    public Vector3 groupPosition;
    
    public MahjongAsset asset;
    public Texture[] textures;

    public Button chow;
    public Button pong;
    public Button kong;
    public Button win;
    public new Text name;
    public Text[] texts;
    
    private MahjongAsset[] __instances;
    private int __index;
    
    public MahjongAsset next
    {
        get
        {
            int count = __instances == null ? 0 : __instances.Length;
            if (count < 1)
                return null;

            return __instances[__index++ % count];
        }
    }

    public void Init(int count, int index)
    {
        if (this.asset != null)
        {
            MahjongAsset asset;
            Transform transform;
            float offset = width * (count * 0.5f - 0.5f), temp;
            for (int i = 0; i < count; ++i)
            {
                temp = i * width - offset;

                //Right
                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 90.0f, 0.0f);
                    transform.position = new Vector3(size, length * 2.0f, -temp);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 0 + (i << 1) + 0] = asset;
                }

                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 90.0f, 0.0f);
                    transform.position = new Vector3(size, length, -temp);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 0 + (i << 1) + 1] = asset;
                }

                //Down
                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 180.0f, 0.0f);
                    transform.position = new Vector3(-temp, length * 2.0f, -size);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 1 + (i << 1) + 0] = asset;
                }

                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 180.0f, 0.0f);
                    transform.position = new Vector3(-temp, length, -size);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 1 + (i << 1) + 1] = asset;
                }

                //Left
                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
                    transform.position = new Vector3(-size, length * 2.0f, temp);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 2 + (i << 1) + 0] = asset;
                }

                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
                    transform.position = new Vector3(-size, length, temp);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 2 + (i << 1) + 1] = asset;
                }

                //Up
                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
                    transform.position = new Vector3(temp, length * 2.0f, size);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 3 + (i << 1) + 0] = asset;
                }

                asset = Instantiate(this.asset);
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.eulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
                    transform.position = new Vector3(temp, length, size);

                    if (__instances == null)
                        __instances = new MahjongAsset[count << 3];

                    __instances[(count << 1) * 3 + (i << 1) + 1] = asset;
                }
            }
        }

        __index = index;
    }

    public void As(MahjongAsset asset, Mahjong.Tile tile)
    {
        if (textures == null)
            return;
        
        byte code = tile;
        if (textures.Length <= code)
            return;
        
        Material material = asset == null || asset.renderer == null ? null : asset.renderer.material;
        if (material != null)
            material.mainTexture = textures[code];
    }
    
    public void Discard(MahjongAsset asset, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        transform.localPosition = discardPosition + new Vector3(width * (index % 5), 0.0f, -height * (index / 5));

        asset.Throw();
    }

    public void Score(MahjongAsset asset, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        transform.localPosition = scorePosition + new Vector3(width * index, 0.0f, 0.0f);

        asset.Throw();
    }

    public void Group(MahjongAsset asset, int groupIndex, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        transform.localPosition = groupPosition - new Vector3((groupIndex * 3 + (index > 2 ? 1 : index)) * width + groupIndex * offset, index > 2 ? -length : 0.0f, 0.0f);
    }

    public void Quit()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
            main.Shutdown();
    }

    void Start()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main == null)
            return;

        if (name != null)
            name.text = main.roomName;
    }

    void OnEnable()
    {
        instance = this;
    }

    void OnDisable()
    {
        instance = null;
    }
}
