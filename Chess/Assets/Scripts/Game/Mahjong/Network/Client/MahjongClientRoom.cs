using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MahjongClientRoom : MonoBehaviour
{
    [Serializable]
    public struct Player
    {
        public GameObject root;
        public RawImage image;
    }

    [Serializable]
    public struct Panel
    {
        public GameObject root;
        public Button ready;
        public MahjongFinishPlayerStyle[] players;

        public void Clear()
        {
            if(players != null)
            {
                foreach(MahjongFinishPlayerStyle player in players)
                {
                    if (player != null)
                        player.Clear();
                }
            }
        }
    }

    [Serializable]
    public struct Finish
    {
        public Panel normal;
        public Panel win;
        
        public GameObject root;
        public Button button;

        public void Clear()
        {
            normal.Clear();
            win.Clear();
        }
    }

    public static MahjongClientRoom instance;

    public float width;
    public float height;
    public float length;
    public float size;
    public float offset;
    public float diceTime;
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

    public Player[] players;

    public Finish finish;

    public Button pass;
    public Button chow;
    public Button pong;
    public Button kong;
    public Button win;
    public Button hide;
    public Button show;
    public new Text name;
    public TextMesh time;
    
    public Dice x;
    public Dice y;
    public AsynchronousTrigger wind;
    public Transform arrow;

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

    public void Play(int point0, int point1, int point2, int point3)
    {
        StartCoroutine(__Play(point0, point1, point2, point3));
    }

    public void Init(int tileCount, int index)
    {
        if (this.asset != null)
        {
            int length = __instances == null ? 0 : __instances.Length, i;
            MahjongAsset asset;
            for (i = tileCount; i < length; ++i)
            {
                asset = __instances[i];
                if (asset != null)
                    Destroy(asset.gameObject);
            }
            
            Array.Resize(ref __instances, tileCount);

            int count = tileCount >> 3, step = tileCount >> 2, j;
            float offset = width * (((step + 1) >> 1) * 0.5f - 0.5f), temp;
            Transform transform;
            
            for (i = 0; i < count; ++i)
            {
                temp = i * width - offset;

                //Right
                j = step * 0 + (i << 1);

                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 90.0f, 0.0f);
                    transform.position = new Vector3(size, this.length * 2.0f, -temp);
                }
                
                asset = __instances[++j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 90.0f, 0.0f);
                    transform.position = new Vector3(size, this.length, -temp);
                }

                //Down
                j = step * 1 + (i << 1);

                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 180.0f, 0.0f);
                    transform.position = new Vector3(-temp, this.length * 2.0f, -size);
                }
                
                asset = __instances[++j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }
                
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 180.0f, 0.0f);
                    transform.position = new Vector3(-temp, this.length, -size);
                }

                //Left
                j = step * 2 + (i << 1);

                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
                    transform.position = new Vector3(-size, this.length * 2.0f, temp);
                }

                asset = __instances[++j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
                    transform.position = new Vector3(-size, this.length, temp);
                }

                //Up
                j = step * 3 + (i << 1);

                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
                    transform.position = new Vector3(temp, this.length * 2.0f, size);
                }

                asset = __instances[++j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
                    transform.position = new Vector3(temp, this.length, size);
                }
            }

            if(tileCount > (count << 3))
            {
                temp = count * width - offset;

                //Right
                j = step * 1  - 1;

                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 90.0f, 0.0f);
                    transform.position = new Vector3(size, this.length, -temp);
                }

                //Down
                j = step * 2 - 1;
                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 180.0f, 0.0f);
                    transform.position = new Vector3(-temp, this.length, -size);
                }

                //Left
                j = step * 3 - 1;
                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, -90.0f, 0.0f);
                    transform.position = new Vector3(-size, this.length, temp);
                }

                //Up
                j = step * 4 - 1;
                asset = __instances[j];
                if (asset == null)
                {
                    asset = Instantiate(this.asset);
                    __instances[j] = asset;
                }
                else
                {
                    asset.onDiscard = null;
                    asset.onSelected = null;
                }

                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(null, false);
                    transform.eulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
                    transform.position = new Vector3(temp, this.length, size);
                }
            }
        }

        finish.Clear();

        GameObject gameObject = arrow == null ? null : arrow.gameObject;
        if (gameObject != null)
            gameObject.SetActive(false);

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
        
        transform.localPosition = discardPosition + new Vector3(width * (index % 5), 0.0f, -height * (index / 5));
        
        asset.Throw();
    }

    public void Throw(MahjongAsset asset, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;
        
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

    private IEnumerator __Play(int point0, int point1, int point2, int point3)
    {
        GameObject gameObject;
        if (x != null)
        {
            gameObject = x.gameObject;
            if (gameObject != null)
                gameObject.SetActive(true);

            x.Play(point0);
        }

        if(y != null)
        {
            gameObject = y.gameObject;
            if (gameObject != null)
                gameObject.SetActive(true);

            y.Play(point1);
        }

        yield return new WaitForSeconds(diceTime);

        if (x != null)
            x.Play(point2);

        if (y != null)
            y.Play(point3);

        yield return new WaitForSeconds(diceTime);

        gameObject = x == null ? null : x.gameObject;
        if (gameObject != null)
            gameObject.SetActive(false);

        gameObject = y == null ? null : y.gameObject;
        if (gameObject != null)
            gameObject.SetActive(false);
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
